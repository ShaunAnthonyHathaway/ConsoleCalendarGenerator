using System.Globalization;

namespace ConsoleCalendarGenerator;

/// <summary>Specifies which of a culture's names for the days of the week label the columns of a rendered month.</summary>
public enum WeekdayLabelStyle
{
    /// <summary>
    /// The culture's shortest day names, as returned by <see cref="DateTimeFormatInfo.GetShortestDayName"/>. For
    /// the invariant culture these are <c>Su Mo Tu We Th Fr Sa</c>.
    /// </summary>
    Shortest = 0,

    /// <summary>
    /// The culture's abbreviated day names, as returned by <see cref="DateTimeFormatInfo.GetAbbreviatedDayName"/>.
    /// For the invariant culture these are <c>Sun Mon Tue Wed Thu Fri Sat</c>.
    /// </summary>
    Abbreviated = 1,
}
