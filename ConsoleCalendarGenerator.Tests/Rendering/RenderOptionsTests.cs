using System.Globalization;
using ConsoleCalendarGenerator.Tests.Support;

namespace ConsoleCalendarGenerator.Tests.Rendering;

/// <summary>
/// The options as an immutable value: every change makes a new instance, the one it was made from stays as it
/// was, and options that were built to the same settings render the same.
/// </summary>
[TestFixture]
public class RenderOptionsTests
{
    private static readonly string[] Labels = { "S", "M", "T", "W", "T", "F", "S" };

    /// <summary>Every setting changed away from its default, one call each.</summary>
    private static readonly (string Name, Func<MonthRenderOptions, MonthRenderOptions> Change)[] Changes =
    {
        ("Culture", options => options.WithCulture(TestCultures.WithForeignWeek())),
        ("FirstDayOfWeek", options => options.WithFirstDayOfWeek(DayOfWeek.Thursday)),
        ("WeekdayLabelStyle", options => options.WithWeekdayLabelStyle(WeekdayLabelStyle.Abbreviated)),
        ("WeekdayLabels", options => options.WithWeekdayLabels(Labels)),
        ("CellWidth", options => options.WithCellWidth(5)),
        ("ColumnGap", options => options.WithColumnGap(2)),
        ("Indent", options => options.WithIndent("  ")),
        ("TitleFormat", options => options.WithTitleFormat("MMMM")),
        ("TitleSuffix", options => options.WithTitleSuffix("!")),
        ("TitleAlignment", options => options.WithTitleAlignment(TitleAlignment.Left)),
    };

    private static string Describe(MonthRenderOptions options) => string.Join("|",
        options.Culture.Name, options.Culture.DateTimeFormat.GetShortestDayName(DayOfWeek.Monday), options.FirstDayOfWeek, options.WeekdayLabelStyle,
        options.WeekdayLabels == null ? "none" : string.Join(",", options.WeekdayLabels),
        options.CellWidth, options.ColumnGap, options.GridWidth, "<" + options.Indent + ">", options.TitleFormat,
        "<" + options.TitleSuffix + ">", options.TitleAlignment);

    [Test]
    public void EveryChange_ReturnsANewInstance_AndLeavesTheOriginalAsItWas()
    {
        string defaults = Describe(MonthRenderOptions.Default);
        string defaultRendering = Rendered.Describe(MonthRenderer.Render(2023, 5, MonthRenderOptions.Default));

        foreach ((string name, Func<MonthRenderOptions, MonthRenderOptions> change) in Changes)
        {
            MonthRenderOptions changed = change(MonthRenderOptions.Default);

            Assert.That(changed, Is.Not.SameAs(MonthRenderOptions.Default), name);
            Assert.That(Describe(changed), Is.Not.EqualTo(defaults), name + " is changed in the new instance");
            Assert.That(Describe(MonthRenderOptions.Default), Is.EqualTo(defaults), name + " is unchanged in the default instance");
        }

        Assert.That(Rendered.Describe(MonthRenderer.Render(2023, 5, MonthRenderOptions.Default)), Is.EqualTo(defaultRendering));
    }

    [Test]
    public void EveryChange_ChangesWhatIsRendered()
    {
        // One wider than usual, so that a longer label is not cut back to the same two letters.
        static string Render(MonthRenderOptions options) =>
            Rendered.Describe(MonthRenderer.Render(2023, 5, options.WithCellWidth(options.CellWidth + 1)));
        string defaultRendering = Render(MonthRenderOptions.Default);

        foreach ((string name, Func<MonthRenderOptions, MonthRenderOptions> change) in Changes)
        {
            Assert.That(Render(change(MonthRenderOptions.Default)), Is.Not.EqualTo(defaultRendering), name);
        }
    }

