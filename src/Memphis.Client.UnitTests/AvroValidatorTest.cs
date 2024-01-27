using Memphis.Client.Constants;
using Memphis.Client.Exception;
using Memphis.Client.Validators;

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
        var schemaUpdate = ValidatorTestHelper.GetSchemaUpdateInit(
            "valid-schema-001",
            validSchema,
            MemphisSchemaTypes.AVRO
        );

        var actual = _validator.AddOrUpdateSchema(schemaUpdate);

        Assert.True(actual);
    }


    [Theory]
    [MemberData(nameof(AvroValidatorTestData.InvalidSchema), MemberType = typeof(AvroValidatorTestData))]
    public void ShouldReturnFalse_WhenParseAndStore_WhereInvalidSchemaPassed(string invalidSchema)
    {
        var schemaUpdate = ValidatorTestHelper.GetSchemaUpdateInit(
            "invalid-schema-001",
            invalidSchema,
            MemphisSchemaTypes.AVRO
        );

        var actual = _validator.AddOrUpdateSchema(schemaUpdate);

        Assert.False(actual);
    }

    #endregion


    #region AvroValidatorTest.ValidateAsync

    [Theory]
    [MemberData(nameof(AvroValidatorTestData.ValidSchemaDetail), MemberType = typeof(AvroValidatorTestData))]
    public async Task ShouldDoSuccess_WhenValidateAsync_WhereValidDataPassed(string schemaKey, string schema, byte[] msg)
    {
        var schemaUpdate = ValidatorTestHelper.GetSchemaUpdateInit(
            schemaKey,
            schema,
            MemphisSchemaTypes.AVRO
        );

        _validator.AddOrUpdateSchema(schemaUpdate);

        var exception = await Record.ExceptionAsync(async () => await _validator.ValidateAsync(msg, schemaKey));

        Assert.Null(exception);
    }

    [Theory]
    [MemberData(nameof(AvroValidatorTestData.InvalidSchemaDetail), MemberType = typeof(AvroValidatorTestData))]
    public async Task ShouldDoThrow_WhenValidateAsync_WhereInvalidDataPassed(string schemaKey, string schema, byte[] msg)
    {
        var schemaUpdate = ValidatorTestHelper.GetSchemaUpdateInit(
            schemaKey,
            schema,
            MemphisSchemaTypes.AVRO
        );

        _validator.AddOrUpdateSchema(schemaUpdate);

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
