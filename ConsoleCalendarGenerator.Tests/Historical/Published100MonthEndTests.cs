using System.Globalization;
using ConsoleCalendarGenerator.Tests.Migration;
using ConsoleCalendarGenerator.Tests.Support;

namespace ConsoleCalendarGenerator.Tests.Historical;

/// <summary>
/// How a month ended in version 1.0.0, complete. Every line was ended with <c>Console.WriteLine</c>, and so with
/// the line terminator of the console's writer, except the last row of days: after the last day 1.0.0 wrote a
/// literal "\n", and then one line end for the blank line. A row was also ended after every Sunday, so a month
/// whose last day is a Sunday had its last row ended twice and was followed by two blank lines.
///
/// <para>
/// Everything here runs the frozen source of 1.0.0. Version 2.0 ends the last row with the console's terminator
/// (D-59) and follows every month with exactly one blank line (D-60); these tests stay as they are, as the
/// record of what was corrected. The corrected behavior is in Legacy/LineEndingTests.cs, and the two side by
/// side in Migration/MonthEndMigrationTests.cs.
/// </para>
/// </summary>
[TestFixture]
public class Published100MonthEndTests
{
    private static readonly string[] Terminators = { "\n", "\r\n", "<EOL>" };

    private static LegacyObservation Month(int month, int year, string newLine = "\n", IReadOnlyList<DateTime>? selected = null) => LegacyObservation.Of(
        LegacySubject.Published100,
        new LegacyScenario("a month")
        {
            Draw = LegacyDraw.MonthWithDates,
            Month = month,
            Year = year,
            Dates = selected ?? Array.Empty<DateTime>(),
            NewLine = newLine,
        },
        CultureInfo.InvariantCulture);

    private static LegacyObservation Year(int year, string newLine = "\n", IReadOnlyList<DateTime>? selected = null) => LegacyObservation.Of(
        LegacySubject.Published100,
        new LegacyScenario("a year") { Draw = LegacyDraw.YearWithDates, Year = year, Dates = selected ?? Array.Empty<DateTime>(), NewLine = newLine },
        CultureInfo.InvariantCulture);

    private static string[] Calls(LegacyObservation observation) =>
        observation.Calls.Select(call => call.Method + "[" + call.Text.Replace("\n", "\\n", StringComparison.Ordinal) + "]").ToArray();

    // ---- the calls that ended a month ----

    [Test]
    public void In100_AMonth_EndedWithALiteralLineFeed_ThenOneLineEnd()
    {
        string[] calls = Calls(Month(5, 2023));

        Assert.That(calls.TakeLast(4), Is.EqualTo(new[] { "S[ 30]", "S[ 31]", "S[\\n]", "L[]" }));
        Assert.That(calls.Count(call => call == "S[\\n]"), Is.EqualTo(1));
    }

    [Test]
    public void In100_AMonthThatEndsOnASunday_EndedItsRow_ThenWroteTheLiteralLineFeed_ThenOneLineEnd()
    {
        string[] calls = Calls(Month(4, 2023));

        Assert.That(calls.TakeLast(5), Is.EqualTo(new[] { "S[ 29]", "S[ 30]", "L[]", "S[\\n]", "L[]" }));
    }

    /// <summary>The calls did not depend on the console's terminator: the literal was a literal under every one.</summary>
    [Test]
    public void In100_TheCalls_WereTheSameWhateverTheTerminator()
    {
        foreach ((int month, int year) in new[] { (5, 2023), (4, 2023) })
        {
            string[] withLineFeed = Calls(Month(month, year, "\n"));

            Assert.That(Calls(Month(month, year, "\r\n")), Is.EqualTo(withLineFeed));
            Assert.That(Calls(Month(month, year, "<EOL>")), Is.EqualTo(withLineFeed));
        }
    }

    // ---- the text ----

    [Test]
    public void In100_WithACarriageReturnLineFeedWriter_AMonthThatEndsOnASunday_HadTheBareLineFeedOnItsFirstBlankLine()
    {
        Assert.That(Month(4, 2023, "\r\n").Text, Is.EqualTo(
            "    April 2023\r\n" +
            "Mo Tu We Th Fr Sa Su\r\n" +
            "                1  2\r\n" +
            " 3  4  5  6  7  8  9\r\n" +
            "10 11 12 13 14 15 16\r\n" +
            "17 18 19 20 21 22 23\r\n" +
            "24 25 26 27 28 29 30\r\n" +
            "\n" +
            "\r\n"));
    }

    [Test]
    public void In100_February2021_FourFullWeeks_WasFollowedByTwoBlankLines()
    {
        Assert.That(Month(2, 2021).Text, Is.EqualTo(Golden.Lines(
            "   February 2021",
            "Mo Tu We Th Fr Sa Su",
            " 1  2  3  4  5  6  7",
            " 8  9 10 11 12 13 14",
            "15 16 17 18 19 20 21",
            "22 23 24 25 26 27 28",
            "",
            "")));
    }

