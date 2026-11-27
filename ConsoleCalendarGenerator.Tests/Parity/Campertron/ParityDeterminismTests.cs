using System.Collections.Concurrent;
using ConsoleCalendarGenerator.Tests.Support;

namespace ConsoleCalendarGenerator.Tests.Parity.Campertron;

/// <summary>
/// The parity output is stable: the same case gives the same spans every time and on every thread, and the order
/// of the dates makes no difference where there is only one state.
/// </summary>
[TestFixture]
public class ParityDeterminismTests
{
    private static string Describe(IReadOnlyList<StyledSpanLine> lines) => string.Join("\n", lines.Select(line =>
        string.Join("|", line.Spans.Select(span => span.Text + "/" + span.Color + "/" + span.Kind + "/" + span.Date?.Day + "/" + span.StateName))));

    private static IReadOnlyList<FixtureCase> AvailableCases() =>
        ParityFixtures.Cases.Where(fixture => TestCultures.TryGet(fixture.Input.Culture) != null).ToList();

    [Test]
    public void TheSameCase_GivesTheSameSpansEveryTime()
    {
        foreach (FixtureCase fixture in AvailableCases())
        {
            string first = Describe(ParityProfile.StyledLines(fixture));

            Assert.That(Describe(ParityProfile.StyledLines(fixture)), Is.EqualTo(first), fixture.ToString());
        }
    }

    [Test]
    public void CasesRenderedInParallel_GiveTheSameSpansAsOneAtATime()
    {
        IReadOnlyList<FixtureCase> cases = AvailableCases();
        string[] expected = cases.Select(fixture => Describe(ParityProfile.StyledLines(fixture))).ToArray();
        var wrong = new ConcurrentBag<string>();

        Parallel.For(0, cases.Count * 20, new ParallelOptions { MaxDegreeOfParallelism = 8 }, index =>
        {
            FixtureCase fixture = cases[index % cases.Count];
            RenderedMonth rendered = MonthRenderer.Render(fixture.Input.Year, fixture.Input.Month,
                ParityProfile.Options(fixture.Input, TestCultures.TryGet(fixture.Input.Culture)!), fixture.Input.DateTimes(), ParityProfile.State());
            if (Describe(SegmentAdapter.ToStyledLines(rendered, ParityProfile.Theme(fixture.Input))) != expected[index % cases.Count])
            {
                wrong.Add(fixture.ToString());
            }
        });

        Assert.That(wrong, Is.Empty);
    }

    /// <summary>With one state, the first entry for a date and any later one are the same, so order cannot matter.</summary>
    [TestCaseSource(typeof(ParityFixtures), nameof(ParityFixtures.AllCases))]
    public void TheOrderOfTheDates_MakesNoDifference(FixtureCase fixture)
    {
        FixtureInput input = fixture.Input;
        MonthRenderOptions options = ParityProfile.Options(input, ParityProfile.CultureOf(fixture));
        DayState state = ParityProfile.State();
        IReadOnlyList<DateTime> dates = input.DateTimes();

        string asGiven = Rendered.Describe(MonthRenderer.Render(input.Year, input.Month, options, dates, state));

        Assert.That(Rendered.Describe(MonthRenderer.Render(input.Year, input.Month, options, dates.Reverse(), state)), Is.EqualTo(asGiven));
        Assert.That(Rendered.Describe(MonthRenderer.Render(input.Year, input.Month, options, dates.OrderBy(date => date.Ticks), state)), Is.EqualTo(asGiven));
        Assert.That(Rendered.Describe(MonthRenderer.Render(input.Year, input.Month, options, dates.Concat(dates), state)), Is.EqualTo(asGiven), "given twice");
    }

    /// <summary>
    /// Where two states compete for a date, the package's rule decides: the first entry wins. The recorded
    /// renderer has one state only, so this is the package's own behavior, shown here for completeness.
    /// </summary>
    [Test]
    public void WithTwoStates_TheFirstEntryForADateWins_AsThePackageDocuments()
    {
        FixtureCase fixture = ParityFixtures.Cases.Single(c => c.File == "marked-dates.json" && c.Id == "two-digit-day");
        MonthRenderOptions options = ParityProfile.Options(fixture.Input, ParityProfile.CultureOf(fixture));
        var open = ParityProfile.State();
        var held = new DayState("held", "?");
        var date = new DateTime(2023, 5, 17);

        RenderedMonth openFirst = MonthRenderer.Render(2023, 5, options, new[] { KeyValuePair.Create(date, open), KeyValuePair.Create(date, held) });
        RenderedMonth heldFirst = MonthRenderer.Render(2023, 5, options, new[] { KeyValuePair.Create(date, held), KeyValuePair.Create(date, open) });

        Assert.That(Rendered.Lines(openFirst), Is.EqualTo(fixture.Lines.Select(line => line.Text)), "with the open state first, the output is the recorded one");
        Assert.That(Rendered.Lines(heldFirst)[4], Is.EqualTo("   15  16 ?17  18  19  20  21"));
    }

    [Test]
    public void RenderingTheCases_ChangesNoProcessWideState()
    {
        object[] before = ProcessState.Snapshot();

        foreach (FixtureCase fixture in AvailableCases())
        {
            _ = ParityProfile.StyledLines(fixture);
        }

        Assert.That(ProcessState.Snapshot(), Is.EqualTo(before));
    }
}
