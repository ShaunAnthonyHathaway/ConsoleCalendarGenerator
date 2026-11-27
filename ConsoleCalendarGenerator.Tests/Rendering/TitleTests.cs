using ConsoleCalendarGenerator.Tests.Support;

namespace ConsoleCalendarGenerator.Tests.Rendering;

/// <summary>
/// The title line: its format, its suffix, where it is placed over the grid, and the indent. With the default
/// options the grid is 20 wide.
/// </summary>
[TestFixture]
public class TitleTests
{
    private static string Title(int year, int month, MonthRenderOptions options) => MonthRenderer.Render(year, month, options).Lines[0].Text;

    private static string[] TitleSegments(int year, int month, MonthRenderOptions options) =>
        Rendered.Describe(MonthRenderer.Render(year, month, options).Lines[0]);

    // ---- format ----

    [Test]
    public void TheDefaultTitle_IsTheCulturesYearAndMonthPattern()
    {
        Assert.That(TitleSegments(2023, 5, MonthRenderOptions.Default), Is.EqualTo(new[] { "Padding[      ]", "Title[2023 May]" }));
    }

    [TestCase("MMMM yyyy", "May 2023")]
    [TestCase("MMM yy", "May 23")]
    [TestCase("yyyy-MM", "2023-05")]
    [TestCase("MMMM", "May")]
    [TestCase("Y", "2023 May")]
    [TestCase("'Bookings for' MMMM", "Bookings for May")]
    [TestCase("'MMMM is not replaced inside quotes'", "MMMM is not replaced inside quotes")]
    [TestCase("MMMM \\'yy", "May '23")]
    public void ATitleFormat_IsADateFormatString(string format, string expected)
    {
        MonthRenderOptions options = MonthRenderOptions.Default.WithTitleFormat(format).WithTitleAlignment(TitleAlignment.Left);

        Assert.That(options.TitleFormat, Is.EqualTo(format));
        Assert.That(Title(2023, 5, options), Is.EqualTo(expected));
    }

    /// <summary>
    /// The title is formatted from a value with no offset from UTC, so a format that shows the offset shows
    /// zero on every machine. Formatting a plain date would show nothing here, or the machine's own offset.
    /// </summary>
    [Test]
    public void TheTitle_NeverDependsOnTheTimeZoneOfTheMachine()
    {
        MonthRenderOptions options = MonthRenderOptions.Default.WithTitleAlignment(TitleAlignment.Left);

        Assert.That(Title(2023, 5, options.WithTitleFormat("MMMM zzz")), Is.EqualTo("May +00:00"));
        Assert.That(Title(2023, 5, options.WithTitleFormat("%K")), Is.EqualTo("+00:00"));
        Assert.That(Title(2023, 5, options.WithTitleFormat("yyyy-MM-dd HH:mm")), Is.EqualTo("2023-05-01 00:00"));
    }

    [TestCase("x")]
    [TestCase("'unterminated")]
    [TestCase("MMMM\\")]
    [TestCase("U")]
    [TestCase("%")]
    public void AnInvalidTitleFormat_IsRejectedWhenTheOptionsAreCreated(string format)
    {
        // Exactly FormatException, the type .NET uses for an invalid format string.
        FormatException exception = Assert.Throws<FormatException>(() => MonthRenderOptions.Default.WithTitleFormat(format))!;

        Assert.That(exception.InnerException, Is.InstanceOf<FormatException>(), "the runtime's own error is kept");
    }

    [Test]
    public void ANullTitleFormat_IsRejected()
    {
        Assert.That(Assert.Throws<ArgumentNullException>(() => MonthRenderOptions.Default.WithTitleFormat(null!))!.ParamName, Is.EqualTo("titleFormat"));
    }

    /// <summary>An empty format string means "the general format" to .NET, which is never wanted here.</summary>
    [Test]
    public void AnEmptyTitleFormat_IsRejected()
    {
        Assert.That(Assert.Throws<ArgumentException>(() => MonthRenderOptions.Default.WithTitleFormat(string.Empty))!.ParamName, Is.EqualTo("titleFormat"));
    }

    [TestCase("MMMM'\n'yyyy")]
    [TestCase("MMMM\r\nyyyy")]
    [TestCase("\n")]
    public void ATitleFormatWithALineBreak_IsRejected(string format)
    {
        Assert.That(Assert.Throws<ArgumentException>(() => MonthRenderOptions.Default.WithTitleFormat(format))!.ParamName, Is.EqualTo("titleFormat"));
    }

    // ---- suffix ----

