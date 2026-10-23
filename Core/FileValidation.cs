using System.Diagnostics;
using System.IO;

namespace StonemachiaModLoader.Core;

internal sealed record ValidationReport(IReadOnlyList<string> Errors, IReadOnlyList<string> Warnings)
{
    public bool IsValid => Errors.Count == 0;
}

/// <summary>Performs conservative checks on selected game, injector and UE4SS files.</summary>
internal static class FileValidation
{
    private const ushort ImageFileMachineI386 = 0x014c;
    private const ushort ImageFileMachineAmd64 = 0x8664;
    private const ushort ImageFileMachineArm64 = 0xAA64;
    private const ushort ImageFileDll = 0x2000;

    public static ValidationReport Validate(string? gamePath, string? injectorPath, string? dllPath)
    {
        var errors = new List<string>();
        var warnings = new List<string>();
        PeInfo? game = null;
        PeInfo? dll = null;

        gamePath = NormalizeExistingFile(gamePath, ".exe", "Game executable", errors);
        injectorPath = NormalizeExistingFile(injectorPath, ".exe", "Injector", errors);
        dllPath = NormalizeExistingFile(dllPath, ".dll", "UE4SS DLL", errors);

        if (gamePath is not null)
        {
            game = ReadPe(gamePath, errors, "Game executable");
            if (game is not null && game.IsDll)
                errors.Add("The selected game file is a DLL, not an executable.");
            if (game is not null && game.Machine != ImageFileMachineAmd64)
                errors.Add($"The game executable is {MachineName(game.Machine)}. This launcher currently expects a 64-bit x64 Unreal game.");

            if (!LooksLikeUnrealGame(gamePath))
                warnings.Add("No common Unreal Engine markers were found beside this EXE. It may be the wrong executable or a non-Unreal game. Continue only if you have verified this is Stonemachia's actual game executable.");
        }

        if (injectorPath is not null)
        {
            var injector = ReadPe(injectorPath, errors, "Injector");
            if (injector is not null && injector.IsDll)
                errors.Add("The selected Injector file is a DLL, not an executable.");
            if (!Path.GetFileName(injectorPath).Equals("Injector.exe", StringComparison.OrdinalIgnoreCase))
                warnings.Add("The injector filename is not Injector.exe. Confirm that this is the official Nefarius/Injector binary, not another program renamed to Injector.exe.");
        }

        if (dllPath is not null)
        {
            dll = ReadPe(dllPath, errors, "UE4SS DLL");
            if (dll is not null && !dll.IsDll)
                errors.Add("The selected UE4SS file is not marked as a Windows DLL.");
            if (dll is not null && dll.Machine != ImageFileMachineAmd64)
                errors.Add($"The selected DLL is {MachineName(dll.Machine)}. UE4SS must match the game's x64 architecture.");
            if (!Path.GetFileName(dllPath).Equals("UE4SS.dll", StringComparison.OrdinalIgnoreCase))
                warnings.Add("The DLL filename is not UE4SS.dll. Make sure you selected the main UE4SS DLL and not a helper or proxy DLL.");
        }

        if (game is not null && dll is not null && game.Machine != dll.Machine)
            errors.Add("The game and DLL architectures do not match.");

        return new ValidationReport(errors, warnings);
    }

    /// <summary>Runs the selected injector's help command and checks for Nefarius CLI switches.</summary>
    public static async Task ValidateNefariusInjectorAsync(string injectorPath, CancellationToken cancellationToken = default)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = injectorPath,
            WorkingDirectory = Path.GetDirectoryName(injectorPath)!,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };
        startInfo.ArgumentList.Add("--help");

        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("Windows could not start the selected injector to verify its command-line interface.");
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(3));
        Task<string> stdout = process.StandardOutput.ReadToEndAsync(timeout.Token);
        Task<string> stderr = process.StandardError.ReadToEndAsync(timeout.Token);
        try
        {
            await process.WaitForExitAsync(timeout.Token);
            string output = (await stdout) + Environment.NewLine + (await stderr);
            if (process.ExitCode != 0 && string.IsNullOrWhiteSpace(output))
                throw new InvalidOperationException($"Injector --help exited with code {process.ExitCode} and returned no help text.");

            if (!output.Contains("--process-id", StringComparison.OrdinalIgnoreCase) ||
                !output.Contains("--inject", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("The selected executable did not advertise the expected Nefarius/Injector options (--process-id and --inject). Select the official Nefarius/Injector binary.");
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                process.Kill(entireProcessTree: true);
            }
            catch
            { 
                /* best effort */
            }
            throw new TimeoutException("The selected injector did not respond to --help within 3 seconds.");
        }
    }

    private static string? NormalizeExistingFile(string? path, string extension, string description, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            errors.Add($"{description}: select a file.");
            return null;
        }
        try
        {
            string fullPath = Path.GetFullPath(path.Trim());
            if (!File.Exists(fullPath))
            {
                errors.Add($"{description} was not found: {fullPath}");
                return null;
            }
            if (!Path.GetExtension(fullPath).Equals(extension, StringComparison.OrdinalIgnoreCase))
            {
                errors.Add($"{description} must have the {extension} extension.");
                return null;
            }
            return fullPath;
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            errors.Add($"{description} path is invalid: {ex.Message}");
            return null;
        }
    }

    private static PeInfo? ReadPe(string path, List<string> errors, string description)
    {
        try
        {
            using var stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
            using var reader = new BinaryReader(stream);
            if (stream.Length < 64 || reader.ReadUInt16() != 0x5A4D)
                throw new InvalidDataException("missing MZ header");
            stream.Position = 0x3C;
            int peOffset = reader.ReadInt32();
            if (peOffset < 64 || peOffset > stream.Length - 26)
                throw new InvalidDataException("invalid PE header offset");
            stream.Position = peOffset;
            if (reader.ReadUInt32() != 0x00004550)
                throw new InvalidDataException("missing PE signature");
            ushort machine = reader.ReadUInt16();
            stream.Position = peOffset + 22;
            ushort characteristics = reader.ReadUInt16();
            return new PeInfo(machine, (characteristics & ImageFileDll) != 0);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException)
        {
            errors.Add($"{description} is not a valid readable Windows PE file: {Path.GetFileName(path)} ({ex.Message}).");
            return null;
        }
    }

    private static bool LooksLikeUnrealGame(string exePath)
    {
        string? directory = Path.GetDirectoryName(exePath);
        if (directory is null) return false;
        string? root = Directory.GetParent(directory)?.Parent?.FullName;
        string[] candidates = { directory, root ?? directory, Directory.GetParent(directory)?.FullName ?? directory };
        foreach (string candidate in candidates.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (Directory.Exists(Path.Combine(candidate, "Content", "Paks")) ||
                Directory.Exists(Path.Combine(candidate, "Engine", "Binaries")) ||
                Directory.Exists(Path.Combine(candidate, "Binaries", "Win64")) && Directory.Exists(Path.Combine(candidate, "Content")))
                return true;
        }
        return false;
    }

    private static string MachineName(ushort machine) => machine switch
    {
        ImageFileMachineI386 => "x86 (32-bit)",
        ImageFileMachineAmd64 => "x64 (64-bit)",
        ImageFileMachineArm64 => "ARM64",
        _ => $"unknown architecture (0x{machine:X4})"
    };

    private sealed record PeInfo(ushort Machine, bool IsDll);
}
