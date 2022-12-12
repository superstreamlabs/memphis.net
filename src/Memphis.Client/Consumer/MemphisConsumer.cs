using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Memphis.Client.Constants;
using Memphis.Client.Core;
using Memphis.Client.Helper;
using NATS.Client;
using NATS.Client.JetStream;

namespace Memphis.Client.Consumer
{
    public class MemphisConsumer : IDisposable
    {
        private readonly MemphisClient _memphisClient;
        private readonly ConsumerOptions _consumerOptions;

        private IJetStreamPullSubscription _pullSubscription;
        private ISyncSubscription _dlqSubscription;

        private readonly CancellationTokenSource _cancellationTokenSource;

        public MemphisConsumer(MemphisClient memphisClient, ConsumerOptions consumerOptions)
        {
            this._memphisClient = memphisClient ?? throw new ArgumentNullException(nameof(memphisClient));
            this._consumerOptions = consumerOptions ?? throw new ArgumentNullException(nameof(consumerOptions));

            this._cancellationTokenSource = new CancellationTokenSource();
        }


        /// <summary>
        /// ConsumeAsync messages
        /// </summary>
        /// <param name="msgCallbackHandler">the event handler for messages consumed from station in which MemphisConsumer created for</param>
        /// <param name="dlqCallbackHandler">event handler for messages consumed from dead letter queue or juts DLQ.</param>
        /// <returns></returns>
        public async Task ConsumeAsync(EventHandler<MemphisMessageHandlerEventArgs> msgCallbackHandler,
            EventHandler<MemphisMessageHandlerEventArgs> dlqCallbackHandler)
        {
            var taskForStationConsumption = Task.Run(async () => await consume(msgCallbackHandler, _cancellationTokenSource.Token),
                _cancellationTokenSource.Token);
            var taskForDlqConsumption = Task.Run(async () => await consumeFromDlq(dlqCallbackHandler, _cancellationTokenSource.Token),
                _cancellationTokenSource.Token);
            await Task.WhenAll(taskForStationConsumption, taskForDlqConsumption);
        }


        /// <summary>
        /// ConsumeAsync messages from station
        /// </summary>
        /// <param name="msgCallbackHandler">the event handler for messages consumed from station in which MemphisConsumer created for</param>
        /// <param name="cancellationToken">token used to cancel operation by Consumer</param>
        /// <returns></returns>
        private async Task consume(EventHandler<MemphisMessageHandlerEventArgs> msgCallbackHandler,
            CancellationToken cancellationToken)
        {
            var internalSubjectName = MemphisUtil.GetInternalName(_consumerOptions.StationName);
            var consumerGroup = MemphisUtil.GetInternalName(_consumerOptions.ConsumerGroup);

            _pullSubscription = _memphisClient.JetStreamConnection.PullSubscribe(
                internalSubjectName + ".final",
                PullSubscribeOptions.BindTo(internalSubjectName, consumerGroup));

            while (!cancellationToken.IsCancellationRequested)
            {
                if (_memphisClient.ConnectionActive)
                {
                    try
                    {
                        IList<Msg> msgList = _pullSubscription.Fetch(_consumerOptions.BatchSize,
                            _consumerOptions.BatchMaxTimeToWaitMs);

                        var memphisMessageList = msgList
                            .Select(item => new MemphisMessage(item, _memphisClient, _consumerOptions.ConsumerGroup,
                                _consumerOptions.MaxAckTimeMs))
                            .ToList();

                        msgCallbackHandler(this, new MemphisMessageHandlerEventArgs(memphisMessageList, null));

                        await Task.Delay(_consumerOptions.PullIntervalMs, cancellationToken);
                    }
                    catch (System.Exception e)
                    {
                        msgCallbackHandler(this, new MemphisMessageHandlerEventArgs(new List<MemphisMessage>(), e));

                        Console.WriteLine(e);
                    }
                }
            }
        }

        /// <summary>
        /// ConsumeAsync messages from dead letter queue namely, DLQ
        /// </summary>
        /// <param name="dlqCallbackHandler">event handler for messages consumed from dead letter queue or juts DLQ.</param>
        /// <param name="cancellationToken">token used to cancel operation by Consumer</param>
        /// <returns></returns>
        private async Task consumeFromDlq(EventHandler<MemphisMessageHandlerEventArgs> dlqCallbackHandler,
            CancellationToken cancellationToken)
        {
            var subjectToConsume = MemphisUtil.GetInternalName(_consumerOptions.StationName);
            var consumerGroup = MemphisUtil.GetInternalName(_consumerOptions.ConsumerGroup);

            var dlqSubscriptionName = MemphisSubcriptions.DLQ_PREFIX + subjectToConsume + "_" + consumerGroup;
            _dlqSubscription = _memphisClient.BrokerConnection.SubscribeSync(dlqSubscriptionName, dlqSubscriptionName);

            while (!cancellationToken.IsCancellationRequested)
            {
                if (_memphisClient.ConnectionActive)
                {
                    try
                    {
                        var msg = _dlqSubscription.NextMessage();

                        var memphisMessageList = new List<MemphisMessage>()
                        {
                            new MemphisMessage(msg, _memphisClient, _consumerOptions.ConsumerGroup,
                                _consumerOptions.MaxAckTimeMs)
                        };

                        dlqCallbackHandler(this, new MemphisMessageHandlerEventArgs(memphisMessageList, null));

                        await Task.Delay(_consumerOptions.PullIntervalMs, cancellationToken);
                    }
                    catch (System.Exception e)
                    {
                        dlqCallbackHandler(this, new MemphisMessageHandlerEventArgs(new List<MemphisMessage>(), e));

                        Console.WriteLine(e);
                    }
                }
            }
        }

        public void Dispose()
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();

            _memphisClient?.Dispose();
            _pullSubscription?.Dispose();
            _dlqSubscription?.Dispose();
        }
    }
}