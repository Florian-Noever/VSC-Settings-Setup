using System.Runtime.InteropServices;

using VSC_Settings_Setup.Data;

namespace VSC_Settings_Setup.Services;

public sealed class VSCodeInstaller(HttpClient httpClient, string downloadDirectory, Architecture osArchitecture)
{
    private const string X64DownloadUrl = "https://code.visualstudio.com/sha/download?build=stable&os=win32-x64-user";
    private const string Arm64DownloadUrl = "https://code.visualstudio.com/sha/download?build=stable&os=win32-arm64-user";
    private static readonly string[] InstallerArguments = ["/verysilent", "/mergetasks=addcontextmenufiles,addcontextmenufolders,!runcode"];
    private static readonly TimeSpan InstallTimeout = TimeSpan.FromMinutes(10);

    // VS Code only ships x64 and ARM64 builds for Windows.
    private readonly string _downloadUrl = osArchitecture == Architecture.Arm64 ? Arm64DownloadUrl : X64DownloadUrl;
    private readonly string _installerPath = Path.Combine(downloadDirectory, "VSCodeInstaller.exe");

    public Task DownloadAsync(IProgress<DownloadProgress>? progress = null) => httpClient.DownloadFileAsync(_downloadUrl, _installerPath, progress);

    /// <summary>Runs the downloaded installer silently and deletes it afterwards.</summary>
    public async Task InstallAsync()
    {
        try
        {
            await ProcessRunner.RunAsync(_installerPath, InstallerArguments, InstallTimeout);
        }
        finally
        {
            TryDeleteInstaller();
        }
    }

    private void TryDeleteInstaller()
    {
        try
        {
            File.Delete(_installerPath);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Not critical, the installer just stays in the temp folder.
        }
    }
}
