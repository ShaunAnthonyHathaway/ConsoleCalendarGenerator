using System.Globalization;
using ConsoleCalendarGenerator.Tests.Support;

namespace ConsoleCalendarGenerator.Tests.Migration;

/// <summary>
/// The correction of the title (D-61): version 1.0.0 and version 2.0 side by side.
///
/// <para>
/// 1.0.0 wrote, above the grid of a Gregorian month, the current culture's name for the month of the same
/// number in the culture's own calendar. Version 2.0 writes the culture's name for the Gregorian month. Under a
/// culture whose dates use the Gregorian calendar the two are the same call and nothing differs. Under any
/// other culture one thing differs: the name inside the text of the title call, and with it the indent, which
/// has its three steps by the length of the name that is written. The call, its color, its place, the year,
/// every other call, the colors left behind and the culture are the same.
/// </para>
/// <para>
/// "1.0.0" here is the frozen source with the four corrections made before this one applied
/// (<see cref="ApprovedCorrection.ExpectedWithout"/>), so that what is left to differ from the library is the
/// title alone. Names are computed from the culture at run time and never spelled out; see
/// <see cref="CultureCalendars"/>.
/// </para>
/// </summary>
[TestFixture]
public class GregorianTitleMigrationTests
{
    private static readonly ApprovedCorrection[] TheTitle = { ApprovedCorrection.GregorianTitle };

    private const LegacyAspects TextAndCalls = LegacyAspects.Text | LegacyAspects.Calls;

    private static LegacyObservation In100(LegacyScenario scenario) => ApprovedCorrection.ExpectedWithout(TheTitle, scenario, CultureOf(scenario));

    private static LegacyObservation In20(LegacyScenario scenario) => LegacyObservation.Of(LegacySubject.Library, scenario, CultureOf(scenario));

    private static CultureInfo CultureOf(LegacyScenario scenario) => scenario.ResolveCulture() ?? CultureInfo.InvariantCulture;

    private static LegacyScenario MonthUnder(CultureInfo culture, int month, int year = 2023) =>
        new($"month {month} of {year}") { Draw = LegacyDraw.Month, Month = month, Year = year, Culture = culture };

    private static IEnumerable<string> Focused() => CultureCalendars.Focused;

    private static CultureInfo Require(string label)
    {
        CultureInfo? culture = CultureCalendars.Resolve(label);
        Assume.That(culture, Is.Not.Null, "This machine does not have " + label + ".");
        return culture!;
    }

    private static CultureInfo RequireOneThatNamesOtherMonths()
    {
        CultureInfo? culture = CultureCalendars.AnyThatNamesOtherMonths();
        Assume.That(culture, Is.Not.Null, "This machine has no culture whose calendar names other months than the Gregorian one.");
        return culture!;
    }

    /// <summary>The positions of the title calls: each month begins with two calls of WriteLine(string), the title first.</summary>
    private static int[] TitlePositions(LegacyObservation observation) => observation.Calls
        .Select((call, index) => (call, index))
        .Where(entry => entry.call.Method == "LS")
        .Where((_, position) => position % 2 == 0)
        .Select(entry => entry.index)
        .ToArray();

    // ---- a month ----

