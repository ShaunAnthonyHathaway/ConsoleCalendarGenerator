using System.Globalization;
using ConsoleCalendarGenerator.Tests.Support;

namespace ConsoleCalendarGenerator.Tests.Migration;

/// <summary>
/// The second correction of version 2.0, the exception for a null list (D-58), as the exact difference between
/// the frozen source of 1.0.0 and the library: the same call given to both, and everything a caller can observe
/// compared.
///
/// <para>
/// What differs is the exception: its type, the parameter it names, and the message the runtime gives an
/// exception of that type. Nothing else differs. In both versions the list is looked at first, before the month
/// and the year, and nothing is written and no color is touched before the exception. A call that passes a
/// list, valid or not, is the same in both.
/// </para>
/// <para>
/// The 1.0.0 side of every comparison is in Historical/Published100NullListTests.cs on its own, and the 2.0
/// side in Legacy/ValidationTests.cs.
/// </para>
/// <para>
/// Version 2.0 has corrected other things as well: the constructor colors before this, and how a month ends
/// after it. So that these tests keep saying what they said, the 1.0.0 side is 1.0.0 with those other
/// corrections applied, except in the one test that is about two corrections together; what is compared is
/// still this correction and nothing else.
/// </para>
/// </summary>
[TestFixture]
public class NullListMigrationTests
{
    private const LegacyAspects TheException =
        LegacyAspects.ExceptionType | LegacyAspects.ExceptionParameterName | LegacyAspects.ExceptionMessage;

    private static readonly DateTime[] Fourteenth = { new DateTime(2023, 2, 14) };

    /// <summary>A null list, with the caller's colors set beforehand.</summary>
    private static LegacyScenario NullList(LegacyDraw draw, int month, int year) => new("a null list")
    {
        Draw = draw,
        Month = month,
        Year = year,
        Dates = null,
        ForegroundBefore = ConsoleColor.DarkCyan,
        BackgroundBefore = ConsoleColor.DarkBlue,
    };

    /// <summary>
    /// What 1.0.0 did in a scenario, with the corrections other than this one applied (the constructor colors,
    /// the end of a month), so that what is left to differ from the library is the exception for a null list
    /// alone.
    /// </summary>
    private static LegacyObservation In100(LegacyScenario scenario) =>
        ApprovedCorrection.ExpectedWithout(new[] { ApprovedCorrection.NullDateList }, scenario, CultureInfo.InvariantCulture);

    /// <summary>What 1.0.0 did in a scenario, with no correction applied at all.</summary>
    private static LegacyObservation InUncorrected100(LegacyScenario scenario) =>
        LegacyObservation.Of(LegacySubject.Published100, scenario, CultureInfo.InvariantCulture);

    private static LegacyObservation In20(LegacyScenario scenario) => LegacyObservation.Of(LegacySubject.Library, scenario, CultureInfo.InvariantCulture);

    // ---- the matrix: every way to pass a null list ----

    private static IEnumerable<TestCaseData> NullLists()
    {
        yield return Case("GenerateMonth, a valid month and year", LegacyDraw.MonthWithDates, 2, 2023);
        foreach ((int month, int year) in new[]
        {
            (0, 2023), (13, 2023), (-1, 2023), (int.MinValue, 2023), (int.MaxValue, 2023),
            (1, 0), (1, 10000), (1, -1), (13, 0), (0, 10000), (int.MinValue, int.MaxValue),
        })
        {
            yield return Case($"GenerateMonth, month {month} of year {year}", LegacyDraw.MonthWithDates, month, year);
        }
        yield return Case("GenerateYear, a valid year", LegacyDraw.YearWithDates, 0, 2023);
        foreach (int year in new[] { 0, 10000, -1 })
        {
            yield return Case($"GenerateYear, year {year}", LegacyDraw.YearWithDates, 0, year);
        }

        static TestCaseData Case(string name, LegacyDraw draw, int month, int year) =>
            new TestCaseData(new ScenarioCase(NullList(draw, month, year))).SetArgDisplayNames(name);
    }

