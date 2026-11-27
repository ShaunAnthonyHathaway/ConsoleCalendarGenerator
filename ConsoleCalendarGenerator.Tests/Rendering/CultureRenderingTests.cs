using System.Globalization;
using ConsoleCalendarGenerator.Tests.Support;

namespace ConsoleCalendarGenerator.Tests.Rendering;

/// <summary>
/// What the culture of the options decides and what it does not. It supplies the language of the title and of
/// the weekday labels. It does not decide the calendar (always Gregorian), the first day of the week (always the
/// option) or the digits of the days. The process's current cultures decide nothing.
/// </summary>
/// <remarks>
/// Month and day names of real cultures come from the operating system's culture data, which differs between
/// versions. Tests of real cultures therefore compare with what the runtime reports, and assert literally only
/// what cannot reasonably change, such as the year. Exact text is tested with cultures built in the test.
/// </remarks>
[TestFixture]
public class CultureRenderingTests
{
    private static CultureInfo Culture(string name)
    {
        CultureInfo? culture = TestCultures.TryGet(name);
        Assume.That(culture, Is.Not.Null, "The culture " + name + " is not available on this system.");
        return culture!;
    }

    /// <summary>The culture's names and patterns for the Gregorian calendar, obtained without the library.</summary>
    private static DateTimeFormatInfo GregorianFormat(CultureInfo culture)
    {
        if (culture.DateTimeFormat.Calendar is GregorianCalendar)
        {
            return culture.DateTimeFormat;
        }
        var format = (DateTimeFormatInfo)culture.DateTimeFormat.Clone();
        format.Calendar = culture.OptionalCalendars.First(calendar => calendar is GregorianCalendar);
        return format;
    }

    private static string TitleOf(RenderedMonth rendered) =>
        rendered.Lines[0].Segments.Single(segment => segment.Kind == RenderedSegmentKind.Title).Text;

    private static string[] LabelsOf(RenderedMonth rendered) => rendered.Lines[1].Segments
        .Where(segment => segment.Kind == RenderedSegmentKind.WeekdayLabel)
        .Select(segment => segment.Text)
        .ToArray();

    // ---- exact text, with cultures built here ----

    [Test]
    public void TheInvariantCulture_GivesEnglishNames_YearFirst()
    {
        RenderedMonth rendered = MonthRenderer.Render(2023, 5, MonthRenderOptions.Default.WithCulture(CultureInfo.InvariantCulture));

        Assert.That(rendered.Lines[0].Text, Is.EqualTo("      2023 May"));
        Assert.That(rendered.Lines[1].Text, Is.EqualTo("Mo Tu We Th Fr Sa Su"));
        Assert.That(Rendered.Describe(rendered), Is.EqualTo(Rendered.Describe(MonthRenderer.Render(2023, 5, MonthRenderOptions.Default))));
    }

    [Test]
    public void TheTitle_UsesTheMonthNamesOfTheCulture()
    {
        CultureInfo culture = TestCultures.WithMonthNames(month => "Month" + month.ToString(CultureInfo.InvariantCulture));

        Assert.That(TitleOf(MonthRenderer.Render(2023, 5, MonthRenderOptions.Default.WithCulture(culture))), Is.EqualTo("2023 Month5"));
        Assert.That(TitleOf(MonthRenderer.Render(2023, 12, MonthRenderOptions.Default.WithCulture(culture))), Is.EqualTo("2023 Month12"));
    }

    [Test]
    public void TheTitle_UsesTheYearMonthPatternOfTheCulture()
    {
        var culture = (CultureInfo)CultureInfo.InvariantCulture.Clone();
        culture.DateTimeFormat.YearMonthPattern = "MMM' of 'yyyy";

        Assert.That(TitleOf(MonthRenderer.Render(2023, 5, MonthRenderOptions.Default.WithCulture(culture))), Is.EqualTo("May of 2023"));
    }

    [Test]
    public void TheWeekdayLabels_UseTheDayNamesOfTheCulture()
    {
        MonthRenderOptions options = MonthRenderOptions.Default.WithCulture(TestCultures.WithForeignWeek());

        Assert.That(MonthRenderer.Render(2023, 5, options).Lines[1].Text, Is.EqualTo("lu ma me je ve sa di"));
        Assert.That(
            MonthRenderer.Render(2023, 5, options.WithWeekdayLabelStyle(WeekdayLabelStyle.Abbreviated).WithCellWidth(3)).Lines[1].Text,
            Is.EqualTo("lun mar mer jeu ven sam dim"));
    }