    [TestCaseSource(nameof(Focused))]
    public void AMonth_DiffersFrom100InTheNameInItsTitle_AndInNothingElse(string label)
    {
        CultureInfo culture = Require(label);

        for (int month = 1; month <= 12; month++)
        {
            LegacyScenario scenario = MonthUnder(culture, month);
            LegacyObservation published = In100(scenario);
            LegacyObservation library = In20(scenario);
            string own = CultureCalendars.OwnMonthName(culture, month);
            string gregorian = GregorianTitle.MonthName(culture, month);
            string at = $"{label}, month {month}";

            // The title call, exactly: in 1.0.0 the culture's own name, in 2.0 its Gregorian name, each after
            // the indent for its length, each followed by a space and the year as given.
            Assert.That(published.Calls[0].Text, Is.EqualTo(Historical.Published100Text.Title(own, 2023)), at);
            Assert.That(library.Calls[0].Text, Is.EqualTo(LegacyReference.TitleIndent(gregorian) + gregorian + " 2023"), at);
            // The same call otherwise: the method, the line end, the colors at that moment.
            Assert.That(library.Calls[0], Is.EqualTo(published.Calls[0] with { Text = library.Calls[0].Text }), at);
            Assert.That(library.Calls[0].Method, Is.EqualTo("LS"), "one WriteLine(string)");
            // Every other call, one by one, and everything else a caller can observe.
            Assert.That(library.Calls.Skip(1), Is.EqualTo(published.Calls.Skip(1)), at);
            Assert.That(library.DifferencesFrom(published, LegacyAspects.All & ~TextAndCalls).Select(difference => difference.ToString()), Is.Empty, at);
            Assert.That(library.ExceptionType, Is.Null, at);

            // Whether anything differs at all is decided by the two names, and by nothing else.
            Assert.That(library.AspectsThatDifferFrom(published), Is.EqualTo(own == gregorian ? LegacyAspects.None : TextAndCalls), at);
        }
    }

    /// <summary>
    /// The matrix the correction was designed against, one row for each focused culture: what the culture is,
    /// what both versions write for September 2023, and whether they differ. It is written to the test's output
    /// as the record for the machine it ran on, and every cell is asserted.
    /// </summary>
    [Test]
    public void TheCultureMatrix_ForSeptember2023()
    {
        var rows = new List<string>
        {
            "culture | calendar of its dates | offers Gregorian | name in 1.0.0 | name in 2.0 | differs | year | weekday row | grid",
        };
        string grid = string.Concat(In20(MonthUnder(CultureInfo.InvariantCulture, 9)).Calls.Skip(2).Select(call => call.Method + "[" + call.Text + "]"));

        foreach (string label in CultureCalendars.Focused)
        {
            CultureInfo? culture = CultureCalendars.Resolve(label);
            if (culture == null)
            {
                rows.Add(label + " | not on this machine");
                continue;
            }
            LegacyObservation published = In100(MonthUnder(culture, 9));
            LegacyObservation library = In20(MonthUnder(culture, 9));
            string own = CultureCalendars.OwnMonthName(culture, 9);
            string gregorian = GregorianTitle.MonthName(culture, 9);
            bool offersGregorian = culture.OptionalCalendars.OfType<GregorianCalendar>().Any();

            Assert.That(offersGregorian, Is.True, label);
            Assert.That(published.Calls[0].Text, Is.EqualTo(Historical.Published100Text.Title(own, 2023)), label);
            Assert.That(library.Calls[0].Text, Is.EqualTo(LegacyReference.Title(gregorian, 2023)), label);
            Assert.That(library.Calls[0].Text, Does.EndWith(" 2023").Using(StringComparison.Ordinal), label);
            Assert.That(library.Calls[1].Text, Is.EqualTo("Mo Tu We Th Fr Sa Su"), label);
            Assert.That(string.Concat(library.Calls.Skip(2).Select(call => call.Method + "[" + call.Text + "]")), Is.EqualTo(grid), label + ": the grid is the one of the invariant culture, from Monday");
            Assert.That(string.Concat(published.Calls.Skip(2).Select(call => call.Method + "[" + call.Text + "]")), Is.EqualTo(grid), label);
            if (CultureCalendars.NamesGregorianMonths(culture))
            {
                Assert.That(gregorian, Is.EqualTo(own), label + ": a culture whose dates are Gregorian is not touched");
            }

            rows.Add(string.Join(" | ", label, culture.DateTimeFormat.Calendar.GetType().Name, offersGregorian ? "yes" : "no", own, gregorian,
                own == gregorian ? "no" : "yes", "2023", library.Calls[1].Text, "as invariant"));
        }

        foreach (string row in rows)
        {
            TestContext.Out.WriteLine(row);
        }
    }

