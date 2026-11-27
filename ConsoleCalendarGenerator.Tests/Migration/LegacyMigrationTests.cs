using System.Globalization;
using ConsoleCalendarGenerator.Tests.Support;

namespace ConsoleCalendarGenerator.Tests.Migration;

/// <summary>
/// The legacy calendar of the library, compared with the frozen source of version 1.0.0.
///
/// <para>
/// The rule these tests enforce: the library does exactly what 1.0.0 did, in every scenario and in every aspect
/// a caller can observe, except for the approved corrections that have been implemented
/// (<see cref="ApprovedCorrection"/>). A difference that no implemented correction accounts for fails here,
/// whether it is an accident or a correction made without being recorded.
/// </para>
/// <para>
/// Implemented so far: the constructor colors (D-57) and the exception for a null list (D-58). Nothing here
/// says what one of the other three corrections will do before it is implemented; until then the expectation
/// for it is 1.0.0.
/// </para>
/// </summary>
[TestFixture]
public class LegacyMigrationTests
{
    /// <summary>The scenarios of the matrix by the text that names them, which is what a test case shows.</summary>
    private static IEnumerable<string> Scenarios() => MigrationMatrix.All.Select(scenario => scenario.ToString());

    [TestCaseSource(nameof(Scenarios))]
    public void TheLibrary_DoesWhat100Did_ApartFromTheCorrectionsThatAreImplemented(string scenario)
    {
        AssertAsExpected(MigrationMatrix.All.Single(candidate => candidate.ToString() == scenario));
    }

    /// <summary>Every month of 201 years and of the first and the last year, one at a time.</summary>
    [Test]
    public void EveryMonthFrom1900Through2100_AndOfTheYears1And9999()
    {
        IEnumerable<int> years = Enumerable.Range(1900, 201).Append(1).Append(9999);
        int compared = 0;
        foreach (int year in years)
        {
            for (int month = 1; month <= 12; month++)
            {
                AssertAsExpected(new LegacyScenario($"month {month} of year {year}") { Draw = LegacyDraw.Month, Month = month, Year = year });
                compared++;
            }
        }
        Assert.That(compared, Is.EqualTo(203 * 12));
    }

    /// <summary>
    /// Every culture of the runtime whose own calendar is not the Gregorian one, for every month of a year. The
    /// two implementations are compared with each other, so the names the system has do not matter.
    /// </summary>
    [Test]
    public void EveryCultureWhoseCalendarIsNotGregorian()
    {
        CultureInfo[] cultures = CultureInfo.GetCultures(CultureTypes.AllCultures)
            .Where(culture => culture.DateTimeFormat.Calendar is not GregorianCalendar)
            .ToArray();
        Assume.That(cultures, Is.Not.Empty, "This machine has no culture whose calendar is not Gregorian.");

        foreach (CultureInfo culture in cultures)
        {
            AssertAsExpected(new LegacyScenario("a year under " + culture.Name) { Draw = LegacyDraw.Year, Year = 2023, Culture = culture });
        }
    }

    /// <summary>
    /// Months, selected dates, colors, terminators and constructors chosen by a generator with a fixed seed, so
    /// that combinations nobody thought of are compared too, and are the same in every run.
    /// </summary>
    [Test]
    public void FiveHundredGeneratedScenarios()
    {
        var random = new Random(20261127);
        string[] terminators = { "\n", "\r\n" };
        for (int index = 0; index < 500; index++)
        {
            int year = random.Next(1990, 2041);
            int month = random.Next(1, 13);
            var dates = new List<DateTime>();
            int count = random.Next(0, 12);
            for (int date = 0; date < count; date++)
            {
                // Mostly the month drawn, sometimes a neighbouring month or year, with a time of day and a kind.
                int dateYear = year + (random.Next(8) == 0 ? random.Next(-1, 2) : 0);
                int dateMonth = random.Next(8) == 0 ? random.Next(1, 13) : month;
                int day = random.Next(1, DateTime.DaysInMonth(dateYear, dateMonth) + 1);
                dates.Add(new DateTime(dateYear, dateMonth, day, random.Next(24), random.Next(60), 0, (DateTimeKind)random.Next(3)));
            }
            ConsoleColor[] arguments = Enumerable.Range(0, random.Next(0, 5)).Select(_ => (ConsoleColor)random.Next(16)).ToArray();
            var setters = new List<(LegacyColorRole, ConsoleColor)>();
            for (int setter = random.Next(0, 3); setter > 0; setter--)
            {
                setters.Add(((LegacyColorRole)random.Next(4), (ConsoleColor)random.Next(16)));
            }

            AssertAsExpected(new LegacyScenario("generated scenario " + index)
            {
                ConstructorArguments = arguments,
                Setters = setters,
                Draw = random.Next(10) == 0 ? LegacyDraw.YearWithDates : random.Next(4) == 0 ? LegacyDraw.Month : LegacyDraw.MonthWithDates,
                Month = month,
                Year = year,
                Dates = dates,
                NewLine = terminators[random.Next(terminators.Length)],
                ForegroundBefore = random.Next(3) == 0 ? (ConsoleColor)random.Next(16) : null,
                BackgroundBefore = random.Next(3) == 0 ? (ConsoleColor)random.Next(16) : null,
            });
        }
    }

