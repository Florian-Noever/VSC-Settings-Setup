using System.Text;

using VSC_Settings_Setup.Data;
using VSC_Settings_Setup.Services;

namespace VSC_Settings_Setup.Tests;

public sealed class ExtensionListReaderTests
{
    private const string Header = "\"name\",\"displayname\",\"publisher\",\"author\",\"description\",\"prerelease\"";

    [Fact]
    public void Read_ParsesQuotedAndUnquotedFields()
    {
        var result = Read($"""
            {Header}
            "gitlens","GitLens — Git supercharged","eamodio","GitKraken","Say ""hi"", world","true"
            excalidraw-editor,Excalidraw,pomdtr,pomdtr,Draw schemas,false
            """);

        VSCodeExtension[] expected =
        [
            new() { Name = "gitlens", DisplayName = "GitLens — Git supercharged", Publisher = "eamodio", Author = "GitKraken", Description = "Say \"hi\", world", PreRelease = true },
            new() { Name = "excalidraw-editor", DisplayName = "Excalidraw", Publisher = "pomdtr", Author = "pomdtr", Description = "Draw schemas", PreRelease = false },
        ];
        Assert.Equal(expected, result.Value);
        Assert.Empty(result.Warnings);
    }

    [Fact]
    public void Read_WithoutPreReleaseColumn_DefaultsToFalse()
    {
        var result = Read("""
            "name","displayname","publisher","author","description"
            "rest-client","REST Client","humao","Huachao Mao","REST Client for Visual Studio Code"
            """);

        var extension = Assert.Single(result.Value);
        Assert.Equal("humao.rest-client", extension.Id);
        Assert.False(extension.PreRelease);
        Assert.Empty(result.Warnings);
    }

    [Fact]
    public void Read_WithOnlyRequiredColumnsInAnyCaseAndOrder_Works()
    {
        var result = Read("""
            Publisher , NAME
            ms-vscode,powershell
            """);

        var extension = Assert.Single(result.Value);
        Assert.Equal("ms-vscode.powershell", extension.Id);
        Assert.Equal(string.Empty, extension.DisplayName);
        Assert.Empty(result.Warnings);
    }

    [Fact]
    public void Read_WithEmptyPreRelease_DefaultsToFalse()
    {
        var result = Read($"""
            {Header}
            json,json,zainchen,ZainChen,Json for Visual Studio Code,
            """);

        Assert.False(Assert.Single(result.Value).PreRelease);
        Assert.Empty(result.Warnings);
    }

    [Fact]
    public void Read_WithInvalidPreRelease_SkipsLineWithWarning()
    {
        var result = Read($"""
            {Header}
            first,First,pub,Author,Description,true
            second,Second,pub,Author,Description,maybe
            third,Third,pub,Author,Description,false
            """);

        Assert.Equal(["pub.first", "pub.third"], result.Value.Select(extension => extension.Id));
        var warning = Assert.Single(result.Warnings);
        Assert.Contains("line 3", warning);
        Assert.Contains("'maybe'", warning);
    }

    [Fact]
    public void Read_WithMissingFields_SkipsLineWithWarning()
    {
        var result = Read($"""
            {Header}
            first,First,pub,Author,Description,true
            short,Short
            third,Third,pub,Author,Description,false
            """);

        Assert.Equal(["pub.first", "pub.third"], result.Value.Select(extension => extension.Id));
        Assert.Contains("line 3", Assert.Single(result.Warnings));
    }

    [Fact]
    public void Read_WithInvalidQuotes_SkipsOnlyThatLine()
    {
        var result = Read($"""
            {Header}
            first,First,pub,Author,Description,true
            "second"x,Second,pub,Author,Description,false
            third,Third,pub,Author,Description,false
            """);

        Assert.Equal(["pub.first", "pub.third"], result.Value.Select(extension => extension.Id));
        Assert.Contains("line 3", Assert.Single(result.Warnings));
    }

    [Theory]
    [InlineData("bad name", "pub")]
    [InlineData("name&calc", "pub")]
    [InlineData("name", "")]
    public void Read_WithInvalidId_SkipsLineWithWarning(string name, string publisher)
    {
        var result = Read($"""
            name,publisher
            "{name}","{publisher}"
            """);

        Assert.Empty(result.Value);
        Assert.Contains("not a valid extension ID", Assert.Single(result.Warnings));
    }

    [Fact]
    public void Read_WithDuplicateIds_KeepsFirstAndWarns()
    {
        var result = Read("""
            name,publisher,displayname
            gitlens,eamodio,First
            GitLens,EAMODIO,Second
            """);

        Assert.Equal("First", Assert.Single(result.Value).DisplayName);
        Assert.Contains("more than once", Assert.Single(result.Warnings));
    }

    [Fact]
    public void Read_WithMissingRequiredColumn_Throws()
    {
        var exception = Assert.Throws<InvalidDataException>(() => Read("""
            name,displayname
            gitlens,GitLens
            """));

        Assert.Contains("publisher", exception.Message);
    }

    [Fact]
    public void Read_WithHtmlInsteadOfCsv_Throws()
    {
        Assert.Throws<InvalidDataException>(() => Read("<!DOCTYPE html><html><head><title>Google Drive</title></head></html>"));
    }

    [Fact]
    public void Read_WithEmptyContent_ReturnsNoExtensions()
    {
        var result = Read(string.Empty);

        Assert.Empty(result.Value);
        Assert.Empty(result.Warnings);
    }

    [Fact]
    public void Read_WithByteOrderMark_MatchesHeader()
    {
        var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes("name,publisher\ngitlens,eamodio\n")).ToArray();
        using var reader = new StreamReader(new MemoryStream(bytes));

        var result = ExtensionListReader.Read(reader);

        Assert.Equal("eamodio.gitlens", Assert.Single(result.Value).Id);
    }

    [Theory]
    [InlineData("extensions-install.csv")]
    [InlineData("extensions-uninstall.csv")]
    public void Read_BundledReferenceFiles_HaveNoWarnings(string fileName)
    {
        var result = ExtensionListReader.Read(Path.Combine(AppContext.BaseDirectory, "Resources", fileName));

        Assert.NotEmpty(result.Value);
        Assert.Empty(result.Warnings);
    }

    private static LoadResult<IReadOnlyList<VSCodeExtension>> Read(string csv) => ExtensionListReader.Read(new StringReader(csv));
}
