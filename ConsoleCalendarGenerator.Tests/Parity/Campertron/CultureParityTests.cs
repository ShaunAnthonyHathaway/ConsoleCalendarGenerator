using System.Globalization;
using ConsoleCalendarGenerator.Tests.Support;

namespace ConsoleCalendarGenerator.Tests.Parity.Campertron;

/// <summary>
/// The culture decides the title and nothing else, on both sides. Under the invariant culture and under cultures
/// whose calendar is Gregorian the titles are the same. Under a culture with another calendar they differ, and
/// are meant to: the recorded renderer titles the month in that calendar, the package titles the Gregorian month
/// it draws.
/// </summary>
[TestFixture]
public class CultureParityTests
{
    private static IEnumerable<TestCaseData> CultureCases(string title) => ParityFixtures.Cases
        .Where(fixture => fixture.File == "cultures.json" && fixture.Title == title)
        .Select(fixture => new TestCaseData(fixture).SetArgDisplayNames(fixture.ToString()));

    private static IEnumerable<TestCaseData> GregorianCases() => CultureCases(FixtureCase.ExactTitle);

    private static IEnumerable<TestCaseData> NonGregorianCases() => CultureCases(FixtureCase.IntentionallyDifferentTitle);

    private static string TitleOf(RenderedMonth rendered) => rendered.Lines[0].Segments.Single(segment => segment.Kind == RenderedSegmentKind.Title).Text;

    [Test]
    public void TheInvariantCulture_GivesTheRecordedTitle_OnEverySystem()
    {
        FixtureCase invariant = ParityFixtures.Cases.Single(fixture => fixture.File == "cultures.json" && fixture.Id == "invariant");

        Assert.That(ParityProfile.Render(invariant).Lines[0].Text, Is.EqualTo("  2023 May (* = open)"));
        Assert.That(invariant.Lines[0].Text, Is.EqualTo("  2023 May (* = open)"));
    }

    [TestCaseSource(nameof(GregorianCases))]
    public void UnderAGregorianCulture_TheTitleIsTheRecordedTitle(FixtureCase fixture)
    {
        ParityProfile.AssumeTheCultureDataIsTheRecordedOne(fixture);

        Assert.That(ParityProfile.Render(fixture).Lines[0].Text, Is.EqualTo(fixture.Lines[0].Text));
    }

    /// <summary>Whatever this system's culture data says, both sides format the month the same way.</summary>
    [TestCaseSource(nameof(GregorianCases))]
    public void UnderAGregorianCulture_TheTitleIsTheCulturesYearAndMonthPattern(FixtureCase fixture)
    {
        CultureInfo culture = ParityProfile.CultureOf(fixture);
        Assume.That(culture.DateTimeFormat.Calendar, Is.InstanceOf<GregorianCalendar>());

        string title = TitleOf(ParityProfile.Render(fixture));

        Assert.That(title, Is.EqualTo(new DateTime(fixture.Input.Year, fixture.Input.Month, 1).ToString("Y", culture)), "what the recorded renderer computes");
    }

    [TestCaseSource(nameof(NonGregorianCases))]
    public void UnderANonGregorianCulture_TheTitlesDiffer_OnPurpose(FixtureCase fixture)
    {
        CultureInfo culture = ParityProfile.CultureOf(fixture);
        Assume.That(culture.DateTimeFormat.Calendar, Is.Not.InstanceOf<GregorianCalendar>(), "On this system the culture's calendar is Gregorian.");
        string gregorianYear = fixture.Input.Year.ToString(CultureInfo.InvariantCulture);

        string packageTitle = ParityProfile.Render(fixture).Lines[0].Text;
        string recordedTitle = fixture.Lines[0].Text;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(packageTitle, Is.Not.EqualTo(recordedTitle));
            Assert.That(packageTitle, Ordinal.Contains(gregorianYear), "the package names the Gregorian year of the month it draws");
            Assert.That(recordedTitle, Ordinal.DoesNotContain(gregorianYear), "the recorded title names the year of the culture's own calendar");
            Assert.That(packageTitle, Does.EndWith(" (* = open)").And.StartsWith(fixture.Input.Indent).Using(StringComparison.Ordinal));
        }
    }

    /// <summary>The grid, the markers and the spans are the recorded ones whatever the culture's calendar is.</summary>
    [TestCaseSource(nameof(NonGregorianCases))]
    public void UnderANonGregorianCulture_EverythingBelowTheTitleIsStillTheSame(FixtureCase fixture)
    {
        IReadOnlyList<StyledSpanLine> adapted = ParityProfile.StyledLines(fixture);

        for (int index = 1; index < fixture.Lines.Count; index++)
        {
            Assert.That(adapted[index].Spans.Select(span => (span.Text, span.Color)), Is.EqualTo(fixture.Lines[index].ColoredSpans()), "line " + index);
        }
    }

    /// <summary>The weeks do not depend on the culture at all: every culture case of the same month has the same rows.</summary>
    [Test]
    public void EveryCultureCaseOfTheSameMonth_HasTheSameRows()
    {
        var mayCases = ParityFixtures.Cases.Where(fixture => fixture.File == "cultures.json" && fixture.Input.Year == 2023 && fixture.Input.Month == 5).ToList();
        Assert.That(mayCases.Select(fixture => fixture.Input.Culture).Distinct().Count(), Is.GreaterThanOrEqualTo(6));

        string[] rows = mayCases[0].Lines.Skip(1).Select(line => line.Text).ToArray();

        foreach (FixtureCase fixture in mayCases)
        {
            Assert.That(fixture.Lines.Skip(1).Select(line => line.Text), Is.EqualTo(rows), "recorded: " + fixture.Input.Culture);
            if (TestCultures.TryGet(fixture.Input.Culture) != null)
            {
                Assert.That(Rendered.Lines(ParityProfile.Render(fixture)).Skip(1), Is.EqualTo(rows), "package: " + fixture.Input.Culture);
            }
        }
    }

    /// <summary>The culture of the process plays no part: the profile passes the culture explicitly.</summary>
    [Test]
    public void TheCurrentCultures_DoNotAffectTheParityOutput()
    {
        FixtureCase fixture = ParityFixtures.Cases.Single(c => c.File == "marked-dates.json" && c.Id == "several");
        string expected = string.Join("\n", fixture.Lines.Select(line => line.Text));

        foreach (CultureInfo ambient in new[] { TestCultures.WithForeignWeek(), TestCultures.WithMonthNames(_ => "Ambient") })
        {
            Assert.That(ProcessState.UnderCultures(ambient, ambient, () => ParityProfile.Render(fixture).ToText()), Is.EqualTo(expected));
        }
    }
}
