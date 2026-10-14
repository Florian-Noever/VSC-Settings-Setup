using System.Net;
using System.Runtime.InteropServices;

using VSC_Settings_Setup.Services;

namespace VSC_Settings_Setup.Tests;

public sealed class VSCodeInstallerTests
{
    [Theory]
    [InlineData(Architecture.X64, "win32-x64-user")]
    [InlineData(Architecture.Arm64, "win32-arm64-user")]
    [InlineData(Architecture.X86, "win32-x64-user")]
    public async Task DownloadAsync_DownloadsInstallerForOSArchitecture(Architecture osArchitecture, string expectedBuild)
    {
        using var directory = new TempDirectory();
        var handler = new RecordingHandler();
        using var httpClient = new HttpClient(handler);
        var installer = new VSCodeInstaller(httpClient, directory.Path, osArchitecture);

        await installer.DownloadAsync();

        Assert.Equal($"https://code.visualstudio.com/sha/download?build=stable&os={expectedBuild}", handler.RequestUri?.AbsoluteUri);
        Assert.Equal("installer", File.ReadAllText(Path.Combine(directory.Path, "VSCodeInstaller.exe")));
    }

    private sealed class RecordingHandler : HttpMessageHandler
    {
        public Uri? RequestUri { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestUri = request.RequestUri;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("installer") });
        }
    }
}
