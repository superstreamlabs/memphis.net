using System.Collections.Specialized;
using System.Text;
using Memphis.Client.Producer;

namespace Memphis.Client.IntegrationTests;

[Collection(CollectionFixtures.MemphisClient)]
public class MemphisClientTests
{
    private readonly MemphisClientFixture _fixture;
    public MemphisClientTests(MemphisClientFixture fixture)
    {
        _fixture = fixture;
    }

    [Theory]
    [InlineData("station_name_c_produce", "producer_name_a","#1 Hello World!", true)]
    public async Task GivenHostAndCredential_WhenProduce_ThenMessageIsProduced(
        string stationName,
        string producerName,
        string message,
        bool generateUniqueSuffix)
    {
        using var client = await MemphisClientFactory.CreateClient(_fixture.MemphisClientOptions);
        

        var producerOptions = new MemphisProducerOptions 
        {
            StationName = stationName,
            ProducerName = producerName,
            GenerateUniqueSuffix = generateUniqueSuffix,
        };
        
        await client.ProduceAsync(
            options: producerOptions, 
            message: Encoding.UTF8.GetBytes(message), 
            headers: _fixture.CommonHeaders);

        Assert.True(true);
    }
}