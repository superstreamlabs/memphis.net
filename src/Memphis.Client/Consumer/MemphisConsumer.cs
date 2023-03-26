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
using NATS.Client;
using NATS.Client.JetStream;

namespace Memphis.Client.Consumer
{
    public sealed class MemphisConsumer : IDisposable
    {
        public event EventHandler<MemphisMessageHandlerEventArgs> MessageReceived;
        public event EventHandler<MemphisMessageHandlerEventArgs> DlsMessageReceived;

        internal string InternalStationName { get; private set; }
        internal string Key => $"{InternalStationName}_{_consumerOptions.RealName}";

        private ISyncSubscription _dlqSubscription;
        private readonly MemphisClient _memphisClient;
        private readonly MemphisConsumerOptions _consumerOptions;
        private IJetStreamPullSubscription _pullSubscription;
        private ConcurrentQueue<MemphisMessage> _dlqMessages;

        private readonly CancellationTokenSource _cancellationTokenSource;

        public MemphisConsumer(MemphisClient memphisClient, MemphisConsumerOptions consumerOptions)
        {
            _memphisClient = memphisClient ?? throw new ArgumentNullException(nameof(memphisClient));
            _consumerOptions = consumerOptions ?? throw new ArgumentNullException(nameof(consumerOptions));
            InternalStationName = MemphisUtil.GetInternalName(consumerOptions.StationName);
            _dlqMessages = new();

            _cancellationTokenSource = new();
        }

        /// <summary>
        /// ConsumeAsync messages
        /// </summary>
        /// <param name="msgCallbackHandler">the event handler for messages consumed from station in which MemphisConsumer created for</param>
        /// <param name="dlqCallbackHandler">event handler for messages consumed from dead letter queue or juts DLQ.</param>
        /// <returns></returns>
        public async Task ConsumeAsync(CancellationToken cancellationToken = default)
        {
            var taskForStationConsumption = Task.Run(
                async () => await Consume(_cancellationTokenSource.Token),
                _cancellationTokenSource.Token);
            var taskForDlqConsumption = Task.Run(
                async () => await ConsumeFromDlq(_cancellationTokenSource.Token),
                _cancellationTokenSource.Token);
            await Task.WhenAll(taskForStationConsumption, taskForDlqConsumption);
        }


        /// <summary>
        /// ConsumeAsync messages from station
        /// </summary>
        /// <param name="msgCallbackHandler">the event handler for messages consumed from station in which MemphisConsumer created for</param>
        /// <param name="cancellationToken">token used to cancel operation by Consumer</param>
        /// <returns></returns>
        private async Task Consume(CancellationToken cancellationToken)
        {
            var internalSubjectName = MemphisUtil.GetInternalName(_consumerOptions.StationName);
            var consumerGroup = MemphisUtil.GetInternalName(_consumerOptions.ConsumerGroup);

            _pullSubscription = _memphisClient.JetStreamConnection.PullSubscribe(
                internalSubjectName + ".final",
                PullSubscribeOptions.BindTo(internalSubjectName, consumerGroup));

            while (!cancellationToken.IsCancellationRequested)
            {
                if (!IsSubscriptionActive(_pullSubscription))
                    continue;

                try
                {
                    var msgList = _pullSubscription.Fetch(_consumerOptions.BatchSize,
                        _consumerOptions.BatchMaxTimeToWaitMs);
                    var memphisMessageList = msgList
                        .Select(item => new MemphisMessage(item, _memphisClient, _consumerOptions.ConsumerGroup,
                            _consumerOptions.MaxAckTimeMs))
                        .ToList();

                    MessageReceived?.Invoke(this, new MemphisMessageHandlerEventArgs(memphisMessageList, null));
                    await Task.Delay(_consumerOptions.PullIntervalMs, cancellationToken);
                }
                catch (System.Exception e)
                {
                    MessageReceived?.Invoke(this, new MemphisMessageHandlerEventArgs(new List<MemphisMessage>(), e));
                }
            }
        }

