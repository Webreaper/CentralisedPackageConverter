using Spectre.Console;

namespace CentralisedPackageConverter;

/// <summary>
/// 3 levels of output for wrapping AnsiConsole.MarkupLine with standard styles.
/// </summary>
public static class Output
{
    public static void InfoLine(string message) => InfoLine(message, Array.Empty<object>());
    public static void InfoLine(string message, params object[] args) => AnsiConsole.MarkupLine($"{message}", args);

    public static void ErrorLine(string message) => ErrorLine(message, Array.Empty<object>());
    public static void ErrorLine(string message, params object[] args) => AnsiConsole.MarkupLine($"[red]{message}[/]", args);

    public static void TraceLine(string message) => TraceLine(message, Array.Empty<object>());
    public static void TraceLine(string message, params object[] args) => AnsiConsole.MarkupLine($"[grey]{message}[/]", args);

    public static void WarningLine(string message) => WarningLine(message, Array.Empty<object>());
    public static void WarningLine(string message, params object[] args) => AnsiConsole.MarkupLine($"[yellow]{message}[/]", args);
}
