using ConsoleCalendarGenerator.Tests.Support;

namespace ConsoleCalendarGenerator.Tests.Rendering;

/// <summary>
/// Cell width and column gap: the exact text for several sizes, how numbers and labels line up, the width of the
/// grid, and the sizes that are refused.
/// </summary>
[TestFixture]
public class CellSizingTests
{
    // ---- exact text ----

    /// <summary>Cells three wide with one space between: room for a marker beside the day in a later phase.</summary>
    [Test]
    public void CellsThreeWide()
    {
        MonthRenderOptions options = MonthRenderOptions.Default.WithCellWidth(3);

        Assert.That(Rendered.Lines(MonthRenderer.Render(2023, 6, options)), Is.EqualTo(new[]
        {
            "         2023 June",
            " Mo  Tu  We  Th  Fr  Sa  Su",
            "              1   2   3   4",
            "  5   6   7   8   9  10  11",
            " 12  13  14  15  16  17  18",
            " 19  20  21  22  23  24  25",
            " 26  27  28  29  30",
        }));
    }

    [Test]
    public void NoGapAtAll()
    {
        MonthRenderOptions options = MonthRenderOptions.Default.WithColumnGap(0);

        Assert.That(Rendered.Lines(MonthRenderer.Render(2023, 6, options)), Is.EqualTo(new[]
        {
            "  2023 June",
            "MoTuWeThFrSaSu",
            "       1 2 3 4",
            " 5 6 7 8 91011",
            "12131415161718",
            "19202122232425",
            "2627282930",
        }));
    }

    [Test]
    public void AGapOfThree()
    {
        MonthRenderOptions options = MonthRenderOptions.Default.WithColumnGap(3);

        Assert.That(Rendered.Lines(MonthRenderer.Render(2023, 6, options)), Is.EqualTo(new[]
        {
            "           2023 June",
            "Mo   Tu   We   Th   Fr   Sa   Su",
            "                1    2    3    4",
            " 5    6    7    8    9   10   11",
            "12   13   14   15   16   17   18",
            "19   20   21   22   23   24   25",
            "26   27   28   29   30",
        }));
    }

    [Test]
    public void WideCellsAndAWideGap_WithAnIndent()
    {
        MonthRenderOptions options = MonthRenderOptions.Default.WithCellWidth(4).WithColumnGap(2).WithIndent("  ").WithFirstDayOfWeek(DayOfWeek.Sunday);

        Assert.That(Rendered.Lines(MonthRenderer.Render(2024, 2, options)), Is.EqualTo(new[]
        {
            "               2024 February",
            "    Su    Mo    Tu    We    Th    Fr    Sa",
            "                             1     2     3",
            "     4     5     6     7     8     9    10",
            "    11    12    13    14    15    16    17",
            "    18    19    20    21    22    23    24",
            "    25    26    27    28    29",
        }));
    }

    // ---- alignment ----

    /// <summary>
    /// The last digit of every day, and the last character of every label, is in the last position of its cell:
    /// the columns line up on the right, for one-digit and two-digit days alike.
    /// </summary>
    [TestCase(2, 1)]
    [TestCase(2, 0)]
    [TestCase(3, 1)]
    [TestCase(5, 2)]
    [TestCase(9, 4)]
    public void DaysAndLabels_AreRightAlignedInTheirCells(int cellWidth, int columnGap)
    {
        MonthRenderOptions options = MonthRenderOptions.Default.WithCellWidth(cellWidth).WithColumnGap(columnGap);

        for (int month = 1; month <= 12; month++)
        {
            RenderedMonth rendered = MonthRenderer.Render(2024, month, options);

            foreach (RenderedLine line in rendered.Lines.Skip(1))
            {
                int position = 0;
                foreach (RenderedSegment segment in line.Segments)
                {
                    if (segment.Kind is RenderedSegmentKind.Day or RenderedSegmentKind.WeekdayLabel)
                    {
                        int column = rendered.Layout.DaysOfWeek.ToList().IndexOf(segment.DayOfWeek!.Value);
                        Assert.That(position, Is.EqualTo(column * (cellWidth + columnGap)), "a cell starts where its column starts");
                        Assert.That(segment.Text, Has.Length.EqualTo(cellWidth));
                        Assert.That(segment.Text[^1], Is.Not.EqualTo(' '), "the text ends at the right edge of the cell");
                        Assert.That(segment.Text.TrimStart(' '), Is.EqualTo(segment.Date?.Day.ToString() ?? segment.Text.Trim()), "only spaces come before it");
                    }
                    position += segment.Text.Length;
                }
            }
        }
    }

