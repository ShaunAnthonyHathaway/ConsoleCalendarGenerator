using ConsoleCalendarGenerator.Tests.Support;

namespace ConsoleCalendarGenerator.Tests.Layout;

/// <summary>
/// The shape of a month: how many days it has, how many weeks it needs, and the first and the last month a
/// <see cref="DateTime"/> can represent.
/// </summary>
[TestFixture]
public class MonthShapeTests
{
    private static IEnumerable<int> DaysOf(MonthLayout layout) =>
        layout.Weeks.SelectMany(week => week.Cells).Where(cell => !cell.IsEmpty).Select(cell => cell.Date!.Value.Day);

    // ---- month lengths ----

    [TestCase(1, 31)]
    [TestCase(2, 28)]
    [TestCase(3, 31)]
    [TestCase(4, 30)]
    [TestCase(5, 31)]
    [TestCase(6, 30)]
    [TestCase(7, 31)]
    [TestCase(8, 31)]
    [TestCase(9, 30)]
    [TestCase(10, 31)]
    [TestCase(11, 30)]
    [TestCase(12, 31)]
    public void EveryMonthOf2023_HoldsExactlyItsDays_InOrder(int month, int lastDay)
    {
        foreach (DayOfWeek firstDayOfWeek in ReferenceCalendar.AllDaysOfWeek)
        {
            Assert.That(DaysOf(MonthLayout.Create(2023, month, firstDayOfWeek)), Is.EqualTo(Enumerable.Range(1, lastDay)),
                $"week starting on {firstDayOfWeek}");
        }
    }

    /// <summary>A year is a leap year when it divides by 4, except centuries, which must divide by 400.</summary>
    [TestCase(1900, 28)]
    [TestCase(2000, 29)]
    [TestCase(2023, 28)]
    [TestCase(2024, 29)]
    [TestCase(2100, 28)]
    [TestCase(2400, 29)]
    [TestCase(1, 28)]
    [TestCase(4, 29)]
    [TestCase(100, 28)]
    [TestCase(400, 29)]
    [TestCase(9996, 29)]
    [TestCase(9999, 28)]
    public void February_HasTheRightNumberOfDays(int year, int lastDay)
    {
        foreach (DayOfWeek firstDayOfWeek in ReferenceCalendar.AllDaysOfWeek)
        {
            Assert.That(DaysOf(MonthLayout.Create(year, 2, firstDayOfWeek)), Is.EqualTo(Enumerable.Range(1, lastDay)),
                $"week starting on {firstDayOfWeek}");
        }
    }

    // ---- four, five and six weeks ----

    [Test]
    public void February2021_WeekStartingOnMonday_FillsExactlyFourWeeks()
    {
        Assert.That(LayoutPicture.Of(MonthLayout.Create(2021, 2, DayOfWeek.Monday)), Is.EqualTo(new[]
        {
            "Mo Tu We Th Fr Sa Su",
            " 1  2  3  4  5  6  7",
            " 8  9 10 11 12 13 14",
            "15 16 17 18 19 20 21",
            "22 23 24 25 26 27 28",
        }));
    }

    [Test]
    public void February2026_WeekStartingOnSunday_FillsExactlyFourWeeks()
    {
        Assert.That(LayoutPicture.Of(MonthLayout.Create(2026, 2, DayOfWeek.Sunday)), Is.EqualTo(new[]
        {
            "Su Mo Tu We Th Fr Sa",
            " 1  2  3  4  5  6  7",
            " 8  9 10 11 12 13 14",
            "15 16 17 18 19 20 21",
            "22 23 24 25 26 27 28",
        }));
    }

    [Test]
    public void February2024_LeapYear_WeekStartingOnMonday_NeedsFiveWeeks()
    {
        Assert.That(LayoutPicture.Of(MonthLayout.Create(2024, 2, DayOfWeek.Monday)), Is.EqualTo(new[]
        {
            "Mo Tu We Th Fr Sa Su",
            " .  .  .  1  2  3  4",
            " 5  6  7  8  9 10 11",
            "12 13 14 15 16 17 18",
            "19 20 21 22 23 24 25",
            "26 27 28 29  .  .  .",
        }));
    }

    [Test]
    public void January2023_WeekStartingOnMonday_NeedsSixWeeks()
    {
        Assert.That(LayoutPicture.Of(MonthLayout.Create(2023, 1, DayOfWeek.Monday)), Is.EqualTo(new[]
        {
            "Mo Tu We Th Fr Sa Su",
            " .  .  .  .  .  .  1",
            " 2  3  4  5  6  7  8",
            " 9 10 11 12 13 14 15",
            "16 17 18 19 20 21 22",
            "23 24 25 26 27 28 29",
            "30 31  .  .  .  .  .",
        }));
    }

