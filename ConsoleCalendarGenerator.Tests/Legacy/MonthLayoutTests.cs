using CalendarGenerator;
using ConsoleCalendarGenerator.Tests.Support;

namespace ConsoleCalendarGenerator.Tests.Legacy;

/// <summary>
/// The text GenerateMonth writes, with the invariant culture (English month names) and a writer whose newline
/// is "\n". The expected output is spelled out line by line; a blank line is an empty string.
/// </summary>
[TestFixture]
public class MonthLayoutTests
{
    private static string Month(int month, int year) =>
        ConsoleCapture.Run(() => new ConsoleCalendar().GenerateMonth(month, year)).Text;

    // ---- one month for every weekday the 1st can fall on ----

    [Test]
    public void FirstOnMonday_31Days_FiveRows()
    {
        Assert.That(Month(5, 2023), Is.EqualTo(Golden.Lines(
            "     May 2023",
            "Mo Tu We Th Fr Sa Su",
            " 1  2  3  4  5  6  7",
            " 8  9 10 11 12 13 14",
            "15 16 17 18 19 20 21",
            "22 23 24 25 26 27 28",
            "29 30 31",
            "")));
    }

    [Test]
    public void FirstOnTuesday_31Days()
    {
        Assert.That(Month(8, 2023), Is.EqualTo(Golden.Lines(
            "    August 2023",
            "Mo Tu We Th Fr Sa Su",
            "    1  2  3  4  5  6",
            " 7  8  9 10 11 12 13",
            "14 15 16 17 18 19 20",
            "21 22 23 24 25 26 27",
            "28 29 30 31",
            "")));
    }

    [Test]
    public void FirstOnWednesday_28DayFebruary()
    {
        Assert.That(Month(2, 2023), Is.EqualTo(Golden.Lines(
            "   February 2023",
            "Mo Tu We Th Fr Sa Su",
            "       1  2  3  4  5",
            " 6  7  8  9 10 11 12",
            "13 14 15 16 17 18 19",
            "20 21 22 23 24 25 26",
            "27 28",
            "")));
    }

    [Test]
    public void FirstOnThursday_30Days()
    {
        Assert.That(Month(6, 2023), Is.EqualTo(Golden.Lines(
            "     June 2023",
            "Mo Tu We Th Fr Sa Su",
            "          1  2  3  4",
            " 5  6  7  8  9 10 11",
            "12 13 14 15 16 17 18",
            "19 20 21 22 23 24 25",
            "26 27 28 29 30",
            "")));
    }

    [Test]
    public void FirstOnFriday_30Days()
    {
        Assert.That(Month(9, 2023), Is.EqualTo(Golden.Lines(
            "   September 2023",
            "Mo Tu We Th Fr Sa Su",
            "             1  2  3",
            " 4  5  6  7  8  9 10",
            "11 12 13 14 15 16 17",
            "18 19 20 21 22 23 24",
            "25 26 27 28 29 30",
            "")));
    }

    /// <summary>A month whose last day is a Sunday is followed by two blank lines instead of one.</summary>
    [Test]
    public void FirstOnSaturday_30Days_EndingOnSunday_IsFollowedByOneBlankLineLikeAnyMonth()
    {
        Assert.That(Month(4, 2023), Is.EqualTo(Golden.Lines(
            "    April 2023",
            "Mo Tu We Th Fr Sa Su",
            "                1  2",
            " 3  4  5  6  7  8  9",
            "10 11 12 13 14 15 16",
            "17 18 19 20 21 22 23",
            "24 25 26 27 28 29 30",
            "")));
    }

    [Test]
    public void FirstOnSunday_31Days_SixRows()
    {
        Assert.That(Month(1, 2023), Is.EqualTo(Golden.Lines(
            "    January 2023",
            "Mo Tu We Th Fr Sa Su",
            "                   1",
            " 2  3  4  5  6  7  8",
            " 9 10 11 12 13 14 15",
            "16 17 18 19 20 21 22",
            "23 24 25 26 27 28 29",
            "30 31",
            "")));
    }

    // ---- February and leap years ----

    [Test]
    public void February2021_FourRows_EndingOnSunday_IsFollowedByOneBlankLineLikeAnyMonth()
    {
        Assert.That(Month(2, 2021), Is.EqualTo(Golden.Lines(
            "   February 2021",
            "Mo Tu We Th Fr Sa Su",
            " 1  2  3  4  5  6  7",
            " 8  9 10 11 12 13 14",
            "15 16 17 18 19 20 21",
            "22 23 24 25 26 27 28",
            "")));
    }

    [Test]
    public void February2024_LeapYear_Has29Days()
    {
        Assert.That(Month(2, 2024), Is.EqualTo(Golden.Lines(
            "   February 2024",
            "Mo Tu We Th Fr Sa Su",
            "          1  2  3  4",
            " 5  6  7  8  9 10 11",
            "12 13 14 15 16 17 18",
            "19 20 21 22 23 24 25",
            "26 27 28 29",
            "")));
    }

    [Test]
    public void February1900_CenturyThatIsNotALeapYear_Has28Days()
    {
        Assert.That(Month(2, 1900), Is.EqualTo(Golden.Lines(
            "   February 1900",
            "Mo Tu We Th Fr Sa Su",
            "          1  2  3  4",
            " 5  6  7  8  9 10 11",
            "12 13 14 15 16 17 18",
            "19 20 21 22 23 24 25",
            "26 27 28",
            "")));
    }

    [Test]
    public void February2000_CenturyThatIsALeapYear_Has29Days()
    {
        Assert.That(Month(2, 2000), Is.EqualTo(Golden.Lines(
            "   February 2000",
            "Mo Tu We Th Fr Sa Su",
            "    1  2  3  4  5  6",
            " 7  8  9 10 11 12 13",
            "14 15 16 17 18 19 20",
            "21 22 23 24 25 26 27",
            "28 29",
            "")));
    }

