using System.Text;

namespace ConsoleCalendarGenerator.Tests.Parity.Campertron;

/// <summary>
/// A piece of a line as a consumer with its own styled output would hold it: text and an optional color. The
/// other properties say where the span came from, which a consumer may or may not keep.
/// </summary>
internal sealed record StyledSpan(string Text, ConsoleColor? Color, RenderedSegmentKind? Kind = null, DateTime? Date = null, string? StateName = null);

/// <summary>A line of <see cref="StyledSpan"/>s.</summary>
internal sealed record StyledSpanLine(IReadOnlyList<StyledSpan> Spans)
{
    public string Text => string.Concat(Spans.Select(span => span.Text));
}

/// <summary>
/// The small piece of code a consumer writes to put a rendered month into styled lines of its own. It is a test
/// aid and is not part of the package: it shows that the package's public model is enough to do this, and how
/// little it takes. It uses <see cref="RenderedLine.Kind"/>, the segments' <see cref="RenderedSegment.Kind"/>,
/// <see cref="RenderedSegment.Text"/>, <see cref="RenderedSegment.Date"/> and <see cref="RenderedSegment.State"/>,
/// and <see cref="CalendarTheme.GetColor"/>. It never looks inside a text to find out what it is.
/// </summary>
internal static class SegmentAdapter
{
    /// <summary>
    /// The title and the weekday row become one span each. A week becomes one span for everything before its
    /// first day (the indent, and empty cells with their gaps; it may be empty), then one span per day cell in
    /// the color the theme gives it, and one span per gap.
    /// </summary>
    public static IReadOnlyList<StyledSpanLine> ToStyledLines(RenderedMonth month, CalendarTheme theme)
    {
        var lines = new List<StyledSpanLine>();
        foreach (RenderedLine line in month.Lines)
        {
            if (line.Kind != RenderedLineKind.Week)
            {
                lines.Add(new StyledSpanLine(new[] { new StyledSpan(line.Text, null) }));
                continue;
            }

            var spans = new List<StyledSpan>();
            var beforeTheFirstDay = new StringBuilder();
            bool firstDaySeen = false;
            foreach (RenderedSegment segment in line.Segments)
            {
                if (!firstDaySeen && segment.Kind != RenderedSegmentKind.Day)
                {
                    beforeTheFirstDay.Append(segment.Text);
                    continue;
                }
                if (!firstDaySeen)
                {
                    spans.Add(new StyledSpan(beforeTheFirstDay.ToString(), null));
                    firstDaySeen = true;
                }
                spans.Add(new StyledSpan(segment.Text, theme.GetColor(segment), segment.Kind, segment.Date, segment.State?.Name));
            }
            lines.Add(new StyledSpanLine(spans));
        }
        return lines;
    }
}