    [Test]
    public void TheSuffix_FollowsTheTitleDirectly_AsItsOwnSegment()
    {
        MonthRenderOptions options = MonthRenderOptions.Default.WithTitleSuffix(" (* = open)").WithTitleAlignment(TitleAlignment.Left);

        Assert.That(TitleSegments(2023, 5, options), Is.EqualTo(new[] { "Title[2023 May]", "TitleSuffix[ (* = open)]" }));
        Assert.That(Title(2023, 5, options), Is.EqualTo("2023 May (* = open)"));
        Assert.That(Title(2023, 5, options.WithTitleSuffix("!")), Is.EqualTo("2023 May!"), "nothing is put between the two");
    }

    [Test]
    public void TheSuffix_IsCenteredTogetherWithTheTitle()
    {
        // "2023 May" and " *" are 10 together, which leaves 10 of the 20: five on each side.
        Assert.That(TitleSegments(2023, 5, MonthRenderOptions.Default.WithTitleSuffix(" *")), Is.EqualTo(new[]
        {
            "Padding[     ]",
            "Title[2023 May]",
            "TitleSuffix[ *]",
        }));
    }

    [Test]
    public void ANullSuffix_IsRejected()
    {
        Assert.That(Assert.Throws<ArgumentNullException>(() => MonthRenderOptions.Default.WithTitleSuffix(null!))!.ParamName, Is.EqualTo("titleSuffix"));
    }

    [TestCase(" one\ntwo")]
    [TestCase("\r")]
    public void ASuffixWithALineBreak_IsRejected(string suffix)
    {
        Assert.That(Assert.Throws<ArgumentException>(() => MonthRenderOptions.Default.WithTitleSuffix(suffix))!.ParamName, Is.EqualTo("titleSuffix"));
    }

    // ---- alignment ----

    [Test]
    public void LeftAligned_TheTitleStartsAtTheLeftEdgeOfTheGrid()
    {
        Assert.That(Title(2023, 5, MonthRenderOptions.Default.WithTitleAlignment(TitleAlignment.Left)), Is.EqualTo("2023 May"));
    }

    /// <summary>Centered over the 20-wide grid: half of what is left over goes before the title.</summary>
    [TestCase(5, "      2023 May", Description = "8 wide, 12 left over: 6 before")]
    [TestCase(4, "     2023 April", Description = "10 wide, 10 left over: 5 before")]
    [TestCase(1, "    2023 January", Description = "12 wide, 8 left over: 4 before")]
    [TestCase(9, "   2023 September", Description = "14 wide, 6 left over: 3 before")]
    public void Centered_WithAnEvenAmountLeftOver_TheTitleIsExactlyInTheMiddle(int month, string expected)
    {
        Assert.That(Title(2023, month, MonthRenderOptions.Default), Is.EqualTo(expected));
    }

    /// <summary>An odd amount cannot be halved. The smaller half goes before the title, so the title leans left.</summary>
    [TestCase(6, "     2023 June", Description = "9 wide, 11 left over: 5 before")]
    [TestCase(8, "    2023 August", Description = "11 wide, 9 left over: 4 before")]
    [TestCase(2, "   2023 February", Description = "13 wide, 7 left over: 3 before")]
    public void Centered_WithAnOddAmountLeftOver_TheExtraSpaceGoesAfterTheTitle(int month, string expected)
    {
        Assert.That(Title(2023, month, MonthRenderOptions.Default), Is.EqualTo(expected));
    }

    [Test]
    public void Centered_WithOneLeftOver_ThereIsNoPadding()
    {
        // "2023 May" and its suffix are 19 together.
        MonthRenderOptions options = MonthRenderOptions.Default.WithTitleSuffix(" (* = open)");

        Assert.That(TitleSegments(2023, 5, options), Is.EqualTo(new[] { "Title[2023 May]", "TitleSuffix[ (* = open)]" }));
    }

    [Test]
    public void Centered_ATitleAsWideAsTheGrid_StartsAtTheLeftEdge()
    {
        MonthRenderOptions options = MonthRenderOptions.Default.WithTitleFormat("'Exactly twenty wide.'");

        Assert.That(TitleSegments(2023, 5, options), Is.EqualTo(new[] { "Title[Exactly twenty wide.]" }));
    }

    /// <summary>A title wider than the grid is not cut and not moved left of the grid.</summary>
    [Test]
    public void Centered_ATitleWiderThanTheGrid_StartsAtTheLeftEdge_AndIsNotCut()
    {
        MonthRenderOptions options = MonthRenderOptions.Default.WithTitleFormat("'The availability calendar for' MMMM yyyy").WithIndent("  ");

        Assert.That(Rendered.Lines(MonthRenderer.Render(2023, 5, options)).Take(3), Is.EqualTo(new[]
        {
            "  The availability calendar for May 2023",
            "  Mo Tu We Th Fr Sa Su",
            "   1  2  3  4  5  6  7",
        }));
    }

