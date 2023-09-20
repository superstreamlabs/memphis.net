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
    [InlineData("station_tst_station_name_a")]
    [InlineData("station_tst_station_name_b")]
    public async Task GivenStationOptions_WhenCreateStation_ThenStationIsCreated(string stationName)
    {
        using var client = await MemphisClientFactory.CreateClient(_fixture.MemphisClientOptions);
        var stationOptions = _fixture.DefaultStationOptions;
        stationOptions.Name = stationName;

        var result = await client.CreateStation(stationOptions);

        await result.DestroyAsync();
        Assert.NotNull(result);
    }

    [Theory]
    [InlineData("station_with_dls_a", "dls_station_a")]
    [InlineData("station_with_dls_b", "dls_station_b")]
    public async Task GivenStationOptionsWithDls_WhenCreateStation_ThenStationWithDlsIsCreated(string stationName, string dlsStationName)
    {
        using var client = await MemphisClientFactory.CreateClient(_fixture.MemphisClientOptions);
        var stationOptions = _fixture.DefaultStationOptions;
        stationOptions.Name = stationName;
        stationOptions.DlsStation = dlsStationName;

        var result = await client.CreateStation(stationOptions);

        await result.DestroyAsync();
        Assert.NotNull(result);
    }
    

    [Theory]
    [InlineData("station_tst_station_name_x")]
    [InlineData("station_tst_station_name_y")]
    public async Task GivenStationName_WhenCreateStation_ThenStationIsCreated(string stationName)
    {
        using var client = await MemphisClientFactory.CreateClient(_fixture.MemphisClientOptions);

        var result = await client.CreateStation(stationName);

        await result.DestroyAsync();
        Assert.NotNull(result);
    }
}