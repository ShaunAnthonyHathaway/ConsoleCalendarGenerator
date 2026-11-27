using System.Text;

namespace ConsoleCalendarGenerator.Tests.Support;

/// <summary>
/// One call the library made on <see cref="Console.Out"/>, with the console colors in effect at that moment.
/// </summary>
/// <param name="Payload">The text passed to the call, without any line terminator the writer adds.</param>
/// <param name="EndsLine">True for <c>WriteLine</c> calls, which append the writer's own newline.</param>
/// <param name="Foreground">The value of <see cref="Console.ForegroundColor"/> when the call was made.</param>
/// <param name="Background">The value of <see cref="Console.BackgroundColor"/> when the call was made.</param>
internal readonly record struct WriteEvent(string Payload, bool EndsLine, ConsoleColor Foreground, ConsoleColor Background);

/// <summary>
/// A <see cref="TextWriter"/> for <see cref="Console.SetOut"/> that keeps every write as a separate event. It
/// tells <c>WriteLine</c> calls apart from literal newline characters, which is what makes the library's mixed
/// line endings visible, and it samples the console colors on every call.
/// </summary>
internal sealed class RecordingWriter : TextWriter
{
    private readonly List<WriteEvent> _events = new();

    public RecordingWriter(string newLine)
    {
        NewLine = newLine;
    }

    public override Encoding Encoding => Encoding.UTF8;

    public IReadOnlyList<WriteEvent> Events => _events;

    /// <summary>Everything written, with this writer's newline after each <c>WriteLine</c> call.</summary>
    public string Text => string.Concat(_events.Select(e => e.EndsLine ? e.Payload + NewLine : e.Payload));

    public override void Write(char value) => Record(value.ToString(), endsLine: false);

    public override void Write(char[] buffer, int index, int count) => Record(new string(buffer, index, count), endsLine: false);

    public override void Write(string? value) => Record(value ?? string.Empty, endsLine: false);

    public override void WriteLine() => Record(string.Empty, endsLine: true);

    public override void WriteLine(string? value) => Record(value ?? string.Empty, endsLine: true);

    private void Record(string payload, bool endsLine) =>
        _events.Add(new WriteEvent(payload, endsLine, Console.ForegroundColor, Console.BackgroundColor));
}
