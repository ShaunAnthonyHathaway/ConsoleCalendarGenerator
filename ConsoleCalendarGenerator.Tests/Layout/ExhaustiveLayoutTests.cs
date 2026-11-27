using ConsoleCalendarGenerator.Tests.Support;

namespace ConsoleCalendarGenerator.Tests.Layout;

/// <summary>
/// Checks layouts in bulk, in two independent ways. One compares each layout with
/// <see cref="ReferenceCalendar"/>, which works the grid out by other means. The other checks the rules every
/// layout must obey, using nothing but the layout itself. The reference is itself checked against dates whose
/// weekday is known, and against <see cref="DateTime"/>.
/// </summary>
[TestFixture]
public class ExhaustiveLayoutTests
{
    // ---- the reference ----

    [TestCase(1, 1, 1, DayOfWeek.Monday)]
    [TestCase(1582, 10, 15, DayOfWeek.Friday)]
    [TestCase(1900, 1, 1, DayOfWeek.Monday)]
    [TestCase(1900, 3, 1, DayOfWeek.Thursday)]
    [TestCase(1969, 7, 20, DayOfWeek.Sunday)]
    [TestCase(1970, 1, 1, DayOfWeek.Thursday)]
    [TestCase(2000, 1, 1, DayOfWeek.Saturday)]
    [TestCase(2000, 2, 29, DayOfWeek.Tuesday)]
    [TestCase(2023, 5, 1, DayOfWeek.Monday)]
    [TestCase(2024, 2, 29, DayOfWeek.Thursday)]
    [TestCase(2100, 3, 1, DayOfWeek.Monday)]
    [TestCase(9999, 12, 31, DayOfWeek.Friday)]
    public void TheReference_GivesTheKnownWeekdayOfAKnownDate(int year, int month, int day, DayOfWeek expected)
    {
        Assert.That(ReferenceCalendar.WeekdayOf(year, month, day), Is.EqualTo(expected));
    }

    /// <summary>
    /// The reference and the runtime agree on the length of every month and on the weekday of its first and last
    /// day, for all 119,988 months from January 0001 through December 9999.
    /// </summary>
    [Test]
    public void TheReference_AgreesWithDateTime_ForEveryMonthThereIs()
    {
        var disagreements = new List<string>();
        int months = 0;

        for (int year = ReferenceCalendar.FirstYear; year <= ReferenceCalendar.LastYear; year++)
        {
            for (int month = 1; month <= 12; month++)
            {
                months++;
                int days = ReferenceCalendar.DaysInMonth(year, month);
                if (days != DateTime.DaysInMonth(year, month)
                    || ReferenceCalendar.WeekdayOf(year, month, 1) != new DateTime(year, month, 1).DayOfWeek
                    || ReferenceCalendar.WeekdayOf(year, month, days) != new DateTime(year, month, days).DayOfWeek)
                {
                    disagreements.Add($"{year}-{month:00}");
                }
            }
        }

        Assert.That(disagreements, Is.Empty);
        Assert.That(months, Is.EqualTo(119_988));
        Assert.That(ReferenceCalendar.DaysSinceFirstDay(9999, 12, 31), Is.EqualTo((DateTime.MaxValue.Date - DateTime.MinValue).Days));
    }

    // ---- 1900 through 2100 ----

    /// <summary>
    /// 2,412 months for each of the seven first days of the week: 16,884 layouts. Among them is every
    /// combination of first day of the week, weekday of the 1st and month length that exists (7 x 7 x 4 = 196).
    /// </summary>
    [Test]
    public void EveryMonthFrom1900Through2100_ForEveryFirstDayOfWeek_MatchesTheReference()
    {
        var mismatches = new List<string>();
        var combinations = new HashSet<(DayOfWeek FirstDayOfWeek, DayOfWeek WeekdayOfFirstDay, int Days)>();
        var weekCounts = new SortedSet<int>();
        int layouts = 0;

        for (int year = 1900; year <= 2100; year++)
        {
            for (int month = 1; month <= 12; month++)
            {
                foreach (DayOfWeek firstDayOfWeek in ReferenceCalendar.AllDaysOfWeek)
                {
                    MonthLayout layout = MonthLayout.Create(year, month, firstDayOfWeek);
                    layouts++;
                    if (DifferenceFromReference(layout, year, month, firstDayOfWeek) is string difference)
                    {
                        mismatches.Add($"{year}-{month:00} from {firstDayOfWeek}: {difference}");
                    }
                    combinations.Add((firstDayOfWeek, ReferenceCalendar.WeekdayOf(year, month, 1), ReferenceCalendar.DaysInMonth(year, month)));
                    weekCounts.Add(layout.Weeks.Count);
                }
            }
        }

        Assert.That(mismatches, Is.Empty);
        Assert.That(layouts, Is.EqualTo(16_884));
        Assert.That(combinations, Has.Count.EqualTo(196));
        Assert.That(weekCounts, Is.EqualTo(new[] { 4, 5, 6 }));
    }

