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
public class InvalidTestModel
{

    [ProtoMember(1, Name = @"field1")]
    [global::System.ComponentModel.DefaultValue("")]
    public string Field1 { get; set; } = "";

    [ProtoMember(3, Name = @"field3")]
    public int Field3 { get; set; }

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
        var base64ValidData = ConvertProtoBufToBase64(validData);
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



    private static string ConvertProtoBufToBase64<TData>(TData obj) where TData : class
    {
        using var stream = new MemoryStream();
        ProtoBuf.Serializer.Serialize(stream, obj);
        return Convert.ToBase64String(stream.ToArray());
    }
}