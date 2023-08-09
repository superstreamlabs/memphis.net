using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Memphis.Client.Constants;
using Memphis.Client.Core;
using Memphis.Client.Exception;
using Memphis.Client.Helper;
using Memphis.Client.Models.Request;
using Memphis.Client.Station;
using NATS.Client;
using NATS.Client.JetStream;

namespace Memphis.Client.Consumer;

public sealed class MemphisConsumer : IMemphisConsumer
{
    public event EventHandler<MemphisMessageHandlerEventArgs> MessageReceived;
    public event EventHandler<MemphisMessageHandlerEventArgs> DlsMessageReceived;

    internal string InternalStationName { get; private set; }
    internal string Key => $"{InternalStationName}_{_consumerOptions.RealName}";

    private ISyncSubscription _dlsSubscription;
    private readonly MemphisClient _memphisClient;
    private readonly MemphisConsumerOptions _consumerOptions;
    private IJetStreamPullSubscription[] _subscriptions;

    private ConcurrentQueue<MemphisMessage> _dlsMessages;

    private readonly CancellationTokenSource _cancellationTokenSource;

    private static bool _subscriptionActive;
    private readonly int _pingConsumerIntervalMs;


    private int[] _partitions;
    internal StationPartitionResolver PartitionResolver { get; set; }
    internal int[] Partitions
    {
        get => _partitions;
        set
        {
            if (value.Length == 0)
            {
                PartitionResolver = new StationPartitionResolver(1);
                _partitions = value;
                return;
            }
            PartitionResolver = new StationPartitionResolver(value.Length);
            _partitions = value;
        }
    }

#pragma warning disable CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.
    public MemphisConsumer(MemphisClient memphisClient, MemphisConsumerOptions options, int[]? partitions = default)
    {
        if (options.StartConsumeFromSequence <= 0)
            throw new MemphisException($"Value of {nameof(options.StartConsumeFromSequence)} must be greater than 0");
        if (options.LastMessages < -1)
            throw new MemphisException($"Value of {nameof(options.LastMessages)} can not be less than -1");
        if (options is { StartConsumeFromSequence: > 1, LastMessages: > -1 })
            throw new MemphisException($"Consumer creation option can't contain both {nameof(options.StartConsumeFromSequence)} and {nameof(options.LastMessages)}");

        _memphisClient = memphisClient ?? throw new ArgumentNullException(nameof(memphisClient));
        _consumerOptions = options ?? throw new ArgumentNullException(nameof(options));
        InternalStationName = MemphisUtil.GetInternalName(options.StationName);
        _dlsMessages = new();

        _cancellationTokenSource = new();


        _subscriptionActive = true;
        _pingConsumerIntervalMs = (int)TimeSpan.FromSeconds(30).TotalMilliseconds;

        Partitions = partitions ?? (new int[0]);

        InitSubscription();

#pragma warning disable 4014
        PingConsumer(_cancellationTokenSource.Token);
#pragma warning restore 4014

    }


    private void InitSubscription()
    {
        var internalSubjectName = MemphisUtil.GetInternalName(_consumerOptions.StationName);

        if (Partitions.Length == 0)
        {
            var options = new ConsumerConfiguration.ConsumerConfigurationBuilder()
                .WithDurable(MemphisUtil.GetInternalName(_consumerOptions.ConsumerGroup))
                .BuildPullSubscribeOptions();

            var subscription = _memphisClient.JetStreamConnection.PullSubscribe(internalSubjectName + ".final", options);
            _subscriptions = new IJetStreamPullSubscription[] { subscription };
            return;
        }

        _subscriptions = new IJetStreamPullSubscription[Partitions.Length];
        for (int i = 0; i < Partitions.Length; i++)
        {
            var streamName = $"{internalSubjectName}${Partitions[i]}.final";
            var options = new ConsumerConfiguration.ConsumerConfigurationBuilder()
                .WithDurable(MemphisUtil.GetInternalName(_consumerOptions.ConsumerGroup))
                .BuildPullSubscribeOptions();

            var subscription = _memphisClient.JetStreamConnection.PullSubscribe(streamName, options);
            _subscriptions[i] = subscription;
        }
    }

