using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Memphis.Client.Constants;
using Memphis.Client.Exception;
using Memphis.Client.Helper;
using Memphis.Client.Models.Request;
using NATS.Client;
using NATS.Client.Internals;
using NATS.Client.JetStream;
using Newtonsoft.Json;

namespace Memphis.Client.Producer
{
    public sealed class MemphisProducer
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
        /// <param name="messageId">Message ID - for idempotent message production</param>
        /// <returns></returns>
        public async Task ProduceAsync(byte[] message, NameValueCollection headers, int ackWaitMs = 15_000,
            string? messageId = default)
        {
            await EnsureMessageIsValid(message, headers);

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

            if (messageId == string.Empty)
                throw new MemphisMessageIdException("Message ID cannot be empty");

            if (!string.IsNullOrWhiteSpace(messageId))
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

        /// <summary>
        /// Produce messages into station
        /// </summary>
        /// <param name="message">the event handler for messages consumed from station in which MemphisConsumer created for</param>
        /// <param name="headers">headers used to send data in the form of key and value</param>
        /// <param name="ackWaitMs">duration of time in milliseconds for acknowledgement</param>
        /// <param name="messageId">Message ID - for idempotent message production</param>
        /// <returns></returns>
        public async Task ProduceAsync<T>(T message, NameValueCollection headers, int ackWaitMs = 15_000,
            string? messageId = default)
        {
            string encodedMessage = IsPrimitiveType(message) ?
                message.ToString() :
                JsonConvert.SerializeObject(message);

            await ProduceAsync(
                Encoding.UTF8.GetBytes(encodedMessage),
                headers,
                ackWaitMs,
                messageId);
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

        /// <summary>
        /// Ensure message is valid
        /// </summary>
        /// <remark>
        /// This method is used to validate message against schema. If message is not valid, it will be sent to DLS, otherwise it will be sent to corresponding station
        /// </remark>
        /// <param name="message">Message to validate</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns></returns>
        private async Task EnsureMessageIsValid(byte[] message, NameValueCollection headers, CancellationToken cancellationToken = default)
        {
            try
            {
                await _memphisClient.ValidateMessageAsync(message, _internalStationName, _producerName);
            }
            catch (MemphisSchemaValidationException exception)
            {
                await SendMessageToDls(message, headers, exception, cancellationToken);
                throw;
            }
            catch
            {
                throw;
            }
        }

        private async Task SendMessageToDls(byte[] message, NameValueCollection headers, MemphisSchemaValidationException validationError, CancellationToken cancellationToken = default)
        {
            if (!_memphisClient.IsSchemaVerseToDlsEnabled(_internalStationName))
                return;
            var headersForDls = new Dictionary<string, string>
            {
                [MemphisHeaders.MEMPHIS_CONNECTION_ID] = _memphisClient.ConnectionId,
                [MemphisHeaders.MEMPHIS_PRODUCED_BY] = _producerName
            };

            foreach (var headerKey in headers.AllKeys)
            {
                headersForDls.Add(headerKey, headers[headerKey]);
            }
            var dlsMessage = new DlsMessage
            {
                StationName = _internalStationName,
                Producer = new ProducerDetails
                {
                    Name = _producerName,
                    ConnectionId = _memphisClient.ConnectionId,
                },
                Message = new MessagePayloadDls
                {
                    Data = BitConverter.ToString(message).Replace("-", string.Empty),
                    Headers = headersForDls,
                },
                ValidationError = validationError.Message,
                TenantName = _memphisClient.TenantName,
            };

            var dlsMessageJson = JsonConvert.SerializeObject(dlsMessage);
            var dlsMessageBytes = Encoding.UTF8.GetBytes(dlsMessageJson);
            _memphisClient.BrokerConnection.Publish(MemphisSubjects.MEMPHIS_SCHEMA_VERSE_DLS, dlsMessageBytes);

            if (!_memphisClient.IsSendingNotificationEnabled)
                return;
            await _memphisClient.SendNotificationAsync(
                "Schema validation has failed",
                $"Schema validation has failed for station {_internalStationName} and producer {_producerName}. Error: {validationError.Message}",
                Encoding.UTF8.GetString(message),
                "schema_validation_fail_alert"
            );

        }

        /// <summary>
        /// Check if data is primitive type
        /// </summary>
        /// <typeparam name="T">Type of data</typeparam>
        /// <param name="data">Data to check</param>
        /// <returns>true if data type is primitive, otherwise false.</returns>
        private bool IsPrimitiveType<T>(T data)
        {
            var type = typeof(T);
            return type.IsPrimitive ||
                type.IsEnum ||
                type == typeof(DateTime) ||
                type == typeof(decimal) ||
                type == typeof(string);
        }
    }
}