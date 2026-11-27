using System.Text.RegularExpressions;

namespace ConsoleCalendarGenerator.Tests.Support;

/// <summary>Recognizes and removes the escape sequences a terminal receives, to look at the text between them.</summary>
internal static class TerminalOutput
{
    private static readonly string Escape = Regex.Escape(((char)27).ToString());

    /// <summary>A sequence that sets or resets a color.</summary>
    public static readonly Regex ColorSequence = new(Escape + @"\[[0-9;]*m");

    private static readonly Regex AnyEscapeSequence = new(Escape + @"\[[0-?]*[ -/]*[@-~]|" + Escape + "[=>]");

    public static string EscapeCharacter => ((char)27).ToString();

    public static string WithoutEscapeSequences(string output) => AnyEscapeSequence.Replace(output, string.Empty);

    /// <summary>A terminal turns "\n" into "\r\n"; removing the carriage returns gives back what the program wrote.</summary>
    public static string WithoutTerminalControl(string output) => WithoutEscapeSequences(output).Replace("\r", string.Empty);
}