    /// <summary>
    /// ConsumeAsync messages
    /// </summary>
    /// <returns></returns>
    public async Task ConsumeAsync(CancellationToken cancellationToken = default)
    {
        var taskForStationConsumption = Task.Run(
            async () => await Consume(_cancellationTokenSource.Token),
            _cancellationTokenSource.Token);
        var taskForDlsConsumption = Task.Run(
            async () => await ConsumeFromDls(_cancellationTokenSource.Token),
            _cancellationTokenSource.Token);
        await Task.WhenAll(taskForStationConsumption, taskForDlsConsumption);
    }

    /// <summary>
    /// Destroy the consumer
    /// </summary>
    /// <returns></returns>
    public async Task DestroyAsync()
    {
        try
        {
            if (_dlsSubscription is { IsValid: true })
            {
                await _dlsSubscription.DrainAsync();
            }

            StopConsume();

            _cancellationTokenSource?.Cancel();

            var removeConsumerModel = new RemoveConsumerRequest()
            {
                ConsumerName = _consumerOptions.ConsumerName,
                StationName = _consumerOptions.StationName,
                ConnectionId = _memphisClient.ConnectionId,
                Username = _memphisClient.Username,
                RequestVersion = MemphisRequestVersions.LastConsumerDestroyRequestVersion,
            };

            var removeConsumerModelJson = JsonSerDes.PrepareJsonString<RemoveConsumerRequest>(removeConsumerModel);

            byte[] removeConsumerReqBytes = Encoding.UTF8.GetBytes(removeConsumerModelJson);

            Msg removeProducerResp = await _memphisClient.BrokerConnection.RequestAsync(
                MemphisStations.MEMPHIS_CONSUMER_DESTRUCTIONS, removeConsumerReqBytes);
            string errResp = Encoding.UTF8.GetString(removeProducerResp.Data);

            if (!string.IsNullOrEmpty(errResp))
            {
                throw new MemphisException(errResp);
            }

            _memphisClient.NotifyRemoveConsumer(_consumerOptions.StationName);
        }
        catch (System.Exception e)
        {
            throw new MemphisException("Failed to destroy consumer", e);
        }
    }

    /// <summary>
    /// Fetch a batch of messages
    /// </summary>
    /// <param name="batchSize">the number of messages to fetch</param>
    /// <param name="cancellationToken">token used to cancel operation by Consumer</param>
    /// <returns>A batch of messages</returns>
    public IEnumerable<MemphisMessage> Fetch(int batchSize, bool prefetch)
    {
        try
        {
            _consumerOptions.BatchSize = batchSize;
            IEnumerable<MemphisMessage> messages = Enumerable.Empty<MemphisMessage>();

            int dlsMessageCount = _dlsMessages.Count();
            if (dlsMessageCount > 0)
            {
                if (dlsMessageCount <= batchSize)
                {
                    messages = _dlsMessages.ToList();
                    _dlsMessages = new();
                }
                else
                {
                    DequeueDlsMessages(batchSize, ref messages);
                }
                return messages;
            }

            if (TryGetAndRemovePrefetchedMessages(batchSize, out IEnumerable<MemphisMessage> prefetchedMessages))
            {
                messages = prefetchedMessages;
            }

            if (prefetch)
            {
                Task.Run(() => Prefetch(), _cancellationTokenSource.Token);
            }

            if (messages.Any())
            {
                return messages;
            }

            return FetchSubscriptionWithTimeOut(batchSize);
        }
        catch (System.Exception ex)
        {
            throw new MemphisException(ex.Message, ex);
        }
    }

