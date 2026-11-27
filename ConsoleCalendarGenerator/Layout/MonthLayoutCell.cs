using System;

namespace ConsoleCalendarGenerator;

/// <summary>
/// One cell of a <see cref="MonthLayoutWeek"/>: a position in a weekday column that holds either a date of the
/// month or nothing.
/// </summary>
/// <remarks>Instances are immutable and are created only by <see cref="MonthLayout.Create"/>.</remarks>
public sealed class MonthLayoutCell
{
    internal MonthLayoutCell(DayOfWeek dayOfWeek, DateTime? date)
    {
        DayOfWeek = dayOfWeek;
        Date = date;
    }

    /// <summary>Gets the day of the week of the column this cell is in, whether or not the cell holds a date.</summary>
    public DayOfWeek DayOfWeek { get; }

    /// <summary>
    /// Gets the date in this cell, or <see langword="null"/> when the cell is empty.
    /// </summary>
    /// <remarks>
    /// The date is always a day of the layout's month, at midnight, with <see cref="DateTimeKind.Unspecified"/>.
    /// </remarks>
    public DateTime? Date { get; }

    /// <summary>
    /// Gets a value indicating whether the cell holds no date. Empty cells come only before the 1st and after
    /// the last day of the month.
    /// </summary>
    public bool IsEmpty => !Date.HasValue;
}
