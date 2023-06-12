namespace ProtoEval.Tests;

public class ProtoBufValidatorTests
{
    // [Theory]
    // [InlineData(
    //     "c3lzdGVtID0gInByb3RvMyI7CgpkYXRhYmFzZSBleGFtcGxlOwpfbWFpbk1lc3NhZ2UgUGVyc29uIHsKICBuYW1lID0gMTsKICBpbnQzMiBhZ2UgPSAyOwoKICBzdHJpbmcgZW1haWwgPSAzOwp9Cg==",
    //     """
    //     {
    //         "version_number": 1,
    //         "descriptor": "\nf\n\u000cprt2_1.proto\"N\n\u0004Test\u0012\u0016\n\u0006field1\u0018\u0001 \u0001(\tR\u0006field1\u0012\u0016\n\u0006field2\u0018\u0002 \u0001(\tR\u0006field2\u0012\u0016\n\u0006field3\u0018\u0003 \u0001(\u0005R\u0006field3b\u0006proto3",
    //         "schema_content": "syntax = \"proto3\";\nmessage Test {\n    string field1 = 1;\n    string  field2 = 2;\n    int32  field3 = 3;\n}",
    //         "message_struct_name": "Test"
    //     }
    //     """,
    //     "prt2"
    // )]
    // public async Task GivenValidPayload_WhenValidate_ThenNoError(string base64Data, string activeSchemaVersion, string schemaName)
    // {
    //     var result = await ProtoBufValidator.Validate(base64Data, activeSchemaVersion, schemaName);


    //     Assert.False(result.HasError);
    //     Assert.Null(result.Error);
    // }

    [Theory]
    [InlineData(
        "c3lzdGVtID0gInByb3RvMyI7CgpkYXRhYmFzZSBleGFtcGxlOwpfbWFpbk1lc3NhZ2UgUGVyc29uIHsKICBuYW1lID0gMTsKICBpbnQzMiBhZ2UgPSAyOwoKICBzdHJpbmcgZW1haWwgPSAzOwp9Cg==",
        """
        {
            "version_number": 1,
            "descriptor": "\nf\n\u000cprt2_1.proto\"N\n\u0004Test\u0012\u0016\n\u0006field1\u0018\u0001 \u0001(\tR\u0006field1\u0012\u0016\n\u0006field2\u0018\u0002 \u0001(\tR\u0006field2\u0012\u0016\n\u0006field3\u0018\u0003 \u0001(\u0005R\u0006field3b\u0006proto3",
            "schema_content": "syntax = \"proto3\";\nmessage Test {\n    string field1 = 1;\n    string  field2 = 2;\n    int32  field3 = 3;\n}",
            "message_struct_name": "Test"
        }
        """,
        "prt2"
    )]
    public async Task GivenInvalidPayload_WhenValidate_ThenHasError(string base64Data, string activeSchemaVersion, string schemaName)
    {
        var result  = await ProtoBufValidator.Validate(base64Data, activeSchemaVersion, schemaName);


        Assert.True(result.HasError);
        Assert.NotNull(result.Error);
    }
}