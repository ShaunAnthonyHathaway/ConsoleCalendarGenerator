using ConsoleCalendarGenerator.Tests.Support;

namespace ConsoleCalendarGenerator.Tests.Rendering;

/// <summary>
/// Text the caller supplies: weekday labels, an indent, a title suffix, the literal text of a title format and
/// the marker of a state. It is put into the rendered month as it is. A line break is refused, and nothing
/// else is examined, removed or escaped, control characters included. The XML comments and SECURITY.md say so,
/// and this is what they rest on.
/// </summary>
[TestFixture]
public class SuppliedTextTests
{
    private static readonly string Escape = ((char)27).ToString();
    private static readonly string Bell = ((char)7).ToString();

    private static MonthRenderOptions Options() => MonthRenderOptions.Default
        .WithIndent(Escape + "[1m")
        .WithTitleSuffix(" " + Bell + "\t!")
        .WithTitleFormat("'" + Escape + "x' MMMM")
        .WithCellWidth(4)
        .WithWeekdayLabels(new[] { Escape + "Su", "Mo", "Tu", "We", "Th", "Fr", "Sa" });

    [Test]
    public void SuppliedText_IsInTheRenderedMonthAsItIs_ControlCharactersIncluded()
    {
        RenderedMonth month = MonthRenderer.Render(2027, 3, Options(), new[] { new DateTime(2027, 3, 5) }, new DayState("marked", Bell));
        string text = month.ToText();

        Assert.That(text, Does.StartWith(Escape + "[1m").Using(StringComparison.Ordinal), "the indent");
        Assert.That(text, Ordinal.Contains(Escape + "x March " + Bell + "\t!"), "the literal text of the title format, then the suffix");
        Assert.That(text, Ordinal.Contains(Escape + "Su"), "a weekday label");
        Assert.That(text, Ordinal.Contains(Bell + "5"), "a marker, directly before the day");
        Assert.That(month.Lines[0].Segments[0].Kind, Is.EqualTo(RenderedSegmentKind.Indent));
        Assert.That(month.Lines[0].Segments[0].Text, Is.EqualTo(Escape + "[1m"), "and so is the text of the segment");
    }

    [Test]
    public void TheWriters_WriteThatTextExactly()
    {
        RenderedMonth month = MonthRenderer.Render(2027, 3, Options(), new[] { new DateTime(2027, 3, 5) }, new DayState("marked", Bell));
        using var writer = new StringWriter { NewLine = "\n" };

        MonthWriter.Write(month, writer);
        string console = ConsoleCapture.Run(() => ConsoleMonthWriter.Write(month, CalendarTheme.None)).Text;

        Assert.That(writer.ToString(), Is.EqualTo(month.ToText() + "\n"));
        Assert.That(console, Is.EqualTo(month.ToText() + "\n"));
    }

    /// <summary>The one thing that is refused is a character that ends a line, in every place text can be supplied.</summary>
    [TestCase(10)]
    [TestCase(13)]
    [TestCase(12)]
    [TestCase(0x85)]
    [TestCase(0x2028)]
    [TestCase(0x2029)]
    public void ALineBreakInSuppliedText_IsRefused(int codePoint)
    {
        string broken = "a" + (char)codePoint + "b";

        Assert.That(() => MonthRenderOptions.Default.WithIndent(broken), Throws.ArgumentException);
        Assert.That(() => MonthRenderOptions.Default.WithTitleSuffix(broken), Throws.ArgumentException);
        Assert.That(() => MonthRenderOptions.Default.WithTitleFormat(broken), Throws.ArgumentException);
        Assert.That(() => MonthRenderOptions.Default.WithWeekdayLabels(new[] { broken, "Mo", "Tu", "We", "Th", "Fr", "Sa" }), Throws.ArgumentException);
        Assert.That(() => new DayState("marked", broken), Throws.ArgumentException);
        Assert.That(() => new DayState(broken, "*"), Throws.ArgumentException);
    }
}
