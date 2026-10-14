namespace VSC_Settings_Setup.Data;

public sealed record AppSettings
{
    public bool AllowManualOverride { get; init; } = true;
    public string ExtToInstallUrl { get; init; } = "https://raw.githubusercontent.com/Florian-Noever/VSC-Settings-Setup/main/VSC-Settings-Setup/Resources/extensions-install.csv";
    public string ExtToUninstallUrl { get; init; } = "https://raw.githubusercontent.com/Florian-Noever/VSC-Settings-Setup/main/VSC-Settings-Setup/Resources/extensions-uninstall.csv";
    public string SettingsToInstallUrl { get; init; } = "https://raw.githubusercontent.com/Florian-Noever/VSC-Settings-Setup/main/VSC-Settings-Setup/Resources/settings-install.json";
}
