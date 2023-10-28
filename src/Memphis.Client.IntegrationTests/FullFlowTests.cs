using System.Collections.Specialized;
using System.Text;
using Memphis.Client.Consumer;
using Memphis.Client.Producer;
using Memphis.Client.Station;

namespace Memphis.Client.IntegrationTests;


[Collection(CollectionFixtures.MemphisClient)]
public class FullFlowTests
{
    private readonly MemphisClientFixture _fixture;
    public FullFlowTests(MemphisClientFixture fixture)
    {
        _fixture = fixture;
    }

    [Theory]
    [InlineData("docker", 100)]
    public async Task CreateStation_Produce_Consume_Destroy(
        string platform,
        int totalMessages)
    {
        using var client = await MemphisClientFactory.CreateClient(_fixture.MemphisClientOptions);

        var station = await client.CreateStation(Config.GetStationOptions(platform));

        var producer = await client.CreateProducer(Config.GetProducerOptions(platform));

        await producer.ShouldProduceMessages(totalMessages);

        var consumer1 = await client.CreateConsumer(Config.GetConsumerOptions(platform, 1));
        var consumer2 = await client.CreateConsumer(Config.GetConsumerOptions(platform, 2));
        var consumer3 = await client.CreateConsumer(Config.GetConsumerOptions(platform, 3, "dotnet.consumers.three.four"));
        var consumer4 = await client.CreateConsumer(Config.GetConsumerOptions(platform, 4, "dotnet.consumers.three.four"));


        int consumer1Count = await consumer1.CountConsumedMessages();
        int consumer2Count = await consumer2.CountConsumedMessages();

        Assert.Equal(totalMessages, consumer1Count);
        Assert.Equal(totalMessages, consumer2Count);

        int consumer34Count = await Extensions.CountMessagesConsumedByGroup(consumer3, consumer4);

        Assert.Equal(totalMessages, consumer34Count);

        await consumer1.DestroyAsync();
        await consumer2.DestroyAsync();
        await consumer3.DestroyAsync();
        await consumer4.DestroyAsync();

        await producer.DestroyAsync();

        await station.DestroyAsync();
    }

    [Theory]
    [Trait("CI", "Skip")]
    [InlineData("docker", "test_partition_key", 100)]
    public async Task CreateStation_ProduceAndConsumeWithPartitionKey_ThenDestroy(
        string platform,
        string partitionKey,
        int totalMessages)
    {
        using var client = await MemphisClientFactory.CreateClient(_fixture.MemphisClientOptions);

        var station = await client.CreateStation(Config.GetStationOptions(platform));

        var producer = await client.CreateProducer(Config.GetProducerOptions(platform));

        await producer.ShouldProduceMessages(totalMessages, partitionKey);

        var consumer1 = await client.CreateConsumer(Config.GetConsumerOptions(platform, 1));
        var consumer2 = await client.CreateConsumer(Config.GetConsumerOptions(platform, 2));
        var consumer3 = await client.CreateConsumer(Config.GetConsumerOptions(platform, 3, "dotnet.consumers.three.four"));
        var consumer4 = await client.CreateConsumer(Config.GetConsumerOptions(platform, 4, "dotnet.consumers.three.four"));


        int consumer1Count = await consumer1.CountConsumedMessages(partitionKey);
        int consumer2Count = await consumer2.CountConsumedMessages(partitionKey);

        Assert.Equal(totalMessages, consumer1Count);
        Assert.Equal(totalMessages, consumer2Count);

        int consumer34Count = await Extensions.CountMessagesConsumedByGroup(consumer3, consumer4, partitionKey);

        Assert.Equal(totalMessages, consumer34Count);

        await consumer1.DestroyAsync();
        await consumer2.DestroyAsync();
        await consumer3.DestroyAsync();
        await consumer4.DestroyAsync();

        await producer.DestroyAsync();

        await station.DestroyAsync();
    }
}


file static class Config
{
    private const string sdk = "dotnet";

    public static readonly NameValueCollection Headers = new()
    {
        { "key-1", "value-1" }
    };

    public static StationOptions GetStationOptions(string platform)
    {
        return new StationOptions
        {
            Name = GetStationName(platform),
            RetentionType = RetentionTypes.MAX_MESSAGE_AGE_SECONDS,
            RetentionValue = 86_400,
            StorageType = StorageTypes.DISK,
            Replicas = 1,
            IdempotenceWindowMs = 0,
            SendPoisonMessageToDls = true,
            SendSchemaFailedMessageToDls = true,
        };
    }

    public static MemphisProducerOptions GetProducerOptions(string platform, bool generateUniqueSuffix = false)
    {
        return new MemphisProducerOptions
        {
            StationName = GetStationName(platform),
            ProducerName = GetProducerName(platform),
            GenerateUniqueSuffix = generateUniqueSuffix
        };
    }

    public static MemphisConsumerOptions GetConsumerOptions(string platform, int index, string? consumerGroup = default, bool generateUniqueSuffix = false, int batchSize = 100)
    {
        return new MemphisConsumerOptions
        {
            StationName = GetStationName(platform),
            ConsumerName = GetConsumerName(platform, index),
            ConsumerGroup = consumerGroup,
            GenerateUniqueSuffix = generateUniqueSuffix,
            BatchSize = batchSize
        };
    }

    public static string GetStationName(string platform)
        => $"{platform}_{sdk}_station";

    public static string GetProducerName(string platform)
        => $"{platform}_{sdk}_producer";

    public static string GetConsumerName(string platform, int index)
        => $"{platform}_{sdk}_consumer_{index}";

}

file static class Extensions
{
    public static async Task<int> CountConsumedMessages(this MemphisConsumer consumer, string partitionKey = null)
    {
        int count = 0;
        consumer.MessageReceived += (sender, args) =>
        {
            if (args is { MessageList.Count: > 0 })
            {
                count += args.MessageList.Count;
                args.MessageList.ForEach(msg => msg.Ack());
            }
        };

        _ = consumer.ConsumeAsync(new ConsumeOptions { PartitionKey = partitionKey });
        await Task.Delay(TimeSpan.FromSeconds(10));
        return count;
    }


    public static async Task ShouldProduceMessages(this MemphisProducer producer, int totalMessages, string partitionKey = null)
    {
        for (int i = 0; i < totalMessages; i++)
        {
            await producer.ProduceAsync($"Message {i + 1}: Hello World", Config.Headers, partitionKey: partitionKey);
        }
    }

    public static async Task<int> CountMessagesConsumedByGroup(MemphisConsumer consumer1, MemphisConsumer consumer2, string partitionKey = null)
    {
        int count = 0;
        consumer1.MessageReceived += (sender, args) =>
        {
            if (args is { MessageList.Count: > 0 })
                count += args.MessageList.Count;
            args.MessageList.ForEach(msg => msg.Ack());
        };
        consumer2.MessageReceived += (sender, args) =>
        {
            if (args is { MessageList.Count: > 0 })
                count += args.MessageList.Count;
            args.MessageList.ForEach(msg => msg.Ack());
        };

        _ = consumer1.ConsumeAsync(new ConsumeOptions { PartitionKey = partitionKey });
        _ = consumer2.ConsumeAsync(new ConsumeOptions { PartitionKey = partitionKey });
        await Task.Delay(TimeSpan.FromSeconds(10));
        return count;
    }
}