    [Test]
    public void April2023_ThirtyDays_WeekStartingOnSunday_NeedsSixWeeks()
    {
        Assert.That(LayoutPicture.Of(MonthLayout.Create(2023, 4, DayOfWeek.Sunday)), Is.EqualTo(new[]
        {
            "Su Mo Tu We Th Fr Sa",
            " .  .  .  .  .  .  1",
            " 2  3  4  5  6  7  8",
            " 9 10 11 12 13 14 15",
            "16 17 18 19 20 21 22",
            "23 24 25 26 27 28 29",
            "30  .  .  .  .  .  .",
        }));
    }

    /// <summary>The same month needs a different number of weeks depending on where the week starts.</summary>
    // February 2021 (28 days from a Monday): four weeks only when the week starts on the weekday of the 1st.
    [TestCase(2021, 2, DayOfWeek.Sunday, 5)]
    [TestCase(2021, 2, DayOfWeek.Monday, 4)]
    [TestCase(2021, 2, DayOfWeek.Tuesday, 5)]
    [TestCase(2021, 2, DayOfWeek.Wednesday, 5)]
    [TestCase(2021, 2, DayOfWeek.Thursday, 5)]
    [TestCase(2021, 2, DayOfWeek.Friday, 5)]
    [TestCase(2021, 2, DayOfWeek.Saturday, 5)]
    // February 2024 (29 days from a Thursday): always five.
    [TestCase(2024, 2, DayOfWeek.Sunday, 5)]
    [TestCase(2024, 2, DayOfWeek.Monday, 5)]
    [TestCase(2024, 2, DayOfWeek.Tuesday, 5)]
    [TestCase(2024, 2, DayOfWeek.Wednesday, 5)]
    [TestCase(2024, 2, DayOfWeek.Thursday, 5)]
    [TestCase(2024, 2, DayOfWeek.Friday, 5)]
    [TestCase(2024, 2, DayOfWeek.Saturday, 5)]
    // April 2023 (30 days from a Saturday): six only when the 1st is in the last column.
    [TestCase(2023, 4, DayOfWeek.Sunday, 6)]
    [TestCase(2023, 4, DayOfWeek.Monday, 5)]
    [TestCase(2023, 4, DayOfWeek.Tuesday, 5)]
    [TestCase(2023, 4, DayOfWeek.Wednesday, 5)]
    [TestCase(2023, 4, DayOfWeek.Thursday, 5)]
    [TestCase(2023, 4, DayOfWeek.Friday, 5)]
    [TestCase(2023, 4, DayOfWeek.Saturday, 5)]
    // January 2023 (31 days from a Sunday): six when the 1st is in one of the last two columns.
    [TestCase(2023, 1, DayOfWeek.Sunday, 5)]
    [TestCase(2023, 1, DayOfWeek.Monday, 6)]
    [TestCase(2023, 1, DayOfWeek.Tuesday, 6)]
    [TestCase(2023, 1, DayOfWeek.Wednesday, 5)]
    [TestCase(2023, 1, DayOfWeek.Thursday, 5)]
    [TestCase(2023, 1, DayOfWeek.Friday, 5)]
    [TestCase(2023, 1, DayOfWeek.Saturday, 5)]
    public void TheNumberOfWeeks_DependsOnTheFirstDayOfWeek(int year, int month, DayOfWeek firstDayOfWeek, int expectedWeeks)
    {
        Assert.That(MonthLayout.Create(year, month, firstDayOfWeek).Weeks, Has.Count.EqualTo(expectedWeeks));
    }

