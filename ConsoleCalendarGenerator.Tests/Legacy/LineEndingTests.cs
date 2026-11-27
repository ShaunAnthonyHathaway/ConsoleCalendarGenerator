using CalendarGenerator;
using ConsoleCalendarGenerator.Tests.Support;

namespace ConsoleCalendarGenerator.Tests.Legacy;

/// <summary>
/// Line endings and the blank line after a month. Every line is ended with Console.WriteLine, and so with the
/// line terminator of the console's writer: the title, the weekday row, every row of days, the last row
/// included, and the one blank line that follows every month. No line feed is ever written as text.
///
/// This is the behavior of version 2.0 (D-59 and D-60). Version 1.0.0 ended the last row of days with a literal
/// "\n" whatever the terminator was, so that its output mixed two kinds of line end where the terminator is
/// "\r\n", as on Windows; and it followed a month whose last day is a Sunday with two blank lines. What 1.0.0
/// did is recorded, unchanged, against its frozen source in Historical/Published100MonthEndTests.cs, and the two
/// are held side by side in Migration/MonthEndMigrationTests.cs.
/// </summary>
[TestFixture]
public class LineEndingTests
{
    private static readonly string[] Terminators = { "\n", "\r\n", "<EOL>" };

    private static string Month(int month, int year, string newLine) =>
        ConsoleCapture.Run(() => new ConsoleCalendar().GenerateMonth(month, year), newLine: newLine).Text;

    private static string Year(int year, string newLine, List<DateTime>? selected = null) =>
        ConsoleCapture.Run(() => new ConsoleCalendar().GenerateYear(year, selected ?? new List<DateTime>()), newLine: newLine).Text;

    // ---- the writer's terminator ends every line ----

    [Test]
    public void WithACarriageReturnLineFeedWriter_EveryLineEndsWithIt_TheLastRowIncluded()
    {
        Assert.That(Month(5, 2023, "\r\n"), Is.EqualTo(
            "     May 2023\r\n" +
            "Mo Tu We Th Fr Sa Su\r\n" +
            " 1  2  3  4  5  6  7\r\n" +
            " 8  9 10 11 12 13 14\r\n" +
            "15 16 17 18 19 20 21\r\n" +
            "22 23 24 25 26 27 28\r\n" +
            "29 30 31\r\n" +
            "\r\n"));
    }

    [Test]
    public void WithACarriageReturnLineFeedWriter_AMonthEndingOnSunday_HasOneBlankLine_EndedTheSameWay()
    {
        Assert.That(Month(4, 2023, "\r\n"), Is.EqualTo(
            "    April 2023\r\n" +
            "Mo Tu We Th Fr Sa Su\r\n" +
            "                1  2\r\n" +
            " 3  4  5  6  7  8  9\r\n" +
            "10 11 12 13 14 15 16\r\n" +
            "17 18 19 20 21 22 23\r\n" +
            "24 25 26 27 28 29 30\r\n" +
            "\r\n"));
    }

    [Test]
    public void WithALineFeedWriter_EveryLineEndsWithALineFeed()
    {
        string text = Month(5, 2023, "\n");

        Assert.That(text, Ordinal.DoesNotContain("\r"));
        Assert.That(text.Count(c => c == '\n'), Is.EqualTo(8));
    }

    /// <summary>A terminator that is no line break at all shows it best: it is the only thing between the lines.</summary>
    [TestCase("\n")]
    [TestCase("\r\n")]
    [TestCase("<EOL>")]
    public void TheWritersNewline_EndsEveryLine_AndNoLineFeedIsWrittenAsText(string newLine)
    {
        foreach ((int month, int year, string name) in new[] { (2, 2024, "February"), (2, 2021, "February"), (4, 2023, "April"), (7, 2023, "July") })
        {
            string text = Month(month, year, newLine);

            Assert.That(text, Is.EqualTo(LegacyReference.Month(year, month, name, newLine)), $"{name} {year}");
            Assert.That(text.Replace(newLine, string.Empty), Does.Not.Contain("\n").And.Not.Contain("\r"), $"{name} {year}: no line end but the writer's");
        }
    }

    // ---- exactly one blank line after every month ----

