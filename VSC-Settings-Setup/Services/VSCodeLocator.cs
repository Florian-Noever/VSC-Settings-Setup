using Microsoft.Win32;

namespace VSC_Settings_Setup.Services;

public static class VSCodeLocator
{
    private const string ExecutableName = "Code.exe";
    private const string CliName = "code.cmd";
    private const string UninstallKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall";
    private const string Wow6432UninstallKey = @"SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall";

    public static string? FindInstallDirectory() => FromKnownDirectories() ?? FromUninstallRegistry() ?? FromPathEnvironment();

    public static string GetCliPath(string installDirectory) => Path.Combine(installDirectory, "bin", CliName);

    private static bool ContainsExecutable(string? directory) => !string.IsNullOrWhiteSpace(directory) && File.Exists(Path.Combine(directory, ExecutableName));

    private static string? FromKnownDirectories()
    {
        string[] directories =
        [
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "Microsoft VS Code"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Microsoft VS Code"),
        ];
        return directories.FirstOrDefault(ContainsExecutable);
    }

    private static string? FromUninstallRegistry() =>
        SearchUninstallEntries(Registry.CurrentUser, UninstallKey)
        ?? SearchUninstallEntries(Registry.LocalMachine, UninstallKey)
        ?? SearchUninstallEntries(Registry.LocalMachine, Wow6432UninstallKey);

    private static string? SearchUninstallEntries(RegistryKey root, string subKeyName)
    {
        using var uninstallKey = root.OpenSubKey(subKeyName);
        if (uninstallKey is null)
            return null;

        foreach (var name in uninstallKey.GetSubKeyNames())
        {
            using var appKey = uninstallKey.OpenSubKey(name);
            if (appKey?.GetValue("DisplayName") is not string displayName || !displayName.Contains("Microsoft Visual Studio Code", StringComparison.OrdinalIgnoreCase))
                continue;

            if (appKey.GetValue("InstallLocation") is string installLocation && ContainsExecutable(installLocation))
                return installLocation;

            // DisplayIcon holds the full path to Code.exe.
            if (appKey.GetValue("DisplayIcon") is string displayIcon && Path.GetDirectoryName(displayIcon.Trim('"')) is { } iconDirectory && ContainsExecutable(iconDirectory))
                return iconDirectory;
        }
        return null;
    }

    private static string? FromPathEnvironment()
    {
        var path = Environment.GetEnvironmentVariable("PATH");
        if (string.IsNullOrEmpty(path))
            return null;

        foreach (var entry in path.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var directory = Path.TrimEndingDirectorySeparator(entry.Trim('"'));
            if (ContainsExecutable(directory))
                return directory;

            // The installer adds the "bin" folder containing code.cmd to PATH, Code.exe is one level up.
            if (File.Exists(Path.Combine(directory, CliName)) && Path.GetDirectoryName(directory) is { } parent && ContainsExecutable(parent))
                return parent;
        }
        return null;
    }
}
