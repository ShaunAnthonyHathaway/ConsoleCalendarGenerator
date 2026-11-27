using System.Collections;
using System.Collections.Concurrent;
using System.Globalization;
using ConsoleCalendarGenerator.Tests.Support;

namespace ConsoleCalendarGenerator.Tests.Layout;

/// <summary>
/// A layout is a function of its three arguments and nothing else: not the culture, not the console, not the
/// thread it is created on. Creating one leaves the process as it was.
/// </summary>
[TestFixture]
public class LayoutDeterminismTests
{
    /// <summary>Every month of a leap year and of the year after it, for every first day of the week: 168 inputs.</summary>
    private static readonly (int Year, int Month, DayOfWeek FirstDayOfWeek)[] Inputs =
        (from year in new[] { 2024, 2025 }
         from month in Enumerable.Range(1, 12)
         from firstDayOfWeek in ReferenceCalendar.AllDaysOfWeek
         select (year, month, firstDayOfWeek)).ToArray();

    private static string[] DescribeAll() =>
        Inputs.Select(input => LayoutPicture.Describe(MonthLayout.Create(input.Year, input.Month, input.FirstDayOfWeek))).ToArray();

    [Test]
    public void TheSameArguments_AlwaysGiveAnEquivalentLayout()
    {
        string[] first = DescribeAll();
        string[] second = DescribeAll();

        Assert.That(second, Is.EqualTo(first));
        Assert.That(first.Distinct().Count(), Is.EqualTo(Inputs.Length), "and different arguments give different layouts");
    }

    // ---- culture ----

    /// <summary>
    /// The current culture and the current UI culture are set to cultures whose week starts on another day or
    /// whose calendar is not the Gregorian one. The layouts are the same as under the invariant culture.
    /// </summary>
    [Test]
    public void TheCurrentCultures_DoNotAffectTheLayout()
    {
        var cultures = new List<CultureInfo> { TestCultures.WithForeignWeek() };
        // Real cultures, where the operating system has them: Persian and Thai Buddhist calendars, weeks that
        // start on Saturday, Sunday and Monday.
        foreach (string name in new[] { "fa-IR", "th-TH", "ar-SA", "he-IL", "en-US", "de-DE" })
        {
            if (TestCultures.TryGet(name) is CultureInfo culture)
            {
                cultures.Add(culture);
            }
        }

        string[] expected = UnderCultures(CultureInfo.InvariantCulture, CultureInfo.InvariantCulture, DescribeAll);

        foreach (CultureInfo culture in cultures)
        {
            Assert.That(UnderCultures(culture, culture, DescribeAll), Is.EqualTo(expected), "CurrentCulture and CurrentUICulture " + culture.Name);
            Assert.That(UnderCultures(culture, CultureInfo.InvariantCulture, DescribeAll), Is.EqualTo(expected), "CurrentCulture " + culture.Name);
            Assert.That(UnderCultures(CultureInfo.InvariantCulture, culture, DescribeAll), Is.EqualTo(expected), "CurrentUICulture " + culture.Name);
        }
    }

    /// <summary>The first day of the week is the one asked for, never the culture's.</summary>
    [Test]
    public void TheFirstDayOfWeek_ComesFromTheArgument_NotFromTheCulture()
    {
        CultureInfo saturdayFirst = TestCultures.WithForeignWeek();
        Assert.That(saturdayFirst.DateTimeFormat.FirstDayOfWeek, Is.EqualTo(DayOfWeek.Saturday));

        string[] picture = UnderCultures(saturdayFirst, saturdayFirst, () => LayoutPicture.Of(MonthLayout.Create(2023, 5, DayOfWeek.Monday)));

        Assert.That(picture[0], Is.EqualTo("Mo Tu We Th Fr Sa Su"));
        Assert.That(picture[1], Is.EqualTo(" 1  2  3  4  5  6  7"));
    }

