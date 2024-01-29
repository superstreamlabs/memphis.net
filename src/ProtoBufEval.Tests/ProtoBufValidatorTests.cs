using Newtonsoft.Json;
using ProtoBuf;
using System.Text;

namespace ProtoBufEval.Tests;



[ProtoContract]
public class Test
{

    [ProtoMember(1, Name = @"field1")]
    [global::System.ComponentModel.DefaultValue("")]
    public string Field1 { get; set; } = "";

    [ProtoMember(2, Name = @"field2")]
    [System.ComponentModel.DefaultValue("")]
    public string Field2 { get; set; } = "";

    [ProtoMember(3, Name = @"field3")]
    public int Field3 { get; set; }

}


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
    [ProtoMember(3, Name = @"field3")]
    public string Field3 { get; set; } = "";

}


public class ProtoBufValidatorTests
{
    [Theory]
    [InlineData(
        """
        {
            "version_number": 1, 
            "descriptor": "CmsKEXNhbXBsZXBiZl8xLnByb3RvIk4KBFRlc3QSFgoGZmllbGQxGAEgASgJUgZmaWVsZDESFgoGZmllbGQyGAIgASgJUgZmaWVsZDISFgoGZmllbGQzGAMgASgFUgZmaWVsZDNiBnByb3RvMw==", 
            "schema_content": "syntax = \"proto3\";\nmessage Test {\n    string field1 = 1;\n    string  field2 = 2;\n    int32  field3 = 3;\n}", 
            "message_struct_name": "Test"
        }
        """,
        "samplepbf"
    )]
    public async Task GivenValidPayload_WhenValidate_ThenNoError(string activeSchemaVersion, string schemaName)
    {
        var validData = new Test
        {
            Field1 = "AwesomeFirst",
            Field2 = "SecondField",
            Field3 = 333,
        };
        var base64ValidData = Proto64(validData);
        var activeSchemaVersionBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(activeSchemaVersion));

        var result = await ProtoBufValidator.Validate(base64ValidData, activeSchemaVersionBase64, schemaName);

        Assert.False(result.HasError);
        Assert.Null(result.Error);
    }

    [Theory]
    [InlineData(
        """
        {
            "version_number": 1, 
            "descriptor": "CmsKEXNhbXBsZXBiZl8xLnByb3RvIk4KBFRlc3QSFgoGZmllbGQxGAEgASgJUgZmaWVsZDESFgoGZmllbGQyGAIgASgJUgZmaWVsZDISFgoGZmllbGQzGAMgASgFUgZmaWVsZDNiBnByb3RvMw==", 
            "schema_content": "syntax = \"proto3\";\nmessage Test {\n    string field1 = 1;\n    string  field2 = 2;\n    int32  field3 = 3;\n}", 
            "message_struct_name": "Test"
        }
        """,
        "samplepbf"
    )]
    public async Task GivenInvalidPayload_WhenValidate_ThenHasError(string activeSchemaVersion, string schemaName)
    {
        var base64InvalidData = Convert.ToBase64String(Encoding.UTF8.GetBytes("CgJmb3JtYWwxCgRmZWlsZDIKCAk="));
        var activeSchemaVersionBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(activeSchemaVersion));

        var result = await ProtoBufValidator.Validate(base64InvalidData, activeSchemaVersionBase64, schemaName);


        Assert.True(result.HasError);
        Assert.NotNull(result.Error);
    }


    [Fact]
    public async Task GivenInvalidData_WhenValidate_ThenHasError()
    {
        var base64InvalidData = Proto64(new InvalidModel
        {
            Field1 = "AwesomeFirst",
            Field2 = "SecondField"
        });
        var activeSchemaVersionBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(ActiveVersion()));

        var result = await ProtoBufValidator.Validate(base64InvalidData, activeSchemaVersionBase64, "testschema");

        Assert.True(result.HasError);
    }

    [Fact]
    public async Task GivenValidData_WhenValidate_ThenHasError()
    {
        var base64InvalidData = Proto64(new ValidModel
        {
            Field1 = "AwesomeFirst",
            Field2 = "SecondField",
            Field3 = 333,
        });
        var activeSchemaVersionBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(ActiveVersion()));

        var result = await ProtoBufValidator.Validate(base64InvalidData, activeSchemaVersionBase64, "testschema");

        Assert.False(result.HasError);
    }


    [Fact]
    public async Task GivenInvalidData_WhenValidate3_ThenHasError()
    {
        var base64InvalidData = Json64(new InvalidModel
        {
            Field1 = "AwesomeFirst",
            Field2 = "SecondField",
            Field3 = "WrongData",
        });

        var activeSchemaVersionBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(ActiveVersion3()));

        var result = await ProtoBufValidator.ValidateJson(base64InvalidData, activeSchemaVersionBase64, "testschema");

        Assert.True(result.HasError);
    }

    [Fact]
    public async Task GivenValidData_WhenValidate3_ThenHasNoError()
    {
        var base64InvalidData = Json64(new ValidModel
        {
            Field1 = "AwesomeFirst",
            Field2 = "SecondField",
            Field3 = 333,
        });
        var activeSchemaVersionBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(ActiveVersion3()));

        var result = await ProtoBufValidator.ValidateJson(base64InvalidData, activeSchemaVersionBase64, "testschema");

        Assert.False(result.HasError);
    }

    private static string Proto64<TData>(TData obj) where TData : class
    {
        using var stream = new MemoryStream();
        Serializer.Serialize(stream, obj);
        return Convert.ToBase64String(stream.ToArray());
    }

    private static string Json64<TData>(TData obj) where TData : class
    {
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(obj)));
    }

    private static string ActiveVersion()
    {
        return JsonConvert.SerializeObject(new
        {
            version_number = 1,
            descriptor = "CmQKEnRlc3RzY2hlbWFfMS5wcm90byJOCgRUZXN0EhYKBmZpZWxkMRgBIAIoCVIGZmllbGQxEhYKBmZpZWxkMhgCIAIoCVIGZmllbGQyEhYKBmZpZWxkMxgDIAIoBVIGZmllbGQz",
            schema_content = "syntax = \"proto2\";\nmessage Test {\n            required string field1 = 1;\n            required string field2 = 2;\n            required int32 field3 = 3;\n}",
            message_struct_name = "Test"
        });
    }

    private static string ActiveVersion3()
    {
        return JsonConvert.SerializeObject(new
        {
            version_number = 1,
            descriptor = "CmwKEnRlc3RzY2hlbWFfMS5wcm90byJOCgRUZXN0EhYKBmZpZWxkMRgBIAEoCVIGZmllbGQxEhYKBmZpZWxkMhgCIAEoCVIGZmllbGQyEhYKBmZpZWxkMxgDIAEoBVIGZmllbGQzYgZwcm90bzM=",
            schema_content = "syntax = \"proto3\";\nmessage Test {\n    string field1 = 1;\n    string  field2 = 2;\n    int32  field3 = 3;\n}",
            message_struct_name = "Test"
        });
    }

}