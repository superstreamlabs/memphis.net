using Memphis.Client.Producer;

namespace Memphis.Client;

public partial class MemphisClient
{
    internal ConcurrentDictionary<string, MemphisProducer> ProducerCache { get => _producerCache; }

    /// <summary>
    /// Create Producer for station 
    /// </summary>
    /// <param name="producerOptions">Producer options</param>
    /// <returns>An <see cref="MemphisProducer"/> object connected to the station to produce data</returns>
    public async Task<MemphisProducer> CreateProducer(MemphisProducerOptions producerOptions, int timeoutRetry = 5, CancellationToken cancellationToken = default)
    {
        string producerName = producerOptions.ProducerName.ToLower();
        bool generateRandomSuffix = producerOptions.GenerateUniqueSuffix;

        if (_brokerConnection.IsClosed())
        {
            throw MemphisExceptions.DeadConnectionException;
        }

        if (generateRandomSuffix)
        {
            producerName = $"{producerName}_{MemphisUtil.GetUniqueKey(8)}";
        }

        producerOptions.EnsureOptionIsValid();

        return producerOptions.StationNames.Any() ?
        CreateMultiStationProducer(producerName, producerOptions)
        : await CreateSingleStationProducer(producerName, producerOptions, timeoutRetry, cancellationToken);
    }

    /// <summary>
    /// Produce a message to a station
    /// </summary>
    /// <param name="options">options for producing a message</param>
    /// <param name="message">message to be produced</param>
    /// <param name="headers">headers of the message</param>
    /// <param name="messageId">Message ID - for idempotent message production</param>
    /// <param name="asyncProduceAck">if true, producer will not wait for ack from broker</param>
    /// <param name="cancellationToken">cancellation token</param>
    /// <returns></returns>
    public async Task ProduceAsync(
        MemphisProducerOptions options,
        byte[] message,
        NameValueCollection headers = default,
        string messageId = default,
        bool asyncProduceAck = true,
        string partitionKey = "",
        int partitionNumber = -1,
        CancellationToken cancellationToken = default)
    {
        options.EnsureOptionIsValid();
        if (!IsConnected())
        {
            throw MemphisExceptions.DeadConnectionException;
        }

        MemphisProducer producer = default;
        if (!string.IsNullOrWhiteSpace(options.StationName))
        {
            var internalStationName = MemphisUtil.GetInternalName(options.StationName);
            var producerKey = $"{internalStationName}_{options.ProducerName.ToLower()}";
            if (_producerCache.TryGetValue(producerKey, out MemphisProducer cacheProducer))
            {
                producer = cacheProducer;
            }

            producer ??= await CreateProducer(options);
            await producer.ProduceToBrokerAsync(message, headers, asyncProduceAck, partitionKey, partitionNumber, options.MaxAckTimeMs, messageId);
            return;
        }

        producer ??= await CreateProducer(options);
        await producer.MultiStationProduceAsync(message, headers, options.MaxAckTimeMs, messageId, asyncProduceAck, partitionKey, partitionNumber, cancellationToken);
    }


    /// <summary>
    /// Produce a message to a station
    /// </summary>
    /// <param name="options">options for producing a message</param>
    /// <param name="message">message to be produced</param>
    /// <param name="headers">headers of the message</param>
    /// <param name="messageId">id of the message</param>
    /// <param name="cancellationToken">cancellation token</param>
    /// <returns></returns>
    public async Task ProduceAsync<T>(
        MemphisProducerOptions options,
        T message,
        NameValueCollection headers = default,
        string messageId = default,
        bool asyncProduceAck = true,
        string partitionKey = "",
        int partitionNumber = -1,
        CancellationToken cancellationToken = default)
    {
        options.EnsureOptionIsValid();
        if (!IsConnected())
        {
            throw MemphisExceptions.DeadConnectionException;
        }

        MemphisProducer producer = default;
        if (!string.IsNullOrWhiteSpace(options.StationName))
        {
            var internalStationName = MemphisUtil.GetInternalName(options.StationName);
            var producerKey = $"{internalStationName}_{options.ProducerName.ToLower()}";
            if (_producerCache.TryGetValue(producerKey, out MemphisProducer cacheProducer))
            {
                producer = cacheProducer;
            }
        }

        producer ??= await CreateProducer(options);
        await producer.ProduceAsync(message, headers, options.MaxAckTimeMs, messageId, asyncProduceAck, partitionKey, partitionNumber);
    }
    