    // ---- a year, with everything else in play ----

    /// <summary>
    /// A whole year with selected dates, a constructor that takes colors, setters, the terminator of Windows
    /// and colors the caller had set: the twelve title calls differ in their name, where the names differ, and
    /// no other call does.
    /// </summary>
    [TestCaseSource(nameof(Focused))]
    public void AYear_DiffersInTheNamesOfItsTwelveTitles_AndInNothingElse(string label)
    {
        CultureInfo culture = Require(label);
        var scenario = new LegacyScenario("the year 2024")
        {
            ConstructorArguments = new[] { ConsoleColor.Red, ConsoleColor.Green, ConsoleColor.Magenta, ConsoleColor.DarkYellow },
            Setters = new[] { (LegacyColorRole.Date, ConsoleColor.Gray) },
            Draw = LegacyDraw.YearWithDates,
            Year = 2024,
            Dates = new[] { new DateTime(2024, 1, 1), new DateTime(2024, 2, 29), new DateTime(2024, 9, 30), new DateTime(2024, 12, 31) },
            NewLine = "\r\n",
            Culture = culture,
            ForegroundBefore = ConsoleColor.DarkCyan,
            BackgroundBefore = ConsoleColor.DarkBlue,
        };

        LegacyObservation published = In100(scenario);
        LegacyObservation library = In20(scenario);
        int[] titles = TitlePositions(library);

        Assert.That(titles, Has.Length.EqualTo(12));
        Assert.That(TitlePositions(published), Is.EqualTo(titles), "the titles are where they were");
        Assert.That(library.Calls, Has.Count.EqualTo(published.Calls.Count));
        for (int index = 0; index < library.Calls.Count; index++)
        {
            int month = Array.IndexOf(titles, index) + 1;
            if (month == 0)
            {
                Assert.That(library.Calls[index], Is.EqualTo(published.Calls[index]), $"{label}, call {index + 1}");
                continue;
            }
            Assert.That(published.Calls[index].Text, Is.EqualTo(GregorianTitle.TitleIn100(culture, month, 2024)), $"{label}, month {month}");
            Assert.That(library.Calls[index], Is.EqualTo(published.Calls[index] with { Text = GregorianTitle.TitleIn20(culture, month, 2024) }), $"{label}, month {month}");
        }
        Assert.That(library.DifferencesFrom(published, LegacyAspects.All & ~TextAndCalls).Select(difference => difference.ToString()), Is.Empty);
        Assert.That(library.AspectsThatDifferFrom(published), Is.EqualTo(CultureCalendars.NamesOtherMonths(culture) ? TextAndCalls : LegacyAspects.None));
        Assert.That(library.Text, Does.EndWith("31\r\n\r\n").Using(StringComparison.Ordinal), "the end of a month is as corrected by D-59 and D-60");
    }

