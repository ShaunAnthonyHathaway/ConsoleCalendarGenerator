using System.Globalization;
using System.Text;
using ConsoleCalendarGenerator.Tests.Support;

namespace ConsoleCalendarGenerator.Tests.Rendering;

/// <summary>
/// The rendered month against its layout. The layout itself is verified exhaustively in Tests/Layout; these
/// tests show that the renderer shows exactly what the layout says, for every month from 1900 through 2100 and
/// every first day of the week, in two different shapes.
/// </summary>
[TestFixture]
public class RenderedLayoutTests
{
    private static readonly MonthRenderOptions[] Shapes =
    {
        MonthRenderOptions.Default,
        MonthRenderOptions.Default.WithCellWidth(3).WithColumnGap(2).WithIndent("    ").WithTitleAlignment(TitleAlignment.Left),
    };

    [Test]
    public void TheRenderedLayout_IsTheLayoutOfTheSameMonthAndFirstDay()
    {
        foreach (DayOfWeek firstDayOfWeek in ReferenceCalendar.AllDaysOfWeek)
        {
            for (int month = 1; month <= 12; month++)
            {
                RenderedMonth rendered = MonthRenderer.Render(2024, month, MonthRenderOptions.Default.WithFirstDayOfWeek(firstDayOfWeek));

                Assert.That(LayoutPicture.Describe(rendered.Layout), Is.EqualTo(LayoutPicture.Describe(MonthLayout.Create(2024, month, firstDayOfWeek))));
            }
        }
    }

    /// <summary>2,412 months x 7 first days x 2 shapes: 33,768 rendered months.</summary>
    [Test]
    public void EveryMonthFrom1900Through2100_ForEveryFirstDayOfWeek_ShowsExactlyItsLayout()
    {
        var problems = new List<string>();
        int rendered = 0;

        foreach (MonthRenderOptions shape in Shapes)
        {
            foreach (DayOfWeek firstDayOfWeek in ReferenceCalendar.AllDaysOfWeek)
            {
                MonthRenderOptions options = shape.WithFirstDayOfWeek(firstDayOfWeek);
                for (int year = 1900; year <= 2100; year++)
                {
                    for (int month = 1; month <= 12; month++)
                    {
                        rendered++;
                        if (Difference(MonthRenderer.Render(year, month, options), MonthLayout.Create(year, month, firstDayOfWeek), options) is string difference
                            && problems.Count < 20)
                        {
                            problems.Add($"{year}-{month:00} from {firstDayOfWeek}: {difference}");
                        }
                    }
                }
            }
        }

        Assert.That(problems, Is.Empty);
        Assert.That(rendered, Is.EqualTo(33_768));
    }

    [TestCase(1)]
    [TestCase(9999)]
    public void EveryMonthOfTheFirstAndTheLastYear_ShowsExactlyItsLayout(int year)
    {
        foreach (MonthRenderOptions shape in Shapes)
        {
            foreach (DayOfWeek firstDayOfWeek in ReferenceCalendar.AllDaysOfWeek)
            {
                MonthRenderOptions options = shape.WithFirstDayOfWeek(firstDayOfWeek);
                for (int month = 1; month <= 12; month++)
                {
                    Assert.That(Difference(MonthRenderer.Render(year, month, options), MonthLayout.Create(year, month, firstDayOfWeek), options),
                        Is.Null, $"{year}-{month:00} from {firstDayOfWeek}");
                }
            }
        }
    }

    /// <summary>
    /// What differs between a rendered month and what the layout says it should show, or null. The expected
    /// week lines are built here from the layout's cells, one cell at a time.
    /// </summary>
    private static string? Difference(RenderedMonth rendered, MonthLayout layout, MonthRenderOptions options)
    {
        if (rendered.Lines.Count != layout.Weeks.Count + 2)
        {
            return $"{rendered.Lines.Count} lines for {layout.Weeks.Count} weeks";
        }
        if (rendered.Lines[0].Kind != RenderedLineKind.Title || rendered.Lines[1].Kind != RenderedLineKind.WeekdayLabels)
        {
            return "the first two lines are not the title and the labels";
        }

        // The labels: one per column, for the weekday of that column.
        DayOfWeek?[] labelWeekdays = rendered.Lines[1].Segments
            .Where(segment => segment.Kind == RenderedSegmentKind.WeekdayLabel).Select(segment => segment.DayOfWeek).ToArray();
        if (!labelWeekdays.SequenceEqual(layout.DaysOfWeek.Cast<DayOfWeek?>()))
        {
            return "the labels are for " + string.Join(" ", labelWeekdays);
        }

        for (int week = 0; week < layout.Weeks.Count; week++)
        {
            RenderedLine line = rendered.Lines[week + 2];
            IReadOnlyList<MonthLayoutCell> cells = layout.Weeks[week].Cells;
            if (line.Kind != RenderedLineKind.Week)
            {
                return $"line {week + 2} is a {line.Kind} line";
            }

            // Text: every cell up to the last one with a date, right-aligned, with the gap between cells.
            int lastWithDate = Enumerable.Range(0, 7).Last(column => !cells[column].IsEmpty);
            var expected = new StringBuilder(options.Indent);
            for (int column = 0; column <= lastWithDate; column++)
            {
                if (column > 0)
                {
                    expected.Append(' ', options.ColumnGap);
                }
                string day = cells[column].Date?.Day.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
                expected.Append(day.PadLeft(options.CellWidth));
            }
            if (line.Text != expected.ToString())
            {
                return $"week {week} is <{line.Text}> instead of <{expected}>";
            }

            // Segments: one cell segment per cell, with the cell's weekday and date.
            RenderedSegment[] cellSegments = line.Segments
                .Where(segment => segment.Kind is RenderedSegmentKind.Day or RenderedSegmentKind.EmptyDay).ToArray();
            if (cellSegments.Length != lastWithDate + 1)
            {
                return $"week {week} has {cellSegments.Length} cell segments for {lastWithDate + 1} cells";
            }
            for (int column = 0; column <= lastWithDate; column++)
            {
                RenderedSegment segment = cellSegments[column];
                MonthLayoutCell cell = cells[column];
                if (segment.Date != cell.Date || segment.DayOfWeek != cell.DayOfWeek
                    || (segment.Kind == RenderedSegmentKind.EmptyDay) != cell.IsEmpty)
                {
                    return $"week {week}, column {column} is {Rendered.Describe(segment)}";
                }
            }
        }
        return null;
    }
}