    internal async Task ProduceAsync(
        MemphisProducer producer,
        byte[] message,
        NameValueCollection headers,
        int ackWaitMs,
        bool asyncProduceAck,
        string? messageId = default,
        string partitionKey = default,
        int partitionNumber = -1)
    {
        MemphisProducerOptions options = new()
        {
            StationName = producer.StationName,
            ProducerName = producer.ProducerName,
            GenerateUniqueSuffix = false,
            MaxAckTimeMs = ackWaitMs
        };

        await ProduceAsync(options, message, headers, messageId, asyncProduceAck, partitionKey, partitionNumber);
    }


    #region  Private Methods
    private async Task<MemphisProducer> CreateSingleStationProducer(
        string producerName,
        MemphisProducerOptions producerOptions,
        int timeoutRetry,
        CancellationToken cancellationToken)
    {
        string stationName = producerOptions.StationName;
        string internalStationName = MemphisUtil.GetInternalName(stationName);

        var existingProducerCacheKey = $"{internalStationName}_{producerName}";
        if (_producerCache.TryGetValue(existingProducerCacheKey, out MemphisProducer cachedProducer))
            return cachedProducer;

        try
        {
            var createProducerModel = new CreateProducerRequest
            {
                ProducerName = producerName,
                StationName = MemphisUtil.GetInternalName(stationName),
                ConnectionId = _connectionId,
                ProducerType = "application",
                RequestVersion = MemphisRequestVersions.LastProducerCreationRequestVersion,
                UserName = _userName,
                ApplicationId = ApplicationId,
                SdkLang = ".NET"
            };

            var createProducerModelJson = JsonSerializer.Serialize(createProducerModel);

            byte[] createProducerReqBytes = Encoding.UTF8.GetBytes(createProducerModelJson);

            Msg createProducerResp = await RequestAsync(MemphisStations.MEMPHIS_PRODUCER_CREATIONS, createProducerReqBytes, timeoutRetry, cancellationToken);
            string respAsJson = Encoding.UTF8.GetString(createProducerResp.Data);
            var createProducerResponse = JsonSerializer.Deserialize<CreateProducerResponse>(respAsJson)!;

            if (!string.IsNullOrEmpty(createProducerResponse.Error))
            {
                throw new MemphisException(createProducerResponse.Error);
            }


            _stationSchemaVerseToDlsMap.AddOrUpdate(internalStationName, createProducerResponse.SchemaVerseToDls, (_, _) => createProducerResponse.SchemaVerseToDls);
            _clusterConfigurations.AddOrUpdate(MemphisSdkClientUpdateTypes.SEND_NOTIFICATION, createProducerResponse.SendNotification, (_, _) => createProducerResponse.SendNotification);

            if (createProducerResponse.PartitionsUpdate is not null)
            {
                _stationPartitions.AddOrUpdate(internalStationName, createProducerResponse.PartitionsUpdate, (_, _) => createProducerResponse.PartitionsUpdate);
            }

            var producer = new MemphisProducer(this, producerName, stationName, producerName.ToLower());
            if (_stationPartitions.TryGetValue(internalStationName, out PartitionsUpdate partitionsUpdate))
            {
                if (partitionsUpdate.PartitionsList == null)
                {
                    producer.PartitionResolver = new(1);
                }
                else
                {
                    producer.PartitionResolver = new(partitionsUpdate.PartitionsList);
                }
            }
            var producerCacheKey = $"{internalStationName}_{producerName.ToLower()}";
            _producerCache.AddOrUpdate(producerCacheKey, producer, (_, _) => producer);
            
            await ListenForSchemaUpdate(internalStationName, createProducerResponse.SchemaUpdate);
            await ListenForFunctionUpdate(internalStationName, createProducerResponse.StationVersion, cancellationToken);

            return producer;
        }
        catch (MemphisException)
        {
            throw;
        }
        catch (System.Exception e)
        {
            throw MemphisExceptions.FailedToCreateProducerException(e);
        }

    }


    private MemphisProducer CreateMultiStationProducer(
        string producerName,
        MemphisProducerOptions producerOptions)
    {
        return new MemphisProducer(this, producerName, producerName.ToLower(), producerOptions.StationNames.ToList());
    }
    #endregion
}