    /// <summary>
    /// The title is still one line written in the color of the title, which a constructor argument now sets
    /// (D-57), and the weekday row follows in its own color. Where the test host has no console that keeps
    /// colors, they are compared with 1.0.0 only.
    /// </summary>
    [Test]
    public void TheTitle_IsStillOneLineInTheTitleColor_FollowedByTheWeekdayRowInItsOwn()
    {
        CultureInfo culture = RequireOneThatNamesOtherMonths();
        var scenario = new LegacyScenario("September 2023, four constructor arguments")
        {
            ConstructorArguments = new[] { ConsoleColor.Red, ConsoleColor.Green, ConsoleColor.Magenta, ConsoleColor.DarkYellow },
            Draw = LegacyDraw.Month,
            Month = 9,
            Year = 2023,
            Culture = culture,
        };

        LegacyObservation published = In100(scenario);
        LegacyObservation library = In20(scenario);

        Assert.That(library.Calls[0].Method, Is.EqualTo("LS"));
        Assert.That(library.Calls[0].EndsLine, Is.True);
        Assert.That(library.Calls[0].Text, Is.EqualTo(GregorianTitle.TitleIn20(culture, 9, 2023)));
        Assert.That(library.Calls[0].Text, Is.Not.EqualTo(published.Calls[0].Text));
        Assert.That(library.Calls[1], Is.EqualTo(published.Calls[1]));
        Assert.That(library.Calls[1].Text, Is.EqualTo("Mo Tu We Th Fr Sa Su"));
        Assert.That(library.Calls.Select(call => (call.Foreground, call.Background)), Is.EqualTo(published.Calls.Select(call => (call.Foreground, call.Background))),
            "every call is made in the colors it was made in");
        Assert.That((library.ForegroundAfter, library.BackgroundAfter), Is.EqualTo((published.ForegroundAfter, published.BackgroundAfter)), "the colors left behind");
        if (!OperatingSystem.IsWindows())
        {
            Assert.That(library.Calls[0].Foreground, Is.EqualTo(ConsoleColor.Red), "the title argument");
            Assert.That(library.Calls[1].Foreground, Is.EqualTo(ConsoleColor.DarkYellow), "the weekday argument");
        }
    }

    // ---- where drawing fails ----

    /// <summary>
    /// Failures come when they came. A title color that is not defined fails before the name is looked up, with
    /// nothing written, as in 1.0.0. A weekday color that is not defined fails after the title, which is then
    /// the corrected one. An invalid month and a null list are refused before anything is drawn.
    /// </summary>
    [Test]
    public void WhereDrawingFails_ItFailsWhereItDid_AndWhatWasWrittenBeforeDiffersInTheTitleOnly()
    {
        CultureInfo culture = RequireOneThatNamesOtherMonths();
        var undefined = (ConsoleColor)16;
        LegacyScenario titleColor = new("a title color that is not defined") { Setters = new[] { (LegacyColorRole.Title, undefined) }, Draw = LegacyDraw.Month, Month = 9, Year = 2023, Culture = culture };
        LegacyScenario weekdayColor = new("a weekday color that is not defined") { Setters = new[] { (LegacyColorRole.DayOfWeek, undefined) }, Draw = LegacyDraw.Month, Month = 9, Year = 2023, Culture = culture };
        LegacyScenario invalidMonth = new("month 13") { Draw = LegacyDraw.Month, Month = 13, Year = 2023, Culture = culture };
        LegacyScenario nullList = new("a null list") { Draw = LegacyDraw.MonthWithDates, Month = 9, Year = 2023, Dates = null, Culture = culture };

        foreach (LegacyScenario same in new[] { titleColor, invalidMonth, nullList })
        {
            LegacyObservation library = In20(same);

            Assert.That(library.DifferencesFrom(In100(same)).Select(difference => difference.ToString()), Is.Empty, same.ToString());
            Assert.That(library.Calls, Is.Empty, same + ": nothing is written");
        }
        Assert.That(In20(invalidMonth).ExceptionType, Is.EqualTo(typeof(ArgumentOutOfRangeException).FullName));
        Assert.That(In20(nullList).ExceptionType, Is.EqualTo(typeof(ArgumentNullException).FullName));

        LegacyObservation published = In100(weekdayColor);
        LegacyObservation afterTheTitle = In20(weekdayColor);
        Assert.That(afterTheTitle.ExceptionType, Is.EqualTo(published.ExceptionType));
        Assert.That(afterTheTitle.ExceptionMessage, Is.EqualTo(published.ExceptionMessage));
        Assert.That(afterTheTitle.Calls, Has.Count.EqualTo(published.Calls.Count));
        if (afterTheTitle.Calls.Count > 0)
        {
            // Where a color that is not defined is refused, the title has been written by then and nothing else.
            Assert.That(afterTheTitle.Calls, Has.Count.EqualTo(1));
            Assert.That(afterTheTitle.Calls[0], Is.EqualTo(published.Calls[0] with { Text = GregorianTitle.TitleIn20(culture, 9, 2023) }));
        }
    }

