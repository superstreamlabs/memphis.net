namespace Memphis.Client.Consumer;

internal record FetchOptions
{
    public int BatchSize { get; set; }
    public int MaxAckTimeMs { get; set; }
    public string ConsumerGroup { get; set; }
    public string InternalStationName { get; set; }
    public int BatchMaxTimeToWaitMs { get; set; }
    public MemphisClient MemphisClient { get; set; }
    public int PartitionNumber { get; set; }

    public FetchOptions(
        MemphisClient memphisClient,
        string internalStationName,
        MemphisConsumerOptions consumerOptions,
        int partitionNumber
    )
    {
        BatchSize = consumerOptions.BatchSize;
        MaxAckTimeMs = consumerOptions.MaxAckTimeMs;
        ConsumerGroup = consumerOptions.ConsumerGroup;
        InternalStationName = internalStationName;
        BatchMaxTimeToWaitMs = consumerOptions.BatchMaxTimeToWaitMs;
        MemphisClient = memphisClient;
        PartitionNumber = partitionNumber;
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
                fetchOptions.InternalStationName,
                fetchOptions.PartitionNumber
            ));
            receivedMessages += 1;
            if (receivedMessages >= fetchOptions.BatchSize)
                break;
            message = fetchConsumer.NextMessage();
        }

        return memphisMessages; 

    }
}