using System.Globalization;
using System.Text;

namespace ConsoleCalendarGenerator.Tests.Support;

/// <summary>One call on a <see cref="CallRecordingWriter"/>, with the console colors in effect at that moment.</summary>
/// <param name="Method">Which method was called, as <see cref="CallRecordingWriter"/> abbreviates it.</param>
/// <param name="Text">The text passed to the call, without any line terminator the writer adds.</param>
/// <param name="EndsLine">True for the <c>WriteLine</c> methods, which append the writer's own line terminator.</param>
/// <param name="Foreground">The value of <see cref="Console.ForegroundColor"/> when the call was made.</param>
/// <param name="Background">The value of <see cref="Console.BackgroundColor"/> when the call was made.</param>
internal readonly record struct RecordedCall(string Method, string Text, bool EndsLine, ConsoleColor Foreground, ConsoleColor Background);

/// <summary>
/// A <see cref="TextWriter"/> for <see cref="Console.SetOut"/> that records which method was called for every
/// write, not only the text: <c>Write(string)</c> as <c>S</c>, <c>Write(int)</c> as <c>I</c>,
/// <c>WriteLine()</c> as <c>L</c>, <c>WriteLine(string)</c> as <c>LS</c>, and anything else as <c>?</c>. A
/// program whose own writer overrides those methods can tell them apart, so they are part of what the legacy
/// class is observed to do. The console colors in effect are sampled on every call.
/// </summary>
internal sealed class CallRecordingWriter : TextWriter
{
    private readonly StringBuilder _calls = new();
    private readonly StringBuilder _colors = new();
    private readonly List<RecordedCall> _recorded = new();

    public CallRecordingWriter(string newLine)
    {
        NewLine = newLine;
    }

    public override Encoding Encoding => Encoding.UTF8;

    /// <summary>Every call in order, as <c>kind[text]</c>.</summary>
    public string Calls => _calls.ToString();

    /// <summary>The foreground and background color in effect for every call, in order.</summary>
    public string Colors => _colors.ToString();

    /// <summary>Every call in order, one entry each, for code that compares calls one by one.</summary>
    public IReadOnlyList<RecordedCall> Recorded => _recorded;

    public int Count { get; private set; }

    public override void Write(string? value) => Record("S", value ?? "<null>");

    public override void Write(int value) => Record("I", value.ToString(CultureInfo.InvariantCulture));

    public override void WriteLine() => Record("L", string.Empty, endsLine: true);

    public override void WriteLine(string? value) => Record("LS", value ?? "<null>", endsLine: true);

    public override void Write(char value) => Record("?char", value.ToString());

    public override void Write(char[] buffer, int index, int count) => Record("?chars", new string(buffer, index, count));

    public override void Write(ReadOnlySpan<char> buffer) => Record("?span", buffer.ToString());

    public override void Write(object? value) => Record("?object", Convert.ToString(value, CultureInfo.InvariantCulture) ?? "<null>");

    public override void WriteLine(int value) => Record("?LI", value.ToString(CultureInfo.InvariantCulture), endsLine: true);

    public override void WriteLine(object? value) => Record("?Lobject", Convert.ToString(value, CultureInfo.InvariantCulture) ?? "<null>", endsLine: true);

    private void Record(string kind, string text, bool endsLine = false)
    {
        Count++;
        ConsoleColor foreground = Console.ForegroundColor;
        ConsoleColor background = Console.BackgroundColor;
        _calls.Append(kind).Append('[').Append(text.Replace("\r", "\\r").Replace("\n", "\\n")).Append(']');
        _colors.Append((int)foreground).Append('/').Append((int)background).Append(' ');
        _recorded.Add(new RecordedCall(kind, text, endsLine, foreground, background));
    }
}
