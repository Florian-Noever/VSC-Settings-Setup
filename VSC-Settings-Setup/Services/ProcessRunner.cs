using System.ComponentModel;
using System.Diagnostics;

namespace VSC_Settings_Setup.Services;

public static class ProcessRunner
{
    /// <summary>Runs a process to completion and returns its standard output.</summary>
    /// <exception cref="ProcessFailedException">The process could not be started, timed out or exited with a non-zero exit code.</exception>
    public static async Task<string> RunAsync(string fileName, IEnumerable<string> arguments, TimeSpan timeout, IReadOnlyDictionary<string, string>? environment = null)
    {
        var name = Path.GetFileName(fileName);
        var startInfo = new ProcessStartInfo(fileName, arguments)
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        if (environment is not null)
        {
            foreach (var (variable, value) in environment)
                startInfo.Environment[variable] = value;
        }

        Process process;
        try
        {
            process = Process.Start(startInfo) ?? throw new ProcessFailedException($"Could not start {name}.");
        }
        catch (Win32Exception ex)
        {
            throw new ProcessFailedException($"Could not start {name}: {ex.Message}", ex);
        }

        using (process)
        {
            var outputTask = process.StandardOutput.ReadToEndAsync();
            var errorTask = process.StandardError.ReadToEndAsync();
            using var timeoutSource = new CancellationTokenSource(timeout);
            string output, error;
            try
            {
                await process.WaitForExitAsync(timeoutSource.Token);
                // Output is awaited with the same timeout in case a child process keeps the redirected streams open.
                output = await outputTask.WaitAsync(timeoutSource.Token);
                error = await errorTask.WaitAsync(timeoutSource.Token);
            }
            catch (OperationCanceledException ex)
            {
                KillProcessTree(process);
                throw new ProcessFailedException($"{name} did not finish within {timeout.TotalMinutes:0.#} minute(s).", ex);
            }

            if (process.ExitCode != 0)
                throw new ProcessFailedException(GetFailureMessage(name, process.ExitCode, error, output));
            return output;
        }
    }

    private static string GetFailureMessage(string name, int exitCode, string error, string output)
    {
        var details = string.IsNullOrWhiteSpace(error) ? output.Trim() : error.Trim();
        return string.IsNullOrEmpty(details) ? $"{name} exited with code {exitCode}." : details;
    }

    private static void KillProcessTree(Process process)
    {
        try
        {
            process.Kill(entireProcessTree: true);
        }
        catch (Exception ex) when (ex is InvalidOperationException or Win32Exception or AggregateException)
        {
            // Best effort: the process may have exited in the meantime.
        }
    }
}
