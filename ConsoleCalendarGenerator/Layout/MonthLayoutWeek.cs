using System.Collections.Generic;

namespace ConsoleCalendarGenerator;

/// <summary>One week of a <see cref="MonthLayout"/>: a row of seven cells.</summary>
/// <remarks>Instances are immutable and are created only by <see cref="MonthLayout.Create"/>.</remarks>
public sealed class MonthLayoutWeek
{
    internal MonthLayoutWeek(IReadOnlyList<MonthLayoutCell> cells)
    {
        Cells = cells;
    }

    /// <summary>
    /// Gets the cells of the week in column order. There are always seven, one for each day of the week,
    /// starting with the layout's <see cref="MonthLayout.FirstDayOfWeek"/>.
    /// </summary>
    /// <remarks>
    /// A cell is empty where the week reaches outside the month: before the 1st in the first week and after the
    /// last day of the month in the last week.
    /// </remarks>
    public IReadOnlyList<MonthLayoutCell> Cells { get; }
}
