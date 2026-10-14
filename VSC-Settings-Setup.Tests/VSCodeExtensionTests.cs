using VSC_Settings_Setup.Data;

namespace VSC_Settings_Setup.Tests;

public sealed class VSCodeExtensionTests
{
    [Theory]
    [InlineData("ms-dotnettools.csharp")]
    [InlineData("Postman.postman-for-vscode")]
    [InlineData("365businessdevelopment.bdev-al-xml-doc")]
    [InlineData("a.b")]
    public void IsValidId_AcceptsMarketplaceIds(string id)
    {
        Assert.True(VSCodeExtension.IsValidId(id));
    }

    [Theory]
    [InlineData("")]
    [InlineData("publisher")]
    [InlineData("publisher.")]
    [InlineData(".name")]
    [InlineData("-publisher.name")]
    [InlineData("publisher.name.extra")]
    [InlineData("publisher.na me")]
    [InlineData("publisher.name&calc")]
    [InlineData("publisher.name\n")]
    [InlineData("publisher.\"name\"")]
    public void IsValidId_RejectsInvalidIds(string id)
    {
        Assert.False(VSCodeExtension.IsValidId(id));
    }

    [Fact]
    public void Id_CombinesPublisherAndName()
    {
        var extension = new VSCodeExtension { Publisher = "eamodio", Name = "gitlens" };

        Assert.Equal("eamodio.gitlens", extension.Id);
    }
}
