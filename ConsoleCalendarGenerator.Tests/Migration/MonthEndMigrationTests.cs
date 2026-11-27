using System.Globalization;
using ConsoleCalendarGenerator.Tests.Historical;
using ConsoleCalendarGenerator.Tests.Support;

namespace ConsoleCalendarGenerator.Tests.Migration;

/// <summary>
/// The third and fourth corrections of version 2.0, which are both about how a month ends, as the exact
/// difference between the frozen source of 1.0.0 and the library:
///
/// <list type="bullet">
/// <item>
/// D-59. The last row of days ends with the console's line terminator. 1.0.0 wrote a literal "\n" there. That is
/// one call made another way for every month, and other text wherever the terminator is not a line feed.
/// </item>
/// <item>
/// D-60. Every month is followed by exactly one blank line. 1.0.0 wrote two after a month whose last day is a
/// Sunday. That is one call fewer for such a month, and one line less.
/// </item>
/// </list>
///
/// <para>
/// Nothing else differs: not one call before the end of the month, not a color, not what is left behind, not an
/// exception. The two differences are not allowed loosely. They are the two functions of
/// <see cref="MonthEnd"/>, applied to the calls 1.0.0 made, and the result must be the calls the library makes,
/// one by one.
/// </para>
/// <para>
/// The 1.0.0 side is in Historical/Published100MonthEndTests.cs on its own, and the 2.0 side in
/// Legacy/LineEndingTests.cs. Here the 1.0.0 side is 1.0.0 with the corrections made before these two applied,
/// so that what is compared is these two alone.
/// </para>
/// </summary>
[TestFixture]
public class MonthEndMigrationTests
{
    private static readonly string[] Terminators = { "\n", "\r\n", "<EOL>" };

    private static readonly ApprovedCorrection[] TheEndOfAMonth = { ApprovedCorrection.LineTerminator, ApprovedCorrection.OneBlankLine };

    private static LegacyScenario Month(int month, int year, string newLine = "\n", IReadOnlyList<DateTime>? selected = null) => new($"month {month} of year {year}")
    {
        Draw = LegacyDraw.MonthWithDates,
        Month = month,
        Year = year,
        Dates = selected ?? Array.Empty<DateTime>(),
        NewLine = newLine,
        ForegroundBefore = ConsoleColor.DarkCyan,
        BackgroundBefore = ConsoleColor.DarkBlue,
    };

    private static LegacyScenario Year(int year, string newLine, IReadOnlyList<DateTime>? selected = null) => new($"year {year}")
    {
        Draw = LegacyDraw.YearWithDates,
        Year = year,
        Dates = selected ?? Array.Empty<DateTime>(),
        NewLine = newLine,
    };

    /// <summary>What 1.0.0 did, with the corrections other than these two applied.</summary>
    private static LegacyObservation In100(LegacyScenario scenario) => ApprovedCorrection.ExpectedWithout(TheEndOfAMonth, scenario, CultureInfo.InvariantCulture);

    private static LegacyObservation In20(LegacyScenario scenario) => LegacyObservation.Of(LegacySubject.Library, scenario, CultureInfo.InvariantCulture);

    private static string[] Calls(LegacyObservation observation) => Calls(observation.Calls);

    private static string[] Calls(IEnumerable<RecordedCall> calls) =>
        calls.Select(call => call.Method + "[" + call.Text.Replace("\n", "\\n", StringComparison.Ordinal) + "]").ToArray();

    private static bool EndsOnSunday(int month, int year) => new DateTime(year, month, DateTime.DaysInMonth(year, month)).DayOfWeek == DayOfWeek.Sunday;

    // ---- the matrix: a month ending on each day of the week ----

