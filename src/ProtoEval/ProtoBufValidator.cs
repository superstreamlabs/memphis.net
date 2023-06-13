using CliWrap;
using CliWrap.Buffered;

namespace ProtoEval;

/// <summary>
/// Result of a validation
/// </summary>
/// <param name="Error"></param>
/// <param name="HasError"></param>
public record ProtoBufValidationResult(string? Error, bool HasError);


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
    /// <param name="base64Data"></param>
    /// <param name="activeSchemaVersion"></param>
    /// <param name="schemaName"></param>
    /// <returns></returns>
    public static async Task<ProtoBufValidationResult> Validate(string base64Data, string activeSchemaVersion, string schemaName)
    {
        var result = await Cli.Wrap(_nativeBinary)
            .WithArguments(new[] {
                "eval",
                "--payload",base64Data,
                "--schema", activeSchemaVersion,
                "--schema-name", schemaName
            })
            .WithValidation(CommandResultValidation.None)
            .ExecuteBufferedAsync();
        if (result is { ExitCode: ProtoBufValidationError.InvalidPayload })
            return new(result.StandardError, true);
        return new(default, false);
    }
}

internal static class ProtoBufValidationError
{
    public const int InvalidPayload = 1;
}