    /// <summary>A month ending on each day of the week, of each length, with four, five and six rows, and the first and the last month.</summary>
    [TestCase(4, 2023, DayOfWeek.Sunday)]
    [TestCase(7, 2023, DayOfWeek.Monday)]
    [TestCase(2, 2023, DayOfWeek.Tuesday)]
    [TestCase(5, 2023, DayOfWeek.Wednesday)]
    [TestCase(2, 2024, DayOfWeek.Thursday)]
    [TestCase(3, 2023, DayOfWeek.Friday)]
    [TestCase(9, 2023, DayOfWeek.Saturday)]
    [TestCase(2, 2021, DayOfWeek.Sunday, Description = "four full weeks")]
    [TestCase(1, 2023, DayOfWeek.Tuesday, Description = "six rows")]
    [TestCase(3, 2024, DayOfWeek.Sunday, Description = "31 days")]
    [TestCase(1, 1, DayOfWeek.Wednesday, Description = "the first month there is")]
    [TestCase(12, 9999, DayOfWeek.Friday, Description = "the last month there is")]
    public void EveryMonth_IsFollowedByExactlyOneBlankLine_WhicheverDayItEndsOn(int month, int year, DayOfWeek lastWeekday)
    {
        Assert.That(new DateTime(year, month, DateTime.DaysInMonth(year, month)).DayOfWeek, Is.EqualTo(lastWeekday), "the premise of the case");

        foreach (string newLine in Terminators)
        {
            string text = Month(month, year, newLine);
            string[] lines = text.Split(newLine);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(lines[^1], Is.Empty, "the text ends with a line end");
                Assert.That(lines[^2], Is.Empty, "one blank line");
                Assert.That(lines[^3], Is.Not.Empty, "and only one: before it is the last row of days");
                Assert.That(lines[^3], Does.EndWith(DateTime.DaysInMonth(year, month).ToString(System.Globalization.CultureInfo.InvariantCulture)));
                Assert.That(lines.Any(line => line.EndsWith(' ')), Is.False, "no line ends with spaces");
            }
        }
    }

    /// <summary>The last three calls of a month are the last day, the end of its row and the blank line, whichever day it ends on.</summary>
    [TestCase(5, 2023, "[ 31]")]
    [TestCase(4, 2023, "[ 30]")]
    [TestCase(2, 2021, "[ 28]")]
    [TestCase(7, 2023, "[31]", Description = "the last day is alone in its row, on a Monday")]
    public void AMonth_EndsWithTheLastDay_OneLineEnd_AndOneMoreForTheBlankLine(int month, int year, string lastDay)
    {
        Captured captured = ConsoleCapture.Run(() => new ConsoleCalendar().GenerateMonth(month, year));

        Assert.That(captured.Transcript, Does.EndWith(lastDay + "<NL><NL>").Using(StringComparison.Ordinal));
        Assert.That(captured.Transcript, Does.Not.EndWith("<NL><NL><NL>"));
        Assert.That(captured.Transcript, Ordinal.DoesNotContain("\\n"), "no call writes a line feed as text");
    }

    // ---- a year ----

    /// <summary>A year is its twelve months, each followed by one blank line, with nothing added between them or at the end.</summary>
    [TestCase(2023, Description = "a common year; April and December end on a Sunday")]
    [TestCase(2024, Description = "a leap year; March and June end on a Sunday")]
    [TestCase(2021, Description = "January, February and October end on a Sunday")]
    [TestCase(1, Description = "the first year")]
    [TestCase(9999, Description = "the last year")]
    public void AYear_HasOneBlankLineAfterEachMonth_AndOneTerminatorThroughout(int year)
    {
        foreach (string newLine in Terminators)
        {
            string text = Year(year, newLine);
            string[] lines = text.Split(newLine);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(text, Is.EqualTo(LegacyReference.Year(year, LegacyReference.EnglishMonthName, newLine)));
                Assert.That(text.Replace(newLine, string.Empty), Does.Not.Contain("\n").And.Not.Contain("\r"), "no line end but the writer's");
                Assert.That(lines.Count(line => line.Length == 0), Is.EqualTo(12 + 1), "twelve blank lines, and the empty piece after the last line end");
                Assert.That(Enumerable.Range(0, lines.Length - 1).Count(i => lines[i].Length == 0 && lines[i + 1].Length == 0), Is.EqualTo(1),
                    "two empty pieces follow each other only at the very end: the last blank line and the end of the text");
                Assert.That(lines.Where(line => line == LegacyReference.WeekdayHeader).Count(), Is.EqualTo(12), "twelve months");
            }
        }
    }

    [Test]
    public void SelectedDates_ChangeNothingAboutTheLineEnds()
    {
        var lastDays = Enumerable.Range(1, 12).Select(month => new DateTime(2023, month, DateTime.DaysInMonth(2023, month))).ToList();

        foreach (string newLine in Terminators)
        {
            Assert.That(Year(2023, newLine, lastDays), Is.EqualTo(Year(2023, newLine)));
        }
    }

    // ---- the platform's own terminator ----

    /// <summary>What a default writer produces on the machine running the tests.</summary>
    [Test]
    public void WithThePlatformsDefaultNewline_EveryLineEndsWithIt()
    {
        string text = Month(5, 2023, Environment.NewLine);

        Assert.That(text, Is.EqualTo(LegacyReference.Month(2023, 5, "May", Environment.NewLine)));
        Assert.That(text, Does.EndWith("29 30 31" + Environment.NewLine + Environment.NewLine).Using(StringComparison.Ordinal));
    }

    [Test]
    [Platform(Include = "Win", Reason = "Windows is where the default newline is \\r\\n.")]
    public void OnWindows_TheDefaultNewlineGivesCarriageReturnLineFeedThroughout()
    {
        string text = Month(5, 2023, Environment.NewLine);

        Assert.That(Environment.NewLine, Is.EqualTo("\r\n"));
        Assert.That(text, Does.StartWith("     May 2023\r\nMo Tu We Th Fr Sa Su\r\n").Using(StringComparison.Ordinal));
        Assert.That(text, Does.EndWith("22 23 24 25 26 27 28\r\n29 30 31\r\n\r\n").Using(StringComparison.Ordinal));
        Assert.That(text.Replace("\r\n", string.Empty), Ordinal.DoesNotContain("\n"), "no bare line feed is left");
    }

    [Test]
    [Platform(Exclude = "Win", Reason = "Linux and macOS are where the default newline is \\n.")]
    public void OnLinuxAndMacOS_TheDefaultNewlineGivesLineFeedsOnly()
    {
        Assert.That(Environment.NewLine, Is.EqualTo("\n"));
        Assert.That(Month(5, 2023, Environment.NewLine), Ordinal.DoesNotContain("\r"));
    }
}