    [Test]
    public void EveryMonthFrom1900Through2100_ForEveryFirstDayOfWeek_ObeysTheLayoutRules()
    {
        var broken = new List<string>();

        for (int year = 1900; year <= 2100; year++)
        {
            for (int month = 1; month <= 12; month++)
            {
                foreach (DayOfWeek firstDayOfWeek in ReferenceCalendar.AllDaysOfWeek)
                {
                    if (BrokenRule(MonthLayout.Create(year, month, firstDayOfWeek), year, month, firstDayOfWeek) is string rule)
                    {
                        broken.Add($"{year}-{month:00} from {firstDayOfWeek}: {rule}");
                    }
                }
            }
        }

        Assert.That(broken, Is.Empty);
    }

    // ---- the years at each end ----

    [TestCase(1)]
    [TestCase(2)]
    [TestCase(9998)]
    [TestCase(9999)]
    public void EveryMonthOfTheFirstAndTheLastYears_MatchesTheReference_AndObeysTheLayoutRules(int year)
    {
        for (int month = 1; month <= 12; month++)
        {
            foreach (DayOfWeek firstDayOfWeek in ReferenceCalendar.AllDaysOfWeek)
            {
                MonthLayout layout = MonthLayout.Create(year, month, firstDayOfWeek);

                Assert.That(DifferenceFromReference(layout, year, month, firstDayOfWeek), Is.Null, $"{year}-{month:00} from {firstDayOfWeek}");
                Assert.That(BrokenRule(layout, year, month, firstDayOfWeek), Is.Null, $"{year}-{month:00} from {firstDayOfWeek}");
            }
        }
    }

    // ---- everything ----

    /// <summary>
    /// Every input the layout accepts: 119,988 months for each of the seven first days of the week, 839,916
    /// layouts, each compared with the reference and checked against the rules.
    /// </summary>
    [Test]
    public void EveryMonthThereIs_ForEveryFirstDayOfWeek_MatchesTheReference_AndObeysTheLayoutRules()
    {
        var problems = new List<string>();
        int layouts = 0;

        for (int year = ReferenceCalendar.FirstYear; year <= ReferenceCalendar.LastYear; year++)
        {
            for (int month = 1; month <= 12; month++)
            {
                foreach (DayOfWeek firstDayOfWeek in ReferenceCalendar.AllDaysOfWeek)
                {
                    MonthLayout layout = MonthLayout.Create(year, month, firstDayOfWeek);
                    layouts++;
                    string? problem = DifferenceFromReference(layout, year, month, firstDayOfWeek)
                        ?? BrokenRule(layout, year, month, firstDayOfWeek);
                    if (problem != null && problems.Count < 20)
                    {
                        problems.Add($"{year}-{month:00} from {firstDayOfWeek}: {problem}");
                    }
                }
            }
        }

        Assert.That(problems, Is.Empty);
        Assert.That(layouts, Is.EqualTo(839_916));
    }

