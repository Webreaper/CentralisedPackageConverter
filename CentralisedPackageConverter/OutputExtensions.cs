using Spectre.Console;

namespace CentralisedPackageConverter;

/// <summary>
/// 3 levels of output for wrapping AnsiConsole.MarkupLine with standard styles.
/// </summary>
public static class Output
{
    public static void InfoLine(string message) => InfoLine(message, Array.Empty<object>());
    public static void InfoLine(string message, params object[] args) => AnsiConsole.WriteLine(message, args);

    public static void ErrorLine(string message) => ErrorLine(message, Array.Empty<object>());
    public static void ErrorLine(string message, params object[] args) => AnsiConsole.MarkupLineInterpolated($"[red]{string.Format(message, args)}[/]");

    public static void TraceLine(string message) => TraceLine(message, Array.Empty<object>());
    public static void TraceLine(string message, params object[] args) => AnsiConsole.MarkupLineInterpolated($"[grey]{string.Format(message, args)}[/]");

    public static void WarningLine(string message) => WarningLine(message, Array.Empty<object>());
    public static void WarningLine(string message, params object[] args) => AnsiConsole.MarkupLineInterpolated($"[yellow]{string.Format(message, args)}[/]");
}