    /// <summary>
    /// A month ending on each day of the week, of each length, with four, five and six rows, and the first and
    /// the last month there is, under three terminators: the calls that end it in 1.0.0 and in 2.0, the text of
    /// both, and that everything up to the last day is the same call for call.
    /// </summary>
    [TestCase(4, 2023, DayOfWeek.Sunday)]
    [TestCase(7, 2023, DayOfWeek.Monday)]
    [TestCase(2, 2023, DayOfWeek.Tuesday)]
    [TestCase(5, 2023, DayOfWeek.Wednesday)]
    [TestCase(2, 2024, DayOfWeek.Thursday)]
    [TestCase(3, 2023, DayOfWeek.Friday)]
    [TestCase(9, 2023, DayOfWeek.Saturday)]
    [TestCase(2, 2021, DayOfWeek.Sunday, Description = "28 days, four full weeks")]
    [TestCase(1, 2023, DayOfWeek.Tuesday, Description = "31 days, six rows")]
    [TestCase(3, 2024, DayOfWeek.Sunday, Description = "31 days")]
    [TestCase(1, 1, DayOfWeek.Wednesday, Description = "the first month there is")]
    [TestCase(12, 9999, DayOfWeek.Friday, Description = "the last month there is")]
    public void TheEndOfAMonth_In100AndIn20(int month, int year, DayOfWeek lastWeekday)
    {
        Assert.That(new DateTime(year, month, DateTime.DaysInMonth(year, month)).DayOfWeek, Is.EqualTo(lastWeekday), "the premise of the case");
        bool sunday = lastWeekday == DayOfWeek.Sunday;
        string name = LegacyReference.EnglishMonthName(month);

        foreach (string newLine in Terminators)
        {
            LegacyObservation published100 = In100(Month(month, year, newLine));
            LegacyObservation library = In20(Month(month, year, newLine));
            int upToTheLastDay = library.Calls.Count - 2;

            using (Assert.EnterMultipleScope())
            {
                // How the month ends.
                Assert.That(Calls(published100).Skip(upToTheLastDay), Is.EqualTo(sunday ? new[] { "L[]", "S[\\n]", "L[]" } : new[] { "S[\\n]", "L[]" }), "1.0.0");
                Assert.That(Calls(library).Skip(upToTheLastDay), Is.EqualTo(new[] { "L[]", "L[]" }), "2.0");
                Assert.That(library.Calls, Has.Count.EqualTo(published100.Calls.Count - (sunday ? 1 : 0)), "one call fewer after a Sunday, and otherwise as many");
                Assert.That(Calls(library), Has.None.EqualTo("S[\\n]"), "2.0 writes no line feed as text");

                // Everything before that is the same call, in the same colors.
                Assert.That(library.Calls.Take(upToTheLastDay), Is.EqualTo(published100.Calls.Take(upToTheLastDay)), "up to the last day");

                // The text of both, from the rules of each version.
                Assert.That(published100.Text, Is.EqualTo(Published100Text.Month(year, month, name, newLine)), "the text of 1.0.0");
                Assert.That(library.Text, Is.EqualTo(LegacyReference.Month(year, month, name, newLine)), "the text of 2.0");
                Assert.That(library.Text.Replace(newLine, string.Empty), Does.Not.Contain("\n").And.Not.Contain("\r"), "2.0 has no line end but the writer's");

                // In which aspects the two differ: the calls always; the text unless the terminator is a line feed
                // and the month does not end on a Sunday; the list of colors only where a call is gone.
                LegacyAspects expected = LegacyAspects.Calls
                    | (sunday || newLine != "\n" ? LegacyAspects.Text : LegacyAspects.None)
                    | (sunday ? LegacyAspects.Colors : LegacyAspects.None);
                Assert.That(library.AspectsThatDifferFrom(published100), Is.EqualTo(expected), "terminator " + newLine.Replace("\r", "\\r").Replace("\n", "\\n"));
            }
        }
    }

    // ---- the two differences, one at a time ----

