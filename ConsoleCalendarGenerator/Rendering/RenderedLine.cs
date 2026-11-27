using System.Collections.Generic;
using System.Linq;

namespace ConsoleCalendarGenerator;

/// <summary>One line of a <see cref="RenderedMonth"/>: an ordered list of segments.</summary>
/// <remarks>Instances are immutable and are created only by <see cref="MonthRenderer"/>.</remarks>
public sealed class RenderedLine
{
    internal RenderedLine(RenderedLineKind kind, IReadOnlyList<RenderedSegment> segments)
    {
        Kind = kind;
        Segments = segments;
        Text = string.Concat(segments.Select(segment => segment.Text));
    }

    /// <summary>Gets what this line shows.</summary>
    public RenderedLineKind Kind { get; }

    /// <summary>
    /// Gets the segments of the line from left to right. Their texts, joined in order, are exactly
    /// <see cref="Text"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A <see cref="RenderedLineKind.Title"/> line is the indent, padding that positions the title, the title and
    /// the title suffix. A <see cref="RenderedLineKind.WeekdayLabels"/> line is the indent and seven label cells
    /// with a gap between each two. A <see cref="RenderedLineKind.Week"/> line is the indent and one cell for
    /// each column up to the last one that holds a day, with a gap between each two.
    /// </para>
    /// <para>
    /// A segment that would be empty is left out: there is no indent segment without an indent, and no gap
    /// segment when the gap is zero. The empty cells after the last day of the month are left out as well, so a
    /// line never ends with padding. A title line with no title text at all has no segments.
    /// </para>
    /// </remarks>
    public IReadOnlyList<RenderedSegment> Segments { get; }

    /// <summary>Gets the complete text of the line. It contains no line break and has no line terminator.</summary>
    public string Text { get; }

    /// <summary>Returns <see cref="Text"/>.</summary>
    /// <returns>The text of the line.</returns>
    public override string ToString() => Text;
}
