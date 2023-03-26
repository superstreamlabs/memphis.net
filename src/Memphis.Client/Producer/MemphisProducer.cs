using System;
using System.Collections.Specialized;
using System.Text;
using System.Threading.Tasks;
using Memphis.Client.Constants;
using Memphis.Client.Exception;
using Memphis.Client.Helper;
using Memphis.Client.Models.Request;
using NATS.Client;
using NATS.Client.Internals;
using NATS.Client.JetStream;

namespace Memphis.Client.Producer
{
    public class MemphisProducer
    {
        internal string Key => $"{_internalStationName}_{_realName}";
        internal string InternalStationName { get => _internalStationName; }

        private readonly string _realName;
        private readonly string _producerName;
        private readonly string _stationName;
        private readonly string _internalStationName;
        private readonly MemphisClient _memphisClient;

        public MemphisProducer(MemphisClient memphisClient, string producerName, string stationName, string realName)
        {
            _realName = realName ?? throw new ArgumentNullException(nameof(realName));
            _memphisClient = memphisClient ?? throw new ArgumentNullException(nameof(memphisClient));
            _producerName = producerName ?? throw new ArgumentNullException(nameof(producerName));
            _stationName = stationName ?? throw new ArgumentNullException(nameof(stationName));
            _internalStationName = MemphisUtil.GetInternalName(stationName);
        }

        /// <summary>
        /// Produce messages into station
        /// </summary>
        /// <param name="message">the event handler for messages consumed from station in which MemphisConsumer created for</param>
        /// <param name="headers">headers used to send data in the form of key and value</param>
        /// <param name="ackWaitMs">duration of time in milliseconds for acknowledgement</param>
        /// <param name="messageId">ID of the message</param>
        /// <returns></returns>
        public async Task ProduceAsync(byte[] message, NameValueCollection headers, int ackWaitMs = 15_000,
            string messageId = default)
        {
            await _memphisClient.ValidateMessageAsync(message, _internalStationName, _producerName);

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
                    .WithTimeout(Duration.OfMillis(ackWaitMs))
                    .Build());

            if (publishAck.HasError)
            {
                throw new MemphisException(publishAck.ErrorDescription);
            }
        }

        public async Task DestroyAsync()
        {
            try
            {
                var removeProducerModel = new RemoveProducerRequest()
                {
                    ProducerName = _producerName,
                    StationName = _stationName,
                };

                var removeProducerModelJson = JsonSerDes.PrepareJsonString<RemoveProducerRequest>(removeProducerModel);

                byte[] removeProducerReqBytes = Encoding.UTF8.GetBytes(removeProducerModelJson);

                Msg removeProducerResp = await _memphisClient.BrokerConnection.RequestAsync(
                    MemphisStations.MEMPHIS_PRODUCER_DESTRUCTIONS, removeProducerReqBytes);
                string errResp = Encoding.UTF8.GetString(removeProducerResp.Data);

                if (!string.IsNullOrEmpty(errResp))
                {
                    throw new MemphisException(errResp);
                }

                await _memphisClient.NotifyRemoveProducer(_stationName);
            }
            catch (System.Exception e)
            {
                throw new MemphisException("Failed to destroy producer", e);
            }
        }
    }
}