    [TestCase(2, 1)]
    [TestCase(2, 0)]
    [TestCase(3, 1)]
    [TestCase(4, 3)]
    [TestCase(12, 7)]
    public void NoWeekOrLabelLine_EndsWithASpace_OrIsWiderThanTheGrid(int cellWidth, int columnGap)
    {
        MonthRenderOptions options = MonthRenderOptions.Default.WithCellWidth(cellWidth).WithColumnGap(columnGap);

        foreach (DayOfWeek firstDayOfWeek in ReferenceCalendar.AllDaysOfWeek)
        {
            for (int month = 1; month <= 12; month++)
            {
                RenderedMonth rendered = MonthRenderer.Render(2023, month, options.WithFirstDayOfWeek(firstDayOfWeek));

                foreach (RenderedLine line in rendered.Lines)
                {
                    Assert.That(line.Text, Does.Not.EndWith(" ").Using(StringComparison.Ordinal));
                    Assert.That(line.Text.Length, Is.LessThanOrEqualTo(options.GridWidth));
                }
                Assert.That(rendered.Lines[1].Text, Has.Length.EqualTo(options.GridWidth), "the label line is exactly as wide as the grid");
            }
        }
    }

    // ---- the width of the grid ----

    [TestCase(2, 1, 20)]
    [TestCase(2, 0, 14)]
    [TestCase(3, 1, 27)]
    [TestCase(3, 0, 21)]
    [TestCase(4, 2, 40)]
    [TestCase(2, 100, 614)]
    [TestCase(100, 0, 700)]
    [TestCase(100, 100, 1300)]
    public void TheGridWidth_IsSevenCellsAndSixGaps(int cellWidth, int columnGap, int expected)
    {
        MonthRenderOptions options = MonthRenderOptions.Default.WithCellWidth(cellWidth).WithColumnGap(columnGap);

        Assert.That(options.GridWidth, Is.EqualTo(expected));
        Assert.That(MonthRenderer.Render(2023, 5, options).Lines[1].Text, Has.Length.EqualTo(expected));
    }

    [Test]
    public void TheIndent_IsNotPartOfTheGridWidth()
    {
        Assert.That(MonthRenderOptions.Default.WithIndent("        ").GridWidth, Is.EqualTo(20));
    }

    // ---- sizes that are accepted and refused ----

    [TestCase(2)]
    [TestCase(3)]
    [TestCase(100)]
    public void ACellWidthFromTwoThroughOneHundred_IsAccepted(int cellWidth)
    {
        Assert.That(MonthRenderOptions.Default.WithCellWidth(cellWidth).CellWidth, Is.EqualTo(cellWidth));
    }

    /// <summary>A cell narrower than two could not hold a two-digit day.</summary>
    [TestCase(1)]
    [TestCase(0)]
    [TestCase(-1)]
    [TestCase(101)]
    [TestCase(int.MinValue)]
    [TestCase(int.MaxValue)]
    public void AnyOtherCellWidth_IsRejected(int cellWidth)
    {
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => MonthRenderOptions.Default.WithCellWidth(cellWidth))!;

        Assert.That(exception.ParamName, Is.EqualTo("cellWidth"));
        Assert.That(exception.ActualValue, Is.EqualTo(cellWidth));
    }

    [TestCase(0)]
    [TestCase(1)]
    [TestCase(100)]
    public void AColumnGapFromZeroThroughOneHundred_IsAccepted(int columnGap)
    {
        Assert.That(MonthRenderOptions.Default.WithColumnGap(columnGap).ColumnGap, Is.EqualTo(columnGap));
    }

    [TestCase(-1)]
    [TestCase(101)]
    [TestCase(int.MinValue)]
    [TestCase(int.MaxValue)]
    public void AnyOtherColumnGap_IsRejected(int columnGap)
    {
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => MonthRenderOptions.Default.WithColumnGap(columnGap))!;

        Assert.That(exception.ParamName, Is.EqualTo("columnGap"));
        Assert.That(exception.ActualValue, Is.EqualTo(columnGap));
    }
}
