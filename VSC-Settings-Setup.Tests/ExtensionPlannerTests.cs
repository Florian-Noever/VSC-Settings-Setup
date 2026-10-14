using VSC_Settings_Setup.Data;
using VSC_Settings_Setup.Services;

namespace VSC_Settings_Setup.Tests;

public sealed class ExtensionPlannerTests
{
    private static readonly VSCodeExtension Copilot = Create("github", "copilot");
    private static readonly VSCodeExtension CopilotChat = Create("github", "copilot-chat");
    private static readonly VSCodeExtension Postman = Create("Postman", "postman-for-vscode");

    [Fact]
    public void GetMissing_MatchesInstalledIdsCaseInsensitively()
    {
        var missing = ExtensionPlanner.GetMissing([Postman], ["postman.postman-for-vscode"]);

        Assert.Empty(missing);
    }

    [Fact]
    public void GetMissing_DoesNotTreatSimilarIdsAsInstalled()
    {
        var missing = ExtensionPlanner.GetMissing([Copilot, CopilotChat, Postman], ["github.copilot-chat"]);

        VSCodeExtension[] expected = [Copilot, Postman];
        Assert.Equal(expected, missing);
    }

    [Fact]
    public void GetInstalled_ReturnsOnlyExactMatchesCaseInsensitively()
    {
        var installed = ExtensionPlanner.GetInstalled([Copilot, CopilotChat, Postman], ["GitHub.Copilot", "postman.postman-for-vscode-preview"]);

        VSCodeExtension[] expected = [Copilot];
        Assert.Equal(expected, installed);
    }

    private static VSCodeExtension Create(string publisher, string name) => new() { Publisher = publisher, Name = name };
}
