using VSC_Settings_Setup.Data;

namespace VSC_Settings_Setup.Services;

public static class ExtensionPlanner
{
    public static IReadOnlyList<VSCodeExtension> GetMissing(IEnumerable<VSCodeExtension> recommended, IEnumerable<string> installedIds)
    {
        var installed = installedIds.ToHashSet(StringComparer.OrdinalIgnoreCase);
        return recommended.Where(extension => !installed.Contains(extension.Id)).ToList();
    }

    public static IReadOnlyList<VSCodeExtension> GetInstalled(IEnumerable<VSCodeExtension> extensions, IEnumerable<string> installedIds)
    {
        var installed = installedIds.ToHashSet(StringComparer.OrdinalIgnoreCase);
        return extensions.Where(extension => installed.Contains(extension.Id)).ToList();
    }
}
