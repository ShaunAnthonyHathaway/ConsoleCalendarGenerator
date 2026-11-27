using System.Globalization;
using ConsoleCalendarGenerator.Tests.Support;

namespace ConsoleCalendarGenerator.Tests.Migration;

/// <summary>
/// The first correction of version 2.0, the constructor colors (D-57), as the exact difference between the
/// frozen source of 1.0.0 and the library: the same constructor arguments given to both, and everything a
/// caller can observe compared.
///
/// <para>
/// What differs is which element the first and the fourth argument color, and what follows from that: the
/// colors a calendar holds, the color the title and the weekday row are drawn in, and, for an argument that is
/// not a defined color, the place where the drawing fails. Nothing else differs: not the text, not one call on
/// the console's writer, not the colors of the days, not the colors left behind, not an exception type.
/// </para>
/// <para>
/// The 1.0.0 side of every comparison is in Historical/Published100ConstructorTests.cs on its own, and the 2.0
/// side in Legacy/ConstructorColorTests.cs.
/// </para>
/// </summary>
[TestFixture]
public class ConstructorColorMigrationTests
{
    private const ConsoleColor TitleArgument = ConsoleColor.Red;
    private const ConsoleColor DateArgument = ConsoleColor.Green;
    private const ConsoleColor SelectedDateArgument = ConsoleColor.Magenta;
    private const ConsoleColor DayOfWeekArgument = ConsoleColor.DarkYellow;

    private static readonly ConsoleColor[] FourArguments = { TitleArgument, DateArgument, SelectedDateArgument, DayOfWeekArgument };

    private static readonly ConsoleColor Undefined = (ConsoleColor)99;

    private const string Title = "   February 2023\n";
    private const string WeekdayRow = "Mo Tu We Th Fr Sa Su\n";
    private const string DaysBeforeThe14th = "       1  2  3  4  5\n 6  7  8  9 10 11 12\n13";

    /// <summary>February 2023 with the 14th selected, drawn after the caller has set colors of its own.</summary>
    private static LegacyScenario February(params ConsoleColor[] constructorArguments) => new("February 2023")
    {
        ConstructorArguments = constructorArguments,
        Draw = LegacyDraw.MonthWithDates,
        Month = 2,
        Year = 2023,
        Dates = new[] { new DateTime(2023, 2, 14) },
        ForegroundBefore = ConsoleColor.DarkCyan,
        BackgroundBefore = ConsoleColor.DarkBlue,
    };

    private static LegacyObservation In100(LegacyScenario scenario) => LegacyObservation.Of(LegacySubject.Published100, scenario, CultureOf(scenario));

    private static LegacyObservation In20(LegacyScenario scenario) => LegacyObservation.Of(LegacySubject.Library, scenario, CultureOf(scenario));

    private static CultureInfo CultureOf(LegacyScenario scenario) => scenario.ResolveCulture() ?? CultureInfo.InvariantCulture;

    /// <summary>The colors differ in what is held, and in what is drawn where a test host can see colors at all.</summary>
    private static LegacyAspects ColorsOnly =>
        OperatingSystem.IsWindows() ? LegacyAspects.StoredColors : LegacyAspects.StoredColors | LegacyAspects.Colors;

    // ---- the matrix: the colors held, before and after ----

