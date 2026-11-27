using ConsoleCalendarGenerator.Tests.Support;

namespace ConsoleCalendarGenerator.Tests.Parity.Campertron;

/// <summary>
/// The text. For every recorded case whose title is to match, the package renders the same input with the
/// parity profile and every line must be the recorded line, character for character.
/// </summary>
[TestFixture]
public class TextParityTests
{
    [TestCaseSource(typeof(ParityFixtures), nameof(ParityFixtures.CasesWithAnExactTitle))]
    public void EveryLine_IsExactlyTheRecordedLine(FixtureCase fixture)
    {
        ParityProfile.AssumeTheCultureDataIsTheRecordedOne(fixture);

        string[] rendered = Rendered.Lines(ParityProfile.Render(fixture));

        Assert.That(rendered, Is.EqualTo(fixture.Lines.Select(line => line.Text)));
    }

    /// <summary>The same through each way of getting text out of the package.</summary>
    [TestCaseSource(typeof(ParityFixtures), nameof(ParityFixtures.CasesWithAnExactTitle))]
    public void TheWholeText_IsTheRecordedText_ThroughToTextAndThroughTheWriter(FixtureCase fixture)
    {
        ParityProfile.AssumeTheCultureDataIsTheRecordedOne(fixture);
        RenderedMonth rendered = ParityProfile.Render(fixture);
        string recorded = string.Join("\n", fixture.Lines.Select(line => line.Text));
        var writer = new StringWriter { NewLine = "\n" };

        MonthWriter.Write(rendered, writer);

        Assert.That(rendered.ToText(), Is.EqualTo(recorded));
        Assert.That(writer.ToString(), Is.EqualTo(recorded + "\n"));
    }

    /// <summary>
    /// Under a culture whose calendar is not Gregorian the title is deliberately not the recorded one, but
    /// everything below the title is.
    /// </summary>
    [TestCaseSource(typeof(ParityFixtures), nameof(ParityFixtures.CasesWithAnIntentionallyDifferentTitle))]
    public void BelowTheTitle_EveryLineIsExactlyTheRecordedLine_AlsoWhereTheTitleDiffers(FixtureCase fixture)
    {
        _ = ParityProfile.CultureOf(fixture);

        string[] rendered = Rendered.Lines(ParityProfile.Render(fixture));

        Assert.That(rendered.Skip(1), Is.EqualTo(fixture.Lines.Skip(1).Select(line => line.Text)));
        Assert.That(rendered, Has.Length.EqualTo(fixture.Lines.Count));
    }

    [TestCaseSource(typeof(ParityFixtures), nameof(ParityFixtures.AllCases))]
    public void TheShape_IsTheRecordedShape(FixtureCase fixture)
    {
        RenderedMonth rendered = ParityProfile.Render(fixture);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(rendered.Lines, Has.Count.EqualTo(fixture.Lines.Count), "the same number of lines");
            Assert.That(rendered.Lines[1].Text, Is.EqualTo(fixture.Lines[1].Text), "the weekday row");
            Assert.That(rendered.Lines.Select(line => line.Text.Length).Skip(1), Is.EqualTo(fixture.Lines.Select(line => line.Text.Length).Skip(1)), "every row as long as recorded");
            Assert.That(rendered.Lines.Select(line => line.Text), Has.None.EndsWith(" ").Using(StringComparison.Ordinal), "no trailing spaces");
            Assert.That(rendered.Layout.FirstDayOfWeek, Is.EqualTo(DayOfWeek.Monday));
        }
    }

    /// <summary>The two calls Campertron's own test makes, with the text that test expects, written out here.</summary>
    [Test]
    public void TheTwoCallsOfTheSourcesOwnTest_GiveTheTextThatTestExpects()
    {
        FixtureCase february = ParityFixtures.Cases.Single(fixture => fixture.Id == "campertron-test-february-2027");
        FixtureCase august = ParityFixtures.Cases.Single(fixture => fixture.Id == "campertron-test-august-2026");
        ParityProfile.AssumeTheCultureDataIsTheRecordedOne(february);

        string[] expectedFebruary =
        {
            "February 2027 (* = open)",
            " Mo  Tu  We  Th  Fr  Sa  Su",
            " *1   2   3   4   5   6   7",
            "  8   9  10  11  12  13  14",
            " 15  16  17  18  19  20  21",
            " 22  23  24  25  26  27 *28",
        };
        string[] expectedAugust =
        {
            "> August 2026 (* = open)",
            ">  Mo  Tu  We  Th  Fr  Sa  Su",
            ">                       1   2",
            ">   3   4   5   6   7   8   9",
            ">  10  11  12  13  14  15  16",
            ">  17  18  19  20  21  22  23",
            ">  24  25  26  27  28  29  30",
            ">  31",
        };

        Assert.That(february.Lines.Select(line => line.Text), Is.EqualTo(expectedFebruary), "the fixture is what the source's test expects");
        Assert.That(august.Lines.Select(line => line.Text), Is.EqualTo(expectedAugust), "the fixture is what the source's test expects");
        Assert.That(Rendered.Lines(ParityProfile.Render(february)), Is.EqualTo(expectedFebruary));
        Assert.That(Rendered.Lines(ParityProfile.Render(august)), Is.EqualTo(expectedAugust));
    }

    /// <summary>
    /// Campertron's results test expects this week of November 2026 inside its output, with the 13th and 14th
    /// open, two spaces of indent, and those two cells colored. It is not a fixture; it is written out here.
    /// </summary>
    [Test]
    public void TheWeekThatTheSourcesResultsTestExpects_IsRenderedTheSame()
    {
        var input = new FixtureInput(2026, 11, "en-US", "  ", ConsoleColor.Cyan, new[]
        {
            new FixtureDate("2026-11-13T00:00:00.0000000", DateTimeKind.Unspecified),
            new FixtureDate("2026-11-14T00:00:00.0000000", DateTimeKind.Unspecified),
        });
        var fixture = new FixtureCase("november-2026", "the week in Campertron's results test", FixtureCase.ExactTitle, input, Array.Empty<FixtureLine>());

        IReadOnlyList<StyledSpanLine> lines = ParityProfile.StyledLines(fixture);
        StyledSpanLine week = lines.Single(line => line.Text == "    9  10  11  12 *13 *14  15");

        Assert.That(week.Spans.Where(span => span.Color.HasValue).Select(span => span.Text), Is.EqualTo(new[] { "*13", "*14" }));
        Assert.That(week.Spans.Where(span => span.Color.HasValue).Select(span => span.Color), Is.All.EqualTo(ConsoleColor.Cyan));
    }
}
