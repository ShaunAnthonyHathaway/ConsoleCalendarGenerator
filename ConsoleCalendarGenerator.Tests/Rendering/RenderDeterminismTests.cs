using System.Collections.Concurrent;
using System.Globalization;
using ConsoleCalendarGenerator.Tests.Support;

namespace ConsoleCalendarGenerator.Tests.Rendering;

/// <summary>
/// Rendering is a function of its arguments and nothing else. The same arguments give the same result, on any
/// thread, and rendering leaves the console and the rest of the process as they were.
/// </summary>
[TestFixture]
public class RenderDeterminismTests
{
    /// <summary>Options that differ in culture and in shape, so that a mix-up between them would show.</summary>
    private static MonthRenderOptions[] OptionSets()
    {
        var sets = new List<MonthRenderOptions>
        {
            MonthRenderOptions.Default,
            MonthRenderOptions.Default.WithCulture(TestCultures.WithMonthNames(month => "Alpha" + month)).WithCellWidth(3),
            MonthRenderOptions.Default.WithCulture(TestCultures.WithMonthNames(month => "Beta" + month)).WithFirstDayOfWeek(DayOfWeek.Sunday),
            MonthRenderOptions.Default.WithCulture(TestCultures.WithForeignWeek()).WithColumnGap(2).WithIndent("  ").WithTitleSuffix(" *"),
        };
        foreach (string name in new[] { "en-US", "de-DE", "th-TH", "fa-IR" })
        {
            if (TestCultures.TryGet(name) is CultureInfo culture)
            {
                sets.Add(MonthRenderOptions.Default.WithCulture(culture).WithWeekdayLabelStyle(WeekdayLabelStyle.Abbreviated).WithCellWidth(4));
            }
        }
        return sets.ToArray();
    }

    private static string[] DescribeYear(MonthRenderOptions options) =>
        Enumerable.Range(1, 12).Select(month => Rendered.Describe(MonthRenderer.Render(2024, month, options))).ToArray();

    [Test]
    public void TheSameArguments_AlwaysGiveAnEquivalentResult()
    {
        foreach (MonthRenderOptions options in OptionSets())
        {
            Assert.That(DescribeYear(options), Is.EqualTo(DescribeYear(options)));
        }
    }

    [Test]
    public void OptionsBuiltTwiceTheSameWay_GiveEquivalentResults()
    {
        MonthRenderOptions[] first = OptionSets();
        MonthRenderOptions[] second = OptionSets();

        for (int index = 0; index < first.Length; index++)
        {
            Assert.That(first[index], Is.Not.SameAs(second[index]).Or.SameAs(MonthRenderOptions.Default));
            Assert.That(DescribeYear(second[index]), Is.EqualTo(DescribeYear(first[index])));
        }
    }

    /// <summary>
    /// Many threads render at once, with different cultures and shapes, several of them with the same options at
    /// the same moment. Every result is the one a single thread gets, so no render sees another's culture.
    /// </summary>
    [Test]
    public void RendersInParallel_WithDifferentCultures_DoNotAffectEachOther()
    {
        MonthRenderOptions[] sets = OptionSets();
        string[][] expected = sets.Select(DescribeYear).ToArray();
        const int Rounds = 40;
        var wrong = new ConcurrentBag<string>();

        Parallel.For(0, sets.Length * 12 * Rounds, new ParallelOptions { MaxDegreeOfParallelism = 8 }, index =>
        {
            int set = index % sets.Length;
            int month = (index / sets.Length % 12) + 1;
            if (Rendered.Describe(MonthRenderer.Render(2024, month, sets[set])) != expected[set][month - 1])
            {
                wrong.Add($"options {set}, month {month}");
            }
        });

        Assert.That(wrong, Is.Empty);
    }

    [Test]
    public void OneRenderedMonth_CanBeReadFromManyThreadsAtOnce()
    {
        RenderedMonth rendered = MonthRenderer.Render(2024, 2, MonthRenderOptions.Default.WithCellWidth(3));
        string description = Rendered.Describe(rendered);
        string text = rendered.ToText("\r\n");
        var wrong = new ConcurrentBag<int>();

        Parallel.For(0, 2000, new ParallelOptions { MaxDegreeOfParallelism = 8 }, index =>
        {
            if (Rendered.Describe(rendered) != description || rendered.ToText("\r\n") != text)
            {
                wrong.Add(index);
            }
        });

        Assert.That(wrong, Is.Empty);
    }

    // ---- the process ----

    [Test]
    public void RenderingAndReadingTheResult_WritesNothingToTheConsole()
    {
        MonthRenderOptions[] sets = OptionSets();
        TextWriter originalError = Console.Error;
        var error = new StringWriter();
        Captured captured;
        try
        {
            Console.SetError(error);
            captured = ConsoleCapture.Run(() =>
            {
                foreach (MonthRenderOptions options in sets)
                {
                    _ = DescribeYear(options);
                    _ = MonthRenderer.Render(2024, 2, options).ToText();
                }
            });
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
    public void Rendering_LeavesTheConsoleColorsAlone()
    {
        Captured captured = ConsoleCapture.Run(
            () => _ = MonthRenderer.Render(2024, 2, MonthRenderOptions.Default).ToText(),
            foregroundBefore: ConsoleColor.DarkCyan,
            backgroundBefore: ConsoleColor.DarkBlue);

        Assert.That(captured.ForegroundAfter, Is.EqualTo(ConsoleColor.DarkCyan));
        Assert.That(captured.BackgroundAfter, Is.EqualTo(ConsoleColor.DarkBlue));
    }

    /// <summary>
    /// The cultures, the default cultures for new threads, the console writers, the environment variables and
    /// the current directory are what they were. That includes creating options, rendering in parallel, and
    /// calls that fail.
    /// </summary>
    [Test]
    public void CreatingOptionsAndRendering_ChangeNoProcessWideState()
    {
        object[] before = ProcessState.Snapshot();

        MonthRenderOptions[] sets = OptionSets();
        Parallel.ForEach(sets, options => _ = DescribeYear(options));
        Assert.That(() => MonthRenderer.Render(0, 0, null!), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => MonthRenderOptions.Default.WithTitleFormat("x"), Throws.TypeOf<FormatException>());
        Assert.That(() => MonthRenderOptions.Default.WithCulture(null!), Throws.TypeOf<ArgumentNullException>());

        Assert.That(ProcessState.Snapshot(), Is.EqualTo(before));
    }
}
