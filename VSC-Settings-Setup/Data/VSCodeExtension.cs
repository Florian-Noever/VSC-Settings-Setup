using System.Text.RegularExpressions;

namespace VSC_Settings_Setup.Data;

public sealed partial record VSCodeExtension
{
    public required string Name { get; init; }
    public required string Publisher { get; init; }
    public string DisplayName { get; init; } = string.Empty;
    public string Author { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public bool PreRelease { get; init; }

    public string Id => $"{Publisher}.{Name}";

    public static bool IsValidId(string id) => IdPattern().IsMatch(id);

    // Same naming rules as the VS Code marketplace (vsce). This also guarantees IDs are safe to pass to code.cmd, which runs through cmd.exe.
    [GeneratedRegex(@"^[a-z0-9][a-z0-9-]*\.[a-z0-9][a-z0-9-]*\z", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex IdPattern();
}
