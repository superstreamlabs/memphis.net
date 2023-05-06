using Memphis.Client.IntegrationTests.Fixtures;

namespace Memphis.Client.IntegrationTests;

[Collection(CollectionFixtures.MemphisClient)]
public class MemphisClientFactoryTests
{
    private readonly MemphisClientFixture _fixture;
    public MemphisClientFactoryTests(MemphisClientFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task GivenHostAndCredential_WhenCreateClient_ThenMemphisClientIsCreated()
    {
        using var client = await MemphisClientFactory.CreateClient(_fixture.MemphisClientOptions);
        
        Assert.NotNull(client);
    }
}