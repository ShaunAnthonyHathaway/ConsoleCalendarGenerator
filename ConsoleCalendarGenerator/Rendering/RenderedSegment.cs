using System;

namespace ConsoleCalendarGenerator;

/// <summary>
/// One piece of a <see cref="RenderedLine"/>: some text, and what that text is.
/// </summary>
/// <remarks>
/// <para>
/// A segment carries meaning, not appearance. Code that colors or writes a rendered month reads
/// <see cref="Kind"/>, <see cref="Date"/>, <see cref="DayOfWeek"/> and <see cref="State"/>; it never needs to
/// parse <see cref="Text"/>.
/// </para>
/// <para>Instances are immutable and are created only by <see cref="MonthRenderer"/>.</para>
/// </remarks>
public sealed class RenderedSegment
{
    internal RenderedSegment(
        RenderedSegmentKind kind, string text, DayOfWeek? dayOfWeek = null, DateTime? date = null, DayState? state = null)
    {
        Kind = kind;
        Text = text;
        DayOfWeek = dayOfWeek;
        Date = date;
        State = state;
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
    /// the cell width. The cell of a day with a <see cref="State"/> also contains the state's marker, directly
    /// before the day number.
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

    /// <summary>
    /// Gets the state of the day shown by a <see cref="RenderedSegmentKind.Day"/> segment, or
    /// <see langword="null"/> when the day has no state. It is always <see langword="null"/> for every other
    /// kind of segment, and for every segment of a month rendered without states.
    /// </summary>
    /// <remarks>This is the instance that was passed to <see cref="MonthRenderer"/>.</remarks>
    public DayState? State { get; }

    /// <summary>Returns <see cref="Text"/>.</summary>
    /// <returns>The text of the segment.</returns>
    public override string ToString() => Text;
}