    private static T UnderCultures<T>(CultureInfo culture, CultureInfo uiCulture, Func<T> action)
    {
        CultureInfo originalCulture = CultureInfo.CurrentCulture;
        CultureInfo originalUiCulture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentCulture = culture;
            CultureInfo.CurrentUICulture = uiCulture;
            return action();
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
            CultureInfo.CurrentUICulture = originalUiCulture;
        }
    }

    // ---- threads ----

    /// <summary>
    /// Many threads create layouts at once, several of them for the same arguments at the same moment. Every
    /// result is the one a single thread gets.
    /// </summary>
    [Test]
    public void LayoutsCreatedInParallel_AreCorrectAndIndependent()
    {
        string[] expected = DescribeAll();
        const int Rounds = 50;
        var wrong = new ConcurrentBag<string>();

        Parallel.For(0, Inputs.Length * Rounds, new ParallelOptions { MaxDegreeOfParallelism = 8 }, index =>
        {
            (int year, int month, DayOfWeek firstDayOfWeek) = Inputs[index % Inputs.Length];
            if (LayoutPicture.Describe(MonthLayout.Create(year, month, firstDayOfWeek)) != expected[index % Inputs.Length])
            {
                wrong.Add($"{year}-{month:00} from {firstDayOfWeek}");
            }
        });

        Assert.That(wrong, Is.Empty);
    }

    [Test]
    public void OneLayout_CanBeReadFromManyThreadsAtOnce()
    {
        MonthLayout layout = MonthLayout.Create(2024, 2, DayOfWeek.Sunday);
        string expected = LayoutPicture.Describe(layout);
        var wrong = new ConcurrentBag<int>();

        Parallel.For(0, 2000, new ParallelOptions { MaxDegreeOfParallelism = 8 }, index =>
        {
            if (LayoutPicture.Describe(layout) != expected)
            {
                wrong.Add(index);
            }
        });

        Assert.That(wrong, Is.Empty);
    }

    // ---- the process ----

    [Test]
    public void CreatingAndReadingLayouts_WritesNothingToTheConsole()
    {
        TextWriter originalError = Console.Error;
        var error = new StringWriter();
        Captured captured;
        try
        {
            Console.SetError(error);
            captured = ConsoleCapture.Run(() => _ = DescribeAll());
        }
        finally
        {
            Console.SetError(originalError);
        }

        Assert.That(captured.Events, Is.Empty, "nothing is written to Console.Out");
        Assert.That(error.ToString(), Is.Empty, "nothing is written to Console.Error");
    }

    [Test]
    [Platform(Exclude = "Win", Reason = ConsoleState.ColorsObservableOnUnixOnly)]
    public void CreatingAndReadingLayouts_LeavesTheConsoleColorsAlone()
    {
        Captured captured = ConsoleCapture.Run(
            () => _ = DescribeAll(),
            foregroundBefore: ConsoleColor.DarkCyan,
            backgroundBefore: ConsoleColor.DarkBlue);

        Assert.That(captured.ForegroundAfter, Is.EqualTo(ConsoleColor.DarkCyan));
        Assert.That(captured.BackgroundAfter, Is.EqualTo(ConsoleColor.DarkBlue));
    }

    /// <summary>
    /// The cultures, the default cultures for new threads, the console writers, the environment variables and
    /// the current directory are what they were. That includes a call that fails.
    /// </summary>
    [Test]
    public void CreatingLayouts_ChangesNoProcessWideState()
    {
        object[] before = ProcessWideState();

        _ = DescribeAll();
        Assert.That(() => MonthLayout.Create(0, 0, (DayOfWeek)7), Throws.TypeOf<ArgumentOutOfRangeException>());

        Assert.That(ProcessWideState(), Is.EqualTo(before));
    }

    private static object[] ProcessWideState() => new object[]
    {
        CultureInfo.CurrentCulture,
        CultureInfo.CurrentUICulture,
        CultureInfo.DefaultThreadCurrentCulture ?? (object)"none",
        CultureInfo.DefaultThreadCurrentUICulture ?? (object)"none",
        Console.Out,
        Console.Error,
        Environment.CurrentDirectory,
        string.Join("\n", Environment.GetEnvironmentVariables().Cast<DictionaryEntry>()
            .Select(variable => variable.Key + "=" + variable.Value)
            .OrderBy(line => line, StringComparer.Ordinal)),
    };
}
