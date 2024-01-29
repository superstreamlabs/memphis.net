using System.Text;
using Memphis.Client.Constants;
using Memphis.Client.Exception;
using Memphis.Client.Models.Response;
using Memphis.Client.Validators;
using Newtonsoft.Json;
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

    [Fact]
    public async Task GivenInvalidJson_WhenValidate_ThenHasError()
    {
        var invalidJson64 = Json64(new Dictionary<string, object>
        {
            ["field1"] = "AwesomeFirst",
            ["field2"] = "SecondField",
            ["field3"] = "WrongData",
        });

        var activeSchemaVersionBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(ActiveVersionProto3()));

        var result = await ProtoBufEval.ProtoBufValidator.ValidateJson(invalidJson64, activeSchemaVersionBase64, "testschema");
        
        Assert.True(result.HasError);
    }

    [Fact]
    public async Task GivenValidJson_WhenValidate3_ThenHasNoError()
    {
        var validJson64 = Json64(new Dictionary<string, object>
        {
            ["field1"] = "AwesomeFirst",
            ["field2"] = "SecondField",
            ["field3"] = 333,
        });
        var activeSchemaVersionBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(ActiveVersionProto3()));

        var result = await ProtoBufEval.ProtoBufValidator.ValidateJson(validJson64, activeSchemaVersionBase64, "testschema");
        
        Assert.False(result.HasError);
    }

    private static byte[] ConvertToProtoBuf<TData>(TData obj) where TData : class
    {
        using var stream = new MemoryStream();
        Serializer.Serialize(stream, obj);
        return stream.ToArray();
    }

    private static string Json64<TData>(TData obj) where TData : class
    {
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(obj)));
    }

     private static string ActiveVersionProto3()
    {
        return JsonConvert.SerializeObject(new
        {
            version_number = 1,
            descriptor = "CmwKEnRlc3RzY2hlbWFfMS5wcm90byJOCgRUZXN0EhYKBmZpZWxkMRgBIAEoCVIGZmllbGQxEhYKBmZpZWxkMhgCIAEoCVIGZmllbGQyEhYKBmZpZWxkMxgDIAEoBVIGZmllbGQzYgZwcm90bzM=",
            schema_content = "syntax = \"proto3\";\nmessage Test {\n    string field1 = 1;\n    string  field2 = 2;\n    int32  field3 = 3;\n}",
            message_struct_name = "Test"
        });
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