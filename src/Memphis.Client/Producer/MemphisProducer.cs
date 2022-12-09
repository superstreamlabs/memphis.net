using System;
using System.Collections.Specialized;
using System.Threading.Tasks;
using Memphis.Client.Constants;
using Memphis.Client.Exception;
using Memphis.Client.Helper;
using NATS.Client;
using NATS.Client.Internals;
using NATS.Client.JetStream;

namespace Memphis.Client.Producer
{
    public class MemphisProducer
    {
        private readonly MemphisClient _memphisClient;
        private readonly string _producerName;
        private readonly string _stationName;
        private readonly string _internalStationName;

        public MemphisProducer(MemphisClient memphisClient, string producerName, string stationName)
        {
            this._memphisClient = memphisClient ?? throw new ArgumentNullException(nameof(memphisClient));
            this._producerName = producerName ?? throw new ArgumentNullException(nameof(producerName));
            this._stationName = stationName ?? throw new ArgumentNullException(nameof(stationName));

            this._internalStationName = MemphisUtil.GetInternalName(stationName);
        }


        /// <summary>
        /// Produce messages into station
        /// </summary>
        /// <param name="message">the event handler for messages consumed from station in which MemphisConsumer created for</param>
        /// <param name="headers">headers used to send data in the form of key and value</param>
        /// <param name="ackWaitSec">duration of time in seconds for acknowledgement</param>
        /// <param name="messageId">ID of the message</param>
        /// <returns></returns>
        public async Task ProduceAsync(byte[] message, NameValueCollection headers, int ackWaitSec = 15,
            string messageId = null)
        {
            //TODO Validate message with schema defined for station, and raise exception when

            var msg = new Msg
            {
                Subject = $"{this._internalStationName}.final",
                Data = message,
                Header = new MsgHeader
                {
                    {MemphisHeaders.MEMPHIS_PRODUCED_BY, _producerName},
                    {MemphisHeaders.MEMPHIS_CONNECTION_ID, _memphisClient.ConnectionId}
                }
            };

            if (string.IsNullOrEmpty(messageId))
            {
                msg.Header.Add(MemphisHeaders.MESSAGE_ID, messageId);
            }

            foreach (var headerKey in headers.AllKeys)
            {
                msg.Header.Add(headerKey, headers[headerKey]);
            }


            var publishAck = await _memphisClient.JetStreamConnection.PublishAsync(
                msg, PublishOptions.Builder()
                    .WithTimeout(Duration.OfSeconds(ackWaitSec))
                    .Build());

            if (publishAck.HasError)
            {
                throw new MemphisException(publishAck.ErrorDescription);
            }
        }

        public string ProducerName => _producerName;

        public string StationName => _stationName;
    }
}