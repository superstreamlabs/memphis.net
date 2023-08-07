using System.Text;
using Memphis.Client.Exception;
using Memphis.Client.Validators;
using ProtoBuf;

namespace Memphis.Client.UnitTests;




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
    private readonly ISchemaValidator _validator;

    public ProtoBufValidatorTests()
    {
        _validator = new ProtoBufValidator();
        _validator.ParseAndStore(
            "samplepbf",
            """
            {
                "version_number": 1, 
                "descriptor": "CmsKEXNhbXBsZXBiZl8xLnByb3RvIk4KBFRlc3QSFgoGZmllbGQxGAEgASgJUgZmaWVsZDESFgoGZmllbGQyGAIgASgJUgZmaWVsZDISFgoGZmllbGQzGAMgASgFUgZmaWVsZDNiBnByb3RvMw==", 
                "schema_content": "syntax = \"proto3\";\nmessage Test {\n    string field1 = 1;\n    string  field2 = 2;\n    int32  field3 = 3;\n}", 
                "message_struct_name": "Test"
            }
            """
            );
    }

    [Fact]
    public async Task GivenValidPayload_WhenValidate_ThenNoError()
    {
        var validData = new Test
        {
            Field1 = "AwesomeFirst",
            Field2 = "SecondField",
            Field3 = 333,
        };
        var message = ConvertToProtoBuf(validData);

        
        var exception = await Record.ExceptionAsync(async () => await _validator.ValidateAsync(message, "samplepbf"));

        Assert.Null(exception);

    }

    [Fact]
    public async Task GivenInvalidPayload_WhenValidate_ThenHasError()
    {
        var message = Convert.FromBase64String("CgJmb3JtYWwxCgRmZWlsZDIKCAk=");

        await Assert.ThrowsAsync<MemphisSchemaValidationException>(
                        () => _validator.ValidateAsync(message, "samplepbf"));
    }



    private static byte[] ConvertToProtoBuf<TData>(TData obj) where TData : class
    {
        using var stream = new MemoryStream();
        ProtoBuf.Serializer.Serialize(stream, obj);
        return stream.ToArray();
    }
}