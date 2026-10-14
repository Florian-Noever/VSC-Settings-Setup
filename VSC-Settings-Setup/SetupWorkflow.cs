using System.Text.Json;
using System.Text.Json.Nodes;

using Spectre.Console;

using VSC_Settings_Setup.Data;
using VSC_Settings_Setup.Services;

namespace VSC_Settings_Setup;

public sealed class SetupWorkflow(IAnsiConsole console, AppSettings settings, PrerequisiteProvider prerequisites, VSCodeInstaller installer)
{
    private const int PageSize = 15;
    private static readonly string UserSettingsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Code", "User", "settings.json");

    /// <returns>The exit code of the application.</returns>
    public async Task<int> RunAsync()
    {
        var installDirectory = VSCodeLocator.FindInstallDirectory();
        if (installDirectory is not null)
        {
            console.Success($"VS Code is installed in {installDirectory}.");
        }
        else
        {
            console.Warning("VS Code was not found.");
            if (!console.Confirm("Do you want to install VS Code?"))
                return 0;

            installDirectory = await InstallVSCodeAsync();
            if (installDirectory is null)
                return 1;
        }

        var cli = new VSCodeCli(installDirectory);

        console.WriteLine();
        if (console.Confirm("Do you want to install recommended VS Code extensions?"))
            await InstallExtensionsAsync(cli);

        console.WriteLine();
        if (console.Confirm("Do you want to uninstall not recommended VS Code extensions?"))
            await UninstallExtensionsAsync(cli);

        console.WriteLine();
        if (console.Confirm("Do you want to update VS Code settings?"))
            await UpdateSettingsAsync();

        return 0;
    }

    private async Task<string?> InstallVSCodeAsync()
    {
        try
        {
            await console.Progress()
                .Columns(new TaskDescriptionColumn(), new ProgressBarColumn(), new PercentageColumn(), new DownloadedColumn(), new TransferSpeedColumn(), new RemainingTimeColumn())
                .StartAsync(context => installer.DownloadAsync(new ProgressTaskReporter(context.AddTask("Downloading VS Code"))));
            await console.Status().StartAsync("Installing VS Code...", _ => installer.InstallAsync());
        }
        catch (Exception ex) when (IsDownloadError(ex) || ex is ProcessFailedException)
        {
            console.Error($"Could not install VS Code: {ex.Message}");
            return null;
        }

        var installDirectory = VSCodeLocator.FindInstallDirectory();
        if (installDirectory is null)
            console.Error("VS Code was installed but could not be found.");
        else
            console.Success($"VS Code has been installed in {installDirectory}.");
        return installDirectory;
    }

    private async Task InstallExtensionsAsync(VSCodeCli cli)
    {
        var recommended = await LoadExtensionListAsync(new PrerequisiteFile("extensions-install.csv", settings.ExtToInstallUrl));
        if (recommended is null)
            return;
        var installedIds = await ListInstalledExtensionsAsync(cli);
        if (installedIds is null)
            return;

        var missing = ExtensionPlanner.GetMissing(recommended, installedIds);
        if (missing.Count == 0)
        {
            console.Success("All recommended extensions are already installed.");
            return;
        }

        var selected = PromptSelection("Which extensions do you want to install?", missing, FormatExtension);
        await ProcessExtensionsAsync(selected, "Installing", "Installed", extension => cli.InstallExtensionAsync(extension.Id, extension.PreRelease));
    }

    private async Task UninstallExtensionsAsync(VSCodeCli cli)
    {
        var notRecommended = await LoadExtensionListAsync(new PrerequisiteFile("extensions-uninstall.csv", settings.ExtToUninstallUrl));
        if (notRecommended is null)
            return;
        var installedIds = await ListInstalledExtensionsAsync(cli);
        if (installedIds is null)
            return;

        var installed = ExtensionPlanner.GetInstalled(notRecommended, installedIds);
        if (installed.Count == 0)
        {
            console.Success("None of the not recommended extensions are installed.");
            return;
        }

        var selected = PromptSelection("Which extensions do you want to uninstall?", installed, FormatExtension);
        await ProcessExtensionsAsync(selected, "Uninstalling", "Uninstalled", extension => cli.UninstallExtensionAsync(extension.Id));
    }

    private async Task UpdateSettingsAsync()
    {
        var file = new PrerequisiteFile("settings-install.json", settings.SettingsToInstallUrl);
        var path = await GetPrerequisiteAsync(file);
        if (path is null)
            return;
        var recommended = await ReadSettingsAsync(path);
        if (recommended is null)
            return;
        var current = await ReadSettingsAsync(UserSettingsPath);
        if (current is null)
            return;

        var changes = SettingsMerger.GetChanges(current, recommended);
        if (changes.Count == 0)
        {
            console.Success("All recommended settings are already applied.");
            return;
        }

        var selected = PromptSelection("Which settings do you want to update?", changes, FormatSettingChange);
        if (selected.Count == 0)
        {
            console.WriteLine("No settings selected.");
            return;
        }

        try
        {
            var merged = SettingsMerger.Merge(current, recommended, selected.Select(change => change.Key));
            Directory.CreateDirectory(Path.GetDirectoryName(UserSettingsPath)!);
            await File.WriteAllTextAsync(UserSettingsPath, SettingsMerger.Serialize(merged));
            console.Success($"Updated {selected.Count} setting(s) in {UserSettingsPath}.");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            console.Error($"Could not save the settings: {ex.Message}");
        }
    }

