namespace Memphis.Client.Consumer;

internal record FetchOptions
{
    public int BatchSize { get; set; }
    public int MaxAckTimeMs { get; set; }
    public string ConsumerGroup { get; set; }
    public string InternalStationName { get; set; }
    public int BatchMaxTimeToWaitMs { get; set; }
    public MemphisClient MemphisClient { get; set; }

    public FetchOptions(
        int batchSize,
        int maxAckTimeMs,
        string consumerGroup,
        string internalStationName,
        int batchMaxTimeToWaitMs,
        MemphisClient memphisClient
    )
    {
        BatchSize = batchSize;
        MaxAckTimeMs = maxAckTimeMs;
        ConsumerGroup = consumerGroup;
        InternalStationName = internalStationName;
        BatchMaxTimeToWaitMs = batchMaxTimeToWaitMs;
        MemphisClient = memphisClient;
    }

    public FetchOptions(
        MemphisClient memphisClient,
        string internalStationName,
        MemphisConsumerOptions consumerOptions
    )
    {
        BatchSize = consumerOptions.BatchSize;
        MaxAckTimeMs = consumerOptions.MaxAckTimeMs;
        ConsumerGroup = consumerOptions.ConsumerGroup;
        InternalStationName = internalStationName;
        BatchMaxTimeToWaitMs = consumerOptions.BatchMaxTimeToWaitMs;
        MemphisClient = memphisClient;
    
    }
}

internal static class IConsumerContextExtensions
{
    public static List<MemphisMessage> FetchMessages(
        this IConsumerContext consumerContext,
        FetchOptions fetchOptions
    )
    {
        FetchConsumeOptions fetchConsumeOptions = FetchConsumeOptions
            .Builder()
            .WithExpiresIn(fetchOptions.BatchMaxTimeToWaitMs)
            .WithMaxMessages(fetchOptions.BatchSize)
            .Build();

        int receivedMessages = 0;
        var memphisMessages = new List<MemphisMessage>();
        using var fetchConsumer = consumerContext.Fetch(fetchConsumeOptions);
        var message = fetchConsumer.NextMessage();
        while (message is not null)
        {
            memphisMessages.Add(new MemphisMessage(
                message,
                fetchOptions.MemphisClient,
                fetchOptions.ConsumerGroup,
                fetchOptions.MaxAckTimeMs,
                fetchOptions.InternalStationName
            ));
            receivedMessages += 1;
            if (receivedMessages >= fetchOptions.BatchSize)
                break;
            message = fetchConsumer.NextMessage();
        }

        return memphisMessages; 

    }
}