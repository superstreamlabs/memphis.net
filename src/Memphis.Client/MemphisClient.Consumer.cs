using Memphis.Client.Consumer;

namespace Memphis.Client;

public partial class MemphisClient
{
    /// <summary>
    /// Create Consumer for station 
    /// </summary>
    /// <param name="consumerOptions">options used to customize the behaviour of consumer</param>
    /// <returns>An <see cref="MemphisConsumer"/> object connected to the station from consuming data</returns>
    public async Task<MemphisConsumer> CreateConsumer(MemphisConsumerOptions consumerOptions, int timeoutRetry = 5, CancellationToken cancellationToken = default)
    {
        EnsureBatchSizeIsValid(consumerOptions.BatchSize);

        if (_brokerConnection.IsClosed())
        {
            throw MemphisExceptions.DeadConnectionException;
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
                ApplicationId = ApplicationId,
                SdkLang = ".NET"
            };

            var createConsumerModelJson = JsonSerializer.Serialize(createConsumerModel);

            byte[] createConsumerReqBytes = Encoding.UTF8.GetBytes(createConsumerModelJson);

            Msg createConsumerResp = await RequestAsync(MemphisStations.MEMPHIS_CONSUMER_CREATIONS, createConsumerReqBytes, timeoutRetry, cancellationToken);
            var responseStr = Encoding.UTF8.GetString(createConsumerResp.Data);
            var createConsumerResponse = JsonSerializer.Deserialize<CreateConsumerResponse>(responseStr);

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

            if(createConsumerResponse is { PartitionsUpdate: { } partitionsUpdate })
            {
                _stationPartitions.AddOrUpdate(consumerOptions.StationName, partitionsUpdate, (_, _) => partitionsUpdate);
            }

            await ListenForSchemaUpdate(consumerOptions.StationName);

            return consumer;

        }
        catch (MemphisException)
        {
            throw;
        }
        catch (System.Exception e)
        {
            throw MemphisExceptions.FailedToCreateConsumerException(e);
        }
    }

    /// <summary>
    /// Create a new consumer
    /// </summary>
    /// <param name="fetchMessageOptions">Fetch message options</param>
    /// <returns>MemphisConsumer</returns>
    public async Task<MemphisConsumer> CreateConsumer(FetchMessageOptions fetchMessageOptions, int timeoutRetry = 5, CancellationToken cancellationToken = default)
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
        }, timeoutRetry, cancellationToken);
    }

    internal IConsumerContext GetConsumerContext(string streamName, string durableName)
    {
        var streamContext = _brokerConnection.GetStreamContext(streamName);
        var consumerContext = streamContext.GetConsumerContext(durableName);
        return consumerContext;
    }
}