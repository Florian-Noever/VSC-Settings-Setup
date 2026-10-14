using VSC_Settings_Setup.Data;
using VSC_Settings_Setup.Services;

namespace VSC_Settings_Setup.Tests;

public sealed class AppSettingsLoaderTests : IDisposable
{
    private readonly TempDirectory _directory = new();

    public void Dispose() => _directory.Dispose();

    [Fact]
    public void Load_WithoutConfigFile_ReturnsDefaults()
    {
        var result = AppSettingsLoader.Load(_directory.Path);

        Assert.Equal(new AppSettings(), result.Value);
        Assert.Empty(result.Warnings);
    }

    [Fact]
    public void Load_ReadsAllSettings()
    {
        WriteConfig("""
            [config]
            AllowManualOverride=false
            ExtToInstallUrl=https://example.com/install.csv
            ExtToUninstallUrl=https://example.com/uninstall.csv?export=download&id=1
            SettingsToInstallUrl=http://example.com/settings.json
            """);

        var result = AppSettingsLoader.Load(_directory.Path);

        var expected = new AppSettings
        {
            AllowManualOverride = false,
            ExtToInstallUrl = "https://example.com/install.csv",
            ExtToUninstallUrl = "https://example.com/uninstall.csv?export=download&id=1",
            SettingsToInstallUrl = "http://example.com/settings.json",
        };
        Assert.Equal(expected, result.Value);
        Assert.Empty(result.Warnings);
    }

    [Fact]
    public void Load_MatchesSectionAndKeysCaseInsensitively()
    {
        WriteConfig("""
            [Config]
            allowmanualoverride=FALSE
            """);

        var result = AppSettingsLoader.Load(_directory.Path);

        Assert.False(result.Value.AllowManualOverride);
        Assert.Empty(result.Warnings);
    }

    [Fact]
    public void Load_WithEmptyValues_UsesDefaults()
    {
        WriteConfig("""
            [config]
            AllowManualOverride=
            ExtToInstallUrl=
            """);

        var result = AppSettingsLoader.Load(_directory.Path);

        Assert.Equal(new AppSettings(), result.Value);
        Assert.Empty(result.Warnings);
    }

    [Fact]
    public void Load_WithInvalidBoolean_WarnsAndStillReadsOtherSettings()
    {
        WriteConfig("""
            [config]
            AllowManualOverride=yes
            ExtToInstallUrl=https://example.com/install.csv
            """);

        var result = AppSettingsLoader.Load(_directory.Path);

        Assert.True(result.Value.AllowManualOverride);
        Assert.Equal("https://example.com/install.csv", result.Value.ExtToInstallUrl);
        Assert.Contains("'yes'", Assert.Single(result.Warnings));
    }

    [Theory]
    [InlineData("example.com/install.csv")]
    [InlineData("ftp://example.com/install.csv")]
    [InlineData(@"C:\install.csv")]
    public void Load_WithInvalidUrl_WarnsAndUsesDefault(string url)
    {
        WriteConfig($"""
            [config]
            ExtToInstallUrl={url}
            """);

        var result = AppSettingsLoader.Load(_directory.Path);

        Assert.Equal(new AppSettings().ExtToInstallUrl, result.Value.ExtToInstallUrl);
        Assert.Contains(url, Assert.Single(result.Warnings));
    }

    [Fact]
    public void Load_WithUnknownKey_Warns()
    {
        WriteConfig("""
            [config]
            AllowOverride=false
            """);

        var result = AppSettingsLoader.Load(_directory.Path);

        Assert.Equal(new AppSettings(), result.Value);
        Assert.Contains("'AllowOverride'", Assert.Single(result.Warnings));
    }

    [Fact]
    public void Load_WithMalformedFile_WarnsAndUsesDefaults()
    {
        WriteConfig("""
            [config]
            this line has no equals sign
            """);

        var result = AppSettingsLoader.Load(_directory.Path);

        Assert.Equal(new AppSettings(), result.Value);
        Assert.Contains(AppSettingsLoader.FileName, Assert.Single(result.Warnings));
    }

    [Fact]
    public void Load_ShippedConfigFile_HasNoWarnings()
    {
        Assert.True(File.Exists(Path.Combine(AppContext.BaseDirectory, AppSettingsLoader.FileName)));

        var result = AppSettingsLoader.Load(AppContext.BaseDirectory);

        Assert.Empty(result.Warnings);
    }

    private void WriteConfig(string content) => _directory.WriteFile(AppSettingsLoader.FileName, content);
}