    internal bool TryGetAndRemovePrefetchedMessages(int batchSize, out IEnumerable<MemphisMessage> messages)
    {
        messages = Enumerable.Empty<MemphisMessage>();
        var lowerCaseStationName = _consumerOptions.StationName.ToLower();
        var consumerGroup = _consumerOptions.ConsumerGroup;
        if (!_memphisClient.PrefetchedMessages.ContainsKey(lowerCaseStationName))
            return false;
        if (!_memphisClient.PrefetchedMessages[lowerCaseStationName].ContainsKey(consumerGroup))
            return false;
        if (!_memphisClient.PrefetchedMessages[lowerCaseStationName][consumerGroup].Any())
            return false;
        var prefetchedMessages = _memphisClient.PrefetchedMessages[lowerCaseStationName][consumerGroup];
        if (prefetchedMessages.Count <= batchSize)
        {
            messages = prefetchedMessages;
            _memphisClient.PrefetchedMessages[lowerCaseStationName][consumerGroup] = new();
            return true;
        }
        messages = prefetchedMessages.Take(batchSize);
        _memphisClient.PrefetchedMessages[lowerCaseStationName][consumerGroup] = prefetchedMessages
            .Skip(batchSize)
            .ToList();
        return true;
    }

    internal void Prefetch()
    {
        var lowerCaseStationName = _consumerOptions.StationName.ToLower();
        var consumerGroup = _consumerOptions.ConsumerGroup;
        if (!_memphisClient.PrefetchedMessages.ContainsKey(lowerCaseStationName))
        {
            _memphisClient.PrefetchedMessages[lowerCaseStationName] = new();
        }
        if (!_memphisClient.PrefetchedMessages[lowerCaseStationName].ContainsKey(consumerGroup))
        {
            _memphisClient.PrefetchedMessages[lowerCaseStationName][consumerGroup] = new();
        }
        var messages = FetchSubscriptionWithTimeOut(_consumerOptions.BatchSize);
        _memphisClient.PrefetchedMessages[lowerCaseStationName][consumerGroup].AddRange(messages);
    }