    [Test]
    public void AChange_KeepsEveryOtherSetting()
    {
        MonthRenderOptions everything = Changes.Where(change => change.Name != "WeekdayLabelStyle")
            .Aggregate(MonthRenderOptions.Default, (options, change) => change.Change(options));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(everything.Culture.DateTimeFormat.GetShortestDayName(DayOfWeek.Monday), Is.EqualTo("lu"));
            Assert.That(everything.FirstDayOfWeek, Is.EqualTo(DayOfWeek.Thursday));
            Assert.That(everything.WeekdayLabels, Is.EqualTo(Labels));
            Assert.That(everything.CellWidth, Is.EqualTo(5));
            Assert.That(everything.ColumnGap, Is.EqualTo(2));
            Assert.That(everything.GridWidth, Is.EqualTo(47));
            Assert.That(everything.Indent, Is.EqualTo("  "));
            Assert.That(everything.TitleFormat, Is.EqualTo("MMMM"));
            Assert.That(everything.TitleSuffix, Is.EqualTo("!"));
            Assert.That(everything.TitleAlignment, Is.EqualTo(TitleAlignment.Left));
        }
    }

    /// <summary>The same settings reached in a different order are the same options.</summary>
    [Test]
    public void TheOrderOfTheChanges_MakesNoDifference()
    {
        var changes = Changes.Where(change => change.Name != "WeekdayLabelStyle").ToArray();
        MonthRenderOptions forwards = changes.Aggregate(MonthRenderOptions.Default, (options, change) => change.Change(options));
        MonthRenderOptions backwards = changes.Reverse().Aggregate(MonthRenderOptions.Default, (options, change) => change.Change(options));

        Assert.That(Describe(backwards), Is.EqualTo(Describe(forwards)));
        for (int month = 1; month <= 12; month++)
        {
            Assert.That(Rendered.Describe(MonthRenderer.Render(2024, month, backwards)), Is.EqualTo(Rendered.Describe(MonthRenderer.Render(2024, month, forwards))));
        }
    }

    [Test]
    public void SettingTheDefaultValuesExplicitly_RendersLikeTheDefault()
    {
        MonthRenderOptions spelledOut = MonthRenderOptions.Default
            .WithCulture(CultureInfo.InvariantCulture)
            .WithFirstDayOfWeek(DayOfWeek.Monday)
            .WithWeekdayLabelStyle(WeekdayLabelStyle.Shortest)
            .WithCellWidth(2)
            .WithColumnGap(1)
            .WithIndent(string.Empty)
            .WithTitleFormat("Y")
            .WithTitleSuffix(string.Empty)
            .WithTitleAlignment(TitleAlignment.Center);

        Assert.That(Describe(spelledOut), Is.EqualTo(Describe(MonthRenderOptions.Default)));
        Assert.That(Rendered.Describe(MonthRenderer.Render(2023, 5, spelledOut)), Is.EqualTo(Rendered.Describe(MonthRenderer.Render(2023, 5, MonthRenderOptions.Default))));
    }

    [Test]
    public void OneOptionsInstance_CanRenderManyMonths()
    {
        MonthRenderOptions options = MonthRenderOptions.Default.WithCellWidth(3).WithTitleFormat("MMMM yyyy");

        string january = MonthRenderer.Render(2023, 1, options).ToText();
        _ = MonthRenderer.Render(2023, 2, options);

        Assert.That(MonthRenderer.Render(2023, 1, options).ToText(), Is.EqualTo(january));
    }

    // ---- invalid arguments, in one place ----

    [Test]
    public void ANullCulture_IsRejected()
    {
        Assert.That(Assert.Throws<ArgumentNullException>(() => MonthRenderOptions.Default.WithCulture(null!))!.ParamName, Is.EqualTo("culture"));
    }

    [TestCase(-1)]
    [TestCase(7)]
    [TestCase(int.MaxValue)]
    public void AFirstDayOfWeekThatIsNotADayOfTheWeek_IsRejected(int value)
    {
        var firstDayOfWeek = (DayOfWeek)value;

        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(
            () => MonthRenderOptions.Default.WithFirstDayOfWeek(firstDayOfWeek))!;

        Assert.That(exception.ParamName, Is.EqualTo("firstDayOfWeek"));
        Assert.That(exception.ActualValue, Is.EqualTo(firstDayOfWeek));
    }

    [Test]
    public void EverySevenDaysOfTheWeek_AreAcceptedAsTheFirst([Values] DayOfWeek firstDayOfWeek)
    {
        Assert.That(MonthRenderOptions.Default.WithFirstDayOfWeek(firstDayOfWeek).FirstDayOfWeek, Is.EqualTo(firstDayOfWeek));
    }

    /// <summary>A rejected change leaves nothing behind: the options it was tried on are still usable and unchanged.</summary>
    [Test]
    public void ARejectedChange_LeavesTheOptionsUnchanged()
    {
        MonthRenderOptions options = MonthRenderOptions.Default.WithCellWidth(3);
        string before = Describe(options);

        Assert.That(() => options.WithCellWidth(1), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => options.WithTitleFormat("x"), Throws.TypeOf<FormatException>());
        Assert.That(() => options.WithWeekdayLabels(new[] { "only one" }), Throws.TypeOf<ArgumentException>());

        Assert.That(Describe(options), Is.EqualTo(before));
    }
}
