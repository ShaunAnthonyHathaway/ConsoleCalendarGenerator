namespace ConsoleCalendarGenerator.Tests.Rendering;

/// <summary>
/// Turning a rendered month into one string. The line terminator is chosen by the caller and is "\n" unless
/// they say otherwise; it never comes from the platform.
/// </summary>
[TestFixture]
public class TextConversionTests
{
    private static readonly RenderedMonth February2021 = MonthRenderer.Render(2021, 2, MonthRenderOptions.Default);

    [Test]
    public void ByDefault_TheLinesAreJoinedByLineFeeds_WithNoneAfterTheLast()
    {
        Assert.That(February2021.ToText(), Is.EqualTo(
            "   2021 February\n" +
            "Mo Tu We Th Fr Sa Su\n" +
            " 1  2  3  4  5  6  7\n" +
            " 8  9 10 11 12 13 14\n" +
            "15 16 17 18 19 20 21\n" +
            "22 23 24 25 26 27 28"));
    }

    [Test]
    public void WithCarriageReturnLineFeed_TheLinesAreJoinedByIt()
    {
        Assert.That(February2021.ToText("\r\n"), Is.EqualTo(
            "   2021 February\r\n" +
            "Mo Tu We Th Fr Sa Su\r\n" +
            " 1  2  3  4  5  6  7\r\n" +
            " 8  9 10 11 12 13 14\r\n" +
            "15 16 17 18 19 20 21\r\n" +
            "22 23 24 25 26 27 28"));
    }

    [Test]
    public void AnExplicitLineFeed_IsTheSameAsTheDefault()
    {
        Assert.That(February2021.ToText("\n"), Is.EqualTo(February2021.ToText()));
    }

    [Test]
    public void ToString_IsTheDefaultText()
    {
        Assert.That(February2021.ToString(), Is.EqualTo(February2021.ToText()));
        Assert.That($"{February2021}", Is.EqualTo(February2021.ToText()));
    }

    /// <summary>The same on Windows, where the platform's own line terminator is "\r\n".</summary>
    [Test]
    public void TheDefaultText_NeverContainsACarriageReturn()
    {
        Assert.That(February2021.ToText().Contains('\r'), Is.False);
        Assert.That(February2021.ToText().Count(character => character == '\n'), Is.EqualTo(February2021.Lines.Count - 1));
    }

    [Test]
    public void TheText_IsTheSameEveryTime()
    {
        string first = February2021.ToText();

        Assert.That(February2021.ToText(), Is.EqualTo(first));
        Assert.That(February2021.ToText("\r\n").Replace("\r\n", "\n"), Is.EqualTo(first));
        Assert.That(MonthRenderer.Render(2021, 2, MonthRenderOptions.Default).ToText(), Is.EqualTo(first));
    }

    [Test]
    public void TheText_IsTheTextOfTheLines()
    {
        RenderedMonth rendered = MonthRenderer.Render(2024, 2,
            MonthRenderOptions.Default.WithIndent("  ").WithCellWidth(3).WithTitleSuffix(" (leap)"));

        Assert.That(rendered.ToText().Split('\n'), Is.EqualTo(rendered.Lines.Select(line => line.Text)));
    }

    [Test]
    public void ANullLineTerminator_IsRejected()
    {
        Assert.That(Assert.Throws<ArgumentNullException>(() => February2021.ToText(null!))!.ParamName, Is.EqualTo("newLine"));
    }

    /// <summary>
    /// Only the two line terminators are accepted. Anything else, including an empty string, a lone carriage
    /// return and arbitrary text, is refused: lines can be joined with anything through <c>Lines</c>.
    /// </summary>
    [TestCase("")]
    [TestCase("\r")]
    [TestCase("\n\n")]
    [TestCase("\n\r")]
    [TestCase(" ")]
    [TestCase("<br>")]
    [TestCase("\r\n ")]
    public void AnyOtherLineTerminator_IsRejected(string newLine)
    {
        Assert.That(Assert.Throws<ArgumentException>(() => February2021.ToText(newLine))!.ParamName, Is.EqualTo("newLine"));
    }

    [Test]
    public void LinesCanBeJoinedWithAnything_ThroughTheLines()
    {
        Assert.That(string.Join(" / ", February2021.Lines.Select(line => line.Text.Trim())), Does.StartWith("2021 February / Mo Tu"));
    }
}