    [TestCase(1900, 28)]
    [TestCase(2000, 29)]
    [TestCase(2023, 28)]
    [TestCase(2024, 29)]
    public void February_EndsOnTheCorrectDay(int year, int lastDay)
    {
        int[] days = Month(2, year).Split('\n').Skip(2)
            .SelectMany(line => line.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            .Select(int.Parse)
            .ToArray();

        Assert.That(days, Is.EqualTo(Enumerable.Range(1, lastDay)));
    }

    // ---- the first and the last month the library can draw ----

    [Test]
    public void January0001_TheYearIsWrittenWithoutPadding()
    {
        Assert.That(Month(1, 1), Is.EqualTo(Golden.Lines(
            "    January 1",
            "Mo Tu We Th Fr Sa Su",
            " 1  2  3  4  5  6  7",
            " 8  9 10 11 12 13 14",
            "15 16 17 18 19 20 21",
            "22 23 24 25 26 27 28",
            "29 30 31",
            "")));
    }

    [Test]
    public void December9999()
    {
        Assert.That(Month(12, 9999), Is.EqualTo(Golden.Lines(
            "   December 9999",
            "Mo Tu We Th Fr Sa Su",
            "       1  2  3  4  5",
            " 6  7  8  9 10 11 12",
            "13 14 15 16 17 18 19",
            "20 21 22 23 24 25 26",
            "27 28 29 30 31",
            "")));
    }

    // ---- title ----

    [Test]
    public void EnglishTitles_AreIndentedFiveFourOrThreeSpacesByNameLength()
    {
        string[] titles = Enumerable.Range(1, 12).Select(month => Month(month, 2023).Split('\n')[0]).ToArray();

        Assert.That(titles, Is.EqualTo(new[]
        {
            "    January 2023",
            "   February 2023",
            "    March 2023",
            "    April 2023",
            "     May 2023",
            "     June 2023",
            "     July 2023",
            "    August 2023",
            "   September 2023",
            "    October 2023",
            "   November 2023",
            "   December 2023",
        }));
    }

    [TestCase(1, "     ")]
    [TestCase(4, "     ")]
    [TestCase(5, "    ")]
    [TestCase(7, "    ")]
    [TestCase(8, "   ")]
    [TestCase(30, "   ")]
    public void TitleIndent_DependsOnlyOnTheLengthOfTheMonthName(int nameLength, string indent)
    {
        string name = new string('x', nameLength);

        string title = ConsoleCapture.Run(
            () => new ConsoleCalendar().GenerateMonth(1, 2023),
            culture: TestCultures.WithMonthNames(_ => name)).Text.Split('\n')[0];

        Assert.That(title, Is.EqualTo(indent + name + " 2023"), "the title is never truncated, even when it is wider than the grid");
    }

    // ---- rules that hold for every month ----

    /// <summary>
    /// Compares the library with <see cref="LegacyReference"/> for all 2,412 months from 1900 through 2100. Among
    /// them is every combination of starting weekday and month length that exists.
    /// </summary>
    [Test]
    public void EveryMonthFrom1900Through2100_MatchesTheLayoutRules()
    {
        var calendar = new ConsoleCalendar();
        var mismatches = new List<string>();
        var shapes = new HashSet<(DayOfWeek FirstDay, int Days)>();

        for (int year = 1900; year <= 2100; year++)
        {
            for (int month = 1; month <= 12; month++)
            {
                int m = month;
                int y = year;
                string actual = ConsoleCapture.Run(() => calendar.GenerateMonth(m, y)).Text;
                if (actual != LegacyReference.Month(year, month, LegacyReference.EnglishMonthName(month)))
                {
                    mismatches.Add($"{year}-{month:00}");
                }
                shapes.Add((new DateTime(year, month, 1).DayOfWeek, DateTime.DaysInMonth(year, month)));
            }
        }

        Assert.That(mismatches, Is.Empty);
        Assert.That(shapes, Has.Count.EqualTo(28), "7 starting weekdays x 28, 29, 30 and 31 days");
    }

    [Test]
    public void EveryMonthFrom1900Through2100_KeepsTheGridShape()
    {
        var calendar = new ConsoleCalendar();
        var rowCounts = new SortedSet<int>();
        var problems = new List<string>();

        for (int year = 1900; year <= 2100; year++)
        {
            for (int month = 1; month <= 12; month++)
            {
                int m = month;
                int y = year;
                string text = ConsoleCapture.Run(() => calendar.GenerateMonth(m, y)).Text;
                string[] lines = text.Split('\n');
                string[] rows = lines.Skip(2).TakeWhile(line => line.Length > 0).ToArray();
                rowCounts.Add(rows.Length);

                int trailingNewlines = text.Length - text.TrimEnd('\n').Length;

                if (lines[1] != "Mo Tu We Th Fr Sa Su"
                    || lines.Any(line => line.EndsWith(' '))
                    || rows.Any(row => row.Length > 20)
                    || rows.Any(row => row.Length % 3 != 2)
                    || rows.Any(row => Enumerable.Range(0, row.Length).Any(i => i % 3 == 2 && row[i] != ' '))
                    || trailingNewlines != 2)
                {
                    problems.Add($"{year}-{month:00}");
                }
            }
        }

        Assert.That(problems, Is.Empty,
            "weekday header, no trailing spaces, rows of at most 20 characters, two-character cells with one space between, " +
            "exactly one blank line after the month, whichever day it ends on");
        Assert.That(rowCounts, Is.EqualTo(new[] { 4, 5, 6 }));
    }
}
