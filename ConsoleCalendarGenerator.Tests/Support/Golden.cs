namespace ConsoleCalendarGenerator.Tests.Support;

/// <summary>Writes expected console output one line per argument, so that it reads like the output.</summary>
internal static class Golden
{
    /// <summary>Every argument followed by "\n". An empty argument is a blank line.</summary>
    public static string Lines(params string[] lines) => string.Concat(lines.Select(line => line + "\n"));
}
