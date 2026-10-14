using System.Text;

using Spectre.Console;

using VSC_Settings_Setup.Services;

namespace VSC_Settings_Setup;

public static class Program
{
    private const string TempFolderName = "VSC-Settings-Setup";

    public static async Task<int> Main()
    {
        var previousEncoding = Console.OutputEncoding;
        // Must be set before the first use of AnsiConsole so Spectre detects Unicode support (spinners, borders, non-ASCII names).
        Console.OutputEncoding = Encoding.UTF8;
        var console = AnsiConsole.Console;
        try
        {
            return await RunAsync(console);
        }
        catch (Exception ex)
        {
            console.WriteException(ex, ExceptionFormats.ShortenEverything);
            return 1;
        }
        finally
        {
            console.WriteLine();
            console.WriteLine("Press any key to exit.");
            if (!Console.IsInputRedirected)
                Console.ReadKey(intercept: true);
            Console.OutputEncoding = previousEncoding;
        }
    }

    private static async Task<int> RunAsync(IAnsiConsole console)
    {
        console.Write(new Rule("[bold]VS Code Settings Setup[/]").LeftJustified());
        console.WriteLine();

        var (settings, warnings) = AppSettingsLoader.Load(AppContext.BaseDirectory);
        foreach (var warning in warnings)
        {
            console.Warning(warning);
        }

        var tempDirectory = Path.Combine(Path.GetTempPath(), TempFolderName);
        using var httpClient = new HttpClient();
        var prerequisites = new PrerequisiteProvider(httpClient, settings.AllowManualOverride, AppContext.BaseDirectory, tempDirectory);
        var installer = new VSCodeInstaller(httpClient, tempDirectory);
        return await new SetupWorkflow(console, settings, prerequisites, installer).RunAsync();
    }
}
