using ConsoleCalendarGenerator.Tests.Support;

namespace ConsoleCalendarGenerator.Tests.Layout;

/// <summary>
/// What the first day of the week does to a layout: which weekday each column stands for, and where the days
/// land. The expected grids and positions are written out by hand.
/// </summary>
[TestFixture]
public class FirstDayOfWeekTests
{
    // ---- the weekday of each column ----

    [TestCase(DayOfWeek.Sunday, "Sunday Monday Tuesday Wednesday Thursday Friday Saturday")]
    [TestCase(DayOfWeek.Monday, "Monday Tuesday Wednesday Thursday Friday Saturday Sunday")]
    [TestCase(DayOfWeek.Tuesday, "Tuesday Wednesday Thursday Friday Saturday Sunday Monday")]
    [TestCase(DayOfWeek.Wednesday, "Wednesday Thursday Friday Saturday Sunday Monday Tuesday")]
    [TestCase(DayOfWeek.Thursday, "Thursday Friday Saturday Sunday Monday Tuesday Wednesday")]
    [TestCase(DayOfWeek.Friday, "Friday Saturday Sunday Monday Tuesday Wednesday Thursday")]
    [TestCase(DayOfWeek.Saturday, "Saturday Sunday Monday Tuesday Wednesday Thursday Friday")]
    public void TheColumns_StartWithTheFirstDayOfWeek_AndFollowTheWeekFromThere(DayOfWeek firstDayOfWeek, string expectedColumns)
    {
        // The columns do not depend on the month: one that starts on each of two different weekdays.
        foreach (MonthLayout layout in new[] { MonthLayout.Create(2023, 5, firstDayOfWeek), MonthLayout.Create(2024, 2, firstDayOfWeek) })
        {
            Assert.That(layout.FirstDayOfWeek, Is.EqualTo(firstDayOfWeek));
            Assert.That(layout.DaysOfWeek, Has.Count.EqualTo(7));
            Assert.That(string.Join(" ", layout.DaysOfWeek), Is.EqualTo(expectedColumns));
        }
    }

    [Test]
    public void EveryCell_HasTheWeekdayOfItsColumn_IncludingTheEmptyOnes([Values] DayOfWeek firstDayOfWeek)
    {
        // April 2023 has empty cells at both ends for most first days of the week.
        MonthLayout layout = MonthLayout.Create(2023, 4, firstDayOfWeek);

        foreach (MonthLayoutWeek week in layout.Weeks)
        {
            Assert.That(week.Cells.Select(cell => cell.DayOfWeek), Is.EqualTo(layout.DaysOfWeek));
        }
    }

    // ---- one month, drawn for each of the seven first days of the week ----
    // May 2023 has 31 days and its 1st is a Monday.

    [Test]
    public void May2023_WeekStartingOnMonday()
    {
        Assert.That(LayoutPicture.Of(MonthLayout.Create(2023, 5, DayOfWeek.Monday)), Is.EqualTo(new[]
        {
            "Mo Tu We Th Fr Sa Su",
            " 1  2  3  4  5  6  7",
            " 8  9 10 11 12 13 14",
            "15 16 17 18 19 20 21",
            "22 23 24 25 26 27 28",
            "29 30 31  .  .  .  .",
        }));
    }

    [Test]
    public void May2023_WeekStartingOnSunday()
    {
        Assert.That(LayoutPicture.Of(MonthLayout.Create(2023, 5, DayOfWeek.Sunday)), Is.EqualTo(new[]
        {
            "Su Mo Tu We Th Fr Sa",
            " .  1  2  3  4  5  6",
            " 7  8  9 10 11 12 13",
            "14 15 16 17 18 19 20",
            "21 22 23 24 25 26 27",
            "28 29 30 31  .  .  .",
        }));
    }

    [Test]
    public void May2023_WeekStartingOnSaturday()
    {
        Assert.That(LayoutPicture.Of(MonthLayout.Create(2023, 5, DayOfWeek.Saturday)), Is.EqualTo(new[]
        {
            "Sa Su Mo Tu We Th Fr",
            " .  .  1  2  3  4  5",
            " 6  7  8  9 10 11 12",
            "13 14 15 16 17 18 19",
            "20 21 22 23 24 25 26",
            "27 28 29 30 31  .  .",
        }));
    }