    // ---- what the culture does not decide ----

    /// <summary>The culture's week starts on Saturday. The rendered week starts where the options say.</summary>
    [Test]
    public void TheFirstDayOfWeek_IsTheOption_NotTheCulturesOwn()
    {
        CultureInfo saturdayFirst = TestCultures.WithForeignWeek();
        Assert.That(saturdayFirst.DateTimeFormat.FirstDayOfWeek, Is.EqualTo(DayOfWeek.Saturday));
        MonthRenderOptions options = MonthRenderOptions.Default.WithCulture(saturdayFirst);

        Assert.That(options.FirstDayOfWeek, Is.EqualTo(DayOfWeek.Monday), "setting a culture does not change the first day");
        Assert.That(MonthRenderer.Render(2023, 5, options).Layout.FirstDayOfWeek, Is.EqualTo(DayOfWeek.Monday));
        Assert.That(MonthRenderer.Render(2023, 5, options).Lines[2].Text, Is.EqualTo(" 1  2  3  4  5  6  7"));

        // A caller who wants the culture's first day says so.
        RenderedMonth asTheCulture = MonthRenderer.Render(2023, 5, options.WithFirstDayOfWeek(saturdayFirst.DateTimeFormat.FirstDayOfWeek));
        Assert.That(asTheCulture.Lines[1].Text, Is.EqualTo("sa di lu ma me je ve"));
        Assert.That(asTheCulture.Lines[2].Text, Is.EqualTo("       1  2  3  4  5"));
    }

    [Test]
    public void TwoCulturesWithDifferentFirstDays_GiveTheSameGrid()
    {
        CultureInfo american = Culture("en-US");
        CultureInfo german = Culture("de-DE");
        Assume.That(american.DateTimeFormat.FirstDayOfWeek, Is.Not.EqualTo(german.DateTimeFormat.FirstDayOfWeek));

        RenderedMonth inAmerican = MonthRenderer.Render(2023, 1, MonthRenderOptions.Default.WithCulture(american));
        RenderedMonth inGerman = MonthRenderer.Render(2023, 1, MonthRenderOptions.Default.WithCulture(german));

        Assert.That(LayoutPicture.Describe(inAmerican.Layout), Is.EqualTo(LayoutPicture.Describe(inGerman.Layout)));
        Assert.That(Rendered.Lines(inAmerican).Skip(2), Is.EqualTo(Rendered.Lines(inGerman).Skip(2)), "the weeks are identical");
    }

    [TestCase("")]
    [TestCase("en-US")]
    [TestCase("de-DE")]
    [TestCase("th-TH")]
    [TestCase("fa-IR")]
    [TestCase("ar-SA")]
    [TestCase("ja-JP")]
    public void TheDays_AreWrittenWithTheSameDigits_InEveryCulture(string name)
    {
        string[] weeks = Rendered.Lines(MonthRenderer.Render(2023, 5, MonthRenderOptions.Default.WithCulture(Culture(name)))).Skip(2).ToArray();

        Assert.That(weeks, Is.EqualTo(new[]
        {
            " 1  2  3  4  5  6  7",
            " 8  9 10 11 12 13 14",
            "15 16 17 18 19 20 21",
            "22 23 24 25 26 27 28",
            "29 30 31",
        }));
    }

    // ---- real cultures ----

    [TestCase("en-US")]
    [TestCase("de-DE")]
    [TestCase("fr-FR")]
    [TestCase("ja-JP")]
    public void ACultureWithTheGregorianCalendar_GivesItsOwnYearAndMonthTitle(string name)
    {
        CultureInfo culture = Culture(name);

        string title = TitleOf(MonthRenderer.Render(2023, 5, MonthRenderOptions.Default.WithCulture(culture)));

        Assert.That(title, Is.EqualTo(new DateTime(2023, 5, 1).ToString("Y", culture)));
        Assert.That(title, Ordinal.Contains("2023"));
    }

