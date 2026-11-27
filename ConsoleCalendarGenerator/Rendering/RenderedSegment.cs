using System;

namespace ConsoleCalendarGenerator;

/// <summary>
/// One piece of a <see cref="RenderedLine"/>: some text, and what that text is.
/// </summary>
/// <remarks>
/// <para>
/// A segment carries meaning, not appearance. Code that colors or writes a rendered month reads
/// <see cref="Kind"/>, <see cref="Date"/> and <see cref="DayOfWeek"/>; it never needs to parse <see cref="Text"/>.
/// </para>
/// <para>Instances are immutable and are created only by <see cref="MonthRenderer"/>.</para>
/// </remarks>
public sealed class RenderedSegment
{
    internal RenderedSegment(RenderedSegmentKind kind, string text, DayOfWeek? dayOfWeek = null, DateTime? date = null)
    {
        Kind = kind;
        Text = text;
        DayOfWeek = dayOfWeek;
        Date = date;
    }

    /// <summary>Gets what this segment is.</summary>
    public RenderedSegmentKind Kind { get; }

    /// <summary>
    /// Gets the text of the segment, exactly as it appears in the line. It is never <see langword="null"/> or
    /// empty and never contains a line break.
    /// </summary>
    /// <remarks>
    /// For a <see cref="RenderedSegmentKind.WeekdayLabel"/>, <see cref="RenderedSegmentKind.EmptyDay"/> or
    /// <see cref="RenderedSegmentKind.Day"/> segment this is the whole cell, including the spaces that pad it to
    /// the cell width.
    /// </remarks>
    public string Text { get; }

    /// <summary>
    /// Gets the day of the week of the column this segment belongs to, or <see langword="null"/> for a segment
    /// that belongs to no column.
    /// </summary>
    /// <remarks>
    /// Set for <see cref="RenderedSegmentKind.WeekdayLabel"/>, <see cref="RenderedSegmentKind.EmptyDay"/> and
    /// <see cref="RenderedSegmentKind.Day"/> segments.
    /// </remarks>
    public DayOfWeek? DayOfWeek { get; }

    /// <summary>
    /// Gets the date shown by a <see cref="RenderedSegmentKind.Day"/> segment, or <see langword="null"/> for
    /// every other kind.
    /// </summary>
    /// <remarks>The date is at midnight with <see cref="DateTimeKind.Unspecified"/>, as in <see cref="MonthLayoutCell.Date"/>.</remarks>
    public DateTime? Date { get; }

    /// <summary>Returns <see cref="Text"/>.</summary>
    /// <returns>The text of the segment.</returns>
    public override string ToString() => Text;
}