    [TestCaseSource(nameof(NullLists))]
    public void ANullList_In100AndIn20(ScenarioCase with)
    {
        LegacyObservation published100 = In100(with.Scenario);
        LegacyObservation library = In20(with.Scenario);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(published100.ExceptionType, Is.EqualTo("System.NullReferenceException"), "1.0.0");
            Assert.That(published100.ExceptionParameterName, Is.Null, "1.0.0 names no parameter");
            Assert.That(published100.Calls, Is.Empty, "1.0.0 writes nothing first");

            Assert.That(library.ExceptionType, Is.EqualTo("System.ArgumentNullException"), "2.0");
            Assert.That(library.ExceptionParameterName, Is.EqualTo("dates"), "2.0 names the parameter");
            Assert.That(library.Calls, Is.Empty, "2.0 writes nothing first");
        }
    }

    [TestCaseSource(nameof(NullLists))]
    public void ForANullList_OnlyTheExceptionDiffers(ScenarioCase with)
    {
        LegacyObservation published100 = In100(with.Scenario);
        LegacyObservation library = In20(with.Scenario);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(library.AspectsThatDifferFrom(published100), Is.EqualTo(TheException));
            Assert.That(library.StoredColors, Is.EqualTo(published100.StoredColors));
            Assert.That((library.ForegroundAfter, library.BackgroundAfter), Is.EqualTo((published100.ForegroundAfter, published100.BackgroundAfter)));
        }
    }

    /// <summary>Neither version had touched the console when it threw: the caller's colors are as they were, not reset.</summary>
    [TestCaseSource(nameof(NullLists))]
    [Platform(Exclude = "Win", Reason = ConsoleState.ColorsObservableOnUnixOnly)]
    public void ForANullList_TheCallersColorsAreAsTheyWere_In100AndIn20(ScenarioCase with)
    {
        foreach (LegacyObservation observation in new[] { In100(with.Scenario), In20(with.Scenario) })
        {
            Assert.That((observation.ForegroundAfter, observation.BackgroundAfter), Is.EqualTo((ConsoleColor.DarkCyan, ConsoleColor.DarkBlue)));
        }
    }

    /// <summary>
    /// The message is the runtime's in both: for the exception the runtime threw in 1.0.0, and for a null
    /// argument named dates in 2.0. The library writes no message of its own.
    /// </summary>
    [Test]
    public void TheMessages_AreTheRuntimesOwn_ForEachExceptionType()
    {
        (string nullReference, string argumentNull) = ProcessState.UnderCultures(CultureInfo.InvariantCulture, CultureInfo.InvariantCulture,
            () => (new NullReferenceException().Message, new ArgumentNullException("dates").Message));

        foreach (LegacyDraw draw in new[] { LegacyDraw.MonthWithDates, LegacyDraw.YearWithDates })
        {
            Assert.That(In100(NullList(draw, 2, 2023)).ExceptionMessage, Is.EqualTo(nullReference), "1.0.0");
            Assert.That(In20(NullList(draw, 2, 2023)).ExceptionMessage, Is.EqualTo(argumentNull), "2.0");
        }
    }

    /// <summary>Neither version changes anything in the process on the way to the exception.</summary>
    [Test]
    public void ForANullList_NothingInTheProcessChanges_In100AndIn20()
    {
        foreach (LegacySubject subject in new[] { LegacySubject.Published100, LegacySubject.Library })
        {
            ILegacyCalendar calendar = subject.Create();
            object[] before = ProcessState.Snapshot();

            Assert.That(() => calendar.GenerateMonth(13, 0, null!), Throws.Exception, subject.Name);
            Assert.That(() => calendar.GenerateYear(2023, null!), Throws.Exception, subject.Name);

            Assert.That(ProcessState.Snapshot(), Is.EqualTo(before), subject.Name);
        }
    }

    // ---- together with the constructor colors ----

    /// <summary>
    /// A calendar made with a constructor that takes colors, given a null list: the two corrections together.
    /// The colors held differ as the first correction says, the exception as the second says, and nothing else,
    /// also when one of the colors is not defined, because the list comes before any color is used.
    /// </summary>
    [Test]
    public void WithAConstructorThatTakesColors_TheColorsHeldAndTheExceptionDiffer_AndNothingElse()
    {
        var undefined = (ConsoleColor)99;
        foreach (ConsoleColor[] arguments in new[]
        {
            new[] { ConsoleColor.Red },
            new[] { ConsoleColor.Red, ConsoleColor.Green, ConsoleColor.Magenta, ConsoleColor.DarkYellow },
            new[] { undefined },
            new[] { ConsoleColor.Red, ConsoleColor.Green, ConsoleColor.Magenta, undefined },
        })
        {
            LegacyScenario scenario = NullList(LegacyDraw.MonthWithDates, 2, 2023).WithConstructorAndSetters(arguments, Array.Empty<(LegacyColorRole, ConsoleColor)>());

            LegacyObservation published100 = InUncorrected100(scenario);
            LegacyObservation library = In20(scenario);

            Assert.That(library.AspectsThatDifferFrom(published100), Is.EqualTo(LegacyAspects.StoredColors | TheException), string.Join(", ", arguments));
            Assert.That(library.ExceptionType, Is.EqualTo("System.ArgumentNullException"));
            Assert.That(library.Calls.Concat(published100.Calls), Is.Empty);
        }
    }

    // ---- a list that is not null: nothing differs ----

    private static IEnumerable<TestCaseData> WithAList()
    {
        var selection = new[] { new DateTime(2023, 2, 1), new DateTime(2023, 2, 14, 23, 59, 59, DateTimeKind.Utc), new DateTime(2023, 2, 14), new DateTime(2024, 2, 29), new DateTime(2022, 2, 14) };
        yield return Case("an empty list", new("") { Draw = LegacyDraw.MonthWithDates, Month = 2, Year = 2023 });
        yield return Case("selected dates", new("") { Draw = LegacyDraw.MonthWithDates, Month = 2, Year = 2023, Dates = selection, ForegroundBefore = ConsoleColor.DarkCyan, BackgroundBefore = ConsoleColor.DarkBlue });
        yield return Case("a year with selected dates, terminator \\r\\n", new("") { Draw = LegacyDraw.YearWithDates, Year = 2024, Dates = selection, NewLine = "\r\n" });
        yield return Case("a year with an empty list", new("") { Draw = LegacyDraw.YearWithDates, Year = 2023 });
        yield return Case("the overload without a list", new("") { Draw = LegacyDraw.Month, Month = 2, Year = 2023 });
        yield return Case("the year overload without a list", new("") { Draw = LegacyDraw.Year, Year = 2023 });
        foreach ((int month, int year) in new[] { (0, 2023), (13, 2023), (-1, 2023), (1, 0), (1, 10000), (13, 0), (0, 10000) })
        {
            yield return Case($"an invalid month {month} of year {year}, with a list", new("") { Draw = LegacyDraw.MonthWithDates, Month = month, Year = year, Dates = selection });
            yield return Case($"an invalid month {month} of year {year}, without a list", new("") { Draw = LegacyDraw.Month, Month = month, Year = year });
        }
        foreach (int year in new[] { 0, 10000, -1 })
        {
            yield return Case($"an invalid year {year}, with a list", new("") { Draw = LegacyDraw.YearWithDates, Year = year, Dates = selection });
            yield return Case($"an invalid year {year}, without a list", new("") { Draw = LegacyDraw.Year, Year = year });
        }

        static TestCaseData Case(string name, LegacyScenario scenario) => new TestCaseData(new ScenarioCase(scenario)).SetArgDisplayNames(name);
    }

    /// <summary>Carries a scenario into a public test method, whose parameters must be public types.</summary>
    public sealed class ScenarioCase
    {
        internal ScenarioCase(LegacyScenario scenario) => Scenario = scenario;

        internal LegacyScenario Scenario { get; }
    }

    /// <summary>
    /// With a list, valid or not, the two versions do the same in every aspect: the same text and calls, the
    /// same selected days, and for an invalid month or year the runtime's own exception with its parameter name
    /// and its message.
    /// </summary>
    [TestCaseSource(nameof(WithAList))]
    public void WithAListThatIsNotNull_NothingDiffers(ScenarioCase with)
    {
        LegacyObservation published100 = In100(with.Scenario);
        LegacyObservation library = In20(with.Scenario);

        Assert.That(library.DifferencesFrom(published100), Is.Empty);
        Assert.That(library.ExceptionType, Is.Not.EqualTo("System.ArgumentNullException"));
    }

    // ---- the correction as the migration tests state it ----

    [Test]
    public void TheCorrection_ChangesTheExceptionOfANullList_AndNothingElseOfTheObservation()
    {
        LegacyScenario scenario = NullList(LegacyDraw.MonthWithDates, 13, 0);
        LegacyObservation published100 = In100(scenario);

        LegacyObservation corrected = ApprovedCorrection.NullDateList.Corrected!(scenario, published100);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(corrected.AspectsThatDifferFrom(published100), Is.EqualTo(TheException));
            Assert.That(corrected.ExceptionType, Is.EqualTo("System.ArgumentNullException"));
            Assert.That(corrected.ExceptionParameterName, Is.EqualTo(ApprovedCorrection.NullDateListParameter));
            Assert.That(corrected.ExceptionParameterName, Is.EqualTo("dates"));
            Assert.That(In20(scenario).DifferencesFrom(corrected), Is.Empty, "and that is exactly what the library does");
        }
    }

    [Test]
    public void TheCorrection_LeavesEveryObservationWithAListAsItIs()
    {
        foreach (LegacyScenario scenario in new LegacyScenario[]
        {
            new("valid") { Draw = LegacyDraw.MonthWithDates, Month = 2, Year = 2023, Dates = Fourteenth },
            new("invalid month") { Draw = LegacyDraw.MonthWithDates, Month = 13, Year = 2023, Dates = Fourteenth },
            new("no list") { Draw = LegacyDraw.Year, Year = 0 },
            new("nothing drawn"),
        })
        {
            LegacyObservation published100 = In100(scenario);

            Assert.That(ApprovedCorrection.NullDateList.Corrected!(scenario, published100), Is.SameAs(published100), scenario.Name);
        }
    }

    /// <summary>The correction replaces the exception 1.0.0 threw for a null list and no other.</summary>
    [Test]
    public void TheCorrection_DoesNotInventAnExceptionWhereThereWasNoNullReference()
    {
        LegacyScenario scenario = NullList(LegacyDraw.MonthWithDates, 2, 2023);
        LegacyObservation completed = LegacyObservation.Capture(() => Console.Write("no exception"));
        LegacyObservation another = LegacyObservation.Capture(() => throw new InvalidOperationException());

        Assert.That(ApprovedCorrection.NullDateList.Corrected!(scenario, completed), Is.SameAs(completed));
        Assert.That(ApprovedCorrection.NullDateList.Corrected!(scenario, another), Is.SameAs(another));
    }
}
