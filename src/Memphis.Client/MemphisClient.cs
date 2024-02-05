using Memphis.Client.Consumer;

using Memphis.Client.Producer;
using Memphis.Client.Station;
using Memphis.Client.Validators;
using Murmur;

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
#pragma warning disable CS8604 // Possible null reference argument.

namespace Memphis.Client;

public sealed partial class MemphisClient : IMemphisClient
{
    private bool _disposed;
    private readonly Options _brokerConnOptions;
    private readonly IConnection _brokerConnection;
    private readonly IJetStream _jetStreamContext;
    private readonly IJetStreamManagement _jetStreamManagement;
    private readonly string _connectionId;
    private readonly string _userName;
    private CancellationTokenSource _cancellationTokenSource;

    // Dictionary key: station (internal)name, value: schema update data for that station
    private readonly ConcurrentDictionary<string, SchemaUpdateInit> _schemaUpdateDictionary;

    // Dictionary key: station (internal)name, value: subscription for fetching schema updates for station
    private readonly ConcurrentDictionary<string, ISyncSubscription> _subscriptionPerSchema;

    /// <summary>
    /// Dictionary key: station (internal)name, value: number of schema update listners for that station. 
    /// Schema update listners can be either producers or consumers
    /// </summary>
    private readonly ConcurrentDictionary<string, int> _stationSchemaUpdateListeners;
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
        _stationSchemaUpdateListeners = new();
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

    public async Task<IEnumerable<MemphisMessage>> FetchMessages(
        FetchMessageOptions options,
        CancellationToken cancellationToken = default
    )
    {
        if (!IsConnected())
        {
            throw MemphisExceptions.DeadConnectionException;
        }

        MemphisConsumer? consumer = default;
        var internalStationName = MemphisUtil.GetInternalName(options.StationName);
        var consumerKey = $"{internalStationName}_{options.ConsumerName.ToLower()}";

        if (_consumerCache.TryGetValue(consumerKey, out var cacheConsumer))
        {
            consumer = cacheConsumer;
        }

        consumer ??= await CreateConsumer(options);

        return consumer.Fetch(options);
    }