    /// <summary>
    /// The number of weeks follows from two things only: the length of the month and the column of the 1st. The
    /// whole table is written out and checked against every month from 1900 through 2100, for every first day
    /// of the week. Every one of its 28 entries occurs.
    /// </summary>
    [Test]
    public void TheNumberOfWeeks_FollowsFromTheLengthOfTheMonthAndTheColumnOfThe1st()
    {
        // expectedWeeks[days][column of the 1st]
        var expectedWeeks = new Dictionary<int, int[]>
        {
            [28] = new[] { 4, 5, 5, 5, 5, 5, 5 },
            [29] = new[] { 5, 5, 5, 5, 5, 5, 5 },
            [30] = new[] { 5, 5, 5, 5, 5, 5, 6 },
            [31] = new[] { 5, 5, 5, 5, 5, 6, 6 },
        };
        var wrong = new List<string>();
        var seen = new HashSet<(int Days, int Column)>();

        for (int year = 1900; year <= 2100; year++)
        {
            for (int month = 1; month <= 12; month++)
            {
                foreach (DayOfWeek firstDayOfWeek in ReferenceCalendar.AllDaysOfWeek)
                {
                    MonthLayout layout = MonthLayout.Create(year, month, firstDayOfWeek);
                    int days = DaysOf(layout).Count();
                    int columnOfFirstDay = layout.Weeks[0].Cells.TakeWhile(cell => cell.IsEmpty).Count();
                    seen.Add((days, columnOfFirstDay));
                    if (layout.Weeks.Count != expectedWeeks[days][columnOfFirstDay])
                    {
                        wrong.Add($"{year}-{month:00} from {firstDayOfWeek}: {layout.Weeks.Count} weeks");
                    }
                }
            }
        }

        Assert.That(wrong, Is.Empty);
        Assert.That(seen, Has.Count.EqualTo(28), "4 month lengths x 7 columns for the 1st");
    }

    // ---- the first and the last month DateTime can represent ----

    [Test]
    public void January0001_WeekStartingOnMonday()
    {
        MonthLayout layout = MonthLayout.Create(1, 1, DayOfWeek.Monday);

        Assert.That(LayoutPicture.Of(layout), Is.EqualTo(new[]
        {
            "Mo Tu We Th Fr Sa Su",
            " 1  2  3  4  5  6  7",
            " 8  9 10 11 12 13 14",
            "15 16 17 18 19 20 21",
            "22 23 24 25 26 27 28",
            "29 30 31  .  .  .  .",
        }));
        Assert.That(layout.Weeks[0].Cells[0].Date, Is.EqualTo(DateTime.MinValue), "the first cell is the earliest DateTime");
    }

    /// <summary>The cells before the 1st are empty: there is no earlier date to put in them.</summary>
    [Test]
    public void January0001_WeekStartingOnSunday_HasAnEmptyCellBeforeTheEarliestDate()
    {
        MonthLayout layout = MonthLayout.Create(1, 1, DayOfWeek.Sunday);

        Assert.That(LayoutPicture.Of(layout), Is.EqualTo(new[]
        {
            "Su Mo Tu We Th Fr Sa",
            " .  1  2  3  4  5  6",
            " 7  8  9 10 11 12 13",
            "14 15 16 17 18 19 20",
            "21 22 23 24 25 26 27",
            "28 29 30 31  .  .  .",
        }));
        Assert.That(layout.Weeks[0].Cells[0].Date, Is.Null);
        Assert.That(layout.Weeks[0].Cells[1].Date, Is.EqualTo(DateTime.MinValue));
    }

    [Test]
    public void December9999_WeekStartingOnMonday()
    {
        MonthLayout layout = MonthLayout.Create(9999, 12, DayOfWeek.Monday);

        Assert.That(LayoutPicture.Of(layout), Is.EqualTo(new[]
        {
            "Mo Tu We Th Fr Sa Su",
            " .  .  1  2  3  4  5",
            " 6  7  8  9 10 11 12",
            "13 14 15 16 17 18 19",
            "20 21 22 23 24 25 26",
            "27 28 29 30 31  .  .",
        }));
        Assert.That(layout.Weeks[4].Cells[4].Date, Is.EqualTo(DateTime.MaxValue.Date), "the 31st is the latest date a DateTime can hold");
        Assert.That(layout.Weeks[4].Cells[5].Date, Is.Null, "there is no later date to put after it");
    }

    [Test]
    public void December9999_WeekStartingOnSaturday_EndsInTheLastColumn()
    {
        MonthLayout layout = MonthLayout.Create(9999, 12, DayOfWeek.Saturday);

        Assert.That(LayoutPicture.Of(layout), Is.EqualTo(new[]
        {
            "Sa Su Mo Tu We Th Fr",
            " .  .  .  .  1  2  3",
            " 4  5  6  7  8  9 10",
            "11 12 13 14 15 16 17",
            "18 19 20 21 22 23 24",
            "25 26 27 28 29 30 31",
        }));
        Assert.That(layout.Weeks[4].Cells[6].Date, Is.EqualTo(DateTime.MaxValue.Date));
    }

