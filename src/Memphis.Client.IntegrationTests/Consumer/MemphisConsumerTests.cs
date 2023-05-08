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
    [InlineData("consumer_tst_station_a", "consumer_tst_consumer_a", default, true)]
    public async Task GivenConsumerOptions_WhenCreateConsumer_ThenConsumerIsCreated(
        string stationName, string consumerName, string consumerGroup, bool generateUniqueSuffix)
    {
        using var client = await MemphisClientFactory.CreateClient(_fixture.MemphisClientOptions);
        var station = await client.CreateStation(stationName);

        var consumerOptions = new MemphisConsumerOptions
        {
            StationName = stationName,
            ConsumerName = consumerName,
            ConsumerGroup = consumerGroup,
            GenerateUniqueSuffix = generateUniqueSuffix
        };

        var consumer = await client.CreateConsumer(consumerOptions);
        
        await consumer.DestroyAsync();
        await station.DestroyAsync();

        Assert.NotNull(station);
        Assert.NotNull(consumer);
    }

    [Theory]
    [InlineData("consumer_tst_station_b", "consumer_tst_consumer_b", default, true, "consumer_tst_producer_b", "Hello, World!")]
    public async Task GivenConsumerOptions_WhenConsumeAsync_ThenMessageIsConsumed(
        string stationName, string consumerName, string consumerGroup, bool generateUniqueSuffix, string producerName, string message)
    {
        using var client = await MemphisClientFactory.CreateClient(_fixture.MemphisClientOptions);
        var station = await client.CreateStation(stationName);

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

        consumer.ConsumeAsync();
        await Task.Delay((int)TimeSpan.FromSeconds(10).TotalMilliseconds);
        
        await consumer.DestroyAsync();
        await station.DestroyAsync();
        Assert.NotNull(consumer);
    }

    [Theory]
    [InlineData("consumer_tst_station_c", "consumer_tst_consumer_c", default, true)]
    public async Task GivenConsumerOptions_WhenDestroyAsync_ThenConsumerIsDestroyed(
        string stationName, string consumerName, string consumerGroup, bool generateUniqueSuffix)
    {
        using var client = await MemphisClientFactory.CreateClient(_fixture.MemphisClientOptions);
        var station = await client.CreateStation(stationName);

        var consumerOptions = new MemphisConsumerOptions
        {
            StationName = stationName,
            ConsumerName = consumerName,
            ConsumerGroup = consumerGroup,
            GenerateUniqueSuffix = generateUniqueSuffix
        };
        var consumer = await client.CreateConsumer(consumerOptions);

        await consumer.DestroyAsync();
        await station.DestroyAsync();

        Assert.NotNull(consumer);
    }

    [Theory]
    [InlineData("consumer_tst_station_d", "consumer_tst_consumer_d", default, true, "consumer_tst_producer_d", "Hello, World!")]
    public async Task GivenConsumerOptions_WhenFetch_ThenMessageIsRetrieved(
            string stationName, string consumerName, string consumerGroup, bool generateUniqueSuffix, string producerName, string message)
    {
        using var client = await MemphisClientFactory.CreateClient(_fixture.MemphisClientOptions);
        var station = await client.CreateStation(stationName);

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

        await consumer.DestroyAsync();
        await station.DestroyAsync();
        Assert.NotNull(consumer);
    }

}