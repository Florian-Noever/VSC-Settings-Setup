using System.Text.Json;
using System.Text.Json.Nodes;

using VSC_Settings_Setup.Data;
using VSC_Settings_Setup.Services;

namespace VSC_Settings_Setup.Tests;

public sealed class SettingsMergerTests
{
    [Fact]
    public void Parse_AllowsCommentsAndTrailingCommas()
    {
        var settings = SettingsMerger.Parse("""
            {
                // line comment
                "editor.formatOnSave": true, /* block comment */
                "files.exclude": { "**/bin": true, },
            }
            """);

        Assert.Equal(["editor.formatOnSave", "files.exclude"], settings.Select(setting => setting.Key));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" \r\n ")]
    public void Parse_EmptyContent_ReturnsEmptyObject(string json)
    {
        Assert.Empty(SettingsMerger.Parse(json));
    }

    [Theory]
    [InlineData("{ \"a\": ")]
    [InlineData("[1, 2]")]
    [InlineData("\"text\"")]
    [InlineData("{ \"a\": 1, \"a\": 2 }")]
    public void Parse_InvalidSettings_ThrowsJsonException(string json)
    {
        Assert.ThrowsAny<JsonException>(() => SettingsMerger.Parse(json));
    }

    [Fact]
    public void GetChanges_ReportsNewAndChangedSettingsOnly()
    {
        var current = SettingsMerger.Parse("""{ "same": 1, "changed": "old", "nested": { "a": 1, "b": [1, 2] }, "empty": null }""");
        var recommended = SettingsMerger.Parse("""{ "same": 1.0, "changed": "new", "nested": { "b": [1, 2], "a": 1 }, "empty": null, "added": false }""");

        var changes = SettingsMerger.GetChanges(current, recommended);

        SettingChange[] expected = [new("changed", IsNew: false), new("added", IsNew: true)];
        Assert.Equal(expected, changes);
    }

    [Fact]
    public void GetChanges_TreatsDifferentCaseAndArrayOrderAsChanges()
    {
        var current = SettingsMerger.Parse("""{ "theme": "dark", "list": [1, 2] }""");
        var recommended = SettingsMerger.Parse("""{ "theme": "Dark", "list": [2, 1] }""");

        var changes = SettingsMerger.GetChanges(current, recommended);

        Assert.Equal(["theme", "list"], changes.Select(change => change.Key));
    }

    [Fact]
    public void Merge_AppliesOnlySelectedKeysAndKeepsInputsUnchanged()
    {
        var current = SettingsMerger.Parse("""{ "keep": 1, "changed": "old" }""");
        var recommended = SettingsMerger.Parse("""{ "changed": "new", "added": [1], "skipped": true }""");

        var merged = SettingsMerger.Merge(current, recommended, ["changed", "added"]);

        Assert.Equal(["keep", "changed", "added"], merged.Select(setting => setting.Key));
        Assert.True(JsonNode.DeepEquals(SettingsMerger.Parse("""{ "keep": 1, "changed": "new", "added": [1] }"""), merged));
        Assert.True(JsonNode.DeepEquals(SettingsMerger.Parse("""{ "keep": 1, "changed": "old" }"""), current));
    }

    [Fact]
    public void Merge_CopiesValues()
    {
        var recommended = SettingsMerger.Parse("""{ "list": [1] }""");

        var merged = SettingsMerger.Merge(new JsonObject(), recommended, ["list"]);
        merged["list"]!.AsArray().Add(2);

        Assert.Single(recommended["list"]!.AsArray());
    }

    [Fact]
    public void Merge_WithKeyThatIsNotRecommended_Throws()
    {
        Assert.Throws<ArgumentException>(() => SettingsMerger.Merge(new JsonObject(), new JsonObject(), ["unknown"]));
    }

    [Fact]
    public void Serialize_KeepsTextReadableAndRoundTrips()
    {
        var settings = SettingsMerger.Parse("""
            {
                "[al]": { "editor.defaultFormatter": "ms-dynamics-smb.al" },
                "alOutline.pageFieldToolTipComment": "de-DE=Gibt den Wert des Feldes %Caption.Comment% an. äöü",
                "todo.embedded.providers.ag.regex": "(?:#|// @|//|/\\*+|<!--) *(TODO)"
            }
            """);

        var json = SettingsMerger.Serialize(settings);

        Assert.Contains("\"[al]\"", json);
        Assert.Contains("an. äöü", json);
        Assert.Contains("""/\\*+|<!--""", json);
        Assert.True(JsonNode.DeepEquals(settings, SettingsMerger.Parse(json)));
    }

    [Fact]
    public void BundledReferenceSettings_AreValid()
    {
        var settings = SettingsMerger.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Resources", "settings-install.json")));

        Assert.NotEmpty(settings);
    }
}