    private static IEnumerable<TestCaseData> Matrix()
    {
        const ConsoleColor yellow = ConsoleColor.Yellow;
        const ConsoleColor cyan = ConsoleColor.Cyan;
        const ConsoleColor white = ConsoleColor.White;
        const ConsoleColor blue = ConsoleColor.Blue;
        yield return new TestCaseData(
            FourArguments.Take(1).ToArray(),
            new LegacyStoredColors(Title: yellow, DayOfWeek: TitleArgument, Date: white, SelectedDate: blue),
            new LegacyStoredColors(Title: TitleArgument, DayOfWeek: cyan, Date: white, SelectedDate: blue)).SetArgDisplayNames("TitleColor");
        yield return new TestCaseData(
            FourArguments.Take(2).ToArray(),
            new LegacyStoredColors(Title: yellow, DayOfWeek: TitleArgument, Date: DateArgument, SelectedDate: blue),
            new LegacyStoredColors(Title: TitleArgument, DayOfWeek: cyan, Date: DateArgument, SelectedDate: blue)).SetArgDisplayNames("TitleColor, DateColor");
        yield return new TestCaseData(
            FourArguments.Take(3).ToArray(),
            new LegacyStoredColors(Title: yellow, DayOfWeek: TitleArgument, Date: DateArgument, SelectedDate: SelectedDateArgument),
            new LegacyStoredColors(Title: TitleArgument, DayOfWeek: cyan, Date: DateArgument, SelectedDate: SelectedDateArgument)).SetArgDisplayNames("TitleColor, DateColor, SelectedDateColor");
        yield return new TestCaseData(
            FourArguments,
            new LegacyStoredColors(Title: DayOfWeekArgument, DayOfWeek: TitleArgument, Date: DateArgument, SelectedDate: SelectedDateArgument),
            new LegacyStoredColors(Title: TitleArgument, DayOfWeek: DayOfWeekArgument, Date: DateArgument, SelectedDate: SelectedDateArgument)).SetArgDisplayNames("TitleColor, DateColor, SelectedDateColor, DayOfWeekColor");
    }

    [TestCaseSource(nameof(Matrix))]
    public void TheColorsHeld_In100AndIn20(ConsoleColor[] arguments, object held100, object held20)
    {
        Assert.That(In100(February(arguments)).StoredColors, Is.EqualTo(held100), "1.0.0");
        Assert.That(In20(February(arguments)).StoredColors, Is.EqualTo(held20), "2.0");
    }