    private async Task<string?> GetPrerequisiteAsync(PrerequisiteFile file)
    {
        if (prerequisites.FindLocalOverride(file) is { } localPath)
        {
            console.MarkupLineInterpolated($"Using local file [blue]{localPath}[/].");
            return localPath;
        }

        try
        {
            return await console.Status().StartAsync($"Downloading {Markup.Escape(file.Name)}...", _ => prerequisites.DownloadAsync(file));
        }
        catch (Exception ex) when (IsDownloadError(ex))
        {
            console.Error($"Could not download {file.Name}: {ex.Message}");
            return null;
        }
    }

    private async Task<IReadOnlyList<VSCodeExtension>?> LoadExtensionListAsync(PrerequisiteFile file)
    {
        var path = await GetPrerequisiteAsync(file);
        if (path is null)
            return null;

        LoadResult<IReadOnlyList<VSCodeExtension>> result;
        try
        {
            result = ExtensionListReader.Read(path);
        }
        catch (Exception ex) when (ex is InvalidDataException or IOException or UnauthorizedAccessException)
        {
            console.Error($"Could not read {file.Name}: {ex.Message}");
            return null;
        }

        foreach (var warning in result.Warnings)
            console.Warning($"{file.Name}: {warning}");

        if (result.Value.Count == 0)
        {
            console.Warning($"{file.Name} does not contain any extensions.");
            return null;
        }
        return result.Value;
    }

    private async Task<IReadOnlyList<string>?> ListInstalledExtensionsAsync(VSCodeCli cli)
    {
        try
        {
            return await console.Status().StartAsync("Checking installed extensions...", _ => cli.ListExtensionsAsync());
        }
        catch (ProcessFailedException ex)
        {
            console.Error($"Could not list the installed extensions: {ex.Message}");
            return null;
        }
    }

    private async Task<JsonObject?> ReadSettingsAsync(string path)
    {
        try
        {
            return File.Exists(path) ? SettingsMerger.Parse(await File.ReadAllTextAsync(path)) : new JsonObject();
        }
        catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
        {
            console.Error($"Could not read {path}: {ex.Message}");
            return null;
        }
    }

    private List<T> PromptSelection<T>(string title, IReadOnlyList<T> choices, Func<T, string> format) where T : notnull
    {
        var prompt = new MultiSelectionPrompt<T>()
            .Title(title)
            .NotRequired()
            .PageSize(PageSize)
            .MoreChoicesText("[grey](Move up and down to reveal more entries)[/]")
            .InstructionsText("[grey](All entries are selected. Press [blue]<space>[/] to toggle an entry, [green]<enter>[/] to accept)[/]")
            .UseConverter(format)
            .AddChoices(choices);
        foreach (var choice in choices)
            prompt.Select(choice);
        return console.Prompt(prompt);
    }

    private async Task ProcessExtensionsAsync(IReadOnlyList<VSCodeExtension> extensions, string action, string completedAction, Func<VSCodeExtension, Task> operation)
    {
        if (extensions.Count == 0)
        {
            console.WriteLine("No extensions selected.");
            return;
        }

        var failures = 0;
        foreach (var extension in extensions)
        {
            var label = FormatExtension(extension);
            try
            {
                await console.Status().StartAsync($"{action} {label}...", _ => operation(extension));
                console.MarkupLine($"[green]{completedAction}[/] {label}");
            }
            catch (ProcessFailedException ex)
            {
                failures++;
                console.MarkupLine($"[red]{action} {label} failed:[/] {Markup.Escape(ex.Message)}");
            }
        }

        if (failures == 0)
            console.Success($"All selected extensions have been {completedAction.ToLowerInvariant()}.");
        else
            console.Warning($"{failures} of {extensions.Count} extension(s) could not be {completedAction.ToLowerInvariant()}.");
    }

    private static string FormatExtension(VSCodeExtension extension)
    {
        var label = string.IsNullOrWhiteSpace(extension.DisplayName)
            ? Markup.Escape(extension.Id)
            : $"{Markup.Escape(extension.DisplayName)} [grey]({Markup.Escape(extension.Id)})[/]";
        return extension.PreRelease ? $"{label} [yellow](pre-release)[/]" : label;
    }

    private static string FormatSettingChange(SettingChange change) => $"{Markup.Escape(change.Key)} [grey]({(change.IsNew ? "new" : "changed")})[/]";

    private static bool IsDownloadError(Exception exception) => exception is HttpRequestException or TaskCanceledException or IOException or UnauthorizedAccessException;

    private sealed class ProgressTaskReporter(ProgressTask task) : IProgress<DownloadProgress>
    {
        public void Report(DownloadProgress value)
        {
            if (value.TotalBytes is { } totalBytes)
                task.MaxValue = totalBytes;
            else
                task.IsIndeterminate = true;
            task.Value = value.BytesReceived;
        }
    }
}