    [Test]
    public void AmericanEnglish_NamesTheMonthBeforeTheYear()
    {
        Assert.That(TitleOf(MonthRenderer.Render(2023, 5, MonthRenderOptions.Default.WithCulture(Culture("en-US")))), Is.EqualTo("May 2023"));
    }

    [TestCase("en-US")]
    [TestCase("de-DE")]
    [TestCase("fr-FR")]
    [TestCase("th-TH")]
    [TestCase("fa-IR")]
    public void TheWeekdayLabels_AreTheCulturesShortestDayNames_FittedToTheCell(string name)
    {
        CultureInfo culture = Culture(name);
        DateTimeFormatInfo format = GregorianFormat(culture);
        // Wide enough for every name, so that the names are compared whole.
        const int Width = 12;

        string[] labels = LabelsOf(MonthRenderer.Render(2023, 5, MonthRenderOptions.Default.WithCulture(culture).WithCellWidth(Width)));

        DayOfWeek[] columns = { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday };
        Assert.That(labels.Select(label => label.TrimStart(' ')), Is.EqualTo(columns.Select(format.GetShortestDayName)));
        Assert.That(labels.Select(label => new StringInfo(label).LengthInTextElements), Is.All.EqualTo(Width));
    }

    [TestCase("en-US")]
    [TestCase("de-DE")]
    [TestCase("fr-FR")]
    public void TheAbbreviatedStyle_GivesTheCulturesAbbreviatedDayNames(string name)
    {
        CultureInfo culture = Culture(name);
        const int Width = 12;

        string[] labels = LabelsOf(MonthRenderer.Render(2023, 5,
            MonthRenderOptions.Default.WithCulture(culture).WithWeekdayLabelStyle(WeekdayLabelStyle.Abbreviated).WithCellWidth(Width)));

        Assert.That(labels[0].TrimStart(' '), Is.EqualTo(culture.DateTimeFormat.GetAbbreviatedDayName(DayOfWeek.Monday)));
        Assert.That(labels[6].TrimStart(' '), Is.EqualTo(culture.DateTimeFormat.GetAbbreviatedDayName(DayOfWeek.Sunday)));
    }

    // ---- cultures whose own calendar is not the Gregorian one ----

    /// <summary>
    /// The Thai Buddhist calendar counts the same months but numbers the year 543 higher. The grid shows May
    /// 2023, so the title says 2023, not 2566.
    /// </summary>
    [Test]
    public void Thai_TheTitleNamesTheGregorianYear()
    {
        CultureInfo thai = Culture("th-TH");
        Assume.That(thai.DateTimeFormat.Calendar, Is.InstanceOf<ThaiBuddhistCalendar>());

        string title = TitleOf(MonthRenderer.Render(2023, 5, MonthRenderOptions.Default.WithCulture(thai)));

        Assert.That(title, Ordinal.Contains("2023"));
        Assert.That(title, Ordinal.DoesNotContain("2566"));
        Assert.That(title, Is.EqualTo(new DateTime(2023, 5, 1).ToString("Y", GregorianFormat(thai))));
        Assert.That(title, Is.Not.EqualTo(new DateTime(2023, 5, 1).ToString("Y", thai)), "the culture's own calendar would say 2566");
    }

    /// <summary>
    /// In the Persian calendar 1 May 2023 is in Ordibehesht 1402. The grid shows the Gregorian month, so the
    /// title names May 2023 in Persian, not a Persian month.
    /// </summary>
    [Test]
    public void Persian_TheTitleNamesTheGregorianMonthAndYear()
    {
        CultureInfo persian = Culture("fa-IR");
        Assume.That(persian.DateTimeFormat.Calendar, Is.InstanceOf<PersianCalendar>());
        DateTimeFormatInfo gregorian = GregorianFormat(persian);

        string title = TitleOf(MonthRenderer.Render(2023, 5, MonthRenderOptions.Default.WithCulture(persian)));

        Assert.That(title, Ordinal.Contains("2023"));
        Assert.That(title, Ordinal.DoesNotContain("1402"));
        Assert.That(title, Ordinal.Contains(gregorian.GetMonthName(5)), "the Persian name of the Gregorian month May");
        Assert.That(title, Is.EqualTo(new DateTime(2023, 5, 1).ToString("Y", gregorian)));
        Assert.That(title, Is.Not.EqualTo(new DateTime(2023, 5, 1).ToString("Y", persian)), "the culture's own calendar names another month");
    }

