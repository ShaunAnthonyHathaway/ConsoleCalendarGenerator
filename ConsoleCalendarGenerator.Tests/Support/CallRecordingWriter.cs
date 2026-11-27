using System.Globalization;
using System.Text;

namespace ConsoleCalendarGenerator.Tests.Support;

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

    public CallRecordingWriter(string newLine)
    {
        NewLine = newLine;
    }

    public override Encoding Encoding => Encoding.UTF8;

    /// <summary>Every call in order, as <c>kind[text]</c>.</summary>
    public string Calls => _calls.ToString();

    /// <summary>The foreground and background color in effect for every call, in order.</summary>
    public string Colors => _colors.ToString();

    public int Count { get; private set; }

    public override void Write(string? value) => Record("S", value ?? "<null>");

    public override void Write(int value) => Record("I", value.ToString(CultureInfo.InvariantCulture));

    public override void WriteLine() => Record("L", string.Empty);

    public override void WriteLine(string? value) => Record("LS", value ?? "<null>");

    public override void Write(char value) => Record("?char", value.ToString());

    public override void Write(char[] buffer, int index, int count) => Record("?chars", new string(buffer, index, count));

    public override void Write(ReadOnlySpan<char> buffer) => Record("?span", buffer.ToString());

    public override void Write(object? value) => Record("?object", Convert.ToString(value, CultureInfo.InvariantCulture) ?? "<null>");

    public override void WriteLine(int value) => Record("?LI", value.ToString(CultureInfo.InvariantCulture));

    public override void WriteLine(object? value) => Record("?Lobject", Convert.ToString(value, CultureInfo.InvariantCulture) ?? "<null>");

    private void Record(string kind, string text)
    {
        Count++;
        _calls.Append(kind).Append('[').Append(text.Replace("\r", "\\r").Replace("\n", "\\n")).Append(']');
        _colors.Append((int)Console.ForegroundColor).Append('/').Append((int)Console.BackgroundColor).Append(' ');
    }
}
