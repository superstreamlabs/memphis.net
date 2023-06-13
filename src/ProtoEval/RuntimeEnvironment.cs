using System.IO.Compression;
using System.Runtime.InteropServices;

namespace ProtoEval;

internal enum OperatingSystem
{
    Unknown,
    Windows,
    Linux,
    OSX
}

internal class RuntimeEnvironment
{
    public static OperatingSystem Platform
    {
        get
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                return OperatingSystem.OSX;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                return OperatingSystem.Linux;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return OperatingSystem.Windows;

            return OperatingSystem.Unknown;
        }
    }

    public static string NativeBinary
    {
        get
        {
            var binaryDir = RuntimeInformation.IsOSPlatform(OSPlatform.OSX)
                ? $"./runtimes/{OS}"
                : $"./runtimes/{OS}-{Arch}";
            Directory.CreateDirectory(binaryDir);
            var compressedBinary = RuntimeInformation.IsOSPlatform(OSPlatform.OSX)
                ? $"./runtimes/{OS}.zip"
                : $"./runtimes/{OS}-{Arch}.zip";
            ZipFile.ExtractToDirectory(compressedBinary, binaryDir, true);
            return Path.Combine(binaryDir, BinaryName);
        }
    }

    private static string BinaryName
    {
        get => Platform switch
        {
            OperatingSystem.Windows => "protoeval.exe",
            _ => "protoeval"
        };
    }

    private static string OS
    {
        get
        {
            return Platform switch
            {
                OperatingSystem.Windows => "win",
                OperatingSystem.Linux => "linux",
                OperatingSystem.OSX => "osx",
                _ => throw new Exception("Unsupported OS")
            };
        }
    }

    private static string Arch
    {
        get
        {
            return RuntimeInformation.OSArchitecture switch
            {
                Architecture.X64 or Architecture.X86 => "x86",
                Architecture.Arm or Architecture.Arm64 => "arm",
                _ => throw new Exception("Unsupported OS architecture")
            };
        }
    }
}