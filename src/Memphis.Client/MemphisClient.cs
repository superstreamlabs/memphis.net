using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Memphis.Client.Constants;
using Memphis.Client.Consumer;
using Memphis.Client.Exception;
using Memphis.Client.Helper;
using Memphis.Client.Models.Request;
using Memphis.Client.Models.Response;
using Memphis.Client.Producer;
using NATS.Client;
using NATS.Client.JetStream;

namespace Memphis.Client
{
    public class MemphisClient : IDisposable
    {
        //TODO replace it with mature solution
        private bool _connectionActive;

        private readonly Options _brokerConnOptions;
        private readonly IConnection _brokerConnection;
        private readonly IJetStream _jetStreamContext;
        private readonly string _connectionId;

        private Dictionary<string, string> _schemaUpdateData = new Dictionary<string, string>();

        public MemphisClient(Options brokerConnOptions, IConnection brokerConnection,
            IJetStream jetStreamContext, string connectionId)
        {
            this._brokerConnOptions = brokerConnOptions;
            this._brokerConnection = brokerConnection;
            this._jetStreamContext = jetStreamContext;
            this._connectionId = connectionId;

            this._connectionActive = true;
        }

        public async Task<MemphisProducer> CreateProducer(string stationName, string producerName, bool generateRandomSuffix = false)
        {
            if (!_connectionActive)
            {
                throw new MemphisConnectionException("Connection is dead");
            }

            if (generateRandomSuffix)
            {
                producerName = $"{producerName}_{MemphisUtil.GetUniqueKey(8)}";
            }

            try
            {
                var createProducerModel = new CreateProducerRequest
                {
                    ProducerName = producerName,
                    StationName = MemphisUtil.GetInternalStationName(stationName),
                    ConnectionId = _connectionId,
                    ProducerType = "application",
                    RequestVersion = 1
                };

                var createProducerModelJson = JsonSerDes.PrepareJsonString<CreateProducerRequest>(createProducerModel);

                byte[] createProducerReqBytes = Encoding.UTF8.GetBytes(createProducerModelJson);

                Msg createProducerResp = await _brokerConnection.RequestAsync(
                    MemphisStations.MEMPHIS_PRODUCER_CREATIONS, createProducerReqBytes);
                string respAsJson = Encoding.UTF8.GetString(createProducerResp.Data);
                var respAsObject =
                    (CreateProducerResponse) JsonSerDes.PrepareObjectFromString<CreateProducerResponse>(respAsJson);

                if (!string.IsNullOrEmpty(respAsObject.Error))
                {
                    throw new MemphisException(respAsObject.Error);
                }

                string internalStationName = MemphisUtil.GetInternalStationName(stationName);

                //TODO start listen for schema updates

                if (_schemaUpdateData.TryGetValue(internalStationName, out string schemaForStation))
                {
                    //TODO if schema data is protoBuf then parse its descriptors
                    //self.schema_updates_data[station_name_internal]['type'] == "protobuf"
                    // elf.parse_descriptor(station_name_internal)
                }

                return new MemphisProducer(this, producerName, stationName);
            }
            catch (System.Exception e)
            {
                throw new MemphisException("Failed to create memphis producer", e);
            }
        }

        public MemphisConsumer CreateConsumer()
        {
            if (_connectionActive)
            {
                throw new MemphisConnectionException("Connection is dead");
            }

            return null;
        }

        public void Dispose()
        {
            _brokerConnection.Dispose();
            _connectionActive = false;
        }

        internal IConnection BrokerConnection
        {
            get
            {
                return _brokerConnection;
            }
        }
        
        internal IJetStream JetStreamConnection
        {
            get
            {
                return _jetStreamContext;
            }
        }
        
        internal string ConnectionId
        {
            get
            {
                return _connectionId;
            }
        }
    }
}