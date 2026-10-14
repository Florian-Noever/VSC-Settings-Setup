using Spectre.Console;

namespace VSC_Settings_Setup;

public static class AnsiConsoleMessageExtensions
{
    public static void Success(this IAnsiConsole console, string message) => console.MarkupLineInterpolated($"[green]{message}[/]");
    public static void Warning(this IAnsiConsole console, string message) => console.MarkupLineInterpolated($"[yellow]{message}[/]");
    public static void Error(this IAnsiConsole console, string message) => console.MarkupLineInterpolated($"[red]{message}[/]");
}