    /// <summary>
    /// The selections that the digests recorded from 1.0.0 use: every n-th day of every month of two years, and
    /// the first and the last day of every month of a year. Compared here call by call, so that this fixture
    /// covers what those digests cover.
    /// </summary>
    [Test]
    public void EveryNthDaySelected_AndTheFirstAndTheLastDayOfEveryMonth()
    {
        foreach (int step in new[] { 1, 2, 3, 7, 10 })
        {
            for (int year = 2023; year <= 2024; year++)
            {
                for (int month = 1; month <= 12; month++)
                {
                    DateTime[] dates = Enumerable.Range(1, DateTime.DaysInMonth(year, month))
                        .Where(day => day % step == 0)
                        .Select(day => new DateTime(year, month, day))
                        .ToArray();
                    AssertAsExpected(new LegacyScenario($"every {step}. day of month {month} of year {year}")
                    {
                        Draw = LegacyDraw.MonthWithDates,
                        Month = month,
                        Year = year,
                        Dates = dates,
                    });
                }
            }
        }
        for (int month = 1; month <= 12; month++)
        {
            foreach (int day in new[] { 1, DateTime.DaysInMonth(2023, month) })
            {
                AssertAsExpected(new LegacyScenario($"day {day} of month {month} selected")
                {
                    Draw = LegacyDraw.MonthWithDates,
                    Month = month,
                    Year = 2023,
                    Dates = new[] { new DateTime(2023, month, day) },
                    NewLine = "\r\n",
                });
            }
        }
    }

    // ---- the corrections ----

    [Test]
    public void ThereAreExactlyFiveApprovedCorrections_EachWithADecisionOfItsOwn()
    {
        Assert.That(ApprovedCorrection.All.Select(correction => correction.Decision), Is.EqualTo(new[] { "D-57", "D-58", "D-59", "D-60", "D-61" }));
    }

    /// <summary>A correction cannot be listed here without the decision that approves it.</summary>
    [Test]
    public void EveryApprovedCorrection_IsADecisionOfTheMaintenanceNotes()
    {
        string decisions = Repository.Read("docs/ai-context/decisions.md");

        foreach (ApprovedCorrection correction in ApprovedCorrection.All)
        {
            Assert.That(decisions, Ordinal.Contains("\n### " + correction.Decision + ": "), correction.ToString());
        }
    }

    /// <summary>
    /// Every correction has scenarios in which 1.0.0 shows the behavior it corrects, so that implementing it in
    /// the library cannot go unnoticed by <see cref="TheLibrary_DoesWhat100Did_ApartFromTheCorrectionsThatAreImplemented"/>.
    /// </summary>
    [Test]
    public void EveryApprovedCorrection_HasScenariosOfItsOwnInTheMatrix()
    {
        foreach (ApprovedCorrection correction in ApprovedCorrection.All)
        {
            Assert.That(correction.Scenarios, Has.Count.GreaterThanOrEqualTo(5), correction.ToString());
            Assert.That(MigrationMatrix.All, Is.SupersetOf(correction.Scenarios), correction.ToString());
        }
        Assert.That(MigrationMatrix.OtherBehavior, Has.Count.GreaterThanOrEqualTo(40), "behavior no correction is about");
        Assert.That(MigrationMatrix.All.Select(scenario => scenario.ToString()), Is.Unique);
    }

    private static void AssertAsExpected(LegacyScenario scenario)
    {
        CultureInfo? culture = scenario.ResolveCulture();
        Assume.That(culture, Is.Not.Null, "The culture " + scenario.CultureName + " is not available on this machine.");

        // What 1.0.0 did in the use of it that the implemented corrections say is the same, with the implemented
        // corrections that change an observation applied. Everything else about the scenario is as given.
        LegacyObservation published100 = LegacyObservation.Of(LegacySubject.Published100, ApprovedCorrection.ScenarioFor100(scenario), culture!);
        LegacyObservation library = LegacyObservation.Of(LegacySubject.Library, scenario, culture!);
        LegacyObservation expected = ApprovedCorrection.ExpectedOfTheLibrary(scenario, published100);

        Assert.That(library.DifferencesFrom(expected).Select(difference => difference.ToString()), Is.Empty,
            scenario + ": the library differs from 1.0.0 in a way that no implemented correction accounts for.");
    }
}
