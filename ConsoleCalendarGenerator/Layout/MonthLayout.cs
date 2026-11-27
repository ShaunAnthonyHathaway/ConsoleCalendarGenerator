using System;
using System.Collections.Generic;

namespace ConsoleCalendarGenerator;

/// <summary>
/// One month of the Gregorian calendar arranged as a grid: weeks from top to bottom, each with seven cells, for
/// a chosen first day of the week.
/// </summary>
/// <remarks>
/// <para>
/// A layout describes position only: which dates belong to the month, and the week and weekday column each of
/// them falls in. It contains no text and does not use the console.
/// </para>
/// <para>
/// A layout depends on nothing but the year, the month and the first day of the week it was created for. The
/// current culture, the console and the operating system have no effect on it.
/// </para>
/// <para>
/// Instances are immutable and can be used from several threads at once.
/// </para>
/// </remarks>
public sealed class MonthLayout
{
    private const int DaysPerWeek = 7;

    private MonthLayout(
        int year,
        int month,
        DayOfWeek firstDayOfWeek,
        IReadOnlyList<DayOfWeek> daysOfWeek,
        IReadOnlyList<MonthLayoutWeek> weeks)
    {
        Year = year;
        Month = month;
        FirstDayOfWeek = firstDayOfWeek;
        DaysOfWeek = daysOfWeek;
        Weeks = weeks;
    }

    /// <summary>Gets the year of the month, from 1 through 9999.</summary>
    public int Year { get; }

    /// <summary>Gets the month, from 1 (January) through 12 (December).</summary>
    public int Month { get; }

    /// <summary>Gets the day of the week in the first column of every week.</summary>
    public DayOfWeek FirstDayOfWeek { get; }

    /// <summary>
    /// Gets the day of the week of each column, in column order. There are always seven, starting with
    /// <see cref="FirstDayOfWeek"/>.
    /// </summary>
    /// <remarks>
    /// The element at an index is the day of the week of the cell at the same index in the
    /// <see cref="MonthLayoutWeek.Cells"/> of every week.
    /// </remarks>
    public IReadOnlyList<DayOfWeek> DaysOfWeek { get; }

    /// <summary>
    /// Gets the weeks of the month in order, the week that contains the 1st first. There are four, five or six,
    /// and never more than the month needs: the first and the last week each contain at least one date.
    /// </summary>
    public IReadOnlyList<MonthLayoutWeek> Weeks { get; }

    /// <summary>Creates the layout of a month for a given first day of the week.</summary>
    /// <param name="year">The year, from 1 through 9999.</param>
    /// <param name="month">The month, from 1 (January) through 12 (December).</param>
    /// <param name="firstDayOfWeek">The day of the week to place in the first column.</param>
    /// <returns>The layout of the month.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="year"/> is less than 1 or greater than 9999, <paramref name="month"/> is less than 1 or
    /// greater than 12, or <paramref name="firstDayOfWeek"/> is not one of the seven <see cref="DayOfWeek"/>
    /// values. The arguments are checked in that order.
    /// </exception>
    public static MonthLayout Create(int year, int month, DayOfWeek firstDayOfWeek)
    {
        if (year < 1 || year > 9999)
        {
            throw new ArgumentOutOfRangeException(nameof(year), year, "The year must be from 1 through 9999.");
        }
        if (month < 1 || month > 12)
        {
            throw new ArgumentOutOfRangeException(nameof(month), month, "The month must be from 1 through 12.");
        }
        if (firstDayOfWeek < DayOfWeek.Sunday || firstDayOfWeek > DayOfWeek.Saturday)
        {
            throw new ArgumentOutOfRangeException(
                nameof(firstDayOfWeek), firstDayOfWeek, "The first day of the week must be a valid day of the week.");
        }

        var daysOfWeek = new DayOfWeek[DaysPerWeek];
        for (int column = 0; column < DaysPerWeek; column++)
        {
            daysOfWeek[column] = (DayOfWeek)(((int)firstDayOfWeek + column) % DaysPerWeek);
        }

        int daysInMonth = DateTime.DaysInMonth(year, month);
        // The number of empty cells before the 1st, which is also the column the 1st is in.
        int columnOfFirstDay = ((int)new DateTime(year, month, 1).DayOfWeek - (int)firstDayOfWeek + DaysPerWeek) % DaysPerWeek;
        int weekCount = (columnOfFirstDay + daysInMonth + DaysPerWeek - 1) / DaysPerWeek;

        var weeks = new MonthLayoutWeek[weekCount];
        for (int week = 0; week < weekCount; week++)
        {
            var cells = new MonthLayoutCell[DaysPerWeek];
            for (int column = 0; column < DaysPerWeek; column++)
            {
                int day = week * DaysPerWeek + column - columnOfFirstDay + 1;
                // Only days of this month are ever turned into dates, so the first and the last month that
                // DateTime can represent need no special handling.
                DateTime? date = day >= 1 && day <= daysInMonth ? new DateTime(year, month, day) : (DateTime?)null;
                cells[column] = new MonthLayoutCell(daysOfWeek[column], date);
            }
            weeks[week] = new MonthLayoutWeek(Array.AsReadOnly(cells));
        }

        // The arrays are handed out only inside read-only wrappers, so a caller cannot get at them to change them.
        return new MonthLayout(year, month, firstDayOfWeek, Array.AsReadOnly(daysOfWeek), Array.AsReadOnly(weeks));
    }
}