    [Test]
    public void December9999_WeekStartingOnFriday_PutsTheLatestDateAloneInASixthWeek()
    {
        MonthLayout layout = MonthLayout.Create(9999, 12, DayOfWeek.Friday);

        Assert.That(LayoutPicture.Of(layout), Is.EqualTo(new[]
        {
            "Fr Sa Su Mo Tu We Th",
            " .  .  .  .  .  1  2",
            " 3  4  5  6  7  8  9",
            "10 11 12 13 14 15 16",
            "17 18 19 20 21 22 23",
            "24 25 26 27 28 29 30",
            "31  .  .  .  .  .  .",
        }));
        Assert.That(layout.Weeks[5].Cells[0].Date, Is.EqualTo(DateTime.MaxValue.Date));
    }

    /// <summary>
    /// Every date in a layout is the 1st at midnight plus a whole number of days, with no time of day and no
    /// time zone attached.
    /// </summary>
    [TestCase(1, 1)]
    [TestCase(2024, 2)]
    [TestCase(9999, 12)]
    public void TheDates_AreMidnight_WithUnspecifiedKind(int year, int month)
    {
        foreach (MonthLayoutCell cell in MonthLayout.Create(year, month, DayOfWeek.Monday).Weeks.SelectMany(week => week.Cells))
        {
            if (cell.Date is DateTime date)
            {
                Assert.That(date, Is.EqualTo(new DateTime(year, month, date.Day)));
                Assert.That(date.TimeOfDay, Is.EqualTo(TimeSpan.Zero));
                Assert.That(date.Kind, Is.EqualTo(DateTimeKind.Unspecified));
            }
        }
    }

    // ---- from one month to the next ----

    /// <summary>
    /// The day after the last day of a month is the 1st of the next, so the 1st of the next month is in the
    /// column after the last day's (or back in the first column). Checked across the year boundaries and both
    /// kinds of February without ever asking for a month before January 0001 or after December 9999.
    /// </summary>
    [TestCase(1, 1)]
    [TestCase(1, 12)]
    [TestCase(1900, 2)]
    [TestCase(1999, 12)]
    [TestCase(2000, 2)]
    [TestCase(2023, 2)]
    [TestCase(2023, 12)]
    [TestCase(2024, 2)]
    [TestCase(9998, 12)]
    [TestCase(9999, 11)]
    public void TheNextMonth_StartsInTheColumnAfterTheLastDay(int year, int month)
    {
        int nextYear = month == 12 ? year + 1 : year;
        int nextMonth = month == 12 ? 1 : month + 1;

        foreach (DayOfWeek firstDayOfWeek in ReferenceCalendar.AllDaysOfWeek)
        {
            MonthLayout layout = MonthLayout.Create(year, month, firstDayOfWeek);
            MonthLayout next = MonthLayout.Create(nextYear, nextMonth, firstDayOfWeek);

            int columnOfLastDay = 6 - layout.Weeks[^1].Cells.Reverse().TakeWhile(cell => cell.IsEmpty).Count();
            int columnOfNextFirstDay = next.Weeks[0].Cells.TakeWhile(cell => cell.IsEmpty).Count();

            Assert.That(columnOfNextFirstDay, Is.EqualTo((columnOfLastDay + 1) % 7), $"week starting on {firstDayOfWeek}");
        }
    }

    /// <summary>The same, for every pair of neighbouring months there is: 119,987 of them.</summary>
    [Test]
    public void EveryMonth_StartsInTheColumnAfterTheLastDayOfTheMonthBefore()
    {
        var broken = new List<string>();
        foreach (DayOfWeek firstDayOfWeek in ReferenceCalendar.AllDaysOfWeek)
        {
            int columnOfPreviousLastDay = -1;
            for (int year = ReferenceCalendar.FirstYear; year <= ReferenceCalendar.LastYear; year++)
            {
                for (int month = 1; month <= 12; month++)
                {
                    MonthLayout layout = MonthLayout.Create(year, month, firstDayOfWeek);
                    int columnOfFirstDay = layout.Weeks[0].Cells.TakeWhile(cell => cell.IsEmpty).Count();
                    if (columnOfPreviousLastDay >= 0 && columnOfFirstDay != (columnOfPreviousLastDay + 1) % 7)
                    {
                        broken.Add($"{year}-{month:00} from {firstDayOfWeek}");
                    }
                    columnOfPreviousLastDay = 6 - layout.Weeks[^1].Cells.Reverse().TakeWhile(cell => cell.IsEmpty).Count();
                }
            }
        }

        Assert.That(broken, Is.Empty);
    }
}
