using CliWrap;
using CliWrap.Buffered;

namespace ProtoBufEval;

/// <summary>
/// Result of a validation
/// </summary>
/// <param name="Error"></param>
/// <param name="HasError"></param>
public class ProtoBufValidationResult
{
    public ProtoBufValidationResult(string? error, bool hasError)
    {
        Error = error;
        HasError = hasError;
    }

    public string? Error { get; }
    public bool HasError { get; }
};

public class JsonValidateAndParseResult : ProtoBufValidationResult
{
    public JsonValidateAndParseResult(string? error, bool hasError, byte[]? proto) : base(error, hasError)
    {
        ProtoBuf = proto;
    }
    public byte[]? ProtoBuf { get; }
}

public static class ProtoBufValidator
{
    private readonly static string _nativeBinary;

    static ProtoBufValidator()
    {
        _nativeBinary = RuntimeEnvironment.NativeBinary;
    }
    /// <summary>
    /// Validate a base64 encoded protobuf payload against a schema
    /// </summary>
    /// <param name="proto64"></param>
    /// <param name="activeSchemaVersionBase64"></param>
    /// <param name="schemaName"></param>
    /// <returns></returns>
    public static async Task<ProtoBufValidationResult> Validate(
        string proto64,
        string activeSchemaVersionBase64,
        string schemaName
    )
    {
        var result = await Cli.Wrap(_nativeBinary)
            .WithArguments(new[] {
                "eval",
                "--payload",proto64,
                "--schema", activeSchemaVersionBase64,
                "--schema-name", schemaName
            })
            .WithValidation(CommandResultValidation.None)
            .ExecuteBufferedAsync();
        if (result is { ExitCode: ProtoBufValidationError.InvalidPayload })
            return new(result.StandardError, true);
        return new(default, false);
    }

    /// <summary>
    /// Validate a base64 encoded protobuf payload against a schema
    /// </summary>
    /// <param name="json64"></param>
    /// <param name="activeSchemaVersionBase64"></param>
    /// <param name="schemaName"></param>
    /// <returns></returns>
    public static async Task<ProtoBufValidationResult> ValidateJson(
        string json64,
        string activeSchemaVersionBase64,
        string schemaName
    )
    {
        var result = await Cli.Wrap(_nativeBinary)
            .WithArguments(new[] {
                "eval",
                "--payload",json64,
                "--schema", activeSchemaVersionBase64,
                "--schema-name", schemaName,
                "--json"
            })
            .WithValidation(CommandResultValidation.None)
            .ExecuteBufferedAsync();
        if (result is { ExitCode: ProtoBufValidationError.InvalidPayload })
            return new(result.StandardError, true);
        return new(default, false);
    }


    /// <summary>
    /// Validate a base64 encoded protobuf payload against a schema
    /// </summary>
    /// <param name="json64"></param>
    /// <param name="activeSchemaVersionBase64"></param>
    /// <param name="schemaName"></param>
    /// <returns></returns>
    public static async Task<JsonValidateAndParseResult> ValidateAndParseJson(
        string json64,
        string activeSchemaVersionBase64,
        string schemaName
    )
    {
        var result = await Cli.Wrap(_nativeBinary)
            .WithArguments(new[] {
                "conv",
                "--payload",json64,
                "--schema", activeSchemaVersionBase64,
                "--schema-name", schemaName,
            })
            .WithValidation(CommandResultValidation.None)
            .ExecuteBufferedAsync();
        if (result is { ExitCode: ProtoBufValidationError.InvalidPayload })
            return new(result.StandardError, true, null);
        if (string.IsNullOrWhiteSpace(result.StandardOutput))
            return new(default, false, null);
        var proto64 = result.StandardOutput.Trim();
        var protoBytes = Convert.FromBase64String(proto64);
        return new(default, false, protoBytes);
    }


}

internal static class ProtoBufValidationError
{
    public const int InvalidPayload = 1;
}