    /// <summary>Every month of a year has its own title, and the twelve Gregorian month names appear in order.</summary>
    [TestCase("th-TH")]
    [TestCase("fa-IR")]
    public void ANonGregorianCulture_TitlesEveryMonthWithItsGregorianName(string name)
    {
        CultureInfo culture = Culture(name);
        DateTimeFormatInfo gregorian = GregorianFormat(culture);
        MonthRenderOptions options = MonthRenderOptions.Default.WithCulture(culture).WithTitleFormat("MMMM");

        string[] titles = Enumerable.Range(1, 12).Select(month => TitleOf(MonthRenderer.Render(2023, month, options))).ToArray();

        Assert.That(titles, Is.EqualTo(Enumerable.Range(1, 12).Select(gregorian.GetMonthName)));
        Assert.That(titles.Distinct().Count(), Is.EqualTo(12));
    }

    /// <summary>
    /// Every culture the runtime knows renders, and for every one the title names the Gregorian year and the
    /// cells keep their width.
    /// </summary>
    [Test]
    public void EveryCultureOfTheRuntime_Renders_WithAGregorianYear_AndCellsOfTheRightWidth()
    {
        var problems = new List<string>();
        int rendered = 0;
        int nonGregorian = 0;

        foreach (CultureInfo culture in CultureInfo.GetCultures(CultureTypes.AllCultures))
        {
            MonthRenderOptions options = MonthRenderOptions.Default.WithCulture(culture).WithTitleFormat("MMMM yyyy");
            RenderedMonth month = MonthRenderer.Render(2023, 5, options);
            rendered++;
            if (culture.DateTimeFormat.Calendar is not GregorianCalendar)
            {
                nonGregorian++;
            }
            if (!TitleOf(month).Contains("2023", StringComparison.Ordinal))
            {
                problems.Add(culture.Name + ": the title is " + TitleOf(month));
            }
            if (LabelsOf(month).Any(label => new StringInfo(label).LengthInTextElements != 2))
            {
                problems.Add(culture.Name + ": a label is not two text elements wide");
            }
            if (month.Layout.FirstDayOfWeek != DayOfWeek.Monday || month.Lines[2].Text != " 1  2  3  4  5  6  7")
            {
                problems.Add(culture.Name + ": the grid changed");
            }
        }

        Assert.That(problems, Is.Empty);
        Assert.That(rendered, Is.GreaterThan(0));
        TestContext.Out.WriteLine($"{rendered} cultures rendered, {nonGregorian} of them with a calendar other than the Gregorian one.");
    }

    // ---- the options hold their own copy of the culture ----

    [Test]
    public void ChangingTheCultureObjectAfterwards_DoesNotChangeTheOptions()
    {
        var culture = (CultureInfo)CultureInfo.InvariantCulture.Clone();
        MonthRenderOptions options = MonthRenderOptions.Default.WithCulture(culture);
        string before = Rendered.Describe(MonthRenderer.Render(2023, 5, options));

        culture.DateTimeFormat.MonthNames = new[] { "A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K", "L", string.Empty };
        culture.DateTimeFormat.ShortestDayNames = new[] { "1", "2", "3", "4", "5", "6", "7" };
        culture.DateTimeFormat.YearMonthPattern = "MMMM";

        Assert.That(Rendered.Describe(MonthRenderer.Render(2023, 5, options)), Is.EqualTo(before));
        Assert.That(options.Culture, Is.Not.SameAs(culture));
        Assert.That(options.Culture.IsReadOnly, Is.True);
        Assert.That(options.Culture.DateTimeFormat.IsReadOnly, Is.True);
    }

    [Test]
    public void AReadOnlyCulture_IsKeptAsItIs()
    {
        CultureInfo culture = Culture("de-DE");
        Assume.That(culture.IsReadOnly, Is.True);

        Assert.That(MonthRenderOptions.Default.WithCulture(culture).Culture, Is.SameAs(culture));
    }

    // ---- the process's cultures ----

