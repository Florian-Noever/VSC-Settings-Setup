using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;

using VSC_Settings_Setup.Data;

namespace VSC_Settings_Setup.Services;

public static class SettingsMerger
{
    // VS Code settings are JSON with comments and trailing commas. Duplicate keys are rejected because they can't be written back faithfully.
    private static readonly JsonDocumentOptions ReadOptions = new() { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true, AllowDuplicateProperties = false };

    // Relaxed escaping keeps non-ASCII text and characters like '<' or '+' readable for the user.
    private static readonly JsonSerializerOptions WriteOptions = new() { WriteIndented = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

    /// <exception cref="JsonException">The text is not a valid JSON object.</exception>
    public static JsonObject Parse(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return new JsonObject();

        return JsonNode.Parse(json, documentOptions: ReadOptions) as JsonObject ?? throw new JsonException("The settings must be a JSON object.");
    }

    public static IReadOnlyList<SettingChange> GetChanges(JsonObject current, JsonObject recommended) => recommended
        .Where(setting => !current.TryGetPropertyValue(setting.Key, out var value) || !JsonNode.DeepEquals(value, setting.Value))
        .Select(setting => new SettingChange(setting.Key, IsNew: !current.ContainsKey(setting.Key)))
        .ToList();

    /// <summary>Returns a copy of <paramref name="current"/> with the given keys taken from <paramref name="recommended"/>.</summary>
    public static JsonObject Merge(JsonObject current, JsonObject recommended, IEnumerable<string> keys)
    {
        var merged = current.DeepClone().AsObject();
        foreach (var key in keys)
        {
            if (!recommended.TryGetPropertyValue(key, out var value))
                throw new ArgumentException($"'{key}' is not a recommended setting.", nameof(keys));
            merged[key] = value?.DeepClone();
        }
        return merged;
    }

    public static string Serialize(JsonObject settings) => settings.ToJsonString(WriteOptions);
}