    /// <summary>
    /// D-59 on its own: the literal line feed becomes a line end, in the same color, and nothing else changes.
    /// A month that ends on a Sunday then still has its second blank line. D-60 after it: that line is gone,
    /// and the result is call for call what the library does.
    /// </summary>
    [TestCase(5, 2023, Description = "ends on a Wednesday")]
    [TestCase(4, 2023, Description = "ends on a Sunday")]
    [TestCase(2, 2021, Description = "ends on a Sunday, four full weeks")]
    [TestCase(7, 2023, Description = "ends on a Monday, alone in its row")]
    public void TheDifference_IsTheLineTerminator_ThenTheBlankLine_AndNothingElse(int month, int year)
    {
        bool sunday = EndsOnSunday(month, year);
        foreach (string newLine in Terminators)
        {
            LegacyObservation published100 = In100(Month(month, year, newLine, new[] { new DateTime(year, month, DateTime.DaysInMonth(year, month)) }));
            LegacyObservation library = In20(Month(month, year, newLine, new[] { new DateTime(year, month, DateTime.DaysInMonth(year, month)) }));

            IReadOnlyList<RecordedCall> withTheTerminator = MonthEnd.WithTheConsolesTerminator(published100.Calls);
            IReadOnlyList<RecordedCall> withOneBlankLine = MonthEnd.WithOneBlankLine(withTheTerminator);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(withTheTerminator, Has.Count.EqualTo(published100.Calls.Count), "D-59 changes how one call is made, not how many there are");
                Assert.That(Enumerable.Range(0, withTheTerminator.Count).Count(index => withTheTerminator[index] != published100.Calls[index]), Is.EqualTo(1), "one call");
                Assert.That(Calls(withTheTerminator).TakeLast(sunday ? 3 : 2), Is.EqualTo(sunday ? new[] { "L[]", "L[]", "L[]" } : new[] { "L[]", "L[]" }), "after D-59");
                Assert.That(withOneBlankLine, Has.Count.EqualTo(withTheTerminator.Count - (sunday ? 1 : 0)), "D-60 removes one call after a Sunday and none otherwise");
                Assert.That(ReferenceEquals(withOneBlankLine, withTheTerminator), Is.EqualTo(!sunday), "and leaves every other month as it is");
                Assert.That(library.Calls, Is.EqualTo(withOneBlankLine), "which is what the library does, call for call, in the same colors");
            }
        }
    }

    // ---- every month ----

    /// <summary>
    /// Every month of 201 years and of the first and the last year. The library does what 1.0.0 did with the two
    /// differences applied; its text is what the rules of 2.0 give; D-59 changes every month and D-60 exactly the
    /// months that end on a Sunday, whose number 1.0.0's own record establishes.
    /// </summary>
    [Test]
    public void EveryMonthFrom1900Through2100_AndOfTheYears1And9999_DiffersInTheseTwoWaysOnly()
    {
        int months = 0;
        int changedByTheTerminator = 0;
        int changedByTheBlankLine = 0;
        int changedByTheBlankLineFrom1900Through2100 = 0;
        foreach (int year in Enumerable.Range(1900, 201).Append(1).Append(9999))
        {
            for (int month = 1; month <= 12; month++)
            {
                LegacyScenario scenario = Month(month, year);
                LegacyObservation published100 = In100(scenario);
                LegacyObservation library = In20(scenario);
                IReadOnlyList<RecordedCall> withTheTerminator = MonthEnd.WithTheConsolesTerminator(published100.Calls);
                IReadOnlyList<RecordedCall> withOneBlankLine = MonthEnd.WithOneBlankLine(withTheTerminator);
                bool blankLineRemoved = !ReferenceEquals(withOneBlankLine, withTheTerminator);

                if (!library.Calls.SequenceEqual(withOneBlankLine)
                    || library.Text != LegacyReference.Month(year, month, LegacyReference.EnglishMonthName(month))
                    || library.ExceptionType != null
                    || (library.ForegroundAfter, library.BackgroundAfter) != (published100.ForegroundAfter, published100.BackgroundAfter)
                    || blankLineRemoved != EndsOnSunday(month, year))
                {
                    Assert.Fail($"{year}-{month:00} differs from 1.0.0 in more than the end of the month.");
                }
                months++;
                changedByTheTerminator += ReferenceEquals(withTheTerminator, published100.Calls) ? 0 : 1;
                changedByTheBlankLine += blankLineRemoved ? 1 : 0;
                changedByTheBlankLineFrom1900Through2100 += blankLineRemoved && year >= 1900 && year <= 2100 ? 1 : 0;
            }
        }

        Assert.That(months, Is.EqualTo(203 * 12));
        Assert.That(changedByTheTerminator, Is.EqualTo(months), "every month ended with the literal line feed in 1.0.0");
        Assert.That(changedByTheBlankLineFrom1900Through2100, Is.EqualTo(Published100MonthEndTests.MonthsEndingOnSundayFrom1900Through2100),
            "the months of 1900 through 2100 that had the second blank line in 1.0.0");
        Assert.That(changedByTheBlankLine, Is.EqualTo(Published100MonthEndTests.MonthsEndingOnSundayFrom1900Through2100 + 1 + 3),
            "and September of the year 1, and January, February and October of the year 9999");
    }

    // ---- a year ----

    /// <summary>
    /// A whole year with selected dates, under each terminator: the twelve months in order, each ending the new
    /// way, with nothing added for the year, and everything else as in 1.0.0.
    /// </summary>
    [TestCase(2023, 2, Description = "a common year; April and December end on a Sunday")]
    [TestCase(2024, 2, Description = "a leap year; March and June end on a Sunday")]
    [TestCase(2021, 3, Description = "January, February and October end on a Sunday")]
    [TestCase(1, 1, Description = "the first year")]
    [TestCase(9999, 3, Description = "the last year")]
    public void AYear_In100AndIn20(int year, int monthsEndingOnSunday)
    {
        Assert.That(Enumerable.Range(1, 12).Count(month => EndsOnSunday(month, year)), Is.EqualTo(monthsEndingOnSunday), "the premise of the case");
        DateTime[] selected = Enumerable.Range(1, 12).SelectMany(month => new[] { new DateTime(year, month, 1), new DateTime(year, month, DateTime.DaysInMonth(year, month)) }).ToArray();

        foreach (string newLine in Terminators)
        {
            LegacyObservation published100 = In100(Year(year, newLine, selected));
            LegacyObservation library = In20(Year(year, newLine, selected));
            string[] titles = library.Calls.Where(call => call.Method == "LS" && call.Text != LegacyReference.WeekdayHeader).Select(call => call.Text).ToArray();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(library.Calls, Is.EqualTo(MonthEnd.Corrected(published100.Calls)), "1.0.0 with the two differences, call for call and color for color");
                Assert.That(titles, Is.EqualTo(Enumerable.Range(1, 12).Select(month => LegacyReference.Title(LegacyReference.EnglishMonthName(month), year))), "twelve months, January first");
                Assert.That(Calls(published100).Count(call => call == "S[\\n]"), Is.EqualTo(12), "1.0.0: one literal line feed for each month");
                Assert.That(Calls(library).Count(call => call == "S[\\n]"), Is.Zero, "2.0: none");
                Assert.That(library.Calls, Has.Count.EqualTo(published100.Calls.Count - monthsEndingOnSunday), "one call fewer for each month that ends on a Sunday");
                Assert.That(library.Text, Is.EqualTo(LegacyReference.Year(year, LegacyReference.EnglishMonthName, newLine)), "the text of 2.0");
                Assert.That(published100.Text, Is.EqualTo(Published100Text.Year(year, Published100Text.EnglishMonthName, newLine)), "the text of 1.0.0");
                Assert.That(library.Text, Does.EndWith(DateTime.DaysInMonth(year, 12).ToString(CultureInfo.InvariantCulture) + newLine + newLine).Using(StringComparison.Ordinal),
                    "the year ends as December ends: nothing is added for the year");
                Assert.That(library.StoredColors, Is.EqualTo(published100.StoredColors));
                Assert.That((library.ForegroundAfter, library.BackgroundAfter), Is.EqualTo((published100.ForegroundAfter, published100.BackgroundAfter)));
            }
        }
    }

    // ---- colors ----

    /// <summary>
    /// The two calls that end a month are made in the color of the last day, selected or not, as the calls that
    /// ended it in 1.0.0 were, and the colors are reset after them, as they were. No color is set that 1.0.0 did
    /// not set, and none is left out.
    /// </summary>
    [Test]
    [Platform(Exclude = "Win", Reason = ConsoleState.ColorsObservableOnUnixOnly)]
    public void TheCallsThatEndAMonth_AreMadeInTheColorOfTheLastDay_AndTheColorsAreResetAfterwards_In100AndIn20()
    {
        foreach ((int month, int year) in new[] { (5, 2023), (4, 2023) })
        {
            var lastDay = new DateTime(year, month, DateTime.DaysInMonth(year, month));
            foreach ((IReadOnlyList<DateTime> selected, ConsoleColor color) in new[] { ((IReadOnlyList<DateTime>)Array.Empty<DateTime>(), ConsoleColor.White), (new[] { lastDay }, ConsoleColor.Blue) })
            {
                LegacyObservation published100 = In100(Month(month, year, selected: selected));
                LegacyObservation library = In20(Month(month, year, selected: selected));

                Assert.That(published100.Calls.TakeLast(3).Select(call => call.Foreground), Is.All.EqualTo(color), "1.0.0");
                Assert.That(library.Calls.TakeLast(3).Select(call => call.Foreground), Is.All.EqualTo(color), "2.0");
                Assert.That(library.Calls.Select(call => call.Background).Distinct(), Is.EqualTo(published100.Calls.Select(call => call.Background).Distinct()), "backgrounds");
                Assert.That((library.ForegroundAfter, library.BackgroundAfter), Is.EqualTo((ConsoleState.Unset, ConsoleState.Unset)), "reset, not restored");
                Assert.That((published100.ForegroundAfter, published100.BackgroundAfter), Is.EqualTo((ConsoleState.Unset, ConsoleState.Unset)));
            }
        }
    }

    // ---- where no month ends, nothing differs ----

    /// <summary>
    /// A call that does not reach the end of a month is the same in both: an invalid month or year, a null list
    /// apart from its exception, and a color that is not defined, which stops the drawing where it always did.
    /// </summary>
    [Test]
    public void WhereNoMonthEnds_NothingDiffers()
    {
        var undefined = (ConsoleColor)99;
        var selection = new[] { new DateTime(2023, 2, 14) };
        LegacyScenario[] scenarios =
        {
            new("an invalid month") { Draw = LegacyDraw.Month, Month = 13, Year = 2023 },
            new("an invalid year") { Draw = LegacyDraw.YearWithDates, Year = 0, Dates = selection },
            new("a null list") { Draw = LegacyDraw.MonthWithDates, Month = 2, Year = 2023, Dates = null },
            new("an undefined title color") { Setters = new[] { (LegacyColorRole.Title, undefined) }, Draw = LegacyDraw.Month, Month = 2, Year = 2023 },
            new("an undefined weekday color") { Setters = new[] { (LegacyColorRole.DayOfWeek, undefined) }, Draw = LegacyDraw.Month, Month = 2, Year = 2023 },
            new("an undefined date color") { Setters = new[] { (LegacyColorRole.Date, undefined) }, Draw = LegacyDraw.Month, Month = 2, Year = 2023 },
            new("an undefined selected color") { Setters = new[] { (LegacyColorRole.SelectedDate, undefined) }, Draw = LegacyDraw.MonthWithDates, Month = 2, Year = 2023, Dates = selection },
            new("an undefined constructor argument") { ConstructorArguments = new[] { ConsoleColor.Red, ConsoleColor.Green, ConsoleColor.Magenta, undefined }, Draw = LegacyDraw.Month, Month = 2, Year = 2023 },
            new("only the colors held") { ConstructorArguments = new[] { ConsoleColor.Red } },
        };

        foreach (LegacyScenario scenario in scenarios)
        {
            Assert.That(In20(scenario).DifferencesFrom(In100(scenario)), Is.Empty, scenario.Name);
        }
    }

    /// <summary>
    /// A color that is not defined and is first used in a later month of a year: the months before it end the
    /// new way, and the drawing stops at the same call as in 1.0.0.
    /// </summary>
    [Test]
    public void AYearThatFailsPartWay_EndsTheMonthsBeforeTheFailureTheNewWay_AndFailsAtTheSameCall()
    {
        var scenario = new LegacyScenario("a year")
        {
            Setters = new[] { (LegacyColorRole.SelectedDate, (ConsoleColor)99) },
            Draw = LegacyDraw.YearWithDates,
            Year = 2023,
            Dates = new[] { new DateTime(2023, 5, 17) },
        };

        LegacyObservation published100 = In100(scenario);
        LegacyObservation library = In20(scenario);

        Assert.That(library.ExceptionType, Is.EqualTo("System.ArgumentException"));
        Assert.That(library.ExceptionType, Is.EqualTo(published100.ExceptionType));
        Assert.That(library.Calls, Is.EqualTo(MonthEnd.Corrected(published100.Calls)));
        Assert.That(Calls(library).TakeLast(1), Is.EqualTo(new[] { "S[ 16]" }), "the day before the selected one is the last thing written");
        Assert.That(published100.Calls.Count - library.Calls.Count, Is.EqualTo(1), "April, which ends on a Sunday, is the one month before the failure with a call fewer");
    }

    // ---- the two functions themselves ----

    private static RecordedCall Text(string text) => new("S", text, false, ConsoleColor.White, ConsoleColor.Black);

    private static RecordedCall LineEnd(ConsoleColor color = ConsoleColor.White) => new("L", string.Empty, true, color, ConsoleColor.Black);

    private static RecordedCall LiteralLineFeed(ConsoleColor color = ConsoleColor.White) => new("S", "\n", false, color, ConsoleColor.Black);

    [Test]
    public void TheLineTerminatorCorrection_ReplacesTheLiteralLineFeed_InTheSameColor_AndTouchesNothingElse()
    {
        RecordedCall[] calls = { Text(" 30"), Text(" 31"), LiteralLineFeed(ConsoleColor.Blue), LineEnd(ConsoleColor.Blue), Text("after") };

        IReadOnlyList<RecordedCall> corrected = MonthEnd.WithTheConsolesTerminator(calls);

        Assert.That(corrected, Is.EqualTo(new[] { Text(" 30"), Text(" 31"), LineEnd(ConsoleColor.Blue), LineEnd(ConsoleColor.Blue), Text("after") }));
    }

    [Test]
    public void TheBlankLineCorrection_RemovesOneOfThreeLineEndsInARow_AndLeavesOneOrTwoAlone()
    {
        RecordedCall[] calls = { Text(" 7"), LineEnd(), Text(" 30"), LineEnd(), LineEnd(), LineEnd(), Text("next"), LineEnd(), LineEnd() };

        IReadOnlyList<RecordedCall> corrected = MonthEnd.WithOneBlankLine(calls);

        Assert.That(corrected, Is.EqualTo(new[] { Text(" 7"), LineEnd(), Text(" 30"), LineEnd(), LineEnd(), Text("next"), LineEnd(), LineEnd() }));
    }

    [Test]
    public void BothCorrections_ReturnTheListTheyWereGiven_WhenThereIsNothingToChange()
    {
        RecordedCall[] calls = { Text("title"), LineEnd(), Text(" 1"), Text("  2"), LineEnd(), LineEnd() };

        Assert.That(MonthEnd.WithTheConsolesTerminator(calls), Is.SameAs(calls));
        Assert.That(MonthEnd.WithOneBlankLine(calls), Is.SameAs(calls));
        Assert.That(MonthEnd.Corrected(calls), Is.SameAs(calls));
    }

    /// <summary>
    /// The corrections are about the end of a month as 1.0.0 wrote it and nothing looser. A line feed written
    /// anywhere else, or more blank lines than 1.0.0 ever wrote, is refused and not quietly tidied.
    /// </summary>
    [Test]
    public void TheCorrections_RefuseWhatIsNotTheEndOfAMonth()
    {
        Assert.That(() => MonthEnd.WithTheConsolesTerminator(new[] { Text(" 31"), LiteralLineFeed(), Text("more") }), Throws.InvalidOperationException);
        Assert.That(() => MonthEnd.WithTheConsolesTerminator(new[] { Text(" 31"), LiteralLineFeed() }), Throws.InvalidOperationException);
        Assert.That(() => MonthEnd.WithOneBlankLine(new[] { Text(" 31"), LineEnd(), LineEnd(), LineEnd(), LineEnd() }), Throws.InvalidOperationException);
        Assert.That(MonthEnd.WithTheConsolesTerminator(new[] { Text("a\nb"), Text("\n\n") }), Has.Count.EqualTo(2), "text that merely contains a line feed is not the literal");
    }

    [Test]
    public void TheLineTerminatorCorrection_ComesBeforeTheBlankLineCorrection()
    {
        var all = ApprovedCorrection.All.ToList();

        Assert.That(all.IndexOf(ApprovedCorrection.LineTerminator), Is.LessThan(all.IndexOf(ApprovedCorrection.OneBlankLine)));
        Assert.That(ApprovedCorrection.LineTerminator.IsImplemented && ApprovedCorrection.OneBlankLine.IsImplemented, Is.True);
    }

    /// <summary>The same two functions for the other form in which the tests record calls.</summary>
    [Test]
    public void TheCorrections_DoTheSameToWriteEvents()
    {
        WriteEvent Write(string payload, bool endsLine = false) => new(payload, endsLine, ConsoleColor.White, ConsoleColor.Black);
        WriteEvent[] sunday = { Write(" 30"), Write(string.Empty, true), Write("\n"), Write(string.Empty, true) };
        WriteEvent[] other = { Write(" 31"), Write("\n"), Write(string.Empty, true) };

        Assert.That(MonthEnd.Corrected(sunday), Is.EqualTo(new[] { Write(" 30"), Write(string.Empty, true), Write(string.Empty, true) }));
        Assert.That(MonthEnd.Corrected(other), Is.EqualTo(new[] { Write(" 31"), Write(string.Empty, true), Write(string.Empty, true) }));
    }
}