    [Test]
    public void TheCenter_IsTheCenterOfTheGrid_WhateverItsWidth()
    {
        // Cells three wide: the grid is 27 wide, "2023 May" is 8, 19 are left over, 9 go before.
        MonthRenderOptions options = MonthRenderOptions.Default.WithCellWidth(3);

        Assert.That(options.GridWidth, Is.EqualTo(27));
        Assert.That(Title(2023, 5, options), Is.EqualTo(new string(' ', 9) + "2023 May"));
    }

    /// <summary>Centering counts text elements: a letter with a combining mark is one, not two.</summary>
    [Test]
    public void Centering_CountsTextElements()
    {
        string eAcute = "e" + (char)0x0301;
        // Ten letters, each with a combining mark: 20 UTF-16 code units, 10 text elements, so 5 before.
        string suffix = string.Concat(Enumerable.Repeat(eAcute, 10));
        MonthRenderOptions options = MonthRenderOptions.Default.WithTitleFormat("''").WithTitleSuffix(suffix);

        Assert.That(TitleSegments(2023, 5, options), Is.EqualTo(new[] { "Padding[     ]", "TitleSuffix[" + suffix + "]" }));
    }

    [TestCase(-1)]
    [TestCase(2)]
    [TestCase(int.MinValue)]
    public void AnAlignmentThatIsNotDefined_IsRejected(int value)
    {
        var alignment = (TitleAlignment)value;

        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(
            () => MonthRenderOptions.Default.WithTitleAlignment(alignment))!;

        Assert.That(exception.ParamName, Is.EqualTo("titleAlignment"));
        Assert.That(exception.ActualValue, Is.EqualTo(alignment));
    }

    // ---- indent ----

    [Test]
    public void TheIndent_StartsEveryLine_TheSameWay()
    {
        MonthRenderOptions options = MonthRenderOptions.Default.WithIndent("    ");

        Assert.That(Rendered.Lines(MonthRenderer.Render(2023, 2, options)), Is.EqualTo(new[]
        {
            "       2023 February",
            "    Mo Tu We Th Fr Sa Su",
            "           1  2  3  4  5",
            "     6  7  8  9 10 11 12",
            "    13 14 15 16 17 18 19",
            "    20 21 22 23 24 25 26",
            "    27 28",
        }));
    }

    [Test]
    public void TheIndent_DoesNotMoveTheTitleRelativeToTheGrid()
    {
        string[] plain = Rendered.Lines(MonthRenderer.Render(2023, 5, MonthRenderOptions.Default));
        string[] indented = Rendered.Lines(MonthRenderer.Render(2023, 5, MonthRenderOptions.Default.WithIndent("-->")));

        Assert.That(indented, Is.EqualTo(plain.Select(line => "-->" + line)));
    }

    [Test]
    public void TheIndent_CanBeAnyTextWithoutALineBreak()
    {
        Assert.That(Title(2023, 5, MonthRenderOptions.Default.WithIndent("\t| ")), Is.EqualTo("\t|       2023 May"));
        Assert.That(Assert.Throws<ArgumentNullException>(() => MonthRenderOptions.Default.WithIndent(null!))!.ParamName, Is.EqualTo("indent"));
        Assert.That(Assert.Throws<ArgumentException>(() => MonthRenderOptions.Default.WithIndent(" \n "))!.ParamName, Is.EqualTo("indent"));
    }

    // ---- a title with no text ----

    /// <summary>
    /// A format that produces nothing leaves the title line in place but empty: no segments, not even the
    /// indent. The other lines are unaffected.
    /// </summary>
    [Test]
    public void ATitleWithNoTextAtAll_IsAnEmptyLine()
    {
        RenderedMonth rendered = MonthRenderer.Render(2023, 5, MonthRenderOptions.Default.WithTitleFormat("''").WithIndent("  "));

        Assert.That(rendered.Lines[0].Kind, Is.EqualTo(RenderedLineKind.Title));
        Assert.That(rendered.Lines[0].Segments, Is.Empty);
        Assert.That(rendered.Lines[0].Text, Is.EqualTo(string.Empty));
        Assert.That(rendered.Lines[1].Text, Is.EqualTo("  Mo Tu We Th Fr Sa Su"));
        Assert.That(rendered.ToText(), Does.StartWith("\n  Mo Tu").Using(StringComparison.Ordinal));
    }

    [Test]
    public void ATitleThatIsOnlyASuffix_HasNoTitleSegment()
    {
        MonthRenderOptions options = MonthRenderOptions.Default.WithTitleFormat("''").WithTitleSuffix("Legend").WithIndent("  ");

        Assert.That(TitleSegments(2023, 5, options), Is.EqualTo(new[] { "Indent[  ]", "Padding[       ]", "TitleSuffix[Legend]" }));
    }
}
