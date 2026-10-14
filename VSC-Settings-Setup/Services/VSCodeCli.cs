using VSC_Settings_Setup.Data;

namespace VSC_Settings_Setup.Services;

public sealed class VSCodeCli(string installDirectory)
{
    private static readonly TimeSpan ListTimeout = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan InstallTimeout = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan UninstallTimeout = TimeSpan.FromMinutes(2);

    // The CLI runs on Node.js, whose deprecation warnings would otherwise end up in the error messages.
    private static readonly Dictionary<string, string> CliEnvironment = new() { ["NODE_NO_WARNINGS"] = "1" };

    private readonly string _cliPath = VSCodeLocator.GetCliPath(installDirectory);

    public async Task<IReadOnlyList<string>> ListExtensionsAsync()
    {
        var output = await RunAsync(["--list-extensions"], ListTimeout);
        return output
            .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(VSCodeExtension.IsValidId)
            .ToList();
    }

    public Task InstallExtensionAsync(string id, bool preRelease)
    {
        EnsureValidId(id);
        string[] arguments = preRelease ? ["--install-extension", id, "--pre-release"] : ["--install-extension", id];
        return RunAsync(arguments, InstallTimeout);
    }

    public Task UninstallExtensionAsync(string id)
    {
        EnsureValidId(id);
        return RunAsync(["--uninstall-extension", id], UninstallTimeout);
    }

    private Task<string> RunAsync(string[] arguments, TimeSpan timeout) => ProcessRunner.RunAsync(_cliPath, arguments, timeout, CliEnvironment);

    private static void EnsureValidId(string id)
    {
        if (!VSCodeExtension.IsValidId(id))
            throw new ArgumentException($"'{id}' is not a valid extension ID.", nameof(id));
    }
}
