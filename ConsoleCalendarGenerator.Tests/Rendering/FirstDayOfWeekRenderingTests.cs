using ConsoleCalendarGenerator.Tests.Support;

namespace ConsoleCalendarGenerator.Tests.Rendering;

/// <summary>
/// Rendering with each of the seven first days of the week: the labels follow the columns, and the days, the
/// empty cells, the number of weeks and the last day land where the layout puts them. The month is May 2023,
/// whose 1st is a Monday.
/// </summary>
[TestFixture]
public class FirstDayOfWeekRenderingTests
{
    private static string[] May2023(DayOfWeek firstDayOfWeek) =>
        Rendered.Lines(MonthRenderer.Render(2023, 5, MonthRenderOptions.Default.WithFirstDayOfWeek(firstDayOfWeek)));

    [Test]
    public void WeekStartingOnMonday()
    {
        Assert.That(May2023(DayOfWeek.Monday), Is.EqualTo(new[]
        {
            "      2023 May",
            "Mo Tu We Th Fr Sa Su",
            " 1  2  3  4  5  6  7",
            " 8  9 10 11 12 13 14",
            "15 16 17 18 19 20 21",
            "22 23 24 25 26 27 28",
            "29 30 31",
        }));
    }

    [Test]
    public void WeekStartingOnSunday()
    {
        Assert.That(May2023(DayOfWeek.Sunday), Is.EqualTo(new[]
        {
            "      2023 May",
            "Su Mo Tu We Th Fr Sa",
            "    1  2  3  4  5  6",
            " 7  8  9 10 11 12 13",
            "14 15 16 17 18 19 20",
            "21 22 23 24 25 26 27",
            "28 29 30 31",
        }));
    }

    [Test]
    public void WeekStartingOnSaturday()
    {
        Assert.That(May2023(DayOfWeek.Saturday), Is.EqualTo(new[]
        {
            "      2023 May",
            "Sa Su Mo Tu We Th Fr",
            "       1  2  3  4  5",
            " 6  7  8  9 10 11 12",
            "13 14 15 16 17 18 19",
            "20 21 22 23 24 25 26",
            "27 28 29 30 31",
        }));
    }

    [Test]
    public void WeekStartingOnFriday()
    {
        Assert.That(May2023(DayOfWeek.Friday), Is.EqualTo(new[]
        {
            "      2023 May",
            "Fr Sa Su Mo Tu We Th",
            "          1  2  3  4",
            " 5  6  7  8  9 10 11",
            "12 13 14 15 16 17 18",
            "19 20 21 22 23 24 25",
            "26 27 28 29 30 31",
        }));
    }

    [Test]
    public void WeekStartingOnThursday_TheMonthFillsItsLastWeek()
    {
        Assert.That(May2023(DayOfWeek.Thursday), Is.EqualTo(new[]
        {
            "      2023 May",
            "Th Fr Sa Su Mo Tu We",
            "             1  2  3",
            " 4  5  6  7  8  9 10",
            "11 12 13 14 15 16 17",
            "18 19 20 21 22 23 24",
            "25 26 27 28 29 30 31",
        }));
    }

    [Test]
    public void WeekStartingOnWednesday_SixWeeks()
    {
        Assert.That(May2023(DayOfWeek.Wednesday), Is.EqualTo(new[]
        {
            "      2023 May",
            "We Th Fr Sa Su Mo Tu",
            "                1  2",
            " 3  4  5  6  7  8  9",
            "10 11 12 13 14 15 16",
            "17 18 19 20 21 22 23",
            "24 25 26 27 28 29 30",
            "31",
        }));
    }

    [Test]
    public void WeekStartingOnTuesday_SixWeeks()
    {
        Assert.That(May2023(DayOfWeek.Tuesday), Is.EqualTo(new[]
        {
            "      2023 May",
            "Tu We Th Fr Sa Su Mo",
            "                   1",
            " 2  3  4  5  6  7  8",
            " 9 10 11 12 13 14 15",
            "16 17 18 19 20 21 22",
            "23 24 25 26 27 28 29",
            "30 31",
        }));
    }

    [Test]
    public void TheLabelCells_CarryTheWeekdayOfTheirColumn([Values] DayOfWeek firstDayOfWeek)
    {
        RenderedMonth rendered = MonthRenderer.Render(2023, 5, MonthRenderOptions.Default.WithFirstDayOfWeek(firstDayOfWeek));

        DayOfWeek?[] labelWeekdays = rendered.Lines[1].Segments
            .Where(segment => segment.Kind == RenderedSegmentKind.WeekdayLabel)
            .Select(segment => segment.DayOfWeek)
            .ToArray();

        Assert.That(rendered.Layout.FirstDayOfWeek, Is.EqualTo(firstDayOfWeek));
        Assert.That(labelWeekdays, Is.EqualTo(rendered.Layout.DaysOfWeek.Cast<DayOfWeek?>()));
        Assert.That(labelWeekdays[0], Is.EqualTo(firstDayOfWeek));
    }

    /// <summary>The 1st and the 31st are in the column of their own weekday, whichever day the week starts on.</summary>
    [Test]
    public void TheFirstAndTheLastDay_AreUnderTheLabelOfTheirWeekday([Values] DayOfWeek firstDayOfWeek)
    {
        string[] lines = May2023(firstDayOfWeek);
        string labels = lines[1];

        // Every cell is two characters wide with one space between, so a column starts every three characters.
        int columnOfFirst = lines[2].IndexOf(" 1", StringComparison.Ordinal) / 3;
        int columnOfLast = lines[^1].IndexOf("31", StringComparison.Ordinal) / 3;

        Assert.That(labels.Substring(columnOfFirst * 3, 2), Is.EqualTo("Mo"), "1 May 2023 is a Monday");
        Assert.That(labels.Substring(columnOfLast * 3, 2), Is.EqualTo("We"), "31 May 2023 is a Wednesday");
        Assert.That(lines[^1], Does.EndWith("31").Using(StringComparison.Ordinal), "nothing follows the last day");
    }
}