    [Test]
    public void May2023_WeekStartingOnFriday()
    {
        Assert.That(LayoutPicture.Of(MonthLayout.Create(2023, 5, DayOfWeek.Friday)), Is.EqualTo(new[]
        {
            "Fr Sa Su Mo Tu We Th",
            " .  .  .  1  2  3  4",
            " 5  6  7  8  9 10 11",
            "12 13 14 15 16 17 18",
            "19 20 21 22 23 24 25",
            "26 27 28 29 30 31  .",
        }));
    }

    /// <summary>The month fills its last week exactly, so no cell after the 31st is empty.</summary>
    [Test]
    public void May2023_WeekStartingOnThursday()
    {
        Assert.That(LayoutPicture.Of(MonthLayout.Create(2023, 5, DayOfWeek.Thursday)), Is.EqualTo(new[]
        {
            "Th Fr Sa Su Mo Tu We",
            " .  .  .  .  1  2  3",
            " 4  5  6  7  8  9 10",
            "11 12 13 14 15 16 17",
            "18 19 20 21 22 23 24",
            "25 26 27 28 29 30 31",
        }));
    }

    [Test]
    public void May2023_WeekStartingOnWednesday_NeedsSixWeeks()
    {
        Assert.That(LayoutPicture.Of(MonthLayout.Create(2023, 5, DayOfWeek.Wednesday)), Is.EqualTo(new[]
        {
            "We Th Fr Sa Su Mo Tu",
            " .  .  .  .  .  1  2",
            " 3  4  5  6  7  8  9",
            "10 11 12 13 14 15 16",
            "17 18 19 20 21 22 23",
            "24 25 26 27 28 29 30",
            "31  .  .  .  .  .  .",
        }));
    }

    [Test]
    public void May2023_WeekStartingOnTuesday_NeedsSixWeeks()
    {
        Assert.That(LayoutPicture.Of(MonthLayout.Create(2023, 5, DayOfWeek.Tuesday)), Is.EqualTo(new[]
        {
            "Tu We Th Fr Sa Su Mo",
            " .  .  .  .  .  .  1",
            " 2  3  4  5  6  7  8",
            " 9 10 11 12 13 14 15",
            "16 17 18 19 20 21 22",
            "23 24 25 26 27 28 29",
            "30 31  .  .  .  .  .",
        }));
    }

    // ---- where the 1st and the last day land, for months of each length ----
    // Weeks and columns are counted from 0. The last week is always the one that holds the last day.