    // ---- every culture ----

    /// <summary>
    /// Every culture the runtime has, a whole year each. The library differs from 1.0.0 under exactly the
    /// cultures whose own calendar names another month than the Gregorian one for some month number, and there
    /// in the text of title calls only. The counts are written to the test's output.
    /// </summary>
    [Test]
    public void UnderEveryCultureOfTheRuntime_TheLibraryDiffersFrom100_ExactlyWhereTheTwoNamesDiffer()
    {
        CultureInfo[] cultures = CultureInfo.GetCultures(CultureTypes.AllCultures);
        int notGregorian = 0;
        var affected = new List<string>();

        foreach (CultureInfo culture in cultures)
        {
            var scenario = new LegacyScenario("the year 2023") { Draw = LegacyDraw.Year, Year = 2023, Culture = culture };
            LegacyObservation published = In100(scenario);
            LegacyObservation library = In20(scenario);
            bool namesOtherMonths = CultureCalendars.NamesOtherMonths(culture);
            notGregorian += CultureCalendars.NamesGregorianMonths(culture) ? 0 : 1;

            Assert.That(library.AspectsThatDifferFrom(published), Is.EqualTo(namesOtherMonths ? TextAndCalls : LegacyAspects.None), culture.Name);
            if (!namesOtherMonths)
            {
                continue;
            }
            affected.Add(culture.Name);
            int[] titles = TitlePositions(library);
            int[] differing = Enumerable.Range(0, library.Calls.Count).Where(index => library.Calls[index] != published.Calls[index]).ToArray();
            Assert.That(library.Calls, Has.Count.EqualTo(published.Calls.Count), culture.Name);
            Assert.That(differing, Is.SubsetOf(titles), culture.Name + ": only title calls differ");
            Assert.That(CultureCalendars.NamesGregorianMonths(culture), Is.False, culture.Name + ": a culture whose dates are Gregorian is never touched");
        }

        TestContext.Out.WriteLine($"Cultures examined: {cultures.Length}; whose dates are not Gregorian: {notGregorian}; under which the library differs from 1.0.0: {affected.Count}: {string.Join(", ", affected)}");
        Assert.That(affected, Has.Count.LessThanOrEqualTo(notGregorian));
    }

    // ---- the allowance itself ----

    private static RecordedCall Line(string text) => new("LS", text, EndsLine: true, ConsoleColor.Yellow, ConsoleColor.Black);

    private static RecordedCall Write(string text) => new("S", text, EndsLine: false, ConsoleColor.White, ConsoleColor.Black);

    private static readonly RecordedCall WeekdayRow = Line("Mo Tu We Th Fr Sa Su");

    [Test]
    public void TheAllowance_ChangesTheTextOfTheTitleCall_AndNothingElse()
    {
        CultureInfo culture = RequireOneThatNamesOtherMonths();
        string title100 = GregorianTitle.TitleIn100(culture, 9, 2023);
        RecordedCall[] calls = { Line(title100), WeekdayRow, Write(title100), Write(" 1") };

        IReadOnlyList<RecordedCall> corrected = GregorianTitle.Corrected(calls, culture, 2023, new[] { 9 });

        Assert.That(corrected, Is.EqualTo(new[] { Line(GregorianTitle.TitleIn20(culture, 9, 2023)), WeekdayRow, Write(title100), Write(" 1") }),
            "the title call has the other text and the same color; a write of the same text that is not a title is left alone");
        Assert.That(calls[0].Text, Is.EqualTo(title100), "the list it was given is not changed");
    }

