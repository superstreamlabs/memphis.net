using Memphis.Client.Producer;

namespace Memphis.Client.IntegrationTests.Producer;

[Collection(CollectionFixtures.MemphisClient)]
public class MemphisProducerTests
{
    private readonly MemphisClientFixture _fixture;
    public MemphisProducerTests(MemphisClientFixture fixture)
    {
        _fixture = fixture;
    }

    [Theory]
    [InlineData("producer_tst_station_a", "producer_tst_producer_a", true)]
    public async Task GivenProducerOptions_WhenCreateProducer_ThenProducerIsCreated(
        string stationName, string producerName, bool generateUniqueSuffix)
    {
        using var client = await MemphisClientFactory.CreateClient(_fixture.MemphisClientOptions);
        var producerOptions = new MemphisProducerOptions
        {
            StationName = stationName,
            ProducerName = producerName,
            GenerateUniqueSuffix = generateUniqueSuffix
        };

        var result = await client.CreateProducer(producerOptions);

        Assert.NotNull(result);
    }

    [Theory]
    [InlineData("producer_tst_station_b", "producer_tst_producer_b", true, "Hello, World!")]
    public async Task GivenProducerOptions_WhenProduceAsync_ThenMessageIsProduced(
        string stationName, string producerName, bool generateUniqueSuffix, string message)
    {
        using var client = await MemphisClientFactory.CreateClient(_fixture.MemphisClientOptions);
        await client.CreateStation(stationName);

        var producerOptions = new MemphisProducerOptions
        {
            StationName = stationName,
            ProducerName = producerName,
            GenerateUniqueSuffix = generateUniqueSuffix
        };
        var producer = await client.CreateProducer(producerOptions);

        await producer.ProduceAsync(message, _fixture.CommonHeaders);
        await producer.DestroyAsync();

        Assert.NotNull(producer);
    }

    [Theory]
    [InlineData("producer_tst_station_destroy_c", "producer_tst_producer_destroy_c", true)]
    public async Task GivenProducerOptions_WhenDestroyAsync_ThenProducerIsDestroyed(
        string stationName, string producerName, bool generateUniqueSuffix)
    {
        using var client = await MemphisClientFactory.CreateClient(_fixture.MemphisClientOptions);
        await client.CreateStation(stationName);

        var producerOptions = new MemphisProducerOptions
        {
            StationName = stationName,
            ProducerName = producerName,
            GenerateUniqueSuffix = generateUniqueSuffix
        };
        var producer = await client.CreateProducer(producerOptions);

        await producer.DestroyAsync();

        Assert.NotNull(producer);
    }

    
}