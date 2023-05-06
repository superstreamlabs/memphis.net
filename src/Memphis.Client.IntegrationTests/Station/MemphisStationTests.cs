namespace Memphis.Client.IntegrationTests.Station;

[Collection(CollectionFixtures.MemphisClient)]
public class MemphisStationTests
{
    private readonly MemphisClientFixture _fixture;
    public MemphisStationTests(MemphisClientFixture fixture)
    {
        _fixture = fixture;
    }

    [Theory]
    [InlineData("station_name_a")]
    [InlineData("station_name_b")]
    public async Task GivenStationOptions_WhenCreateStation_ThenStationIsCreated(string stationName)
    {
        using var client = await MemphisClientFactory.CreateClient(_fixture.MemphisClientOptions);
        var stationOptions = _fixture.DefaultStationOptions; 
        stationOptions.Name = stationName;

        var result = await client.CreateStation(stationOptions);
        
        Assert.NotNull(result);
    }

    [Theory]
    [InlineData("station_name_x")]
    [InlineData("station_name_y")]
    public async Task GivenStationName_WhenCreateStation_ThenStationIsCreated(string stationName)
    {
        using var client = await MemphisClientFactory.CreateClient(_fixture.MemphisClientOptions);

        var result = await client.CreateStation(stationName);
        
        Assert.NotNull(result);
    }
}