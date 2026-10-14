using Microsoft.Extensions.Configuration;

using VSC_Settings_Setup.Data;

namespace VSC_Settings_Setup.Services;

public static class AppSettingsLoader
{
    public const string FileName = "config.ini";
    private const string SectionName = "config";

    private static readonly string[] KnownKeys = [nameof(AppSettings.AllowManualOverride), nameof(AppSettings.ExtToInstallUrl), nameof(AppSettings.ExtToUninstallUrl), nameof(AppSettings.SettingsToInstallUrl)];

    public static LoadResult<AppSettings> Load(string directory)
    {
        var defaults = new AppSettings();
        IConfigurationSection section;
        try
        {
            section = new ConfigurationBuilder()
                .AddIniFile(Path.Combine(directory, FileName), optional: true)
                .Build()
                .GetSection(SectionName);
        }
        catch (Exception ex) when (ex is InvalidDataException or IOException or UnauthorizedAccessException)
        {
            return new(defaults, [$"Could not read {FileName}, using default settings. {(ex.InnerException ?? ex).Message}"]);
        }

        var warnings = section.GetChildren()
            .Where(setting => !KnownKeys.Contains(setting.Key, StringComparer.OrdinalIgnoreCase))
            .Select(setting => $"Unknown setting '{setting.Key}' in {FileName} is ignored.")
            .ToList();

        var settings = new AppSettings
        {
            AllowManualOverride = ReadBool(section, nameof(AppSettings.AllowManualOverride), defaults.AllowManualOverride, warnings),
            ExtToInstallUrl = ReadUrl(section, nameof(AppSettings.ExtToInstallUrl), defaults.ExtToInstallUrl, warnings),
            ExtToUninstallUrl = ReadUrl(section, nameof(AppSettings.ExtToUninstallUrl), defaults.ExtToUninstallUrl, warnings),
            SettingsToInstallUrl = ReadUrl(section, nameof(AppSettings.SettingsToInstallUrl), defaults.SettingsToInstallUrl, warnings),
        };
        return new(settings, warnings);
    }

    private static bool ReadBool(IConfigurationSection section, string key, bool defaultValue, List<string> warnings)
    {
        var value = section[key];
        if (string.IsNullOrWhiteSpace(value))
            return defaultValue;
        if (bool.TryParse(value, out var result))
            return result;

        warnings.Add($"Invalid value '{value}' for {key} in {FileName}, using the default '{defaultValue}'.");
        return defaultValue;
    }

    private static string ReadUrl(IConfigurationSection section, string key, string defaultValue, List<string> warnings)
    {
        var value = section[key];
        if (string.IsNullOrWhiteSpace(value))
            return defaultValue;
        if (Uri.TryCreate(value, UriKind.Absolute, out var uri) && (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp))
            return value;

        warnings.Add($"Invalid URL '{value}' for {key} in {FileName}, using the default URL.");
        return defaultValue;
    }
}
