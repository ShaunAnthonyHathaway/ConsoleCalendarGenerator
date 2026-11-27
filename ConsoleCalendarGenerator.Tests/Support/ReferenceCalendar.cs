namespace ConsoleCalendarGenerator.Tests.Support;

/// <summary>
/// The Gregorian calendar and a month's grid worked out with whole-number arithmetic alone, so that the layout
/// can be compared with something that shares nothing with it. It uses no <see cref="DateTime"/> and no calendar
/// class: the leap-year rule and the month lengths are written out, weekdays are counted from 0001-01-01 (a
/// Monday), and the grid is filled one day at a time rather than computed from the position of the 1st.
/// </summary>
internal static class ReferenceCalendar
{
    public const int FirstYear = 1;
    public const int LastYear = 9999;

    public static readonly IReadOnlyList<DayOfWeek> AllDaysOfWeek = new[]
    {
        DayOfWeek.Sunday, DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday,
        DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday,
    };

    public static bool IsLeapYear(int year) => year % 4 == 0 && (year % 100 != 0 || year % 400 == 0);

    public static int DaysInMonth(int year, int month) => month switch
    {
        2 => IsLeapYear(year) ? 29 : 28,
        4 or 6 or 9 or 11 => 30,
        _ => 31,
    };

    /// <summary>The number of days from 0001-01-01 to the given date; 0 for 0001-01-01 itself.</summary>
    public static int DaysSinceFirstDay(int year, int month, int day)
    {
        int completeYears = year - 1;
        int days = completeYears * 365 + completeYears / 4 - completeYears / 100 + completeYears / 400;
        for (int earlier = 1; earlier < month; earlier++)
        {
            days += DaysInMonth(year, earlier);
        }
        return days + day - 1;
    }

    public static DayOfWeek WeekdayOf(int year, int month, int day)
    {
        // 0001-01-01 is a Monday.
        DayOfWeek weekday = DayOfWeek.Monday;
        for (int step = DaysSinceFirstDay(year, month, day) % 7; step > 0; step--)
        {
            weekday = DayAfter(weekday);
        }
        return weekday;
    }

    public static DayOfWeek DayAfter(DayOfWeek day) => day switch
    {
        DayOfWeek.Sunday => DayOfWeek.Monday,
        DayOfWeek.Monday => DayOfWeek.Tuesday,
        DayOfWeek.Tuesday => DayOfWeek.Wednesday,
        DayOfWeek.Wednesday => DayOfWeek.Thursday,
        DayOfWeek.Thursday => DayOfWeek.Friday,
        DayOfWeek.Friday => DayOfWeek.Saturday,
        DayOfWeek.Saturday => DayOfWeek.Sunday,
        _ => throw new ArgumentOutOfRangeException(nameof(day)),
    };

    /// <summary>The seven weekdays in column order for a week that starts on <paramref name="firstDayOfWeek"/>.</summary>
    public static DayOfWeek[] Columns(DayOfWeek firstDayOfWeek)
    {
        var columns = new DayOfWeek[7];
        columns[0] = firstDayOfWeek;
        for (int column = 1; column < 7; column++)
        {
            columns[column] = DayAfter(columns[column - 1]);
        }
        return columns;
    }

    /// <summary>
    /// The month as rows of seven day numbers, 0 for an empty cell. Every day is put in the column of its own
    /// weekday, and a new row starts whenever a day lands in the first column.
    /// </summary>
    public static int[][] Weeks(int year, int month, DayOfWeek firstDayOfWeek)
    {
        DayOfWeek[] columns = Columns(firstDayOfWeek);
        var rows = new List<int[]>();
        int lastDay = DaysInMonth(year, month);
        for (int day = 1; day <= lastDay; day++)
        {
            int column = Array.IndexOf(columns, WeekdayOf(year, month, day));
            if (column == 0 || rows.Count == 0)
            {
                rows.Add(new int[7]);
            }
            rows[^1][column] = day;
        }
        return rows.ToArray();
    }
}
