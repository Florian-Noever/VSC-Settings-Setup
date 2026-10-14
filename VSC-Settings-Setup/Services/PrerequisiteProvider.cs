using VSC_Settings_Setup.Data;

namespace VSC_Settings_Setup.Services;

public sealed class PrerequisiteProvider(HttpClient httpClient, bool allowManualOverride, string localDirectory, string downloadDirectory)
{
    /// <summary>Returns the path of a local copy next to the executable if manual overrides are allowed and one exists.</summary>
    public string? FindLocalOverride(PrerequisiteFile file)
    {
        var path = Path.Combine(localDirectory, file.Name);
        return allowManualOverride && File.Exists(path) ? path : null;
    }

    public async Task<string> DownloadAsync(PrerequisiteFile file)
    {
        var path = Path.Combine(downloadDirectory, file.Name);
        await httpClient.DownloadFileAsync(file.Url, path);
        return path;
    }
}