    [Test]
    public void TheAllowance_TouchesNothingUnderACultureWhoseDatesAreGregorian_OrWhoseTwoNamesAreTheSame()
    {
        RecordedCall[] calls = { Line("anything at all"), Write("x") };

        Assert.That(GregorianTitle.Corrected(calls, CultureInfo.InvariantCulture, 2023, new[] { 9 }), Is.SameAs(calls), "not even looked at");

        CultureInfo? sameMonths = new[] { "th-TH", "ja-JP+JapaneseCalendar", "ko-KR+KoreanCalendar", "zh-TW+TaiwanCalendar" }
            .Select(CultureCalendars.Resolve)
            .FirstOrDefault(culture => culture != null && !CultureCalendars.NamesGregorianMonths(culture) && !CultureCalendars.NamesOtherMonths(culture));
        Assume.That(sameMonths, Is.Not.Null, "This machine has no culture whose other calendar has the Gregorian months.");
        RecordedCall[] year = Enumerable.Range(1, 12)
            .SelectMany(month => new[] { Line(GregorianTitle.TitleIn100(sameMonths!, month, 2023)), WeekdayRow, Write(" 1") })
            .ToArray();

        Assert.That(GregorianTitle.Corrected(year, sameMonths!, 2023, Enumerable.Range(1, 12).ToArray()), Is.SameAs(year));
    }

    /// <summary>
    /// Anything that is not the title 1.0.0 wrote for the month that is due is refused, so the allowance cannot
    /// let another difference through: another name, another indent, another year, a title out of order, a
    /// title too many, or something else where the weekday row belongs.
    /// </summary>
    [Test]
    public void TheAllowance_RefusesWhatIsNotTheTitleOf100()
    {
        CultureInfo culture = RequireOneThatNamesOtherMonths();
        string september = GregorianTitle.TitleIn100(culture, 9, 2023);
        int[] nine = { 9 };

        Assert.That(() => GregorianTitle.Corrected(new[] { Line(GregorianTitle.TitleIn100(culture, 8, 2023)), WeekdayRow }, culture, 2023, nine), Throws.InvalidOperationException, "another month");
        Assert.That(() => GregorianTitle.Corrected(new[] { Line(" " + september), WeekdayRow }, culture, 2023, nine), Throws.InvalidOperationException, "another indent");
        Assert.That(() => GregorianTitle.Corrected(new[] { Line(GregorianTitle.TitleIn100(culture, 9, 2024)), WeekdayRow }, culture, 2023, nine), Throws.InvalidOperationException, "another year");
        Assert.That(() => GregorianTitle.Corrected(new[] { Line(september.TrimStart()), WeekdayRow }, culture, 2023, nine), Throws.InvalidOperationException, "no indent");
        Assert.That(() => GregorianTitle.Corrected(new[] { Line(september), Line("Su Mo Tu We Th Fr Sa") }, culture, 2023, nine), Throws.InvalidOperationException, "another weekday row");
        Assert.That(() => GregorianTitle.Corrected(new[] { Line(september), WeekdayRow, Line(september), WeekdayRow }, culture, 2023, nine), Throws.InvalidOperationException, "a month too many");
        Assert.That(GregorianTitle.Corrected(new[] { Line(september), WeekdayRow }, culture, 2023, nine), Has.Count.EqualTo(2), "the title of 1.0.0 is accepted");
        Assert.That(GregorianTitle.Corrected(Array.Empty<RecordedCall>(), culture, 2023, nine), Is.Empty, "a drawing that failed before the title");
    }

    [Test]
    public void AllFiveCorrectionsAreImplemented_AndTheTitleIsTheLast()
    {
        Assert.That(ApprovedCorrection.All.Select(correction => correction.IsImplemented), Is.All.True);
        Assert.That(ApprovedCorrection.All.Last(), Is.SameAs(ApprovedCorrection.GregorianTitle));
        Assert.That(ApprovedCorrection.GregorianTitle.Corrected, Is.Not.Null, "stated as a change of what 1.0.0 wrote");
        Assert.That(ApprovedCorrection.GregorianTitle.EquivalentIn100, Is.Null);
    }
}
