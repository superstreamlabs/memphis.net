using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Memphis.Client.Constants;
using Memphis.Client.Exception;
using Memphis.Client.Helper;
using Memphis.Client.Models.Response;
using NATS.Client;
using NATS.Client.Internals;
using NATS.Client.JetStream;
using NJsonSchema;

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
            await validateMessage(message);

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
        
        private async Task validateMessage(byte[] message, CancellationToken cancellationToken = default)
        {
            if (!_memphisClient.TryGetSchemaDetail(_stationName, out ProducerSchemaUpdateInit schema))
                return;
                
            switch (schema.SchemaType)
            {
                case MemphisSchemaTypes.NONE:
                    return;
                case MemphisSchemaTypes.JSON:
                    await validateJsonMessage(message, schema.ActiveVersion.Content, cancellationToken);
                    break;
                case MemphisSchemaTypes.GRAPH_QL:
                case MemphisSchemaTypes.PROTO_BUF:
                default:
                    throw new NotImplementedException(schema.SchemaType);
            }
        }

        private async Task validateJsonMessage(byte[] message, string schema, CancellationToken cancellationToken = default)
        {
            try
            {
                var jsonSchema = await JsonSchema.FromJsonAsync(schema, cancellationToken);
                var json = Encoding.UTF8.GetString(message);
                var errors = jsonSchema.Validate(json);

                if (errors.Any())
                {
                    var sb = new StringBuilder();
                    foreach (var error in errors)
                    {
                        sb.AppendLine(error.ToString());
                    }
                    throw new MemphisException(sb.ToString());
                }
            }
            catch (System.Exception e)
            {
                throw new MemphisException("Message does not match schema", e);
            }
        }

        public string ProducerName => _producerName;

        public string StationName => _stationName;
    }
}