    internal async Task<Msg> RequestAsync(
        string subject,
        byte[] message,
        int timeoutRetry,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            int timeoutMilliSeconds = (int)TimeSpan.FromSeconds(20).TotalMilliseconds;
            return await _brokerConnection.RequestAsync(subject, message, timeoutMilliSeconds);
        }
        catch (NATSTimeoutException)
        {
            if (timeoutRetry <= 0)
                throw;
            return await RequestAsync(subject, message, timeoutRetry - 1, cancellationToken);
        }
    }

    internal async Task<PublishAck> PublishWithJetStreamAsync(
        Msg message,
        PublishOptions options
    )
    {
        return await _jetStreamContext.PublishAsync(message, options);
    }

    /// <summary>
    /// Creates schema from the specified file path. In case schema is already exist a new version will be created
    /// </summary>
    /// <param name="schemaName">Name of the schema</param>
    /// <param name="schemaType">Type of schema(Eg. JSON, GraphQL, ProtoBuf)</param>
    /// <param name="schemaFilePath">Path of the schema file</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task CreateSchema(
        string schemaName,
        string schemaType,
        string schemaFilePath,
        int timeoutRetry = 5,
        CancellationToken cancellationToken = default)
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

        var requestJson = JsonSerializer.Serialize(createSchemaRequest);
        var result = await RequestAsync(
           MemphisSubjects.SCHEMA_CREATION,
           Encoding.UTF8.GetBytes(requestJson),
           timeoutRetry,
           cancellationToken);

        HandleSchemaCreationErrorResponse(result.Data);

        static void EnsureSchemaNameIsValid(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw MemphisExceptions.EmptySchemaNameException;
            if (name.Length > 128)
                throw MemphisExceptions.SchemaNameTooLongException;
            string validNameRegex = "^[a-z0-9_.-]*$";
            if (Regex.Match(name, validNameRegex) is { Success: false })
                throw MemphisExceptions.InvalidSchemaNameException;
            if (!char.IsLetterOrDigit(name[0]) || !char.IsLetterOrDigit(name[name.Length - 1]))
                throw MemphisExceptions.InvalidSchemaStartEndCharsException;
        }

        static void EnsureSchemaTypeIsValid(string type)
        {
            if (string.IsNullOrWhiteSpace(type))
                throw MemphisExceptions.EmptySchemaTypeException;
            switch (type)
            {
                case MemphisSchemaTypes.JSON:
                case MemphisSchemaTypes.PROTO_BUF:
                case MemphisSchemaTypes.GRAPH_QL:
                case MemphisSchemaTypes.AVRO:
                    return;
                default:
                    throw MemphisExceptions.UnsupportedSchemaTypeException;
            }
        }

        static void EnsureSchemaFileExists(string path)
        {
            if (!File.Exists(path))
                throw MemphisExceptions.SchemaDoesNotExistException(path);
        }

        static void HandleSchemaCreationErrorResponse(byte[] responseBytes)
        {
            string responseStr = Encoding.UTF8.GetString(responseBytes);
            try
            {
                var response = JsonSerializer.Deserialize<CreateSchemaResponse>(responseStr);
                if (!string.IsNullOrWhiteSpace(response.Error) && !response.Error.Contains("already exists"))
                    throw new MemphisException(response.Error);
            }
            catch (System.Exception e) when (e is not MemphisException)
            {
                if (!string.IsNullOrWhiteSpace(responseStr))
                    throw new MemphisException(responseStr);
            }
        }
    }

    internal static void EnsureBatchSizeIsValid(int batchSize)
    {
        if (batchSize > MemphisGlobalVariables.MAX_BATCH_SIZE ||
           batchSize < 1)
            throw new MemphisException($"Batch size should be between 1 and {MemphisGlobalVariables.MAX_BATCH_SIZE}");
    }

    internal string GetStationSchemaType(string internalStationName)
    {
        if (_schemaUpdateDictionary.TryGetValue(internalStationName,
            out SchemaUpdateInit schemaUpdateInit))
        {
            return schemaUpdateInit.SchemaType;
        }

        return string.Empty;
    }

    internal async Task ValidateMessageAsync(byte[] message, string internalStationName, string producerName)
    {
        if (!_schemaUpdateDictionary.TryGetValue(internalStationName, out SchemaUpdateInit schemaUpdateInit) ||
            schemaUpdateInit is null)
            return;

        try
        {
            var validatorType = MemphisSchemaTypes.ToValidator(schemaUpdateInit.SchemaType);
            if (_schemaValidators.TryGetValue(validatorType, out ISchemaValidator validator))
                await validator.ValidateAsync(message, schemaUpdateInit.SchemaName);
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
        await RemoveFunctionUpdateListenerAsync(stationName);
        await RemoveFromSchemaUpdateListener(stationName);
    }

    internal async Task NotifyRemoveConsumer(string stationName)
    {
        await RemoveFromSchemaUpdateListener(stationName);
    }

    private async Task RemoveFromSchemaUpdateListener(string stationName)
    {
        var internalStationName = MemphisUtil.GetInternalName(stationName);
        if (_stationSchemaUpdateListeners.TryGetValue(internalStationName, out int updateListenersCount))
        {
            if (updateListenersCount == 0)
            {
                return;
            }

            var updateListenerCountAfterRemove = updateListenersCount - 1;
            _stationSchemaUpdateListeners.TryUpdate(internalStationName, updateListenerCountAfterRemove, updateListenersCount);

            if (updateListenerCountAfterRemove <= 0)
            {
                if (_subscriptionPerSchema.TryGetValue(internalStationName, out ISyncSubscription subscription))
                {
                    await subscription.DrainAsync();
                    _subscriptionPerSchema.TryRemove(internalStationName, out _);
                }

                if (_schemaUpdateDictionary.TryRemove(internalStationName,
                    out SchemaUpdateInit schemaUpdateInit)
                )
                {
                    foreach (var schemaValidator in _schemaValidators.Values)
                    {
                        schemaValidator.RemoveSchema(schemaUpdateInit.SchemaName);
                    }
                }
            }
        }
    }


    internal async Task RemoveStation(MemphisStation station, int timeoutRetry = 5, CancellationToken cancellationToken = default)
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
        _schemaUpdateDictionary.TryRemove(station.InternalName, out SchemaUpdateInit _);
        _subscriptionPerSchema.TryRemove(station.InternalName, out ISyncSubscription _);
        _stationSchemaUpdateListeners.TryRemove(station.InternalName, out int _);

        var requestJson = JsonSerializer.Serialize(request);
        var result = await RequestAsync(
            MemphisStations.MEMPHIS_STATION_DESTRUCTION,
            Encoding.UTF8.GetBytes(requestJson),
            timeoutRetry,
            cancellationToken);

        string errResp = Encoding.UTF8.GetString(result.Data);

        if (!string.IsNullOrEmpty(errResp))
        {
            throw new MemphisException(errResp);
        }

        RemoveStationConsumers(station.Name);
        RemoveStationProducers(station.Name);
    }

    internal int GetPartitionFromKey(string key, string stationName)
    {
        var hasher = MurmurHash.Create32(MemphisGlobalVariables.MURMUR_HASH_SEED);
        var hash = hasher.ComputeHash(Encoding.UTF8.GetBytes(key));
        var unsignedHash = BitConverter.ToUInt32(hash, 0);
        var partitionLength = _stationPartitions[stationName].PartitionsList.Length;
        var partitionIndex = unsignedHash % partitionLength;

        return _stationPartitions[stationName].PartitionsList[partitionIndex];
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

        var notificationModelJson = JsonSerializer.Serialize(notificationModel);

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
        if (message is null)
            return;

        string respAsJson = Encoding.UTF8.GetString(message.Data);
        var respAsObject = JsonSerializer.Deserialize<ProducerSchemaUpdate>(respAsJson);

        await ProcessAndStoreSchemaUpdate(internalStationName, respAsObject.Init);
    }

    private async Task ProcessAndStoreSchemaUpdate(string internalStationName, SchemaUpdateInit schemaUpdate)
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

        var validatorType = MemphisSchemaTypes.ToValidator(schemaUpdate.SchemaType);
        if (_schemaValidators.TryGetValue(validatorType, out ISchemaValidator schemaValidatorCache))
        {
            var schemaStored = schemaValidatorCache.AddOrUpdateSchema(schemaUpdate);
            if (!schemaStored)
            {
                //TODO raise notification regarding unable to parse schema pushed by Memphis
                throw new InvalidOperationException($"Unable to parse and store " +
                                                    $"schema: {schemaUpdate?.SchemaName}, type: {schemaUpdate?.SchemaType}" +
                                                    $"in local cache");
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

    private async Task ListenForSchemaUpdate(string stationName)
    {
        string internalStationName = MemphisUtil.GetInternalName(stationName);
        var schemaUpdateSubject = MemphisSubjects.MEMPHIS_SCHEMA_UPDATE + internalStationName;

        if (_subscriptionPerSchema.TryGetValue(internalStationName, out _))
        {
            _stationSchemaUpdateListeners.AddOrUpdate(internalStationName, 1, (key, val) => val + 1);
            return;
        }

        var subscription = _brokerConnection.SubscribeSync(schemaUpdateSubject);

        if (!_subscriptionPerSchema.TryAdd(internalStationName, subscription))
        {
            throw MemphisExceptions.SchemaUpdateSubscriptionFailedException;
        }

        Task.Factory.StartNew(async () =>
        {
            while (!_cancellationTokenSource.IsCancellationRequested)
            {
                if (subscription is null || !subscription.IsValid)
                    break;

                var schemaUpdateMsg = subscription.NextMessage();
                await ProcessAndStoreSchemaUpdate(internalStationName, schemaUpdateMsg);
            }
        }, _cancellationTokenSource.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);

        _stationSchemaUpdateListeners.AddOrUpdate(internalStationName, 1, (key, val) => val + 1);
    }

    private async Task ListenForSchemaUpdate(string internalStationName, SchemaUpdateInit schemaUpdateInit)
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
                _stationSchemaUpdateListeners.AddOrUpdate(internalStationName, 1, (key, val) => val + 1);
                return;
            }

            var subscription = _brokerConnection.SubscribeSync(schemaUpdateSubject);

            if (!_subscriptionPerSchema.TryAdd(internalStationName, subscription))
            {
                throw MemphisExceptions.SchemaUpdateSubscriptionFailedException;
            }

            await ProcessAndStoreSchemaUpdate(internalStationName, schemaUpdateInit);

            Task.Factory.StartNew(async () =>
            {
                while (!_cancellationTokenSource.IsCancellationRequested)
                {
                    if (subscription is null || !subscription.IsValid)
                        break;

                    var schemaUpdateMsg = subscription.NextMessage();
                    await ProcessAndStoreSchemaUpdate(internalStationName, schemaUpdateMsg);
                }
            }, _cancellationTokenSource.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);

            _stationSchemaUpdateListeners.AddOrUpdate(internalStationName, 1, (key, val) => val + 1);
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

        Task.Factory.StartNew(SyncSdkClientUpdate, _cancellationTokenSource.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);

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
                        sdkClientUpdate = JsonSerializer.Deserialize<SdkClientsUpdate>(respAsJson);
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
                                RemoveFunctionUpdateListenerAsync(sdkClientUpdate.StationName);
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
            catch when (!IsConnected())
            {
                // Connection is closed   
            }
            catch (System.Exception exception)
            {
                throw new MemphisException(exception.Message, exception);
            }
        }

        return Task.CompletedTask;
    }

    internal void EnsurePartitionNumberIsValid(int partitionNumber, string stationName)
    {
        var (isValue, error) = ValidatePartitionNumber(partitionNumber, stationName);
        if (!isValue)
        {
            throw new MemphisException(error);
        }

        (bool IsValue, string Error) ValidatePartitionNumber(int partitionNumber, string stationName)
        {
            if (partitionNumber < 0 || partitionNumber > _stationPartitions[stationName].PartitionsList.Length)
            {
                return (false, "Partition number is out of range");
            }
            foreach (var partition in _stationPartitions[stationName].PartitionsList)
            {
                if (partition == partitionNumber)
                {
                    return (true, string.Empty);
                }
            }
            return (false, $"Partition {partitionNumber} does not exist in station {stationName}");
        }
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
        if (_disposed)
            return;

        if (disposing)
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            _brokerConnection.Dispose();
        }

        _disposed = true;
    }
}
