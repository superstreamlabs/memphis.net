using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Memphis.Client.Constants;
using Memphis.Client.Consumer;
using Memphis.Client.Core;
using Memphis.Client.Exception;
using Memphis.Client.Helper;
using Memphis.Client.Models.Request;
using Memphis.Client.Models.Response;
using Memphis.Client.Producer;
using Memphis.Client.Station;
using Memphis.Client.Validators;
using NATS.Client;
using NATS.Client.JetStream;
using Newtonsoft.Json;

#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
#pragma warning disable CS8604 // Possible null reference argument.

namespace Memphis.Client;

public sealed class MemphisClient : IMemphisClient
{
    private bool _desposed;
    private readonly Options _brokerConnOptions;
    private readonly IConnection _brokerConnection;
    private readonly IJetStream _jetStreamContext;
    private readonly IJetStreamManagement _jetStreamManagement;
    private readonly string _connectionId;
    private readonly string _userName;
    private CancellationTokenSource _cancellationTokenSource;

    // Dictionary key: station (internal)name, value: schema update data for that station
    private readonly ConcurrentDictionary<string, ProducerSchemaUpdateInit> _schemaUpdateDictionary;

    // Dictionary key: station (internal)name, value: subscription for fetching schema updates for station
    private readonly ConcurrentDictionary<string, ISyncSubscription> _subscriptionPerSchema;

    // Dictionary key: station (internal)name, value: number of producer created per that station
    private readonly ConcurrentDictionary<string, int> _producerPerStations;

    private readonly ConcurrentDictionary<ValidatorType, ISchemaValidator> _schemaValidators;
    private readonly ConcurrentDictionary<string, MemphisProducer> _producerCache;
    private readonly ConcurrentDictionary<string, MemphisConsumer> _consumerCache;
    private readonly ConcurrentDictionary<string, bool> _stationSchemaVerseToDlsMap;
    private readonly ConcurrentDictionary<string, bool> _clusterConfigurations;


    private readonly ConcurrentDictionary<string, PartitionsUpdate> _stationPartitions;


    private readonly SemaphoreSlim _schemaUpdateSemaphore = new(1, 1);
    private readonly SemaphoreSlim _sdkClientUpdateSemaphore = new(1, 1);

    static MemphisClient()
    {
        ApplicationId = Guid
            .NewGuid()
            .ToString();
    }

    public MemphisClient(Options brokerConnOptions, IConnection brokerConnection,
        IJetStream jetStreamContext, string connectionId)
    {
        _brokerConnOptions = brokerConnOptions ?? throw new ArgumentNullException(nameof(brokerConnOptions));
        _brokerConnection = brokerConnection ?? throw new ArgumentNullException(nameof(brokerConnection));
        _jetStreamContext = jetStreamContext ?? throw new ArgumentNullException(nameof(jetStreamContext));
        _connectionId = connectionId ?? throw new ArgumentNullException(nameof(connectionId));
        _userName = brokerConnOptions.User;
        int usernameSeparatorIndex = _userName.LastIndexOf('$');
        if (usernameSeparatorIndex >= 0)
        {
            _userName = _userName.Substring(0, usernameSeparatorIndex);
        }

        _cancellationTokenSource = new();

        _schemaUpdateDictionary = new();
        _subscriptionPerSchema = new();
        _producerPerStations = new();
        _producerCache = new();
        _consumerCache = new();

        _stationSchemaVerseToDlsMap = new();
        _clusterConfigurations = new();

        _schemaValidators = new();
        RegisterSchemaValidators();

        PrefetchedMessages = new();

        JetStreamOptions options = JetStreamOptions.Builder()
            .WithRequestTimeout((int)TimeSpan.FromSeconds(15).TotalMilliseconds)
            .Build();

        _jetStreamManagement = brokerConnection.CreateJetStreamManagementContext(options);
        _stationPartitions = new();
    }

    /// <summary>
    /// Check if Memphis is connected
    /// </summary>
    /// <returns>
    /// True if the Memphis is connected; otherwise, false.
    /// </returns>
    private bool IsConnected()
    {
        return !_brokerConnection.IsClosed();
    }

    /// <summary>
    /// Create Producer for station 
    /// </summary>
    /// <param name="producerOptions">Producer options</param>
    /// <returns>An <see cref="MemphisProducer"/> object connected to the station to produce data</returns>
    public async Task<MemphisProducer> CreateProducer(MemphisProducerOptions producerOptions)
    {
        string stationName = producerOptions.StationName;
        string producerName = producerOptions.ProducerName.ToLower();
        bool generateRandomSuffix = producerOptions.GenerateUniqueSuffix;
        string internalStationName = MemphisUtil.GetInternalName(stationName);

        if (_brokerConnection.IsClosed())
        {
            throw new MemphisConnectionException("Connection is dead");
        }

        if (generateRandomSuffix)
        {
            producerName = $"{producerName}_{MemphisUtil.GetUniqueKey(8)}";
        }
        else
        {
            var producerCacheKey = $"{internalStationName}_{producerName}";
            if (_producerCache.TryGetValue(producerCacheKey, out MemphisProducer producer))
            {
                return producer;
            }
        }

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
                ApplicationId = ApplicationId
            };

