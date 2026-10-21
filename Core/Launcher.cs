using System.Diagnostics;
using System.IO;

namespace StonemachiaModLoader.Core;

/// <summary>Starts Stonemachia and injects UE4SS with Nefarius/Injector.</summary>
internal static class Launcher
{
    private static readonly TimeSpan ProcessStartTimeout = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan ProcessPollInterval = TimeSpan.FromMilliseconds(250);

    public static async Task LaunchModdedAsync(
        string gameExe,
        string injectorExe,
        string ue4ssDll,
        CancellationToken cancellationToken = default)
    {
        string gamePath = ValidateFile(gameExe, "Game executable");
        string injectorPath = ValidateFile(injectorExe, "Nefarius Injector executable");
        string dllPath = ValidateFile(ue4ssDll, "UE4SS DLL");
        ValidationReport validation = FileValidation.Validate(gamePath, injectorPath, dllPath);
        if (!validation.IsValid)
            throw new InvalidOperationException("Launch validation failed:" + Environment.NewLine +
                string.Join(Environment.NewLine, validation.Errors));
        await FileValidation.ValidateNefariusInjectorAsync(injectorPath, cancellationToken);
        string gameDirectory = Path.GetDirectoryName(gamePath)
            ?? throw new InvalidOperationException("Could not determine the game directory.");

        using Process launchedGame = Process.Start(new ProcessStartInfo
        {
            FileName = gamePath,
            WorkingDirectory = gameDirectory,
            UseShellExecute = false
        }) ?? throw new InvalidOperationException("Windows could not start Stonemachia.");

        Process targetProcess = await WaitForTargetProcessAsync(
            launchedGame, Path.GetFileNameWithoutExtension(gamePath), cancellationToken);

        using (targetProcess)
        using (Process injector = Process.Start(new ProcessStartInfo
        {
            FileName = injectorPath,
            WorkingDirectory = Path.GetDirectoryName(injectorPath)!,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            ArgumentList =
            {
                "--process-id", targetProcess.Id.ToString(System.Globalization.CultureInfo.InvariantCulture),
                "--inject", dllPath
            }
        }) ?? throw new InvalidOperationException("Windows could not start Nefarius Injector."))
        {
            Task<string> stdoutTask = injector.StandardOutput.ReadToEndAsync(cancellationToken);
            Task<string> stderrTask = injector.StandardError.ReadToEndAsync(cancellationToken);
            await injector.WaitForExitAsync(cancellationToken);
            string stdout = await stdoutTask;
            string stderr = await stderrTask;

            if (injector.ExitCode != 0)
            {
                string details = string.Join(Environment.NewLine,
                    new[] { stdout.Trim(), stderr.Trim() }.Where(s => !string.IsNullOrWhiteSpace(s)));
                throw new InvalidOperationException(
                    $"UE4SS injection failed (Injector exit code {injector.ExitCode})." +
                    (details.Length > 0 ? Environment.NewLine + details : string.Empty));
            }
        }
    }

    private static async Task<Process> WaitForTargetProcessAsync(
        Process launchedGame,
        string expectedProcessName,
        CancellationToken cancellationToken)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();
        while (stopwatch.Elapsed < ProcessStartTimeout)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                if (launchedGame.HasExited)
                    throw new InvalidOperationException(
                        $"Stonemachia exited before the target process was ready (exit code {launchedGame.ExitCode}). " +
                        "If the configured executable is only a launcher, select the actual game executable.");
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("No process is associated", StringComparison.OrdinalIgnoreCase))
            {
                // Process state can be unavailable if the process exited during startup.
            }

            Process? match = null;
            foreach (Process candidate in Process.GetProcessesByName(expectedProcessName))
            {
                try
                {
                    if (candidate.Id == launchedGame.Id && !candidate.HasExited)
                    {
                        match = candidate;
                        break;
                    }
                    candidate.Dispose();
                }
                catch (InvalidOperationException)
                {
                    candidate.Dispose();
                }
            }

            if (match is not null)
                return match;

            await Task.Delay(ProcessPollInterval, cancellationToken);
        }

        throw new TimeoutException(
            $"Could not find the Stonemachia process '{expectedProcessName}' within " +
            $"{ProcessStartTimeout.TotalSeconds:0} seconds. Check that the configured executable is the actual game executable.");
    }

    private static string ValidateFile(string? path, string description)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new InvalidOperationException($"{description} path is not configured.");

        string fullPath = Path.GetFullPath(path.Trim());
        if (!File.Exists(fullPath))
            throw new FileNotFoundException($"{description} was not found.", fullPath);

        return fullPath;
    }
}
