using System.Collections.Concurrent;
using System.Text;
using GraphQL.Types;
using Memphis.Client.Exception;
using Memphis.Client.UnitTests.Validators.TestData;
using Memphis.Client.Validators;
using Xunit;

namespace Memphis.Client.UnitTests.Validators;

public class AvroValidatorTest
{


    private readonly ISchemaValidator _validator;

    public AvroValidatorTest()
    {
        _validator = new AvroValidator();
    }

    #region AvroValidatorTest.ParseAndStore
    [Theory]
    [MemberData(nameof(AvroValidatorTestData.ValidSchema), MemberType = typeof(AvroValidatorTestData))]
    public void ShouldReturnTrue_WhenParseAndStore_WhereValidSchemaPassed(string validSchema)
    {
        var actual = _validator.ParseAndStore("vaid-schema-001", validSchema);

        Assert.True(actual);
    }


    [Theory]
    [MemberData(nameof(AvroValidatorTestData.InvalidSchema), MemberType = typeof(AvroValidatorTestData))]
    public void ShouldReturnFalse_WhenParseAndStore_WhereInvalidSchemaPassed(string invalidSchema)
    {
        var actual = _validator.ParseAndStore("invalid-schema-001", invalidSchema);

        Assert.False(actual);
    }

    #endregion


    #region AvroValidatorTest.ValidateAsync

    [Theory]
    [MemberData(nameof(AvroValidatorTestData.ValidSchemaDetail), MemberType = typeof(AvroValidatorTestData))]
    public async Task ShouldDoSuccess_WhenValidateAsync_WhereValidDataPassed(string schemaKey, string schema, byte[] msg)
    {
        _validator.ParseAndStore(schemaKey, schema);

        var exception = await Record.ExceptionAsync(async () => await _validator.ValidateAsync(msg, schemaKey));
        
        Assert.Null(exception);
    }

    [Theory]
    [MemberData(nameof(AvroValidatorTestData.InvalidSchemaDetail), MemberType = typeof(AvroValidatorTestData))]
    public async Task ShouldDoThrow_WhenValidateAsync_WhereInvalidDataPassed(string schemaKey, string schema, byte[] msg)
    {
        _validator.ParseAndStore(schemaKey, schema);

        await Assert.ThrowsAsync<MemphisSchemaValidationException>(
             () => _validator.ValidateAsync(msg, schemaKey));
    }

    [Theory]
    [MemberData(nameof(AvroValidatorTestData.Message), MemberType = typeof(AvroValidatorTestData))]
    public async Task ShouldDoThrow_WhenValidateAsync_WhereSchemaNotFoundInCache(byte[] msg)
    {
        var nonexistentSchema = Guid.NewGuid().ToString();

        await Assert.ThrowsAsync<MemphisSchemaValidationException>(
            () => _validator.ValidateAsync(msg, nonexistentSchema));
    }

    #endregion
}