    [TestCaseSource(nameof(Matrix))]
    [Platform(Exclude = "Win", Reason = ConsoleState.ColorsObservableOnUnixOnly)]
    public void TheColorsDrawn_AreTheColorsHeld_In100AndIn20(ConsoleColor[] arguments, object held100, object held20)
    {
        foreach ((LegacyObservation observation, LegacyStoredColors held) in new[]
        {
            (In100(February(arguments)), (LegacyStoredColors)held100),
            (In20(February(arguments)), (LegacyStoredColors)held20),
        })
        {
            RenderedRoleColors drawn = RenderedRoleColors.Of(observation, 14);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(drawn.Title, Is.EqualTo(held.Title), "title");
                Assert.That(drawn.DayOfWeek, Is.EqualTo(held.DayOfWeek), "weekday row");
                Assert.That(drawn.Dates, Is.EqualTo(new[] { held.Date }), "days that are not selected");
                Assert.That(drawn.SelectedDates, Is.EqualTo(new[] { held.SelectedDate }), "the selected day");
            }
        }
    }

    // ---- with the same arguments, only the colors differ ----

    [TestCaseSource(nameof(Matrix))]
    public void WithTheSameArguments_OnlyColorsDiffer_AndNoTextNoCallAndNoExceptionDoes(ConsoleColor[] arguments, object held100, object held20)
    {
        LegacyObservation published100 = In100(February(arguments));
        LegacyObservation library = In20(February(arguments));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(library.AspectsThatDifferFrom(published100), Is.EqualTo(ColorsOnly));
            Assert.That(library.Text, Is.EqualTo(published100.Text), "text");
            Assert.That(library.Text, Is.EqualTo(Title + WeekdayRow + DaysBeforeThe14th + " 14 15 16 17 18 19\n20 21 22 23 24 25 26\n27 28\n\n"), "text");
            Assert.That(library.Calls.Select(call => (call.Method, call.Text)), Is.EqualTo(published100.Calls.Select(call => (call.Method, call.Text))), "calls");
            Assert.That(library.ExceptionType, Is.Null);
            Assert.That(published100.ExceptionType, Is.Null);
        }
    }

    /// <summary>
    /// Among the colors, only those of the title and of the weekday row differ. Every day, selected or not, and
    /// every empty cell and line end keeps its color; the caller's background lasts as long as it did; and the
    /// colors are reset at the end, as they were.
    /// </summary>
    [TestCaseSource(nameof(Matrix))]
    [Platform(Exclude = "Win", Reason = ConsoleState.ColorsObservableOnUnixOnly)]
    public void AmongTheColors_OnlyThoseOfTheTitleAndTheWeekdayRowDiffer(ConsoleColor[] arguments, object held100, object held20)
    {
        LegacyObservation published100 = In100(February(arguments));
        LegacyObservation library = In20(February(arguments));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(library.Calls[0].Foreground, Is.Not.EqualTo(published100.Calls[0].Foreground), "the title");
            Assert.That(library.Calls[1].Foreground, Is.Not.EqualTo(published100.Calls[1].Foreground), "the weekday row");
            Assert.That(library.Calls.Skip(2).Select(call => call.Foreground), Is.EqualTo(published100.Calls.Skip(2).Select(call => call.Foreground)), "everything after them");
            Assert.That(library.Calls.Select(call => call.Background), Is.EqualTo(published100.Calls.Select(call => call.Background)), "the background of every call");
            Assert.That(library.Calls[0].Background, Is.EqualTo(ConsoleColor.DarkBlue), "the caller's background is still there for the title");
            Assert.That((library.ForegroundAfter, library.BackgroundAfter), Is.EqualTo((published100.ForegroundAfter, published100.BackgroundAfter)));
            Assert.That((library.ForegroundAfter, library.BackgroundAfter), Is.EqualTo((ConsoleState.Unset, ConsoleState.Unset)), "reset, not restored");
        }
    }

    [Test]
    public void TheParameterlessConstructor_DiffersInNothing()
    {
        Assert.That(In20(February()).AspectsThatDifferFrom(In100(February())), Is.EqualTo(LegacyAspects.None));
    }

    /// <summary>
    /// The correction touches colors only, wherever the calendar is drawn: across a year, with another line
    /// terminator, under cultures with other month names, in a month that ends on a Sunday, and when the call
    /// fails for a reason that has nothing to do with colors. (A null list was among these until 2.0 corrected
    /// its exception too, D-58; the two corrections together are in NullListMigrationTests.)
    /// </summary>
    [Test]
    public void WhateverElseTheScenarioIs_TheDifferenceIsInColorsAlone()
    {
        var selection = new[] { new DateTime(2023, 4, 30), new DateTime(2024, 2, 29), new DateTime(2024, 6, 30) };
        LegacyScenario[] scenarios =
        {
            new("a year") { ConstructorArguments = FourArguments, Draw = LegacyDraw.YearWithDates, Year = 2024, Dates = selection },
            new("a year, terminator \\r\\n") { ConstructorArguments = FourArguments, Draw = LegacyDraw.YearWithDates, Year = 2024, Dates = selection, NewLine = "\r\n" },
            new("a month that ends on a Sunday") { ConstructorArguments = FourArguments, Draw = LegacyDraw.MonthWithDates, Month = 4, Year = 2023, Dates = selection },
            new("other month names") { ConstructorArguments = FourArguments, Draw = LegacyDraw.Year, Year = 2023, Culture = TestCultures.WithMonthNames(month => "Mois" + month) },
            new("another week and UI culture") { ConstructorArguments = FourArguments, Draw = LegacyDraw.Month, Month = 5, Year = 2023, Culture = TestCultures.WithForeignWeek(), UICulture = TestCultures.WithMonthNames(_ => "ui") },
            new("an invalid month") { ConstructorArguments = FourArguments, Draw = LegacyDraw.Month, Month = 13, Year = 2023 },
            new("an invalid year") { ConstructorArguments = FourArguments.Take(1).ToArray(), Draw = LegacyDraw.Year, Year = 0 },
        };

        foreach (LegacyScenario scenario in scenarios)
        {
            LegacyAspects differing = In20(scenario).AspectsThatDifferFrom(In100(scenario));

            Assert.That(differing & ~(LegacyAspects.StoredColors | LegacyAspects.Colors), Is.EqualTo(LegacyAspects.None), scenario.Name);
            Assert.That(differing.HasFlag(LegacyAspects.StoredColors), Is.True, scenario.Name);
        }
    }

    /// <summary>The same under a culture whose own calendar is not the Gregorian one: the title, wrong or not, is the same text.</summary>
    [Test]
    public void UnderACultureWithAnotherCalendar_TheTitleIsStillTheSameText()
    {
        CultureInfo? persian = TestCultures.TryGet("fa-IR");
        Assume.That(persian, Is.Not.Null, "The culture fa-IR is not available on this machine.");
        var scenario = new LegacyScenario("fa-IR") { ConstructorArguments = FourArguments, Draw = LegacyDraw.Year, Year = 2023, CultureName = "fa-IR" };

        LegacyObservation published100 = In100(scenario);
        LegacyObservation library = In20(scenario);

        Assert.That(library.Text, Is.EqualTo(published100.Text));
        Assert.That(library.AspectsThatDifferFrom(published100), Is.EqualTo(ColorsOnly));
    }

    // ---- how a program written for 1.0.0 keeps what it drew ----

    /// <summary>
    /// A program that passed four colors keeps its appearance by passing the first and the fourth in each
    /// other's place. That is the whole difference for it: every aspect is then the same.
    /// </summary>
    [Test]
    public void FourArgumentsIn100_AreTheSameAs20WithTheFirstAndTheFourthSwapped()
    {
        LegacyObservation published100 = In100(February(TitleArgument, DateArgument, SelectedDateArgument, DayOfWeekArgument));
        LegacyObservation library = In20(February(DayOfWeekArgument, DateArgument, SelectedDateArgument, TitleArgument));

        Assert.That(library.DifferencesFrom(published100), Is.Empty);
    }

    /// <summary>
    /// A program that passed one to three colors got a yellow title and its first color on the weekday row. In
    /// 2.0 that is the four-argument constructor with yellow first and that color last.
    /// </summary>
    [Test]
    public void OneToThreeArgumentsIn100_AreTheSameAs20WithAYellowTitleAndTheFirstColorLast()
    {
        const ConsoleColor first = ConsoleColor.Red;

        Assert.That(In20(February(ConsoleColor.Yellow, ConsoleColor.White, ConsoleColor.Blue, first)).DifferencesFrom(In100(February(first))), Is.Empty);
        Assert.That(In20(February(ConsoleColor.Yellow, DateArgument, ConsoleColor.Blue, first)).DifferencesFrom(In100(February(first, DateArgument))), Is.Empty);
        Assert.That(In20(February(ConsoleColor.Yellow, DateArgument, SelectedDateArgument, first)).DifferencesFrom(In100(February(first, DateArgument, SelectedDateArgument))), Is.Empty);
    }

    /// <summary>
    /// The path that needs no thought about versions: the parameterless constructor and the setters draw the
    /// same in 1.0.0 and in 2.0, whatever colors are set.
    /// </summary>
    [Test]
    public void TheParameterlessConstructorAndTheSetters_DrawTheSameIn100AndIn20()
    {
        LegacyScenario scenario = February().WithConstructorAndSetters(Array.Empty<ConsoleColor>(), new[]
        {
            (LegacyColorRole.Title, TitleArgument),
            (LegacyColorRole.DayOfWeek, DayOfWeekArgument),
            (LegacyColorRole.Date, DateArgument),
            (LegacyColorRole.SelectedDate, SelectedDateArgument),
        });

        Assert.That(In20(scenario).DifferencesFrom(In100(scenario)), Is.Empty);
        Assert.That(In20(scenario).StoredColors, Is.EqualTo(new LegacyStoredColors(TitleArgument, DayOfWeekArgument, DateArgument, SelectedDateArgument)));
    }

    // ---- the two descriptions of the constructors, against what they describe ----

    private static IEnumerable<ConsoleColor[]> ArgumentLists()
    {
        for (int count = 0; count <= 4; count++)
        {
            yield return FourArguments.Take(count).ToArray();
        }
        // With a value that is not a color in each position, where the description also has to get the failure right.
        for (int count = 1; count <= 4; count++)
        {
            for (int undefined = 0; undefined < count; undefined++)
            {
                ConsoleColor[] arguments = FourArguments.Take(count).ToArray();
                arguments[undefined] = Undefined;
                yield return arguments;
            }
        }
        yield return new[] { Undefined, Undefined, Undefined, Undefined };
    }

    /// <summary>In 1.0.0 a constructor did what its parameterless constructor and these setters did.</summary>
    [Test]
    public void TheConstructorsOf100_AreDescribedBy_ConstructorColorRolesIn100()
    {
        foreach (ConsoleColor[] arguments in ArgumentLists())
        {
            LegacyScenario bySetters = February().WithConstructorAndSetters(Array.Empty<ConsoleColor>(), ConstructorColorRoles.In100(arguments));

            Assert.That(In100(bySetters).DifferencesFrom(In100(February(arguments))), Is.Empty, string.Join(", ", arguments));
        }
    }

    /// <summary>In 2.0 a constructor does what the parameterless constructor and these setters do.</summary>
    [Test]
    public void TheConstructorsOf20_AreDescribedBy_ConstructorColorRolesIn20()
    {
        foreach (ConsoleColor[] arguments in ArgumentLists())
        {
            LegacyScenario bySetters = February().WithConstructorAndSetters(Array.Empty<ConsoleColor>(), ConstructorColorRoles.In20(arguments));

            Assert.That(In20(bySetters).DifferencesFrom(In20(February(arguments))), Is.Empty, string.Join(", ", arguments));
        }
    }

    /// <summary>
    /// The subject the recorded digests are computed with gives the library's calendars the colors 1.0.0 gave
    /// them, so it holds and draws exactly what the frozen source does with the same arguments.
    /// </summary>
    [Test]
    public void TheLibraryWithTheConstructorColorsOf100_DoesWhat100DidWithTheSameArguments()
    {
        foreach (ConsoleColor[] arguments in ArgumentLists())
        {
            LegacyScenario scenario = February(arguments);
            LegacyObservation library = LegacyObservation.Of(LegacySubject.LibraryWithTheConstructorColorsOf100, scenario, CultureInfo.InvariantCulture);

            Assert.That(library.DifferencesFrom(In100(scenario)), Is.Empty, string.Join(", ", arguments));
        }
    }

    // ---- an argument that is not a defined color: when the drawing fails ----

    // No constructor checks its arguments, in 1.0.0 or in 2.0. A value that is not a color fails when the console
    // is first asked to use it. Because the correction changes which element the first and the fourth argument
    // color, it also changes where such a value fails. That is an intended consequence of D-57.

    private static IEnumerable<TestCaseData> UndefinedArguments()
    {
        const ConsoleColor u = (ConsoleColor)99;
        const ConsoleColor r = TitleArgument;
        const ConsoleColor g = DateArgument;
        const ConsoleColor m = SelectedDateArgument;
        const ConsoleColor d = DayOfWeekArgument;
        const string nothing = "";
        const string afterTheTitle = Title;
        const string afterTheWeekdayRow = Title + WeekdayRow;
        const string atTheFirstSelectedDay = Title + WeekdayRow + DaysBeforeThe14th;

        // An undefined TitleColor: 1.0.0 fails at the weekday row, 2.0 at the title.
        yield return Case("TitleColor, of one", new[] { u }, afterTheTitle, nothing);
        yield return Case("TitleColor, of two", new[] { u, g }, afterTheTitle, nothing);
        yield return Case("TitleColor, of three", new[] { u, g, m }, afterTheTitle, nothing);
        yield return Case("TitleColor, of four", new[] { u, g, m, d }, afterTheTitle, nothing);
        // An undefined DayOfWeekColor: 1.0.0 fails at the title, 2.0 at the weekday row.
        yield return Case("DayOfWeekColor", new[] { r, g, m, u }, nothing, afterTheTitle);
        // An undefined DateColor or SelectedDateColor: the same place in both.
        yield return Case("DateColor, of two", new[] { r, u }, afterTheWeekdayRow, afterTheWeekdayRow);
        yield return Case("DateColor, of three", new[] { r, u, m }, afterTheWeekdayRow, afterTheWeekdayRow);
        yield return Case("DateColor, of four", new[] { r, u, m, d }, afterTheWeekdayRow, afterTheWeekdayRow);
        yield return Case("SelectedDateColor, of three", new[] { r, g, u }, atTheFirstSelectedDay, atTheFirstSelectedDay);
        yield return Case("SelectedDateColor, of four", new[] { r, g, u, d }, atTheFirstSelectedDay, atTheFirstSelectedDay);
        // Several undefined: the element drawn first fails, and the later ones are never reached.
        yield return Case("TitleColor and DayOfWeekColor", new[] { u, g, m, u }, nothing, nothing);
        yield return Case("all four", new[] { u, u, u, u }, nothing, nothing);
        yield return Case("all but TitleColor", new[] { r, u, u, u }, nothing, afterTheTitle);
        yield return Case("all but DayOfWeekColor", new[] { u, u, u, d }, afterTheTitle, nothing);

        static TestCaseData Case(string name, ConsoleColor[] arguments, string writtenIn100, string writtenIn20) =>
            new TestCaseData(arguments, writtenIn100, writtenIn20).SetArgDisplayNames("undefined " + name);
    }

    [TestCaseSource(nameof(UndefinedArguments))]
    public void AnUndefinedArgument_FailsWhenItsElementIsFirstDrawn_WhichIsAnotherPlaceIn20ForTheFirstAndTheFourth(
        ConsoleColor[] arguments, string writtenIn100, string writtenIn20)
    {
        LegacyObservation published100 = In100(February(arguments));
        LegacyObservation library = In20(February(arguments));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(published100.Text, Is.EqualTo(writtenIn100), "written before the failure in 1.0.0");
            Assert.That(library.Text, Is.EqualTo(writtenIn20), "written before the failure in 2.0");
            Assert.That(published100.ExceptionType, Is.EqualTo("System.ArgumentException"));
            Assert.That(library.ExceptionType, Is.EqualTo(published100.ExceptionType), "the exception type does not change");
            Assert.That(library.ExceptionParameterName, Is.EqualTo(published100.ExceptionParameterName));
            Assert.That(library.ExceptionMessage, Is.EqualTo(published100.ExceptionMessage));
        }
    }

    /// <summary>What is written before such a failure is written in the color that element now has.</summary>
    [Test]
    [Platform(Exclude = "Win", Reason = ConsoleState.ColorsObservableOnUnixOnly)]
    public void BeforeAFailureAtTheWeekdayRow_TheTitleIsWrittenInItsOwnColor_AndTheColorsAreLeftReset()
    {
        LegacyObservation published100 = In100(February(Undefined));
        LegacyObservation library = In20(February(TitleArgument, DateArgument, SelectedDateArgument, Undefined));

        Assert.That(published100.Calls.Single().Foreground, Is.EqualTo(ConsoleColor.Yellow), "1.0.0: the title stays yellow");
        Assert.That(library.Calls.Single().Foreground, Is.EqualTo(TitleArgument), "2.0: the title has its argument's color");
        foreach (LegacyObservation observation in new[] { published100, library })
        {
            Assert.That((observation.ForegroundAfter, observation.BackgroundAfter), Is.EqualTo((ConsoleState.Unset, ConsoleState.Unset)),
                "the title's reset was the last thing done to the colors");
        }
    }

    /// <summary>Nothing is validated early in either version: making the calendar never fails.</summary>
    [Test]
    public void NoConstructorFails_WhateverTheArguments()
    {
        foreach (ConsoleColor[] arguments in ArgumentLists())
        {
            foreach (LegacySubject subject in new[] { LegacySubject.Published100, LegacySubject.Library })
            {
                Assert.That(() => subject.Create(arguments), Throws.Nothing);
            }
        }
    }
}
