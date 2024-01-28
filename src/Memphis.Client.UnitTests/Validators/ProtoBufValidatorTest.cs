using Memphis.Client.Constants;
using Memphis.Client.Exception;
using Memphis.Client.Models.Response;
using Memphis.Client.Validators;
using ProtoBuf;

namespace Memphis.Client.UnitTests;

[ProtoContract]
file class ValidModel
{

    [ProtoMember(1, Name = @"field1")]
    public string Field1 { get; set; } = "";

    [ProtoMember(2, Name = @"field2")]
    public string Field2 { get; set; } = "";

    [ProtoMember(3, Name = @"field3")]
    public int Field3 { get; set; }
}


[ProtoContract]
public class InvalidModel
{
    [ProtoMember(1, Name = @"field1")]
    public string Field1 { get; set; } = "";

    [ProtoMember(2, Name = @"field2")]
    public string Field2 { get; set; } = "";
}

public class ProtoBufValidatorTests
{
    private readonly ISchemaValidator _validator;

    public ProtoBufValidatorTests()
    {
        _validator = new ProtoBufValidator();
        _validator.AddOrUpdateSchema(TestSchema());
    }

    [Fact]
    public async Task GivenValidPayload_WhenValidate_ThenNoError()
    {
        var validData = new ValidModel
        {
            Field1 = "AwesomeFirst",
            Field2 = "SecondField",
            Field3 = 333,
        };
        var message = ConvertToProtoBuf(validData);


        var exception = await Record.ExceptionAsync(async () => await _validator.ValidateAsync(message, "testschema"));

        Assert.Null(exception);
    }

    [Fact]
    public async Task GivenInvalidPayload_WhenValidate_ThenHasError()
    {
        var invalidData = new InvalidModel
        {
            Field1 = "AwesomeFirst",
            Field2 = "SecondField"
        };
        var message = ConvertToProtoBuf(invalidData);

        var exception = await Record.ExceptionAsync(async () => await _validator.ValidateAsync(message, "testschema"));

        Assert.NotNull(exception);
        Assert.IsType<MemphisSchemaValidationException>(exception);
    }

    private static byte[] ConvertToProtoBuf<TData>(TData obj) where TData : class
    {
        using var stream = new MemoryStream();
        Serializer.Serialize(stream, obj);
        return stream.ToArray();
    }

    private static SchemaUpdateInit TestSchema()
    {
        var schemaUpdate = new SchemaUpdateInit
        {
            SchemaName = "testschema",
            ActiveVersion = new ProducerSchemaUpdateVersion
            {
                VersionNumber = "1",
                Descriptor = "CmQKEnRlc3RzY2hlbWFfMS5wcm90byJOCgRUZXN0EhYKBmZpZWxkMRgBIAIoCVIGZmllbGQxEhYKBmZpZWxkMhgCIAIoCVIGZmllbGQyEhYKBmZpZWxkMxgDIAIoBVIGZmllbGQz",
                Content = """
                syntax = "proto2";
                message Test {
                            required string field1 = 1;
                            required string field2 = 2;
                            required int32 field3 = 3;
                }
                """,
                MessageStructName = "Test"
            },
            SchemaType = MemphisSchemaTypes.PROTO_BUF
        };
        return schemaUpdate;
    }
}