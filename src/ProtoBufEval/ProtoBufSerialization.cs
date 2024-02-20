using System.Text;
using CliWrap;
using CliWrap.Buffered;

namespace ProtoBufEval;

file class ErrorCodes
{
    public const int InvalidInput = 1;

    public static void ThrowIfError(BufferedCommandResult result)
    {
        if (result.ExitCode != 0)
            throw new Exception(result.StandardError);
    }
}

file class CmdArgs
{
    public string Payload { get; set; } = null!;
    public string Descriptor { get; set; } = null!;
    public string FileName { get; set; } = null!;
    public string MessageName { get; set; } = null!;

    public string[] ToP2J()
    {
        return new[] {
            "p2j",
            "--payload", Payload,
            "--desc", Descriptor,
            "--mname", MessageName,
            "--fname", FileName
        };
    }

    public string[] ToJ2P()
    {
        return new[]{
            "j2p",
            "--payload", Payload,
            "--desc", Descriptor,
            "--mname", MessageName,
            "--fname", FileName
        };
    }

    public string[] ToCompile()
    {
        return new[]{
            "compile",
            "--desc", Descriptor,
            "--mname", MessageName,
            "--fname", FileName
        };
    }
}

public static class ProtoBufSerialization
{
    private readonly static string _nativeBinary;

    static ProtoBufSerialization()
    {
        _nativeBinary = RuntimeEnvironment.NativeBinary;
    }

    private static async Task<string?> Exec(string[] args)
    {
        var result = await Cli.Wrap(_nativeBinary)
            .WithArguments(args)
            .WithValidation(CommandResultValidation.None)
            .ExecuteBufferedAsync();

        ErrorCodes.ThrowIfError(result);
        return result.StandardOutput;
    }

    public static async Task<string?> ProtoToJson(
        byte[] bytes,
        string descriptor,
        string fileName,
        string messageName
    )
    {
        var p64 = Convert.ToBase64String(bytes);
        var args = new CmdArgs
        {
            Payload = p64,
            Descriptor = descriptor,
            FileName = fileName,
            MessageName = messageName
        };
        return await Exec(args.ToP2J());
    }

    public static async Task<byte[]?> JsonToProto(
        string json,
        string descriptor,
        string fileName,
        string messageName
    )
    {
        var j64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
        var args = new CmdArgs
        {
            Payload = j64,
            Descriptor = descriptor,
            FileName = fileName,
            MessageName = messageName
        };
        var result = await Exec(args.ToJ2P());
        if (string.IsNullOrEmpty(result))
            return null;
        return Convert.FromBase64String(result);
    }

    public static async Task<string?> Compile(
        byte[] descriptor, 
        string fileName, 
        string messageName)
    {
        var args = new CmdArgs
        {
            Descriptor = Convert.ToBase64String(descriptor),
            FileName = fileName,
            MessageName = messageName
        };
        return await Exec(args.ToCompile());
    }
}
