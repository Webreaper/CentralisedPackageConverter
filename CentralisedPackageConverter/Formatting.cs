using System.Text;

namespace CentralisedPackageConverter;

/// <summary>
/// Text formats of files.
/// </summary>
public static class Formatting
{
    private static readonly IReadOnlyDictionary<string, string> LineWrapOptions =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "cr", "\r" },
            { "lf", "\n" },
            { "crlf", "\r\n" },
        };

    /// <summary>
    /// Get the encoding to use when writing files, by web name or default.
    /// </summary>
    /// <param name="encodingWebName">The web name, as in <see cref="Encoding.WebName"/>.</param>
    public static Encoding GetCommonEncoding(string? encodingWebName)
    {
        var encoding = !string.IsNullOrWhiteSpace(encodingWebName)
            ? Encoding.GetEncoding(encodingWebName)
            : Encoding.Default;
        Console.WriteLine("Writing files with encoding: " + encoding.WebName);
        return encoding;
    }

    /// <summary>
    /// Create actual line wrap from command line options value.
    /// <list type="bullet">
    /// <item>"cr" => "\r" (Mac)</item>
    /// <item>"lf" => "\n" (Unix)</item>
    /// <item>"crlf" => "\r\n" (Windows)</item>
    /// </list>
    /// </summary>
    /// <param name="cmdlineValue">Value passed with -l option.</param>
    public static string GetLineWrap(string? cmdlineValue)
    {
        if (string.IsNullOrEmpty(cmdlineValue))
        {
            return Environment.NewLine;
        }

        if (LineWrapOptions.TryGetValue(cmdlineValue, out var lineWrap))
        {
            return lineWrap;
        }

        throw new InvalidOperationException("Invalid -l --linewrap option value. Possible are: " +
                                            string.Join(",", LineWrapOptions.Keys.Select(o => $"\"{o}\"")));
    }

    /// <summary>
    /// Set all line wraps in <paramref name="text"/> to <paramref name="lineWrap" />.
    /// </summary>
    /// <param name="text">Where to format line wraps.</param>
    /// <param name="lineWrap">The line wrap character(s).</param>
    public static string FormatLineWraps(string text, string lineWrap)
    {
        return new StringBuilder(text)
            .Replace("\r\n", "\n")
            .Replace("\r", "\n") // Normalize Windows, Mac to Unix, to get distinct, single char line wraps.
            .Replace("\n", lineWrap) // replace normalized line wraps by requested.
            .ToString();
    }
}