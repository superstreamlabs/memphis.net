using Memphis.Client.Station;

namespace Memphis.Client;

public partial class MemphisClient
{
    private readonly ConcurrentDictionary<string, FunctionsDetails> _functionDetails = new();
    private readonly ConcurrentDictionary<string, IAsyncSubscription> _functionDetailSubscriptions = new();
    private readonly ConcurrentDictionary<string, int> _functionDetailSubscriptionCounter = new();

    internal ConcurrentDictionary<string, FunctionsDetails> FunctionDetails { get => _functionDetails; }

    /// <summary>
    /// Create Station 
    /// </summary>
    /// <param name="stationOptions">options used to customize the parameters of station</param>
    /// <param name="cancellationToken">cancellation token</param>
    /// <returns>An <see cref="MemphisStation"/> object representing the created station</returns>
    public async Task<MemphisStation> CreateStation(StationOptions stationOptions, int timeoutRetry = 5, CancellationToken cancellationToken = default)
    {
        if (_brokerConnection.IsClosed())
        {
            throw MemphisExceptions.DeadConnectionException;
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
                PartitionsNumber = stationOptions.PartitionsNumber,
                DlsStation = stationOptions.DlsStation
            };

            var createStationModelJson = JsonSerializer.Serialize(createStationModel);

            byte[] createStationReqBytes = Encoding.UTF8.GetBytes(createStationModelJson);

            Msg createStationResp = await RequestAsync(MemphisStations.MEMPHIS_STATION_CREATIONS, createStationReqBytes, timeoutRetry, cancellationToken);
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
            throw MemphisExceptions.FailedToCreateStationException(e);
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
    /// Applies schema to an existing station
    /// </summary>
    /// <param name="stationName">station name</param>
    /// <param name="schemaName">schema name</param>
    /// <returns></returns>
    public async Task EnforceSchema(string stationName, string schemaName, int timeoutRetry = 5, CancellationToken cancellationToken = default)
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

            var attachSchemaModelJson = JsonSerializer.Serialize(attachSchemaRequestModel);

            byte[] attachSchemaReqBytes = Encoding.UTF8.GetBytes(attachSchemaModelJson);

            Msg attachSchemaResp = await RequestAsync(MemphisStations.MEMPHIS_SCHEMA_ATTACHMENTS, attachSchemaReqBytes, timeoutRetry, cancellationToken);
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
            throw MemphisExceptions.FailedToAttachSchemaException(e);
        }
    }


    /// <summary>
    /// DetachSchema Schema from station
    /// </summary>
    /// <param name="stationName">station name</param>
    /// <returns>No object or value is returned by this method when it completes.</returns>
    public async Task DetachSchema(string stationName, int timeoutRetry = 5, CancellationToken cancellationToken = default)
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

            var detachSchemaModelJson = JsonSerializer.Serialize(detachSchemaRequestModel);

            byte[] detachSchemaReqBytes = Encoding.UTF8.GetBytes(detachSchemaModelJson);

            Msg detachSchemaResp = await RequestAsync(MemphisStations.MEMPHIS_SCHEMA_DETACHMENTS, detachSchemaReqBytes, timeoutRetry, cancellationToken);
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
            throw MemphisExceptions.FailedToAttachSchemaException(e);
        }
    }


    /// <summary>
    /// Create Station
    /// </summary>
    /// <param name="stationName">station name</param>
    /// <param name="cancellationToken">cancellation token</param>
    /// <returns>An <see cref="MemphisStation"/> object representing the created station</returns>
    public async Task<MemphisStation> CreateStation(string stationName, int timeoutRetry = 5, CancellationToken cancellationToken = default)
    {
        return await CreateStation(new StationOptions { Name = stationName }, timeoutRetry, cancellationToken);
    }


    private Task ListenForFunctionUpdate(string stationName, int stationVersion, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(stationName) ||
            stationVersion < 2)
            return Task.CompletedTask;

        try
        {
            var internalStationName = MemphisUtil.GetInternalName(stationName);
            if (_functionDetailSubscriptions.TryGetValue(internalStationName, out _))
            {
                _functionDetailSubscriptionCounter.AddOrUpdate(internalStationName, 1, (_, count) => count + 1);
                return Task.CompletedTask;
            }
            var functionUpdateSubject = $"{MemphisSubjects.FUNCTIONS_UPDATE}{internalStationName}";
            var subscription = _brokerConnection.SubscribeAsync(functionUpdateSubject, FunctionUpdateEventHandler);
            if (!_functionDetailSubscriptions.TryAdd(internalStationName, subscription))
                throw new MemphisException($"Could not add subscription for {functionUpdateSubject}.");
            _functionDetailSubscriptionCounter.AddOrUpdate(internalStationName, 1, (_, count) => count + 1);
            return Task.CompletedTask;
        }
        catch (System.Exception e)
        {
            throw new MemphisException(e.Message, e);
        }

        void FunctionUpdateEventHandler(object sender, MsgHandlerEventArgs e)
        {
            if (e is null || e.Message is null)
                return;

            var jsonData = Encoding.UTF8.GetString(e.Message.Data);
            var functionsUpdate = JsonSerializer.Deserialize<FunctionsUpdate>(jsonData);
            if (functionsUpdate is null)
                return;

            _functionDetails.AddOrUpdate(
                e.Message.Subject,
                new FunctionsDetails { PartitionsFunctions = functionsUpdate.Functions },
                (_, _) => new FunctionsDetails { PartitionsFunctions = functionsUpdate.Functions });
        }
    }

    private async Task RemoveFunctionUpdateListenerAsync(string stationName)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(stationName))
                return;

            var internalStationName = MemphisUtil.GetInternalName(stationName);
            if (!_functionDetailSubscriptionCounter.TryGetValue(internalStationName, out var count) ||
                count <= 0)
                return;

            int countAfterRemoval = count - 1;
            _functionDetailSubscriptionCounter.TryUpdate(internalStationName, countAfterRemoval, count);
            if (countAfterRemoval <= 0)
            {
                if (_functionDetailSubscriptions.TryGetValue(internalStationName, out var subscriptionToRemove))
                {
                    await subscriptionToRemove.DrainAsync();
                    _functionDetailSubscriptions.TryRemove(internalStationName, out _);
                }
                _functionDetails.TryRemove(internalStationName, out _);
            }
        }
        catch (System.Exception e)
        {
            throw new MemphisException(e.Message, e);
        }
    }

    private async Task CreateStationsAsync(IEnumerable<StationOptions> stations, int timeoutRetry, CancellationToken cancellationToken)
    {
        if (stations is null)
            return;

        var tasks = stations.Select(station => CreateStation(station, timeoutRetry, cancellationToken));
        await Task.WhenAll(tasks);
    }
}
