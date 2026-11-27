using System.Globalization;
using ConsoleCalendarGenerator.Tests.Support;

namespace ConsoleCalendarGenerator.Tests.Parity.Campertron;

/// <summary>
/// The package's side of a fixture: the options, the state and the theme that reproduce the recorded renderer,
/// built from the fixture's manifest and from one case with public API only. Nothing here exists in the package,
/// which has no profile for any consumer; a consumer makes these choices itself.
/// </summary>
internal static class ParityProfile
{
    /// <summary>The name is the consumer's choice. The package gives no meaning to it.</summary>
    public const string StateName = "open";

    /// <summary>The labels of the recorded weekday row, indexed by day of the week, Sunday first.</summary>
    private static readonly string[] WeekdayLabels = { "Su", "Mo", "Tu", "We", "Th", "Fr", "Sa" };

    public static DayState State() => new(StateName, ParityFixtures.Manifest.FixedByTheSource.Marker);

    public static MonthRenderOptions Options(FixtureInput input, CultureInfo culture)
    {
        ManifestFixed source = ParityFixtures.Manifest.FixedByTheSource;
        return MonthRenderOptions.Default
            .WithCulture(culture)
            .WithFirstDayOfWeek(source.FirstDayOfWeek)
            .WithWeekdayLabels(WeekdayLabels)
            .WithCellWidth(source.CellWidth)
            .WithColumnGap(source.ColumnGap)
            .WithIndent(input.Indent)
            .WithTitleFormat(source.TitleFormat)
            .WithTitleSuffix(source.TitleSuffix)
            .WithTitleAlignment(Enum.Parse<TitleAlignment>(source.TitleAlignment));
    }

    public static CalendarTheme Theme(FixtureInput input) => CalendarTheme.None.WithStateColor(StateName, input.MarkColor);

    /// <summary>The culture of a case, or an inconclusive test where this system does not have it.</summary>
    public static CultureInfo CultureOf(FixtureCase fixture)
    {
        CultureInfo? culture = TestCultures.TryGet(fixture.Input.Culture);
        Assume.That(culture, Is.Not.Null, "The culture " + fixture.Input.Culture + " is not available on this system.");
        return culture!;
    }

    /// <summary>The month of a case rendered by the package with the profile.</summary>
    public static RenderedMonth Render(FixtureCase fixture) =>
        MonthRenderer.Render(fixture.Input.Year, fixture.Input.Month, Options(fixture.Input, CultureOf(fixture)), fixture.Input.DateTimes(), State());

    /// <summary>The same, as the styled lines a consumer would build from it.</summary>
    public static IReadOnlyList<StyledSpanLine> StyledLines(FixtureCase fixture) =>
        SegmentAdapter.ToStyledLines(Render(fixture), Theme(fixture.Input));

    /// <summary>
    /// The title of a real culture comes from the operating system's culture data. Where this system formats the
    /// month differently than the system the fixture was recorded on, a comparison of titles says nothing about
    /// the package, and the test is inconclusive. The invariant culture is the same everywhere.
    /// </summary>
    public static void AssumeTheCultureDataIsTheRecordedOne(FixtureCase fixture)
    {
        if (fixture.Input.Culture.Length == 0)
        {
            return;
        }
        CultureInfo culture = CultureOf(fixture);
        string recorded = fixture.Lines[0].Text;
        string here = fixture.Input.Indent
            + new DateTime(fixture.Input.Year, fixture.Input.Month, 1).ToString(ParityFixtures.Manifest.FixedByTheSource.TitleFormat, culture)
            + ParityFixtures.Manifest.FixedByTheSource.TitleSuffix;
        Assume.That(here, Is.EqualTo(recorded),
            "This system's culture data for " + culture.Name + " formats the month differently than the system the fixture was recorded on.");
    }
}