    /// <summary>
    /// Pings Memphis every 30 seconds to check if the consumer is still alive.
    /// If Memphis does not respond, the consumer is stopped.
    /// </summary>
    /// <param name="cancellationToken">token used to cancel operation by Consumer</param>
    /// <returns></returns>
    internal async Task PingConsumer(CancellationToken cancellationToken = default)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            foreach (var subscription in _subscriptions)
            {
                try
                {
                    _ = subscription.GetConsumerInformation();
                }
                catch
                {
                    MessageReceived?.Invoke(this, new MemphisMessageHandlerEventArgs(
                        new List<MemphisMessage>(),
                        subscription?.Context,
                        new MemphisException("Station unreachable")));
                }
            }
            await Task.Delay(_pingConsumerIntervalMs, cancellationToken);
        }
    }

    /// <summary>
    /// Stop consuming messages
    /// </summary>
    internal void StopConsume()
    {
        _subscriptionActive = false;
    }

    private IEnumerable<MemphisMessage> FetchSubscriptionWithTimeOut(int batchSize)
    {
        var durableName = MemphisUtil.GetInternalName(_consumerOptions.ConsumerName);
        if (!string.IsNullOrWhiteSpace(_consumerOptions.ConsumerGroup))
        {
            durableName = MemphisUtil.GetInternalName(_consumerOptions.ConsumerGroup);
        }

        var subscription = Partitions.Length == 0 ? _subscriptions[0] : _subscriptions[PartitionResolver.Resolve()];

        var batch = subscription.Fetch(batchSize, _consumerOptions.BatchMaxTimeToWaitMs);
        return batch
            .Select<Msg, MemphisMessage>(
                msg => new(msg, _memphisClient, _consumerOptions.ConsumerGroup,
                    _consumerOptions.MaxAckTimeMs)
            )
            .ToList();
    }

    /// <summary>
    /// ConsumeAsync messages from station
    /// </summary>
    /// <param name="msgCallbackHandler">the event handler for messages consumed from station in which MemphisConsumer created for</param>
    /// <param name="cancellationToken">token used to cancel operation by Consumer</param>
    /// <returns></returns>
    private async Task Consume(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            if (!_subscriptionActive)
            {
                break;
            }

            FetchFromPartition(cancellationToken);
            await Task.Delay(_consumerOptions.PullIntervalMs, cancellationToken);
        }
    }

    private void FetchFromPartition(CancellationToken cancellationToken)
    {
        var subscription = Partitions.Length == 0 ? _subscriptions[0] : _subscriptions[PartitionResolver.Resolve()];

        try
        {
            var msgList = subscription.Fetch(_consumerOptions.BatchSize,
                _consumerOptions.BatchMaxTimeToWaitMs);
            var memphisMessageList = msgList
                .Select(item => new MemphisMessage(item, _memphisClient, _consumerOptions.ConsumerGroup,
                    _consumerOptions.MaxAckTimeMs))
                .ToList();
                
            MessageReceived?.Invoke(this, new MemphisMessageHandlerEventArgs(memphisMessageList, subscription.Context, null));
        }
        catch (System.Exception e)
        {
            MessageReceived?.Invoke(this, new MemphisMessageHandlerEventArgs(new List<MemphisMessage>(), subscription?.Context, e));
        }
    }

    /// <summary>
    /// ConsumeAsync messages from dead letter queue namely, DLS
    /// </summary>
    /// <returns></returns>
    private async Task ConsumeFromDls(CancellationToken cancellationToken)
    {
        var subjectToConsume = MemphisUtil.GetInternalName(_consumerOptions.StationName);
        var consumerGroup = MemphisUtil.GetInternalName(_consumerOptions.ConsumerGroup);

        var dlsSubscriptionName = MemphisSubscriptions.DLS_PREFIX + subjectToConsume + "_" + consumerGroup;
        _dlsSubscription = _memphisClient.BrokerConnection.SubscribeSync(dlsSubscriptionName, dlsSubscriptionName);

        while (!cancellationToken.IsCancellationRequested)
        {
            if (!IsSubscriptionActive(_dlsSubscription))
                continue;

            if (!_subscriptionActive)
            {
                break;
            }

            try
            {
                var msg = _dlsSubscription.NextMessage();
                if (msg is null)
                {
                    continue;
                }

                var memphisMsg = new MemphisMessage(msg, _memphisClient, _consumerOptions.ConsumerGroup,
                        _consumerOptions.MaxAckTimeMs);
                if (DlsMessageReceived is null)
                {
                    EnqueueDlsMessage(memphisMsg);
                    continue;
                }

                var memphisMessageList = new List<MemphisMessage> { memphisMsg };

                var subscription = Partitions.Length == 0 ? _subscriptions[0] : _subscriptions[PartitionResolver.Resolve()];

                DlsMessageReceived?.Invoke(this, new MemphisMessageHandlerEventArgs(memphisMessageList, subscription.Context, null));

                await Task.Delay(_consumerOptions.PullIntervalMs, cancellationToken);
            }
            catch (System.Exception e)
            {
                var subscription = Partitions.Length == 0 ? _subscriptions[0] : _subscriptions[PartitionResolver.Resolve()];
                DlsMessageReceived?.Invoke(this, new MemphisMessageHandlerEventArgs(new List<MemphisMessage>(), subscription?.Context, e));
            }

        }
    }

    private bool IsSubscriptionActive(ISyncSubscription subscription)
    {
        return
            subscription.IsValid &&
            subscription.Connection.State != ConnState.CLOSED;
    }

    private void EnqueueDlsMessage(MemphisMessage message)
    {
        int insertToIndex = _dlsMessages.Count();
        if (insertToIndex > 10_000)
        {
            _dlsMessages.TryDequeue(out MemphisMessage _);
        }
        _dlsMessages.Enqueue(message);
    }

    private void DequeueDlsMessages(int batchSize, ref IEnumerable<MemphisMessage> messages)
    {
        if (messages is not { })
            messages = Enumerable.Empty<MemphisMessage>();
        List<MemphisMessage> batchMessages = new();
        while (_dlsMessages.TryDequeue(out MemphisMessage message))
        {
            batchSize -= 1;
            batchMessages.Add(message);
            if (batchSize <= 0)
                break;
        }
        messages.Concat(batchMessages);
    }

    public async void Dispose()
    {
        StopConsume();
        await _dlsSubscription.DrainAsync();

        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource?.Dispose();

        _memphisClient?.Dispose();
        _dlsSubscription?.Dispose();
    }
}