    /// <summary>What differs between the layout and the reference's grid for the same input, or null.</summary>
    private static string? DifferenceFromReference(MonthLayout layout, int year, int month, DayOfWeek firstDayOfWeek)
    {
        if (!layout.DaysOfWeek.SequenceEqual(ReferenceCalendar.Columns(firstDayOfWeek)))
        {
            return "the columns are " + string.Join(" ", layout.DaysOfWeek);
        }

        int[][] expected = ReferenceCalendar.Weeks(year, month, firstDayOfWeek);
        if (layout.Weeks.Count != expected.Length)
        {
            return $"{layout.Weeks.Count} weeks instead of {expected.Length}";
        }
        for (int week = 0; week < expected.Length; week++)
        {
            IReadOnlyList<MonthLayoutCell> cells = layout.Weeks[week].Cells;
            if (cells.Count != 7)
            {
                return $"week {week} has {cells.Count} cells";
            }
            for (int column = 0; column < 7; column++)
            {
                int actualDay = cells[column].Date?.Day ?? 0;
                if (actualDay != expected[week][column])
                {
                    return $"week {week}, column {column} holds {actualDay} instead of {expected[week][column]}";
                }
            }
        }
        return null;
    }

    /// <summary>
    /// The first rule the layout breaks, or null. The rules use the layout alone, apart from the length of the
    /// month, which comes from the reference.
    /// </summary>
    private static string? BrokenRule(MonthLayout layout, int year, int month, DayOfWeek firstDayOfWeek)
    {
        if (layout.Year != year || layout.Month != month || layout.FirstDayOfWeek != firstDayOfWeek)
        {
            return "the layout does not report the year, month and first day of the week it was created for";
        }

        // The columns: seven different weekdays, each the day after the one before.
        if (layout.DaysOfWeek.Count != 7 || layout.DaysOfWeek[0] != firstDayOfWeek || layout.DaysOfWeek.Distinct().Count() != 7)
        {
            return "the columns are not seven different weekdays starting with the first day of the week";
        }
        for (int column = 1; column < 7; column++)
        {
            if (layout.DaysOfWeek[column] != ReferenceCalendar.DayAfter(layout.DaysOfWeek[column - 1]))
            {
                return $"column {column} is not the day after column {column - 1}";
            }
        }

        if (layout.Weeks.Count < 4 || layout.Weeks.Count > 6)
        {
            return $"{layout.Weeks.Count} weeks";
        }

        var cells = new List<MonthLayoutCell>();
        foreach (MonthLayoutWeek week in layout.Weeks)
        {
            if (week.Cells.Count != 7)
            {
                return $"a week with {week.Cells.Count} cells";
            }
            for (int column = 0; column < 7; column++)
            {
                MonthLayoutCell cell = week.Cells[column];
                if (cell.DayOfWeek != layout.DaysOfWeek[column])
                {
                    return $"a cell in column {column} with the weekday {cell.DayOfWeek}";
                }
                if (cell.IsEmpty != (cell.Date == null))
                {
                    return "a cell whose IsEmpty contradicts its Date";
                }
                if (cell.Date is DateTime date)
                {
                    if (date.Year != year || date.Month != month)
                    {
                        return $"a date outside the month: {date:yyyy-MM-dd}";
                    }
                    if (date.DayOfWeek != cell.DayOfWeek)
                    {
                        return $"day {date.Day} is a {date.DayOfWeek} in a {cell.DayOfWeek} column";
                    }
                    if (date.TimeOfDay != TimeSpan.Zero || date.Kind != DateTimeKind.Unspecified)
                    {
                        return $"day {date.Day} has a time of day or a kind";
                    }
                }
                cells.Add(cell);
            }
        }

        // In reading order: empty cells, then the days 1, 2, 3 ... to the end of the month with no gap, then
        // empty cells. So every date appears exactly once, in increasing order, and nothing else does.
        int before = cells.TakeWhile(cell => cell.IsEmpty).Count();
        int after = cells.AsEnumerable().Reverse().TakeWhile(cell => cell.IsEmpty).Count();
        int[] days = cells.Skip(before).Take(cells.Count - before - after).Select(cell => cell.Date?.Day ?? 0).ToArray();
        if (!days.SequenceEqual(Enumerable.Range(1, ReferenceCalendar.DaysInMonth(year, month))))
        {
            return "the dates are not exactly the days of the month, once each, in order: " + string.Join(" ", days);
        }

        // No week is wholly empty, so there are no more weeks than the month needs.
        if (before > 6 || after > 6)
        {
            return $"{before} empty cells before the 1st and {after} after the last day";
        }
        return null;
    }
}
