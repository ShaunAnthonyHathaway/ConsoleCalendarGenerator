using System.Globalization;

namespace ConsoleCalendarGenerator.Tests.Parity.Campertron;

/// <summary>
/// How wide the output is. Widths are counted in text elements, which is what the package can count; how many
/// columns a terminal uses to show a text element is not something these tests can know or claim.
/// </summary>
[TestFixture]
public class WidthTests
{
    private static int Width(string text) => new StringInfo(text).LengthInTextElements;

    [TestCaseSource(typeof(ParityFixtures), nameof(ParityFixtures.AllCases))]
    public void NoLine_IsWiderThanTheWidthTheSourceWritesTo(FixtureCase fixture)
    {
        int maximum = ParityFixtures.Manifest.LiveCallSites.MaximumWidth;

        Assert.That(fixture.Lines.Select(line => Width(line.Text)), Is.All.LessThanOrEqualTo(maximum), "as recorded");
        Assert.That(ParityProfile.Render(fixture).Lines.Select(line => Width(line.Text)), Is.All.LessThanOrEqualTo(maximum), "as the package renders it");
    }

    /// <summary>The grid is 27 wide: seven cells of three and six gaps of one. The indent comes on top of that.</summary>
    [TestCaseSource(typeof(ParityFixtures), nameof(ParityFixtures.AllCases))]
    public void TheWeekdayRow_IsTheIndentPlusTheGrid_AndNoRowIsWider(FixtureCase fixture)
    {
        RenderedMonth rendered = ParityProfile.Render(fixture);
        MonthRenderOptions options = ParityProfile.Options(fixture.Input, ParityProfile.CultureOf(fixture));
        int indent = Width(fixture.Input.Indent);

        Assert.That(options.GridWidth, Is.EqualTo(27));
        Assert.That(Width(rendered.Lines[1].Text), Is.EqualTo(indent + 27), "the indent is part of the line");
        Assert.That(Width(fixture.Lines[1].Text), Is.EqualTo(indent + 27), "as recorded");
        Assert.That(rendered.Lines.Skip(1).Select(line => Width(line.Text)), Is.All.LessThanOrEqualTo(indent + 27));
    }

    [Test]
    public void WithTheIndentOfTheLiveCallSites_TheGridEndsAtColumn29()
    {
        string liveIndent = ParityFixtures.Manifest.LiveCallSites.Indent;

        foreach (FixtureCase fixture in ParityFixtures.Cases.Where(c => c.Input.Indent == liveIndent))
        {
            Assert.That(fixture.Lines.Skip(1).Max(line => line.Text.Length), Is.EqualTo(29), fixture.ToString());
        }
    }

    /// <summary>The marker shares the cell with the number: a marked row is exactly as wide as an unmarked one.</summary>
    [Test]
    public void MarkingDays_DoesNotWidenAnyLine()
    {
        FixtureCase none = ParityFixtures.Cases.Single(c => c.File == "marked-dates.json" && c.Id == "none");
        FixtureCase everyDay = ParityFixtures.Cases.Single(c => c.File == "marked-dates.json" && c.Id == "every-day");

        Assert.That(everyDay.Lines.Select(line => line.Text.Length), Is.EqualTo(none.Lines.Select(line => line.Text.Length)), "as recorded");
        Assert.That(ParityProfile.Render(everyDay).Lines.Select(line => line.Text.Length),
            Is.EqualTo(ParityProfile.Render(none).Lines.Select(line => line.Text.Length)), "as the package renders it");
    }
}