        /// <summary>
        /// ConsumeAsync messages from dead letter queue namely, DLQ
        /// </summary>
        /// <param name="dlqCallbackHandler">event handler for messages consumed from dead letter queue or juts DLQ.</param>
        /// <param name="cancellationToken">token used to cancel operation by Consumer</param>
        /// <returns></returns>
        private async Task ConsumeFromDlq(CancellationToken cancellationToken)
        {
            var subjectToConsume = MemphisUtil.GetInternalName(_consumerOptions.StationName);
            var consumerGroup = MemphisUtil.GetInternalName(_consumerOptions.ConsumerGroup);

            var dlqSubscriptionName = MemphisSubcriptions.DLQ_PREFIX + subjectToConsume + "_" + consumerGroup;
            _dlqSubscription = _memphisClient.BrokerConnection.SubscribeSync(dlqSubscriptionName, dlqSubscriptionName);

            while (!cancellationToken.IsCancellationRequested)
            {
                if (IsSubscriptionActive(_dlqSubscription))
                {
                    try
                    {
                        var msg = _dlqSubscription.NextMessage();
                        if (msg is null)
                        {
                            continue;
                        }

                        var memphisMsg = new MemphisMessage(msg, _memphisClient, _consumerOptions.ConsumerGroup,
                                _consumerOptions.MaxAckTimeMs);
                        if (DlsMessageReceived is null)
                        {
                            EnqueueDlqMessage(memphisMsg);
                            continue;
                        }

                        var memphisMessageList = new List<MemphisMessage> { memphisMsg };

                        DlsMessageReceived?.Invoke(this, new MemphisMessageHandlerEventArgs(memphisMessageList, null));

                        await Task.Delay(_consumerOptions.PullIntervalMs, cancellationToken);
                    }
                    catch (System.Exception e)
                    {
                        DlsMessageReceived?.Invoke(this, new MemphisMessageHandlerEventArgs(new List<MemphisMessage>(), e));
                    }
                }
            }
        }

        public async Task DestroyAsync()
        {
            try
            {
                if (_dlqSubscription.IsValid)
                {
                    await _dlqSubscription.DrainAsync();
                }

                if (_pullSubscription.IsValid)
                {
                    await _pullSubscription.DrainAsync();
                }

                _cancellationTokenSource?.Cancel();

                var removeConsumerModel = new RemoveConsumerRequest()
                {
                    ConsumerName = _consumerOptions.ConsumerName,
                    StationName = _consumerOptions.StationName,
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

        internal async Task<IEnumerable<MemphisMessage>> Fetch(int batchSize, CancellationToken cancellationToken)
        {
            try
            {
                _consumerOptions.BatchSize = batchSize;
                IEnumerable<MemphisMessage> messages = Enumerable.Empty<MemphisMessage>();

                int dlsMessageCount = _dlqMessages.Count();
                if (dlsMessageCount > 0)
                {
                    if (dlsMessageCount <= batchSize)
                    {
                        messages = _dlqMessages.ToList();
                        _dlqMessages = new();
                    }
                    else
                    {
                        DequeueDlsMessages(batchSize, ref messages);
                    }
                    return messages;
                }

                var durableName = MemphisUtil.GetInternalName(_consumerOptions.ConsumerName);
                if (!string.IsNullOrWhiteSpace(_consumerOptions.ConsumerGroup))
                {
                    durableName = MemphisUtil.GetInternalName(_consumerOptions.ConsumerGroup);
                }
                
                var subscription = _memphisClient.JetStreamConnection.PullSubscribe(
                    $"{InternalStationName}.final",
                    PullSubscribeOptions.BindTo(InternalStationName, durableName));
                var batch = subscription.Fetch(batchSize, _consumerOptions.BatchMaxTimeToWaitMs);
                return batch
                    .Select<Msg, MemphisMessage>(
                        msg => new(msg, _memphisClient, _consumerOptions.ConsumerGroup,
                            _consumerOptions.MaxAckTimeMs)
                    )
                    .ToList();
            }
            catch (System.Exception ex)
            {
                throw new MemphisException(ex.Message, ex);
            }
        }

        private bool IsSubscriptionActive(ISyncSubscription subscription)
        {
            return
                subscription.IsValid &&
                subscription.Connection.State != ConnState.CLOSED;
        }

        private void EnqueueDlqMessage(MemphisMessage message)
        {
            int inserToIndex = _dlqMessages.Count();
            if (inserToIndex > 10_000)
            {
                _dlqMessages.TryDequeue(out MemphisMessage _);
            }
            _dlqMessages.Enqueue(message);
        }

        private void DequeueDlsMessages(int batchSize, ref IEnumerable<MemphisMessage> messages)
        {
            if (messages is not { })
                messages = Enumerable.Empty<MemphisMessage>();
            List<MemphisMessage> batchMessages = new();
            while (_dlqMessages.TryDequeue(out MemphisMessage message))
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
            await _pullSubscription.DrainAsync();
            await _dlqSubscription.DrainAsync();

            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();

            _memphisClient?.Dispose();
            _pullSubscription?.Dispose();
            _dlqSubscription?.Dispose();
        }
    }
}