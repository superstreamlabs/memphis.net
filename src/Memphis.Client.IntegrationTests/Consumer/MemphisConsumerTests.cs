using System.Text;
using Memphis.Client.Consumer;
using Memphis.Client.Producer;

namespace Memphis.Client.IntegrationTests.Consumer;

[Collection(CollectionFixtures.MemphisClient)]
public class MemphisConsumerTests
{
    private readonly MemphisClientFixture _fixture;

    public MemphisConsumerTests(MemphisClientFixture fixture)
    {
        _fixture = fixture;
    }

    [Theory]
    [InlineData("test-station", "test-consumer", default, true)]
    public async Task GivenConsumerOptions_WhenCreateConsumer_ThenConsumerIsCreated(
        string stationName, string consumerName, string consumerGroup, bool generateUniqueSuffix)
    {
        using var client = await MemphisClientFactory.CreateClient(_fixture.MemphisClientOptions);
        await client.CreateStation(stationName);

        var consumerOptions = new MemphisConsumerOptions
        {
            StationName = stationName,
            ConsumerName = consumerName,
            ConsumerGroup = consumerGroup,
            GenerateUniqueSuffix = generateUniqueSuffix
        };

        var result = await client.CreateConsumer(consumerOptions);

        Assert.NotNull(result);
    }

    [Theory]
    [InlineData("test-station", "test-consumer", default, true, "test-producer", "Hello, World!")]
    public async Task GivenConsumerOptions_WhenConsumeAsync_ThenMessageIsConsumed(
        string stationName, string consumerName, string consumerGroup, bool generateUniqueSuffix, string producerName, string message)
    {
        using var client = await MemphisClientFactory.CreateClient(_fixture.MemphisClientOptions);
        await client.CreateStation(stationName);

        var producerOptions = new MemphisProducerOptions
        {
            StationName = stationName,
            ProducerName = producerName,
            GenerateUniqueSuffix = generateUniqueSuffix
        };

        await client.ProduceAsync(producerOptions, message, _fixture.CommonHeaders);

        var consumerOptions = new MemphisConsumerOptions
        {
            StationName = stationName,
            ConsumerName = consumerName,
            ConsumerGroup = consumerGroup,
            GenerateUniqueSuffix = generateUniqueSuffix
        };
        var consumer = await client.CreateConsumer(consumerOptions);
        consumer.MessageReceived += (sender, args) =>
        {
            var firstMessage = args.MessageList.First();
            var decodedMessage = Encoding.UTF8.GetString(firstMessage.GetData());
            Assert.Equal(message, decodedMessage);
            firstMessage.Ack();
        };

        await consumer.ConsumeAsync();
        await Task.Delay((int)TimeSpan.FromSeconds(30).TotalMicroseconds);
        await consumer.DestroyAsync();

        Assert.NotNull(consumer);
    }

    [Theory]
    [InlineData("test-station", "test-consumer", default, true)]
    public async Task GivenConsumerOptions_WhenDestroyAsync_ThenConsumerIsDestroyed(
        string stationName, string consumerName, string consumerGroup, bool generateUniqueSuffix)
    {
        using var client = await MemphisClientFactory.CreateClient(_fixture.MemphisClientOptions);
        await client.CreateStation(stationName);

        var consumerOptions = new MemphisConsumerOptions
        {
            StationName = stationName,
            ConsumerName = consumerName,
            ConsumerGroup = consumerGroup,
            GenerateUniqueSuffix = generateUniqueSuffix
        };
        var consumer = await client.CreateConsumer(consumerOptions);

        await consumer.DestroyAsync();

        Assert.NotNull(consumer);
    }

    [Theory]
    [InlineData("test-station", "test-consumer", default, true, "test-producer", "Hello, World!")]
    public async Task GivenConsumerOptions_WhenFetch_ThenMessageIsRetrieved(
            string stationName, string consumerName, string consumerGroup, bool generateUniqueSuffix, string producerName, string message)
    {
        using var client = await MemphisClientFactory.CreateClient(_fixture.MemphisClientOptions);
        await client.CreateStation(stationName);

        var producerOptions = new MemphisProducerOptions
        {
            StationName = stationName,
            ProducerName = producerName,
            GenerateUniqueSuffix = generateUniqueSuffix
        };

        await client.ProduceAsync(producerOptions, message, _fixture.CommonHeaders);

        var consumerOptions = new MemphisConsumerOptions
        {
            StationName = stationName,
            ConsumerName = consumerName,
            ConsumerGroup = consumerGroup,
            GenerateUniqueSuffix = generateUniqueSuffix
        };
        var consumer = await client.CreateConsumer(consumerOptions);
        var messages = consumer.Fetch(10, false);
        var firstMessage = messages.First();
        var decodedMessage = Encoding.UTF8.GetString(firstMessage.GetData());
        
        Assert.Equal(message, decodedMessage);

        await consumer.ConsumeAsync();
        await Task.Delay((int)TimeSpan.FromSeconds(30).TotalMicroseconds);
        await consumer.DestroyAsync();

        Assert.NotNull(consumer);
    }

}