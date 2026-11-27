namespace ConsoleCalendarGenerator.Tests.Parity.Campertron;

/// <summary>
/// The spans. A consumer's adapter over the package's segments must produce the recorded spans: the same text in
/// the same order, cut in the same places, with color on the same spans.
/// </summary>
[TestFixture]
public class SpanParityTests
{
    private static IEnumerable<(string Text, ConsoleColor? Color)> Pairs(StyledSpanLine line) => line.Spans.Select(span => (span.Text, span.Color));

    [TestCaseSource(typeof(ParityFixtures), nameof(ParityFixtures.CasesWithAnExactTitle))]
    public void EverySpan_IsExactlyTheRecordedSpan(FixtureCase fixture)
    {
        ParityProfile.AssumeTheCultureDataIsTheRecordedOne(fixture);

        IReadOnlyList<StyledSpanLine> adapted = ParityProfile.StyledLines(fixture);

        Assert.That(adapted, Has.Count.EqualTo(fixture.Lines.Count));
        for (int index = 0; index < adapted.Count; index++)
        {
            Assert.That(Pairs(adapted[index]), Is.EqualTo(fixture.Lines[index].ColoredSpans()), "line " + index);
        }
    }

    [TestCaseSource(typeof(ParityFixtures), nameof(ParityFixtures.CasesWithAnIntentionallyDifferentTitle))]
    public void BelowTheTitle_EverySpanIsExactlyTheRecordedSpan_AlsoWhereTheTitleDiffers(FixtureCase fixture)
    {
        IReadOnlyList<StyledSpanLine> adapted = ParityProfile.StyledLines(fixture);

        Assert.That(adapted, Has.Count.EqualTo(fixture.Lines.Count));
        Assert.That(adapted[0].Spans, Has.Count.EqualTo(fixture.Lines[0].Spans.Count), "the title is one uncolored span on both sides");
        for (int index = 1; index < adapted.Count; index++)
        {
            Assert.That(Pairs(adapted[index]), Is.EqualTo(fixture.Lines[index].ColoredSpans()), "line " + index);
        }
    }

    [TestCaseSource(typeof(ParityFixtures), nameof(ParityFixtures.AllCases))]
    public void TheSpansOfEveryAdaptedLine_JoinToTheLine(FixtureCase fixture)
    {
        RenderedMonth rendered = ParityProfile.Render(fixture);

        IReadOnlyList<StyledSpanLine> adapted = SegmentAdapter.ToStyledLines(rendered, ParityProfile.Theme(fixture.Input));

        Assert.That(adapted.Select(line => line.Text), Is.EqualTo(rendered.Lines.Select(line => line.Text)));
    }

    /// <summary>
    /// The colored spans are the marked days and nothing else: each is a whole cell, padding included, in the
    /// mark color, and each carries its date and the state's name from the segment it came from.
    /// </summary>
    [TestCaseSource(typeof(ParityFixtures), nameof(ParityFixtures.AllCases))]
    public void TheColoredSpans_AreTheMarkedDays_AsWholeCells_WithTheirDateAndState(FixtureCase fixture)
    {
        FixtureInput input = fixture.Input;
        DateTime[] markedDays = input.DateTimes().Where(date => date.Year == input.Year && date.Month == input.Month)
            .Select(date => date.Date).Distinct().OrderBy(date => date).Select(date => DateTime.SpecifyKind(date, DateTimeKind.Unspecified)).ToArray();

        StyledSpan[] colored = ParityProfile.StyledLines(fixture).SelectMany(line => line.Spans).Where(span => span.Color.HasValue).ToArray();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(colored.Select(span => span.Date), Is.EqualTo(markedDays.Cast<DateTime?>()), "the colored spans are the marked days, in order");
            Assert.That(colored.Select(span => span.Color), Is.All.EqualTo(input.MarkColor));
            Assert.That(colored.Select(span => span.StateName), Is.All.EqualTo(ParityProfile.StateName));
            Assert.That(colored.Select(span => span.Kind), Is.All.EqualTo(RenderedSegmentKind.Day));
            Assert.That(colored.Select(span => span.Text.Length), Is.All.EqualTo(ParityFixtures.Manifest.FixedByTheSource.CellWidth), "the padding is inside the colored span");
            Assert.That(colored.Select(span => span.Text.TrimStart(' ')), Is.EqualTo(markedDays.Select(date => "*" + date.Day)));
        }
    }

    /// <summary>
    /// Everything that is not a marked day has no color, and the unmarked days still say which day they are, so
    /// a consumer needs no text to tell them apart.
    /// </summary>
    [TestCaseSource(typeof(ParityFixtures), nameof(ParityFixtures.AllCases))]
    public void TheUncoloredDaySpans_AreTheUnmarkedDays(FixtureCase fixture)
    {
        FixtureInput input = fixture.Input;
        var markedDays = new HashSet<int>(input.DateTimes().Where(date => date.Year == input.Year && date.Month == input.Month).Select(date => date.Day));

        StyledSpan[] days = ParityProfile.StyledLines(fixture).SelectMany(line => line.Spans).Where(span => span.Kind == RenderedSegmentKind.Day).ToArray();

        Assert.That(days.Select(span => span.Date!.Value.Day), Is.EqualTo(Enumerable.Range(1, DateTime.DaysInMonth(input.Year, input.Month))));
        foreach (StyledSpan day in days)
        {
            bool marked = markedDays.Contains(day.Date!.Value.Day);
            Assert.That(day.Color.HasValue, Is.EqualTo(marked), "day " + day.Date.Value.Day);
            Assert.That(day.StateName, Is.EqualTo(marked ? ParityProfile.StateName : null), "day " + day.Date.Value.Day);
        }
    }

    /// <summary>What is on the screen: runs of one color. This is the same whatever the spans are cut into.</summary>
    [TestCaseSource(typeof(ParityFixtures), nameof(ParityFixtures.CasesWithAnExactTitle))]
    public void TheRunsOfColor_AreTheRecordedRuns(FixtureCase fixture)
    {
        ParityProfile.AssumeTheCultureDataIsTheRecordedOne(fixture);

        IReadOnlyList<StyledSpanLine> adapted = ParityProfile.StyledLines(fixture);

        for (int index = 0; index < adapted.Count; index++)
        {
            Assert.That(Runs(Pairs(adapted[index])), Is.EqualTo(Runs(fixture.Lines[index].ColoredSpans())), "line " + index);
        }
    }

    private static List<(string Text, ConsoleColor? Color)> Runs(IEnumerable<(string Text, ConsoleColor? Color)> spans)
    {
        var runs = new List<(string Text, ConsoleColor? Color)>();
        foreach ((string text, ConsoleColor? color) in spans.Where(span => span.Text.Length > 0))
        {
            if (runs.Count > 0 && runs[^1].Color == color)
            {
                runs[^1] = (runs[^1].Text + text, color);
            }
            else
            {
                runs.Add((text, color));
            }
        }
        return runs;
    }
}
