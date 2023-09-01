namespace Memphis.Client.IntegrationTests.Schema;

[Collection(CollectionFixtures.MemphisClient)]
public class MemphisSchemaTests
{
    private readonly MemphisClientFixture _fixture;
    public MemphisSchemaTests(MemphisClientFixture fixture)
    {
        _fixture = fixture;
    }

    [Theory]
    [Trait("CI","Skip")]
    [InlineData("test_schema_person", "json", "TestFiles/Schema/JSON/person.json")]
    public async Task GivenSchemaOptions_WhenCreateSchema_ThenSchemaIsCreated(
        string schemaName, string schemaType, string schemaFilePath)
    {
        using var client = await MemphisClientFactory.CreateClient(_fixture.MemphisClientOptions);
        var exception = await Record.ExceptionAsync(() => client.CreateSchema(schemaName, schemaType, schemaFilePath));

        Assert.Null(exception);
    }
}
