using VSC_Settings_Setup.Services;

namespace VSC_Settings_Setup.Tests;

public sealed class ProcessRunnerTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(30);

    [Fact]
    public async Task RunAsync_ReturnsStandardOutput()
    {
        var output = await ProcessRunner.RunAsync("cmd.exe", ["/c", "echo hello"], Timeout);

        Assert.Equal("hello", output.Trim());
    }

    [Fact]
    public async Task RunAsync_PassesEnvironmentVariables()
    {
        var output = await ProcessRunner.RunAsync("cmd.exe", ["/c", "echo %VSC_SETUP_TEST%"], Timeout, new Dictionary<string, string> { ["VSC_SETUP_TEST"] = "value" });

        Assert.Equal("value", output.Trim());
    }

    [Fact]
    public async Task RunAsync_WithNonZeroExitCode_ThrowsWithStandardError()
    {
        var exception = await Assert.ThrowsAsync<ProcessFailedException>(() => ProcessRunner.RunAsync("cmd.exe", ["/c", "echo something went wrong 1>&2 & exit /b 3"], Timeout));

        Assert.Equal("something went wrong", exception.Message);
    }

    [Fact]
    public async Task RunAsync_WithNonZeroExitCodeAndNoOutput_ThrowsWithExitCode()
    {
        var exception = await Assert.ThrowsAsync<ProcessFailedException>(() => ProcessRunner.RunAsync("cmd.exe", ["/c", "exit /b 3"], Timeout));

        Assert.Equal("cmd.exe exited with code 3.", exception.Message);
    }

    [Fact]
    public async Task RunAsync_WhenTimedOut_Throws()
    {
        var exception = await Assert.ThrowsAsync<ProcessFailedException>(() => ProcessRunner.RunAsync("cmd.exe", ["/c", "ping -n 30 127.0.0.1 > nul"], TimeSpan.FromSeconds(1)));

        Assert.Contains("did not finish", exception.Message);
    }

    [Fact]
    public async Task RunAsync_WithMissingExecutable_Throws()
    {
        var missingPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.exe");

        var exception = await Assert.ThrowsAsync<ProcessFailedException>(() => ProcessRunner.RunAsync(missingPath, [], Timeout));

        Assert.StartsWith("Could not start", exception.Message);
    }
}