    // February 2021: 28 days, Monday the 1st through Sunday the 28th.
    [TestCase(2021, 2, DayOfWeek.Sunday, 1, 4, 0)]
    [TestCase(2021, 2, DayOfWeek.Monday, 0, 3, 6)]
    [TestCase(2021, 2, DayOfWeek.Tuesday, 6, 4, 5)]
    [TestCase(2021, 2, DayOfWeek.Wednesday, 5, 4, 4)]
    [TestCase(2021, 2, DayOfWeek.Thursday, 4, 4, 3)]
    [TestCase(2021, 2, DayOfWeek.Friday, 3, 4, 2)]
    [TestCase(2021, 2, DayOfWeek.Saturday, 2, 4, 1)]
    // February 2024: 29 days, Thursday the 1st through Thursday the 29th.
    [TestCase(2024, 2, DayOfWeek.Sunday, 4, 4, 4)]
    [TestCase(2024, 2, DayOfWeek.Monday, 3, 4, 3)]
    [TestCase(2024, 2, DayOfWeek.Tuesday, 2, 4, 2)]
    [TestCase(2024, 2, DayOfWeek.Wednesday, 1, 4, 1)]
    [TestCase(2024, 2, DayOfWeek.Thursday, 0, 4, 0)]
    [TestCase(2024, 2, DayOfWeek.Friday, 6, 4, 6)]
    [TestCase(2024, 2, DayOfWeek.Saturday, 5, 4, 5)]
    // April 2023: 30 days, Saturday the 1st through Sunday the 30th.
    [TestCase(2023, 4, DayOfWeek.Sunday, 6, 5, 0)]
    [TestCase(2023, 4, DayOfWeek.Monday, 5, 4, 6)]
    [TestCase(2023, 4, DayOfWeek.Tuesday, 4, 4, 5)]
    [TestCase(2023, 4, DayOfWeek.Wednesday, 3, 4, 4)]
    [TestCase(2023, 4, DayOfWeek.Thursday, 2, 4, 3)]
    [TestCase(2023, 4, DayOfWeek.Friday, 1, 4, 2)]
    [TestCase(2023, 4, DayOfWeek.Saturday, 0, 4, 1)]
    // January 2023: 31 days, Sunday the 1st through Tuesday the 31st.
    [TestCase(2023, 1, DayOfWeek.Sunday, 0, 4, 2)]
    [TestCase(2023, 1, DayOfWeek.Monday, 6, 5, 1)]
    [TestCase(2023, 1, DayOfWeek.Tuesday, 5, 5, 0)]
    [TestCase(2023, 1, DayOfWeek.Wednesday, 4, 4, 6)]
    [TestCase(2023, 1, DayOfWeek.Thursday, 3, 4, 5)]
    [TestCase(2023, 1, DayOfWeek.Friday, 2, 4, 4)]
    [TestCase(2023, 1, DayOfWeek.Saturday, 1, 4, 3)]
    public void TheFirstAndTheLastDay_AreInTheExpectedWeekAndColumn(
        int year, int month, DayOfWeek firstDayOfWeek, int columnOfFirstDay, int weekOfLastDay, int columnOfLastDay)
    {
        MonthLayout layout = MonthLayout.Create(year, month, firstDayOfWeek);
        int lastDay = layout.Weeks.SelectMany(week => week.Cells).Max(cell => cell.Date?.Day ?? 0);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(PositionOf(layout, 1), Is.EqualTo((0, columnOfFirstDay)), "the 1st, as (week, column)");
            Assert.That(PositionOf(layout, lastDay), Is.EqualTo((weekOfLastDay, columnOfLastDay)), "the last day, as (week, column)");
            Assert.That(layout.Weeks, Has.Count.EqualTo(weekOfLastDay + 1), "the week of the last day is the last week");
        }
    }

    [TestCase(2021, 2, DayOfWeek.Monday, 28, DayOfWeek.Sunday)]
    [TestCase(2024, 2, DayOfWeek.Thursday, 29, DayOfWeek.Thursday)]
    [TestCase(2023, 4, DayOfWeek.Saturday, 30, DayOfWeek.Sunday)]
    [TestCase(2023, 1, DayOfWeek.Sunday, 31, DayOfWeek.Tuesday)]
    public void TheFirstAndTheLastDay_AreInTheColumnOfTheirWeekday_WhateverTheFirstDayOfWeek(
        int year, int month, DayOfWeek weekdayOfFirstDay, int lastDay, DayOfWeek weekdayOfLastDay)
    {
        foreach (DayOfWeek firstDayOfWeek in ReferenceCalendar.AllDaysOfWeek)
        {
            MonthLayout layout = MonthLayout.Create(year, month, firstDayOfWeek);
            (int Week, int Column) first = PositionOf(layout, 1);
            (int Week, int Column) last = PositionOf(layout, lastDay);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(layout.DaysOfWeek[first.Column], Is.EqualTo(weekdayOfFirstDay), $"column of the 1st, week starting on {firstDayOfWeek}");
                Assert.That(layout.Weeks[first.Week].Cells[first.Column].DayOfWeek, Is.EqualTo(weekdayOfFirstDay));
                Assert.That(layout.DaysOfWeek[last.Column], Is.EqualTo(weekdayOfLastDay), $"column of the last day, week starting on {firstDayOfWeek}");
                Assert.That(layout.Weeks[last.Week].Cells[last.Column].DayOfWeek, Is.EqualTo(weekdayOfLastDay));
            }
        }
    }

    /// <summary>The (week, column) of the cell that holds the given day of the month; fails unless there is exactly one.</summary>
    private static (int Week, int Column) PositionOf(MonthLayout layout, int day)
    {
        var positions = new List<(int Week, int Column)>();
        for (int week = 0; week < layout.Weeks.Count; week++)
        {
            for (int column = 0; column < layout.Weeks[week].Cells.Count; column++)
            {
                if (layout.Weeks[week].Cells[column].Date?.Day == day)
                {
                    positions.Add((week, column));
                }
            }
        }
        Assert.That(positions, Has.Count.EqualTo(1), $"day {day} must be in exactly one cell");
        return positions[0];
    }
}
