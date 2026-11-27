using ConsoleCalendarGenerator.Tests.Support;

namespace ConsoleCalendarGenerator.Tests.Parity.Campertron;

/// <summary>
/// A marked day is recognizable without color. The marker is in the text, the state is on the segment, and the
/// title says what the marker means; color only adds to that.
/// </summary>
[TestFixture]
public class MarkerAccessibilityTests
{
    private static IEnumerable<TestCaseData> CasesWithMarkedDays() => ParityFixtures.Cases
        .Where(fixture => fixture.Lines.SelectMany(line => line.ColoredSpans()).Any(span => span.Color.HasValue))
        .Select(fixture => new TestCaseData(fixture).SetArgDisplayNames(fixture.ToString()));

    [TestCaseSource(nameof(CasesWithMarkedDays))]
    public void EveryMarkedDay_ShowsItsMarkerInThePlainText(FixtureCase fixture)
    {
        RenderedMonth rendered = ParityProfile.Render(fixture);

        RenderedSegment[] marked = rendered.Lines.SelectMany(line => line.Segments).Where(segment => segment.State != null).ToArray();

        Assert.That(marked, Is.Not.Empty);
        foreach (RenderedSegment segment in marked)
        {
            Assert.That(segment.Text.TrimStart(' '), Is.EqualTo("*" + segment.Date!.Value.Day), "the marker is directly before the number");
            Assert.That(segment.State!.Name, Is.EqualTo(ParityProfile.StateName), "and the state can be read from the segment");
        }
        Assert.That(rendered.ToText().Count(character => character == '*'), Is.EqualTo(marked.Length + 1), "one marker per marked day, and one in the legend");
    }

    /// <summary>With no colors at all, the text is the same and still tells the marked days apart.</summary>
    [TestCaseSource(nameof(CasesWithMarkedDays))]
    public void WithoutColors_TheTextIsUnchanged_AndNoSpanHasAColor(FixtureCase fixture)
    {
        RenderedMonth rendered = ParityProfile.Render(fixture);

        IReadOnlyList<StyledSpanLine> withColors = SegmentAdapter.ToStyledLines(rendered, ParityProfile.Theme(fixture.Input));
        IReadOnlyList<StyledSpanLine> withoutColors = SegmentAdapter.ToStyledLines(rendered, CalendarTheme.None);

        Assert.That(withoutColors.Select(line => line.Text), Is.EqualTo(withColors.Select(line => line.Text)));
        Assert.That(withoutColors.SelectMany(line => line.Spans).Select(span => span.Color), Is.All.Null);
        Assert.That(withoutColors.SelectMany(line => line.Spans).Count(span => span.StateName != null),
            Is.EqualTo(withColors.SelectMany(line => line.Spans).Count(span => span.Color.HasValue)), "the state is still there to read");
    }

    /// <summary>The same through the console writer with no theme: plain text with the markers in it.</summary>
    [TestCaseSource(nameof(CasesWithMarkedDays))]
    public void WrittenToTheConsoleWithoutColors_TheRecordedTextArrives_MarkersIncluded(FixtureCase fixture)
    {
        ParityProfile.AssumeTheCultureDataIsTheRecordedOne(fixture);
        RenderedMonth rendered = ParityProfile.Render(fixture);

        Captured captured = ConsoleCapture.Run(() => ConsoleMonthWriter.Write(rendered, CalendarTheme.None));

        // Where the title is deliberately different, the lines below it are compared.
        int skipped = fixture.Title == FixtureCase.ExactTitle ? 0 : 1;
        Assert.That(captured.Text.Split('\n').Skip(skipped), Is.EqualTo(fixture.Lines.Skip(skipped).Select(line => line.Text).Append(string.Empty)));
    }

    [TestCaseSource(typeof(ParityFixtures), nameof(ParityFixtures.AllCases))]
    public void TheTitle_ExplainsTheMarker(FixtureCase fixture)
    {
        RenderedLine title = ParityProfile.Render(fixture).Lines[0];

        RenderedSegment suffix = title.Segments.Single(segment => segment.Kind == RenderedSegmentKind.TitleSuffix);

        Assert.That(suffix.Text, Is.EqualTo(" (* = open)"), "the wording is the caller's, passed as the title suffix");
        Assert.That(title.Text, Does.EndWith(suffix.Text).Using(StringComparison.Ordinal));
    }

    /// <summary>Color is given to exactly the days that already carry a marker, never to anything else.</summary>
    [TestCaseSource(typeof(ParityFixtures), nameof(ParityFixtures.AllCases))]
    public void ColorIsOnlyEverAddedToASpanThatAlsoHasAMarker(FixtureCase fixture)
    {
        foreach (StyledSpan span in ParityProfile.StyledLines(fixture).SelectMany(line => line.Spans))
        {
            Assert.That(span.Color.HasValue, Is.EqualTo(span.Text.Contains('*') && span.Kind == RenderedSegmentKind.Day), "<" + span.Text + ">");
        }
    }
}