    [Test]
    public void TheCurrentCultures_DoNotAffectRendering()
    {
        var ambient = new List<CultureInfo> { TestCultures.WithForeignWeek(), TestCultures.WithMonthNames(_ => "Ambient") };
        foreach (string name in new[] { "fa-IR", "th-TH", "de-DE", "ja-JP" })
        {
            if (TestCultures.TryGet(name) is CultureInfo culture)
            {
                ambient.Add(culture);
            }
        }
        var optionSets = new List<MonthRenderOptions>
        {
            MonthRenderOptions.Default,
            MonthRenderOptions.Default.WithCulture(TestCultures.WithMonthNames(_ => "Explicit")).WithTitleFormat("MMMM yyyy"),
        };
        if (TestCultures.TryGet("en-US") is CultureInfo american)
        {
            optionSets.Add(MonthRenderOptions.Default.WithCulture(american).WithWeekdayLabelStyle(WeekdayLabelStyle.Abbreviated).WithCellWidth(3));
        }

        foreach (MonthRenderOptions options in optionSets)
        {
            string expected = ProcessState.UnderCultures(CultureInfo.InvariantCulture, CultureInfo.InvariantCulture,
                () => Rendered.Describe(MonthRenderer.Render(2023, 5, options)));

            foreach (CultureInfo culture in ambient)
            {
                Assert.That(ProcessState.UnderCultures(culture, culture, () => Rendered.Describe(MonthRenderer.Render(2023, 5, options))),
                    Is.EqualTo(expected), "CurrentCulture and CurrentUICulture " + culture.Name);
                Assert.That(ProcessState.UnderCultures(culture, CultureInfo.InvariantCulture, () => Rendered.Describe(MonthRenderer.Render(2023, 5, options))),
                    Is.EqualTo(expected), "CurrentCulture " + culture.Name);
                Assert.That(ProcessState.UnderCultures(CultureInfo.InvariantCulture, culture, () => Rendered.Describe(MonthRenderer.Render(2023, 5, options))),
                    Is.EqualTo(expected), "CurrentUICulture " + culture.Name);
            }
        }
    }

    /// <summary>Options created while another culture is current do not remember it.</summary>
    [Test]
    public void OptionsCreatedUnderAnotherCurrentCulture_AreTheSameOptions()
    {
        CultureInfo ambient = TestCultures.WithMonthNames(_ => "Ambient");

        MonthRenderOptions created = ProcessState.UnderCultures(ambient, ambient,
            () => MonthRenderOptions.Default.WithCellWidth(3).WithTitleFormat("MMMM yyyy"));

        Assert.That(created.Culture, Is.SameAs(CultureInfo.InvariantCulture));
        Assert.That(MonthRenderer.Render(2023, 5, created).Lines[0].Text, Does.EndWith("May 2023").Using(StringComparison.Ordinal));
    }

    // ---- a culture that would break a line ----

    [Test]
    public void ACultureWhoseMonthNameContainsALineBreak_IsRejectedWhenItIsRendered()
    {
        MonthRenderOptions options = MonthRenderOptions.Default.WithCulture(TestCultures.WithMonthNames(_ => "Two\nLines"));

        ArgumentException exception = Assert.Throws<ArgumentException>(() => MonthRenderer.Render(2023, 5, options))!;

        Assert.That(exception.ParamName, Is.EqualTo("options"));
    }

    [Test]
    public void ACultureWhoseDayNameContainsALineBreak_IsRejectedWhenItIsRendered()
    {
        var culture = (CultureInfo)CultureInfo.InvariantCulture.Clone();
        culture.DateTimeFormat.ShortestDayNames = new[] { "Su", "M\r", "Tu", "We", "Th", "Fr", "Sa" };
        MonthRenderOptions options = MonthRenderOptions.Default.WithCulture(culture);

        ArgumentException exception = Assert.Throws<ArgumentException>(() => MonthRenderer.Render(2023, 5, options))!;

        Assert.That(exception.ParamName, Is.EqualTo("options"));
        // Explicit labels replace the culture's, so the same culture renders with them.
        Assert.That(() => MonthRenderer.Render(2023, 5, options.WithWeekdayLabels(new[] { "S", "M", "T", "W", "T", "F", "S" })), Throws.Nothing);
    }
}