    /// <summary>
    /// A month ending on each day of the week, of each length, with four, five and six rows, and the first and
    /// the last month there is: the text under three terminators, the calls at the end, and the blank lines.
    /// </summary>
    [TestCase(4, 2023, DayOfWeek.Sunday, 30, 5)]
    [TestCase(7, 2023, DayOfWeek.Monday, 31, 6)]
    [TestCase(2, 2023, DayOfWeek.Tuesday, 28, 5)]
    [TestCase(5, 2023, DayOfWeek.Wednesday, 31, 5)]
    [TestCase(2, 2024, DayOfWeek.Thursday, 29, 5)]
    [TestCase(3, 2023, DayOfWeek.Friday, 31, 5)]
    [TestCase(9, 2023, DayOfWeek.Saturday, 30, 5)]
    [TestCase(2, 2021, DayOfWeek.Sunday, 28, 4)]
    [TestCase(1, 2023, DayOfWeek.Tuesday, 31, 6)]
    [TestCase(3, 2024, DayOfWeek.Sunday, 31, 5)]
    [TestCase(1, 1, DayOfWeek.Wednesday, 31, 5)]
    [TestCase(12, 9999, DayOfWeek.Friday, 31, 5)]
    public void In100_AMonthEnded_AsItsLastWeekdayDecided(int month, int year, DayOfWeek lastWeekday, int days, int rows)
    {
        Assert.That(new DateTime(year, month, DateTime.DaysInMonth(year, month)).DayOfWeek, Is.EqualTo(lastWeekday), "the premise of the case");
        Assert.That(DateTime.DaysInMonth(year, month), Is.EqualTo(days), "the premise of the case");
        bool endsOnSunday = lastWeekday == DayOfWeek.Sunday;
        string name = Published100Text.EnglishMonthName(month);

        foreach (string newLine in Terminators)
        {
            LegacyObservation observation = Month(month, year, newLine);
            string[] calls = Calls(observation);
            string lastRow = observation.Text.Split(new[] { newLine, "\n" }, StringSplitOptions.None).Skip(2).TakeWhile(line => line.Length > 0).Last();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(observation.Text, Is.EqualTo(Published100Text.Month(year, month, name, newLine)), "the text");
                Assert.That(observation.Text.Split(new[] { newLine, "\n" }, StringSplitOptions.None).Skip(2).TakeWhile(line => line.Length > 0).Count(), Is.EqualTo(rows), "rows");
                Assert.That(calls.TakeLast(endsOnSunday ? 3 : 2), Is.EqualTo(endsOnSunday ? new[] { "L[]", "S[\\n]", "L[]" } : new[] { "S[\\n]", "L[]" }), "the calls after the last day");
                Assert.That(observation.Text, Does.EndWith(lastRow + (endsOnSunday ? newLine : string.Empty) + "\n" + newLine).Using(StringComparison.Ordinal),
                    "the last row, the literal line feed, the line end");
                Assert.That(observation.ExceptionType, Is.Null);
            }
        }
    }

    /// <summary>
    /// Every month of 201 years: the text is what the rules of 1.0.0 give, and the months followed by two blank
    /// lines are exactly those whose last day is a Sunday. Which months those are is worked out twice without
    /// the library: with <see cref="DateTime"/> and with the whole-number calendar of the tests.
    /// </summary>
    [Test]
    public void In100_EveryMonthFrom1900Through2100_EndedByTheseRules_AndTheSundayMonthsHadTwoBlankLines()
    {
        int months = 0;
        int withTwoBlankLines = 0;
        int endingOnSunday = 0;
        int endingOnSundayByArithmetic = 0;
        for (int year = 1900; year <= 2100; year++)
        {
            for (int month = 1; month <= 12; month++)
            {
                string text = Month(month, year).Text;
                Assert.That(text, Is.EqualTo(Published100Text.Month(year, month, Published100Text.EnglishMonthName(month))), $"{year}-{month}");

                int blankLines = text.Length - text.TrimEnd('\n').Length - 1;
                Assert.That(blankLines, Is.InRange(1, 2), $"{year}-{month}");
                months++;
                withTwoBlankLines += blankLines == 2 ? 1 : 0;
                bool sunday = new DateTime(year, month, DateTime.DaysInMonth(year, month)).DayOfWeek == DayOfWeek.Sunday;
                endingOnSunday += sunday ? 1 : 0;
                endingOnSundayByArithmetic += ReferenceCalendar.WeekdayOf(year, month, ReferenceCalendar.DaysInMonth(year, month)) == DayOfWeek.Sunday ? 1 : 0;
                Assert.That(blankLines == 2, Is.EqualTo(sunday), $"{year}-{month}");
            }
        }

        Assert.That(months, Is.EqualTo(2412));
        Assert.That(endingOnSundayByArithmetic, Is.EqualTo(endingOnSunday), "the two calendars agree");
        Assert.That(withTwoBlankLines, Is.EqualTo(endingOnSunday));
        Assert.That(withTwoBlankLines, Is.EqualTo(MonthsEndingOnSundayFrom1900Through2100));
    }

    /// <summary>
    /// How many of the 2,412 months from 1900 through 2100 end on a Sunday, and so had the second blank line in
    /// 1.0.0. The test above works the number out with two calendars that share nothing; this is their answer.
    /// </summary>
    internal const int MonthsEndingOnSundayFrom1900Through2100 = 345;

    // ---- a year ----

    /// <summary>A year was its twelve months one after the other, each ending by the same rules, with nothing added.</summary>
    [TestCase(2023, 2, Description = "a common year; April and December end on a Sunday")]
    [TestCase(2024, 2, Description = "a leap year; March and June end on a Sunday")]
    [TestCase(2021, 3, Description = "January, February with its four full weeks, and October end on a Sunday")]
    [TestCase(2022, 1, Description = "only July ends on a Sunday")]
    [TestCase(1, 1, Description = "the first year; September ends on a Sunday")]
    [TestCase(9999, 3, Description = "the last year; January, February and October end on a Sunday")]
    public void In100_AYear_WasItsTwelveMonths_EachEndingByTheSameRules(int year, int monthsEndingOnSunday)
    {
        int sundays = Enumerable.Range(1, 12).Count(month => new DateTime(year, month, DateTime.DaysInMonth(year, month)).DayOfWeek == DayOfWeek.Sunday);
        Assert.That(sundays, Is.EqualTo(monthsEndingOnSunday), "the premise of the case");

        foreach (string newLine in Terminators)
        {
            LegacyObservation observation = Year(year, newLine);

            Assert.That(observation.Text, Is.EqualTo(Published100Text.Year(year, Published100Text.EnglishMonthName, newLine)));
            Assert.That(Calls(observation).Count(call => call == "S[\\n]"), Is.EqualTo(12), "one literal line feed for each month");
            Assert.That(Calls(observation).Count(call => call == "L[]"), Is.EqualTo(
                Enumerable.Range(1, 12).Sum(month => LineEndsOf(year, month))), "row ends and blank lines, and one more for each month that ends on a Sunday");
        }
    }

    /// <summary>The empty <c>WriteLine</c> calls of a month in 1.0.0: one after each Sunday, and one for the blank line.</summary>
    private static int LineEndsOf(int year, int month) =>
        Enumerable.Range(1, DateTime.DaysInMonth(year, month)).Count(day => new DateTime(year, month, day).DayOfWeek == DayOfWeek.Sunday) + 1;

    // ---- colors ----

    /// <summary>The literal line feed and the line end after it were written in the color of the last day, selected or not.</summary>
    [Test]
    [Platform(Exclude = "Win", Reason = ConsoleState.ColorsObservableOnUnixOnly)]
    public void In100_TheCallsAfterTheLastDay_WereMadeInTheColorOfTheLastDay()
    {
        LegacyObservation plain = Month(4, 2023);
        LegacyObservation lastDaySelected = Month(4, 2023, selected: new[] { new DateTime(2023, 4, 30) });

        Assert.That(plain.Calls.TakeLast(4).Select(call => call.Foreground), Is.All.EqualTo(ConsoleColor.White));
        Assert.That(lastDaySelected.Calls.TakeLast(4).Select(call => call.Foreground), Is.All.EqualTo(ConsoleColor.Blue));
        Assert.That((plain.ForegroundAfter, plain.BackgroundAfter), Is.EqualTo((ConsoleState.Unset, ConsoleState.Unset)), "then the colors were reset");
    }

    // ---- the platform's own terminator ----

    /// <summary>What 1.0.0 gave a default console on Windows: two kinds of line end in one month.</summary>
    [Test]
    [Platform(Include = "Win", Reason = "Windows is where the default newline is \\r\\n.")]
    public void In100_OnWindows_TheDefaultTerminatorGaveMixedLineEndings()
    {
        string text = Month(5, 2023, Environment.NewLine).Text;

        Assert.That(Environment.NewLine, Is.EqualTo("\r\n"));
        Assert.That(text, Does.EndWith("22 23 24 25 26 27 28\r\n29 30 31\n\r\n").Using(StringComparison.Ordinal));
    }

    [Test]
    [Platform(Exclude = "Win", Reason = "Linux and macOS are where the default newline is \\n.")]
    public void In100_OnLinuxAndMacOS_TheDefaultTerminatorGaveLineFeedsOnly()
    {
        Assert.That(Month(5, 2023, Environment.NewLine).Text, Ordinal.DoesNotContain("\r"));
    }
}