            var createProducerModelJson = JsonSerDes.PrepareJsonString<CreateProducerRequest>(createProducerModel);

            byte[] createProducerReqBytes = Encoding.UTF8.GetBytes(createProducerModelJson);

            Msg createProducerResp = await _brokerConnection.RequestAsync(
                MemphisStations.MEMPHIS_PRODUCER_CREATIONS, createProducerReqBytes);
            string respAsJson = Encoding.UTF8.GetString(createProducerResp.Data);
            var createProducerResponse = JsonConvert.DeserializeObject<CreateProducerResponse>(respAsJson)!;

            if (!string.IsNullOrEmpty(createProducerResponse.Error))
            {
                throw new MemphisException(createProducerResponse.Error);
            }


            _stationSchemaVerseToDlsMap.AddOrUpdate(internalStationName, createProducerResponse.SchemaVerseToDls, (_, _) => createProducerResponse.SchemaVerseToDls);
            _clusterConfigurations.AddOrUpdate(MemphisSdkClientUpdateTypes.SEND_NOTIFICATION, createProducerResponse.SendNotification, (_, _) => createProducerResponse.SendNotification);

            await ListenForSchemaUpdate(internalStationName, createProducerResponse.SchemaUpdate);

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
            return producer;
        }
        catch (MemphisException)
        {
            throw;
        }
        catch (System.Exception e)
        {
            throw new MemphisException("Failed to create memphis producer", e);
        }
    }

    public async Task<IEnumerable<MemphisMessage>> FetchMessages(
        FetchMessageOptions options,
        CancellationToken cancellationToken = default
    )
    {
        if (!IsConnected())
        {
            throw new MemphisConnectionException("Connection is dead. Can't produce a message without being connected!");
        }

        MemphisConsumer? consumer = default;
        var internalStationName = MemphisUtil.GetInternalName(options.StationName);
        var consumerKey = $"{internalStationName}_{options.ConsumerName.ToLower()}";

        if (_consumerCache.TryGetValue(consumerKey, out var cacheConsumer))
        {
            consumer = cacheConsumer;
        }

        consumer ??= await CreateConsumer(options);

        return consumer.Fetch(options.BatchSize, options.Prefetch);
    }

    /// <summary>
    /// Produce a message to a station
    /// </summary>
    /// <param name="options">options for producing a message</param>
    /// <param name="message">message to be produced</param>
    /// <param name="headers">headers of the message</param>
    /// <param name="messageId">Message ID - for idempotent message production</param>
    /// <param name="cancellationToken">cancellation token</param>
    /// <returns></returns>
    public async Task ProduceAsync(
        MemphisProducerOptions options,
        byte[] message,
        NameValueCollection headers = default,
        string messageId = default,
        CancellationToken cancellationToken = default)
    {
        if (!IsConnected())
        {
            throw new MemphisConnectionException("Connection is dead. Can't produce a message without being connected!");
        }

        MemphisProducer producer = default;
        var internalStationName = MemphisUtil.GetInternalName(options.StationName);
        var producerKey = $"{internalStationName}_{options.ProducerName.ToLower()}";
        if (_producerCache.TryGetValue(producerKey, out MemphisProducer cacheProducer))
        {
            producer = cacheProducer;
        }

        producer ??= await CreateProducer(options);

        await producer.ProduceToBrokerAsync(message, headers, options.MaxAckTimeMs, messageId);
    }

    internal async Task ProduceAsync(
        MemphisProducer producer,
        byte[] message,
        NameValueCollection headers,
        int ackWaitMs,
        string? messageId = default)
    {
        MemphisProducerOptions options = new()
        {
            StationName = producer.StationName,
            ProducerName = producer.ProducerName,
            GenerateUniqueSuffix = false,
            MaxAckTimeMs = ackWaitMs
        };

        await ProduceAsync(options, message, headers, messageId);
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
        CancellationToken cancellationToken = default)
    {
        if (!IsConnected())
        {
            throw new MemphisConnectionException("Connection is dead. Can't produce a message without being connected!");
        }

        MemphisProducer producer = default;
        var internalStationName = MemphisUtil.GetInternalName(options.StationName);
        var producerKey = $"{internalStationName}_{options.ProducerName.ToLower()}";
        if (_producerCache.TryGetValue(producerKey, out MemphisProducer cacheProducer))
        {
            producer = cacheProducer;
        }

        if (producer is null)
        {
            producer = await CreateProducer(options);
        }

        await producer.ProduceAsync(message, headers, options.MaxAckTimeMs, messageId);
    }

    /// <summary>
    /// Create Consumer for station 
    /// </summary>
    /// <param name="consumerOptions">options used to customize the behaviour of consumer</param>
    /// <returns>An <see cref="MemphisConsumer"/> object connected to the station from consuming data</returns>
    public async Task<MemphisConsumer> CreateConsumer(MemphisConsumerOptions consumerOptions)
    {
        if (_brokerConnection.IsClosed())
        {
            throw new MemphisConnectionException("Connection is dead");
        }

        consumerOptions.RealName = consumerOptions.ConsumerName.ToLower();

        if (consumerOptions.GenerateUniqueSuffix)
        {
            consumerOptions.ConsumerName = $"{consumerOptions.ConsumerName}_{MemphisUtil.GetUniqueKey(8)}";
        }

        if (string.IsNullOrEmpty(consumerOptions.ConsumerGroup))
        {
            consumerOptions.ConsumerGroup = consumerOptions.ConsumerName;
        }

        try
        {
            var createConsumerModel = new CreateConsumerRequest
            {
                ConsumerName = consumerOptions.ConsumerName,
                StationName = consumerOptions.StationName,
                ConnectionId = _connectionId,
                ConsumerType = "application",
                ConsumerGroup = consumerOptions.ConsumerGroup,
                MaxAckTimeMs = consumerOptions.MaxAckTimeMs,
                MaxMsgCountForDelivery = consumerOptions.MaxMsgDeliveries,
                UserName = _userName,
                StartConsumeFromSequence = consumerOptions.StartConsumeFromSequence,
                LastMessages = consumerOptions.LastMessages,
                RequestVersion = MemphisRequestVersions.LastConsumerCreationRequestVersion,
                ApplicationId = ApplicationId
            };

            var createConsumerModelJson = JsonSerDes.PrepareJsonString<CreateConsumerRequest>(createConsumerModel);

            byte[] createConsumerReqBytes = Encoding.UTF8.GetBytes(createConsumerModelJson);

            Msg createConsumerResp = await _brokerConnection.RequestAsync(
                MemphisStations.MEMPHIS_CONSUMER_CREATIONS, createConsumerReqBytes);
            var responseStr = Encoding.UTF8.GetString(createConsumerResp.Data);
            var createConsumerResponse = JsonConvert.DeserializeObject<CreateConsumerResponse>(responseStr);

            if (createConsumerResponse is null)
            {
                if (!string.IsNullOrEmpty(responseStr))
                {
                    throw new MemphisException(responseStr);
                }

                var oldconsumer = new MemphisConsumer(this, consumerOptions);
                _consumerCache.AddOrUpdate(oldconsumer.Key, oldconsumer, (_, _) => oldconsumer);

                return oldconsumer;
            }


            if (!string.IsNullOrEmpty(createConsumerResponse.Error))
            {
                throw new MemphisException(responseStr);
            }

            var consumer = new MemphisConsumer(this, consumerOptions, createConsumerResponse.PartitionsUpdate.PartitionsList);
            _consumerCache.AddOrUpdate(consumer.Key, consumer, (_, _) => consumer);

            return consumer;

        }
        catch (MemphisException)
        {
            throw;
        }
        catch (System.Exception e)
        {
            throw new MemphisException("Failed to create memphis consumer", e);
        }
    }


    /// <summary>
    /// Create Station 
    /// </summary>
    /// <param name="stationOptions">options used to customize the parameters of station</param>
    /// <param name="cancellationToken">cancellation token</param>
    /// <returns>An <see cref="MemphisStation"/> object representing the created station</returns>
    public async Task<MemphisStation> CreateStation(StationOptions stationOptions, CancellationToken cancellationToken = default)
    {
        if (_brokerConnection.IsClosed())
        {
            throw new MemphisConnectionException("Connection is dead");
        }

        try
        {
            var createStationModel = new CreateStationRequest()
            {
                StationName = stationOptions.Name,
                RetentionType = stationOptions.RetentionType,
                RetentionValue = stationOptions.RetentionValue,
                StorageType = stationOptions.StorageType,
                Replicas = stationOptions.Replicas,
                IdempotencyWindowsInMs = stationOptions.IdempotenceWindowMs,
                SchemaName = stationOptions.SchemaName,
                DlsConfiguration = new DlsConfiguration()
                {
                    Poison = stationOptions.SendPoisonMessageToDls,
                    SchemaVerse = stationOptions.SendSchemaFailedMessageToDls,
                },
                UserName = _userName,
                TieredStorageEnabled = stationOptions.TieredStorageEnabled,
                PartitionsNumber = stationOptions.PartitionsNumber
            };

            var createStationModelJson = JsonSerDes.PrepareJsonString<CreateStationRequest>(createStationModel);

            byte[] createStationReqBytes = Encoding.UTF8.GetBytes(createStationModelJson);

            Msg createStationResp = await _brokerConnection.RequestAsync(
                MemphisStations.MEMPHIS_STATION_CREATIONS, createStationReqBytes);
            string errResp = Encoding.UTF8.GetString(createStationResp.Data);

            if (!string.IsNullOrEmpty(errResp))
            {
                if (errResp.Contains("already exist"))
                {
                    return new MemphisStation(this, stationOptions.Name);
                }

                throw new MemphisException(errResp);
            }

            return new MemphisStation(this, stationOptions.Name);
        }
        catch (MemphisException)
        {
            throw;
        }
        catch (System.Exception e)
        {
            throw new MemphisException("Failed to create memphis station", e);
        }
    }

    /// <summary>
    /// Create Station
    /// </summary>
    /// <param name="stationName">station name</param>
    /// <param name="cancellationToken">cancellation token</param>
    /// <returns>An <see cref="MemphisStation"/> object representing the created station</returns>
    public async Task<MemphisStation> CreateStation(string stationName, CancellationToken cancellationToken = default)
    {
        return await CreateStation(new StationOptions { Name = stationName }, cancellationToken);
    }

    /// <summary>
    /// Applies schema to an existing station
    /// </summary>
    /// <param name="stationName">station name</param>
    /// <param name="schemaName">schema name</param>
    /// <returns></returns>
    public async Task EnforceSchema(string stationName, string schemaName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(stationName))
        {
            throw new ArgumentException($"{nameof(stationName)} cannot be null or empty");
        }

        if (string.IsNullOrEmpty(schemaName))
        {
            throw new ArgumentException($"{nameof(schemaName)} cannot be null or empty");
        }

        try
        {
            var attachSchemaRequestModel = new AttachSchemaRequest()
            {
                SchemaName = schemaName,
                StationName = stationName,
                UserName = _userName
            };

            var attachSchemaModelJson = JsonSerDes.PrepareJsonString<AttachSchemaRequest>(attachSchemaRequestModel);

            byte[] attachSchemaReqBytes = Encoding.UTF8.GetBytes(attachSchemaModelJson);

            Msg attachSchemaResp = await _brokerConnection.RequestAsync(
                MemphisStations.MEMPHIS_SCHEMA_ATTACHMENTS, attachSchemaReqBytes);
            string errResp = Encoding.UTF8.GetString(attachSchemaResp.Data);

            if (!string.IsNullOrEmpty(errResp))
            {
                throw new MemphisException(errResp);
            }
        }
        catch (MemphisException)
        {
            throw;
        }
        catch (System.Exception e)
        {
            throw new MemphisException("Failed to attach schema to station", e);
        }

    }

    /// <summary>
    /// Attach schema to an existing station
    /// </summary>
    /// <param name="stationName">station name</param>
    /// <param name="schemaName">schema name</param>
    /// <returns></returns>
    [Obsolete("This method is depricated. call EnforceSchema instead.")]
    public async Task AttachSchema(string stationName, string schemaName)
    {
        await EnforceSchema(stationName, schemaName);
    }

    /// <summary>
    /// DetachSchema Schema from station
    /// </summary>
    /// <param name="stationName">station name</param>
    /// <returns>No object or value is returned by this method when it completes.</returns>
    public async Task DetachSchema(string stationName)
    {
        if (string.IsNullOrEmpty(stationName))
        {
            throw new ArgumentException($"{nameof(stationName)} cannot be null or empty");
        }

        try
        {
            var detachSchemaRequestModel = new DetachSchemaRequest()
            {
                StationName = stationName,
                UserName = _userName
            };

            var detachSchemaModelJson = JsonSerDes.PrepareJsonString<DetachSchemaRequest>(detachSchemaRequestModel);

            byte[] detachSchemaReqBytes = Encoding.UTF8.GetBytes(detachSchemaModelJson);

            Msg detachSchemaResp = await _brokerConnection.RequestAsync(
                MemphisStations.MEMPHIS_SCHEMA_DETACHMENTS, detachSchemaReqBytes);
            string errResp = Encoding.UTF8.GetString(detachSchemaResp.Data);

            if (!string.IsNullOrEmpty(errResp))
            {
                throw new MemphisException(errResp);
            }
        }
        catch (MemphisException)
        {
            throw;
        }
        catch (System.Exception e)
        {
            throw new MemphisException("Failed to attach schema to station", e);
        }
    }

    /// <summary>
    /// Creates schema from the specified file path.
    /// </summary>
    /// <param name="schemaName">Name of the schema</param>
    /// <param name="schemaType">Type of schema(Eg. JSON, GraphQL, ProtoBuf)</param>
    /// <param name="schemaFilePath">Path of the schema file</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task CreateSchema(string schemaName, string schemaType, string schemaFilePath, CancellationToken cancellationToken = default)
    {
        EnsureSchemaNameIsValid(schemaName);
        EnsureSchemaTypeIsValid(schemaType);
        EnsureSchemaFileExists(schemaFilePath);

        var createSchemaRequest = new CreateSchemaRequest
        {
            Name = schemaName,
            Type = schemaType,
            SchemaContent = File.ReadAllText(schemaFilePath),
            CreatedByUsername = _userName,
            MessageStructName = string.Empty
        };

        var requestJson = JsonSerDes.PrepareJsonString<CreateSchemaRequest>(createSchemaRequest);
        var result = await _brokerConnection.RequestAsync(
           MemphisSubjects.SCHEMA_CREATION,
           Encoding.UTF8.GetBytes(requestJson),
           (int)TimeSpan.FromSeconds(5).TotalMilliseconds,
           cancellationToken);

        HandleSchemaCreationErrorResponse(result.Data);

        static void EnsureSchemaNameIsValid(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new MemphisException("Schema name can not be empty");
            if (name.Length > 128)
                throw new MemphisException("Schema name should be under 128 characters");
            string validNameRegex = "^[a-z0-9_.-]*$";
            if (Regex.Match(name, validNameRegex) is { Success: false })
                throw new MemphisException("Only alphanumeric and the '_', '-', '.' characters are allowed in schema name");
            if (!char.IsLetterOrDigit(name[0]) || !char.IsLetterOrDigit(name[name.Length - 1]))
                throw new MemphisException("Schema name can not start or end with non alphanumeric character");
        }

        static void EnsureSchemaTypeIsValid(string type)
        {
            if (string.IsNullOrWhiteSpace(type))
                throw new MemphisException("Schema type can not be empty");
            switch (type)
            {
                case MemphisSchemaTypes.JSON:
                case MemphisSchemaTypes.PROTO_BUF:
                case MemphisSchemaTypes.GRAPH_QL:
                case MemphisSchemaTypes.AVRO:
                    return;
                default:
                    throw new MemphisException("Unsupported schema type");
            }
        }

        static void EnsureSchemaFileExists(string path)
        {
            if (!File.Exists(path))
                throw new MemphisException("Schema file does not exist", new FileNotFoundException(path));
        }

        static void HandleSchemaCreationErrorResponse(byte[] responseBytes)
        {
            string responseStr = Encoding.UTF8.GetString(responseBytes);
            try
            {
                var response = (CreateSchemaResponse)JsonSerDes.PrepareObjectFromString<CreateSchemaResponse>(responseStr);
                if (!string.IsNullOrWhiteSpace(response.Error))
                    throw new MemphisException(response.Error);
            }
            catch (System.Exception e) when (e is not MemphisException)
            {
                if (!string.IsNullOrWhiteSpace(responseStr))
                    throw new MemphisException(responseStr);
            }
        }
    }

    internal string GetStationSchemaType(string internalStationName)
    {
        if (_schemaUpdateDictionary.TryGetValue(internalStationName,
            out ProducerSchemaUpdateInit schemaUpdateInit))
        {
            return schemaUpdateInit.SchemaType;
        }

        return string.Empty;
    }


    internal async Task ValidateMessageAsync(byte[] message, string internalStationName, string producerName)
    {
        if (!_schemaUpdateDictionary.TryGetValue(internalStationName,
            out ProducerSchemaUpdateInit schemaUpdateInit))
        {
            return;
        }

        try
        {
            switch (schemaUpdateInit.SchemaType)
            {
                case ProducerSchemaUpdateInit.SchemaTypes.JSON:
                    {
                        if (_schemaValidators.TryGetValue(ValidatorType.JSON, out ISchemaValidator schemaValidator))
                        {
                            await schemaValidator.ValidateAsync(message, schemaUpdateInit.SchemaName);
                        }

                        break;
                    }
                case ProducerSchemaUpdateInit.SchemaTypes.GRAPHQL:
                    {
                        if (_schemaValidators.TryGetValue(ValidatorType.GRAPHQL, out ISchemaValidator schemaValidator))
                        {
                            await schemaValidator.ValidateAsync(message, schemaUpdateInit.SchemaName);
                        }

                        break;
                    }
                case ProducerSchemaUpdateInit.SchemaTypes.PROTOBUF:
                    {
                        if (_schemaValidators.TryGetValue(ValidatorType.PROTOBUF, out ISchemaValidator schemaValidator))
                        {
                            await schemaValidator.ValidateAsync(message, schemaUpdateInit.SchemaName);
                        }

                        break;
                    }
                default:
                    throw new NotImplementedException($"Schema of type: {schemaUpdateInit.SchemaType} not implemented");
            }
        }
        catch (MemphisSchemaValidationException e)
        {
            await SendNotificationAsync(title: "Schema validation has failed",
                message: $"Station: {MemphisUtil.GetStationName(internalStationName)}"
                + $"\nProducer: {producerName}"
                + $"\nError: {e.Message}",
                code: Encoding.UTF8.GetString(message),
                msgType: "schema_validation_fail_alert");
            throw;
        }
    }

    internal async Task NotifyRemoveProducer(string stationName)
    {
        var internalStationName = MemphisUtil.GetInternalName(stationName);
        if (_producerPerStations.TryGetValue(internalStationName, out int prodCnt))
        {
            if (prodCnt == 0)
            {
                return;
            }

            var prodCntAfterRemove = prodCnt - 1;
            _producerPerStations.TryUpdate(internalStationName, prodCntAfterRemove, prodCnt);

            // Is there any producer for given station ?
            if (prodCntAfterRemove == 0)
            {
                //unsubscribe for listening schema updates
                if (_subscriptionPerSchema.TryGetValue(internalStationName, out ISyncSubscription subscription))
                {
                    await subscription.DrainAsync();
                }

                if (_schemaUpdateDictionary.TryRemove(internalStationName,
                    out ProducerSchemaUpdateInit schemaUpdateInit)
                )
                {
                    // clean up cache from unused schema data
                    foreach (var schemaValidator in _schemaValidators.Values)
                    {
                        schemaValidator.RemoveSchema(schemaUpdateInit.SchemaName);
                    }
                }
            }
        }
    }

    internal void NotifyRemoveConsumer(string stationName)
    {
        return;
    }

    /// <summary>
    /// Create a new consumer
    /// </summary>
    /// <param name="fetchMessageOptions">Fetch message options</param>
    /// <returns>MemphisConsumer</returns>
    public async Task<MemphisConsumer> CreateConsumer(FetchMessageOptions fetchMessageOptions)
    {
        return await CreateConsumer(new MemphisConsumerOptions
        {
            StationName = fetchMessageOptions.StationName,
            ConsumerName = fetchMessageOptions.ConsumerName,
            ConsumerGroup = fetchMessageOptions.ConsumerGroup,
            BatchSize = fetchMessageOptions.BatchSize,
            BatchMaxTimeToWaitMs = fetchMessageOptions.BatchMaxTimeToWaitMs,
            MaxAckTimeMs = fetchMessageOptions.MaxAckTimeMs,
            MaxMsgDeliveries = fetchMessageOptions.MaxMsgDeliveries,
            GenerateUniqueSuffix = fetchMessageOptions.GenerateUniqueSuffix,
            StartConsumeFromSequence = fetchMessageOptions.StartConsumeFromSequence,
            LastMessages = fetchMessageOptions.LastMessages,
        });
    }


    internal async Task RemoveStation(MemphisStation station, CancellationToken cancellationToken = default)
    {
        var request = new RemoveStationRequest
        {
            StationName = station.Name,
            Username = _userName
        };

        if (_subscriptionPerSchema.TryGetValue(station.InternalName, out ISyncSubscription subscription) &&
            subscription.IsValid)
        {
            subscription.Unsubscribe();
        }
        _stationSchemaVerseToDlsMap.TryRemove(station.InternalName, out bool _);
        _schemaUpdateDictionary.TryRemove(station.InternalName, out ProducerSchemaUpdateInit _);
        _subscriptionPerSchema.TryRemove(station.InternalName, out ISyncSubscription _);
        _producerPerStations.TryRemove(station.InternalName, out int _);

        var requestJson = JsonSerDes.PrepareJsonString<RemoveStationRequest>(request);
        var result = await _brokerConnection.RequestAsync(
            MemphisStations.MEMPHIS_STATION_DESTRUCTION, Encoding.UTF8.GetBytes(requestJson), cancellationToken);

        string errResp = Encoding.UTF8.GetString(result.Data);

        if (!string.IsNullOrEmpty(errResp))
        {
            throw new MemphisException(errResp);
        }

        RemoveStationConsumers(station.Name);
        RemoveStationProducers(station.Name);
    }

    internal async Task SendNotificationAsync(string title, string message, string code, string msgType)
    {
        var notificationModel = new NotificationRequest()
        {
            Title = title,
            Message = message,
            Code = code,
            Type = msgType
        };

        var notificationModelJson = JsonSerDes.PrepareJsonString<NotificationRequest>(notificationModel);

        byte[] notificationReqBytes = Encoding.UTF8.GetBytes(notificationModelJson);

        _brokerConnection.Publish(MemphisStations.MEMPHIS_NOTIFICATIONS, notificationReqBytes);
    }

    internal IConnection BrokerConnection
    {
        get { return _brokerConnection; }
    }

    internal string Username
    {
        get { return _userName; }
    }

    internal ConcurrentDictionary<string, Dictionary<string, List<MemphisMessage>>> PrefetchedMessages { get; set; }

    internal IJetStream JetStreamConnection
    {
        get => _jetStreamContext;
    }

    internal IJetStreamManagement JetStreamManagement
    {
        get => _jetStreamManagement;
    }

    internal string ConnectionId
    {
        get { return _connectionId; }
    }

    internal static readonly string ApplicationId;

    internal bool IsSchemaVerseToDlsEnabled(string stationName)
        => _stationSchemaVerseToDlsMap.TryGetValue(stationName, out bool schemaVerseToDls) && schemaVerseToDls;

    internal bool IsSendingNotificationEnabled
       => _clusterConfigurations.TryGetValue(MemphisSdkClientUpdateTypes.SEND_NOTIFICATION, out bool sendNotification) && sendNotification;

    internal ConcurrentDictionary<string, PartitionsUpdate> StationPartitions { get => _stationPartitions; }

    private async Task ProcessAndStoreSchemaUpdate(string internalStationName, Msg message)
    {
        string respAsJson = Encoding.UTF8.GetString(message.Data);
        var respAsObject =
            (ProducerSchemaUpdate)JsonSerDes.PrepareObjectFromString<ProducerSchemaUpdate>(respAsJson);

        await ProcessAndStoreSchemaUpdate(internalStationName, respAsObject.Init);
    }

    private async Task ProcessAndStoreSchemaUpdate(string internalStationName, ProducerSchemaUpdateInit schemaUpdate)
    {
        if (string.IsNullOrWhiteSpace(internalStationName))
        {
            throw new MemphisException(
                $"Unable to save schema: invalid internal station name: {internalStationName}");
        }

        if (schemaUpdate is null || string.IsNullOrWhiteSpace(schemaUpdate.SchemaName))
            return;

        _schemaUpdateDictionary.AddOrUpdate(internalStationName,
            schemaUpdate,
            (key, _) => schemaUpdate);

        switch (schemaUpdate.SchemaType)
        {
            case ProducerSchemaUpdateInit.SchemaTypes.JSON:
                {
                    if (_schemaValidators.TryGetValue(ValidatorType.JSON, out ISchemaValidator schemaValidator))
                    {
                        bool isDone = schemaValidator.ParseAndStore(
                            schemaUpdate.SchemaName,
                            schemaUpdate.ActiveVersion?.Content);

                        if (!isDone)
                        {
                            //TODO raise notification regarding unable to parse schema pushed by Memphis
                            throw new InvalidOperationException($"Unable to parse and store " +
                                                                $"schema: {schemaUpdate?.SchemaName}, type: {schemaUpdate?.SchemaType}" +
                                                                $"in local cache");
                        }
                    }

                    break;
                }
            case ProducerSchemaUpdateInit.SchemaTypes.GRAPHQL:
                {
                    if (_schemaValidators.TryGetValue(ValidatorType.GRAPHQL, out ISchemaValidator schemaValidator))
                    {
                        bool isDone = schemaValidator.ParseAndStore(
                            schemaUpdate.SchemaName,
                            schemaUpdate.ActiveVersion?.Content);

                        if (!isDone)
                        {
                            //TODO raise notification regarding unable to parse schema pushed by Memphis
                            throw new InvalidOperationException($"Unable to parse and store " +
                                                                $"schema: {schemaUpdate?.SchemaName}, type: {schemaUpdate?.SchemaType}" +
                                                                $"in local cache");
                        }
                    }

                    break;
                }
            case ProducerSchemaUpdateInit.SchemaTypes.PROTOBUF:
                {
                    if (_schemaValidators.TryGetValue(ValidatorType.PROTOBUF, out ISchemaValidator schemaValidator))
                    {
                        bool isDone = schemaValidator.ParseAndStore(
                            schemaUpdate.SchemaName,
                            JsonConvert.SerializeObject(schemaUpdate.ActiveVersion));

                        if (!isDone)
                        {
                            //TODO raise notification regarding unable to parse schema pushed by Memphis
                            throw new InvalidOperationException($"Unable to parse and store " +
                                                                $"schema: {schemaUpdate?.SchemaName}, type: {schemaUpdate?.SchemaType}" +
                                                                $"in local cache");
                        }
                    }

                    break;
                }
        }
    }

    private void RegisterSchemaValidators()
    {
        if (!_schemaValidators.TryAdd(ValidatorType.GRAPHQL, new GraphqlValidator()))
        {
            throw new InvalidOperationException($"Unable to register schema validator: {nameof(GraphqlValidator)}");
        }

        if (!_schemaValidators.TryAdd(ValidatorType.JSON, new JsonValidator()))
        {
            throw new InvalidOperationException($"Unable to register schema validator: {nameof(JsonValidator)}");
        }

        if (!_schemaValidators.TryAdd(ValidatorType.PROTOBUF, new ProtoBufValidator()))
        {
            throw new InvalidOperationException($"Unable to register schema validator: {nameof(ProtoBufValidator)}");
        }

        if (!_schemaValidators.TryAdd(ValidatorType.AVRO, new AvroValidator()))
        {
            throw new InvalidOperationException($"Unable to register schema validator: {nameof(AvroValidator)}");
        }
    }

    private async Task ListenForSchemaUpdate(string internalStationName, ProducerSchemaUpdateInit schemaUpdateInit)
    {
        try
        {
            await _schemaUpdateSemaphore.WaitAsync();


            var schemaUpdateSubject = MemphisSubjects.MEMPHIS_SCHEMA_UPDATE + internalStationName;

            if (!string.IsNullOrEmpty(schemaUpdateInit.SchemaName))
            {
                _schemaUpdateDictionary.TryAdd(internalStationName, schemaUpdateInit);
            }


            if (_subscriptionPerSchema.TryGetValue(internalStationName, out ISyncSubscription schemaSub))
            {
                _producerPerStations.AddOrUpdate(internalStationName, 1, (key, val) => val + 1);
                return;
            }

            var subscription = _brokerConnection.SubscribeSync(schemaUpdateSubject);

            if (!_subscriptionPerSchema.TryAdd(internalStationName, subscription))
            {
                throw new MemphisException("Unable to add subscription of schema updates for station");
            }

            await ProcessAndStoreSchemaUpdate(internalStationName, schemaUpdateInit);

            Task.Run(async () =>
            {
                while (!_cancellationTokenSource.IsCancellationRequested)
                {
                    var schemaUpdateMsg = subscription.NextMessage();
                    await ProcessAndStoreSchemaUpdate(internalStationName, schemaUpdateMsg);
                }
            }, _cancellationTokenSource.Token);

            _producerPerStations.AddOrUpdate(internalStationName, 1, (key, val) => val + 1);
        }
        finally
        {
            _schemaUpdateSemaphore.Release();
        }
    }


    private async Task RemoveSchemaUpdateListener(string stationName)
    {
        try
        {
            await _schemaUpdateSemaphore.WaitAsync();
            var internalStationName = MemphisUtil.GetInternalName(stationName);
            if (_subscriptionPerSchema.TryRemove(internalStationName, out ISyncSubscription subscription))
            {
                subscription.Unsubscribe();
            }
        }
        finally
        {
            _schemaUpdateSemaphore.Release();
        }
    }

    internal Task ListenForSdkClientUpdate()
    {
        var subscription = _brokerConnection.SubscribeSync(MemphisSubjects.SDK_CLIENTS_UPDATE);

        Task.Run(SyncSdkClientUpdate, _cancellationTokenSource.Token);

        void SyncSdkClientUpdate()
        {
            try
            {
                while (!_cancellationTokenSource.IsCancellationRequested)
                {
                    var updateMsg = subscription.NextMessage();
                    if (updateMsg is null)
                        continue;
                    string respAsJson = Encoding.UTF8.GetString(updateMsg.Data);

                    SdkClientsUpdate sdkClientUpdate = default;
                    try
                    {
                        sdkClientUpdate = JsonConvert.DeserializeObject<SdkClientsUpdate>(respAsJson);
                    }
                    catch (System.Exception exc)
                    {
                        throw new MemphisException($"Unable to deserialize sdk client update: {respAsJson}", exc);
                    }

                    try
                    {
                        _sdkClientUpdateSemaphore.WaitAsync();
                        bool sdkClientShouldUpdate = sdkClientUpdate!.Update ?? false;
                        switch (sdkClientUpdate!.Type)
                        {
                            case MemphisSdkClientUpdateTypes.SEND_NOTIFICATION:
                                _clusterConfigurations.AddOrUpdate(sdkClientUpdate.Type, sdkClientShouldUpdate, (key, _) => sdkClientShouldUpdate);
                                break;
                            case MemphisSdkClientUpdateTypes.SCHEMA_VERSE_TO_DLS:
                                _stationSchemaVerseToDlsMap.AddOrUpdate(sdkClientUpdate.StationName, sdkClientShouldUpdate, (key, _) => sdkClientShouldUpdate);
                                break;
                            case MemphisSdkClientUpdateTypes.REMOVE_STATION:
                                RemoveStationProducers(sdkClientUpdate.StationName);
                                RemoveStationConsumers(sdkClientUpdate.StationName);
                                RemoveSchemaUpdateListener(sdkClientUpdate.StationName);
                                break;
                            default:
                                break;
                        }
                    }
                    finally
                    {
                        _sdkClientUpdateSemaphore.Release();
                    }
                }
            }
            catch (System.Exception exception)
            {
                throw new MemphisException(exception.Message, exception);
            }
        }

        return Task.CompletedTask;
    }

    private void RemoveStationProducers(string stationName)
    {
        var internalStationName = MemphisUtil.GetInternalName(stationName);
        foreach (var entry in _producerCache)
        {
            if (entry.Value.InternalStationName.Equals(internalStationName))
            {
                _producerCache.TryRemove(entry.Key, out MemphisProducer _);
            }
        }
    }

    private void RemoveProducer(MemphisProducer producer)
    {
        _producerCache.TryRemove(producer.Key, out MemphisProducer _);
    }

    internal void RemoveConsumer(MemphisConsumer consumer)
    {
        _consumerCache.TryRemove(consumer.Key, out MemphisConsumer _);
    }

    private void RemoveStationConsumers(string stationName)
    {
        var internalStationName = MemphisUtil.GetInternalName(stationName);
        foreach (var entry in _consumerCache)
        {
            if (entry.Value.InternalStationName.Equals(internalStationName))
            {
                _producerCache.TryRemove(entry.Key, out MemphisProducer _);
            }
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(true);
    }

    public void Dispose(bool disposing)
    {
        if (_desposed)
            return;

        if (disposing)
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            _brokerConnection.Dispose();
        }

        _desposed = true;
    }
}
