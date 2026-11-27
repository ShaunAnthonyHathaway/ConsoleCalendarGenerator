using System.Globalization;
using System.Runtime.ExceptionServices;

namespace ConsoleCalendarGenerator.Tests.Support;

/// <summary>What a piece of code did to the console while <see cref="ConsoleCapture"/> was watching.</summary>
internal sealed class Captured
{
    public Captured(RecordingWriter writer, Exception? exception, ConsoleColor foregroundAfter, ConsoleColor backgroundAfter)
    {
        Events = writer.Events;
        Text = writer.Text;
        Exception = exception;
        ForegroundAfter = foregroundAfter;
        BackgroundAfter = backgroundAfter;
    }

    public IReadOnlyList<WriteEvent> Events { get; }

    /// <summary>The complete text, as a redirected console would have received it.</summary>
    public string Text { get; }

    /// <summary>The exception that ended the code, for <see cref="ConsoleCapture.TryRun"/>; otherwise null.</summary>
    public Exception? Exception { get; }

    public ConsoleColor ForegroundAfter { get; }

    public ConsoleColor BackgroundAfter { get; }

    /// <summary>
    /// The write calls in order: <c>[text]</c> for each call, followed by <c>&lt;NL&gt;</c> when the call was a
    /// <c>WriteLine</c>. A newline character inside a payload is shown as <c>\n</c>.
    /// </summary>
    public string Transcript => string.Concat(Events.Select(e =>
        (e.Payload.Length > 0 || !e.EndsLine ? "[" + e.Payload.Replace("\r", "\\r").Replace("\n", "\\n") + "]" : string.Empty)
        + (e.EndsLine ? "<NL>" : string.Empty)));

    /// <summary>The text grouped by the foreground color it was written in ("\n" for every line end).</summary>
    public IReadOnlyList<(ConsoleColor Color, string Text)> ForegroundRuns => Runs(e => e.Foreground);

    /// <summary>The text grouped by the background color it was written in ("\n" for every line end).</summary>
    public IReadOnlyList<(ConsoleColor Color, string Text)> BackgroundRuns => Runs(e => e.Background);

    private List<(ConsoleColor Color, string Text)> Runs(Func<WriteEvent, ConsoleColor> color)
    {
        var runs = new List<(ConsoleColor Color, string Text)>();
        foreach (WriteEvent e in Events)
        {
            string text = e.EndsLine ? e.Payload + "\n" : e.Payload;
            if (runs.Count > 0 && runs[^1].Color == color(e))
            {
                runs[^1] = (runs[^1].Color, runs[^1].Text + text);
            }
            else
            {
                runs.Add((color(e), text));
            }
        }
        return runs;
    }
}

/// <summary>
/// Runs code with <see cref="Console.Out"/> replaced by a <see cref="RecordingWriter"/> and a known culture, and
/// always puts the process-wide state back: the writer, both cultures and the console colors.
/// </summary>
internal static class ConsoleCapture
{
    /// <summary>Runs <paramref name="render"/> and returns what it wrote. An exception is rethrown.</summary>
    public static Captured Run(
        Action render,
        string newLine = "\n",
        CultureInfo? culture = null,
        CultureInfo? uiCulture = null,
        ConsoleColor? foregroundBefore = null,
        ConsoleColor? backgroundBefore = null)
    {
        Captured captured = TryRun(render, newLine, culture, uiCulture, foregroundBefore, backgroundBefore);
        if (captured.Exception != null)
        {
            ExceptionDispatchInfo.Capture(captured.Exception).Throw();
        }
        return captured;
    }

    /// <summary>
    /// Runs <paramref name="render"/> and returns what it wrote before it finished or threw. The exception, if
    /// any, is in <see cref="Captured.Exception"/>. The culture is the invariant culture unless one is given.
    /// </summary>
    public static Captured TryRun(
        Action render,
        string newLine = "\n",
        CultureInfo? culture = null,
        CultureInfo? uiCulture = null,
        ConsoleColor? foregroundBefore = null,
        ConsoleColor? backgroundBefore = null)
    {
        TextWriter originalOut = Console.Out;
        CultureInfo originalCulture = CultureInfo.CurrentCulture;
        CultureInfo originalUiCulture = CultureInfo.CurrentUICulture;
        var writer = new RecordingWriter(newLine);
        Exception? thrown = null;
        ConsoleColor foregroundAfter;
        ConsoleColor backgroundAfter;
        try
        {
            CultureInfo.CurrentCulture = culture ?? CultureInfo.InvariantCulture;
            CultureInfo.CurrentUICulture = uiCulture ?? CultureInfo.InvariantCulture;
            if (foregroundBefore.HasValue)
            {
                Console.ForegroundColor = foregroundBefore.Value;
            }
            if (backgroundBefore.HasValue)
            {
                Console.BackgroundColor = backgroundBefore.Value;
            }
            Console.SetOut(writer);
            try
            {
                render();
            }
            catch (Exception ex)
            {
                thrown = ex;
            }
            foregroundAfter = Console.ForegroundColor;
            backgroundAfter = Console.BackgroundColor;
        }
        finally
        {
            Console.SetOut(originalOut);
            CultureInfo.CurrentCulture = originalCulture;
            CultureInfo.CurrentUICulture = originalUiCulture;
            Console.ResetColor();
        }
        return new Captured(writer, thrown, foregroundAfter, backgroundAfter);
    }
}
