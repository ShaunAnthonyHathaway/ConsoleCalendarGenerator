using System.Globalization;
using ConsoleCalendarGenerator.Tests.Support;

namespace ConsoleCalendarGenerator.Tests.States;

/// <summary>
/// How a state shows in the text: its marker directly before the day number, inside the cell, with nothing
/// else about the month moving. Widths are counted in text elements, as everywhere in rendering.
/// </summary>
[TestFixture]
public class MarkerRenderingTests
{
    private static readonly MonthRenderOptions ThreeWide = MonthRenderOptions.Default.WithCellWidth(3);
    private static readonly DayState Open = new("open", "*");

    // Text that cannot be typed safely into every editor is built from code points.
    private static readonly string Smiley = char.ConvertFromUtf32(0x1F600);
    private static readonly string EAcute = "e" + (char)0x0301;

    private static DateTime[] February(params int[] days) => days.Select(day => new DateTime(2023, 2, day)).ToArray();

    // ---- exact text ----

    [Test]
    public void AMarkedMonth_CellsThreeWide()
    {
        RenderedMonth rendered = MonthRenderer.Render(2023, 2, ThreeWide.WithTitleSuffix(" (* = open)"), February(1, 9, 14, 28), Open);

        Assert.That(Rendered.Lines(rendered), Is.EqualTo(new[]
        {
            " 2023 February (* = open)",
            " Mo  Tu  We  Th  Fr  Sa  Su",
            "         *1   2   3   4   5",
            "  6   7   8  *9  10  11  12",
            " 13 *14  15  16  17  18  19",
            " 20  21  22  23  24  25  26",
            " 27 *28",
        }));
    }

    [Test]
    public void TheMarkedSegments_AreWholeCells_WithTheirDateWeekdayAndState()
    {
        RenderedMonth rendered = MonthRenderer.Render(2023, 2, ThreeWide, February(1, 14), Open);

        Assert.That(Rendered.Describe(rendered.Lines[2]), Is.EqualTo(new[]
        {
            "EmptyDay[   ] Monday",
            "Gap[ ]",
            "EmptyDay[   ] Tuesday",
            "Gap[ ]",
            "Day[ *1] Wednesday 2023-02-01 state=open",
            "Gap[ ]",
            "Day[  2] Thursday 2023-02-02",
            "Gap[ ]",
            "Day[  3] Friday 2023-02-03",
            "Gap[ ]",
            "Day[  4] Saturday 2023-02-04",
            "Gap[ ]",
            "Day[  5] Sunday 2023-02-05",
        }));
        Assert.That(Rendered.Describe(rendered.Lines[4]).Take(3), Is.EqualTo(new[]
        {
            "Day[ 13] Monday 2023-02-13",
            "Gap[ ]",
            "Day[*14] Tuesday 2023-02-14 state=open",
        }));
    }

    /// <summary>The marker and the number are right-aligned together, so the numbers stay in their columns.</summary>
    [TestCase(3, " *5", "*15")]
    [TestCase(4, "  *5", " *15")]
    [TestCase(6, "    *5", "   *15")]
    public void TheMarker_IsDirectlyBeforeTheNumber_ForOneAndTwoDigitDays(int cellWidth, string the5th, string the15th)
    {
        RenderedMonth rendered = MonthRenderer.Render(2023, 2, MonthRenderOptions.Default.WithCellWidth(cellWidth), February(5, 15), Open);

        RenderedSegment[] marked = rendered.Lines.SelectMany(line => line.Segments).Where(segment => segment.State != null).ToArray();

        Assert.That(marked.Select(segment => segment.Text), Is.EqualTo(new[] { the5th, the15th }));
    }

    [TestCase("**", 4, "**15", " **5")]
    [TestCase(">>", 4, ">>15", " >>5")]
    [TestCase("* ", 4, "* 15", " * 5")]
    [TestCase("[x]", 5, "[x]15", " [x]5")]
    public void ALongerMarker_NeedsAWiderCell(string marker, int cellWidth, string the15th, string the5th)
    {
        var state = new DayState("marked", marker);

        RenderedMonth rendered = MonthRenderer.Render(2023, 2, MonthRenderOptions.Default.WithCellWidth(cellWidth), February(5, 15), state);

        Assert.That(rendered.Lines.SelectMany(line => line.Segments).Where(segment => segment.State != null).Select(segment => segment.Text),
            Is.EqualTo(new[] { the5th, the15th }));
        Assert.That(
            () => MonthRenderer.Render(2023, 2, MonthRenderOptions.Default.WithCellWidth(cellWidth - 1), February(5, 15), state),
            Throws.TypeOf<ArgumentException>(), "one narrower does not fit");
    }

    // ---- text elements ----

    [Test]
    public void AMarkerOutsideTheBasicPlane_CountsAsOne()
    {
        // Two UTF-16 code units, one text element: it fits a cell three wide beside a two-digit day.
        var happy = new DayState("happy", Smiley);

        RenderedMonth rendered = MonthRenderer.Render(2023, 2, ThreeWide, February(5, 15), happy);
        string[] marked = rendered.Lines.SelectMany(line => line.Segments).Where(segment => segment.State != null).Select(segment => segment.Text).ToArray();

        Assert.That(marked, Is.EqualTo(new[] { " " + Smiley + "5", Smiley + "15" }));
        Assert.That(marked.Select(text => new StringInfo(text).LengthInTextElements), Is.All.EqualTo(3));
    }

    [Test]
    public void AMarkerWithACombiningMark_CountsAsOne()
    {
        var accented = new DayState("accented", EAcute);

        RenderedMonth rendered = MonthRenderer.Render(2023, 2, ThreeWide, February(5, 15), accented);

        Assert.That(rendered.Lines.SelectMany(line => line.Segments).Where(segment => segment.State != null).Select(segment => segment.Text),
            Is.EqualTo(new[] { " " + EAcute + "5", EAcute + "15" }));
    }

    [Test]
    public void TwoCharactersOutsideTheBasicPlane_NeedFour()
    {
        var state = new DayState("double", Smiley + Smiley);

        Assert.That(() => MonthRenderer.Render(2023, 2, ThreeWide, February(15), state), Throws.TypeOf<ArgumentException>());
        Assert.That(MonthRenderer.Render(2023, 2, ThreeWide.WithCellWidth(4), February(15), state).Lines[4].Segments[4].Text, Is.EqualTo(Smiley + Smiley + "15"));
    }

    // ---- a marker that does not fit ----

    /// <summary>
    /// A marker never disappears and is never cut: when the cell cannot hold it beside a two-digit day, nothing
    /// is rendered. The default cell, two wide, holds no marker at all.
    /// </summary>
    [Test]
    public void AMarkerThatDoesNotFit_IsRefused_NotCutOrDropped()
    {
        ArgumentException fromDates = Assert.Throws<ArgumentException>(() => MonthRenderer.Render(2023, 2, MonthRenderOptions.Default, February(14), Open))!;
        ArgumentException fromEntries = Assert.Throws<ArgumentException>(() => MonthRenderer.Render(2023, 2, MonthRenderOptions.Default,
            new[] { new KeyValuePair<DateTime, DayState>(new DateTime(2023, 2, 14), Open) }))!;

        Assert.That(fromDates.ParamName, Is.EqualTo("state"));
        Assert.That(fromEntries.ParamName, Is.EqualTo("dayStates"));
        Assert.That(fromDates.Message, Does.Contain("open").And.Contain("3").And.Contain("2"), "the message names the state, the width needed and the width set");
    }

    /// <summary>
    /// The check does not depend on the data: it is made for one-digit days, for a state with no date in the
    /// month, and for a state with no dates at all, so the same states work for every month or for none.
    /// </summary>
    [Test]
    public void TheFit_IsCheckedWhateverTheDatesAre()
    {
        MonthRenderOptions tooNarrow = MonthRenderOptions.Default;

        Assert.That(() => MonthRenderer.Render(2023, 2, tooNarrow, February(5), Open), Throws.TypeOf<ArgumentException>(), "a one-digit day");
        Assert.That(() => MonthRenderer.Render(2023, 2, tooNarrow, new[] { new DateTime(2023, 3, 14) }, Open), Throws.TypeOf<ArgumentException>(), "a date in another month");
        Assert.That(() => MonthRenderer.Render(2023, 2, tooNarrow, Array.Empty<DateTime>(), Open), Throws.TypeOf<ArgumentException>(), "no dates");
        Assert.That(() => MonthRenderer.Render(2023, 2, tooNarrow,
            new[] { new KeyValuePair<DateTime, DayState>(new DateTime(1999, 1, 1), Open) }), Throws.TypeOf<ArgumentException>(), "an entry for another year");
    }

    [Test]
    public void TheMinimumCellWidth_IsTheMarkerPlusTwo()
    {
        foreach (int markerLength in new[] { 1, 2, 3, 5 })
        {
            var state = new DayState("m", new string('#', markerLength));

            Assert.That(() => MonthRenderer.Render(2023, 2, MonthRenderOptions.Default.WithCellWidth(markerLength + 2), February(28), state), Throws.Nothing);
            Assert.That(() => MonthRenderer.Render(2023, 2, MonthRenderOptions.Default.WithCellWidth(markerLength + 1), February(28), state),
                Throws.TypeOf<ArgumentException>());
        }
    }

    // ---- nothing else moves ----

    /// <summary>
    /// For every month of two years, every first day of the week and several sets of marked days: the title
    /// and the labels are unchanged, every line is as wide as it was, every unmarked cell is identical, and
    /// every marked cell is the same width with the same number in the same place.
    /// </summary>
    [Test]
    public void MarkingDays_ChangesOnlyTheMarkedCells()
    {
        var problems = new List<string>();
        MonthRenderOptions options = ThreeWide.WithIndent("  ");

        foreach (DayOfWeek firstDayOfWeek in ReferenceCalendar.AllDaysOfWeek)
        {
            for (int year = 2023; year <= 2024; year++)
            {
                for (int month = 1; month <= 12; month++)
                {
                    foreach (int step in new[] { 1, 2, 7, 11 })
                    {
                        int y = year;
                        int m = month;
                        DateTime[] marked = Enumerable.Range(1, DateTime.DaysInMonth(year, month))
                            .Where(day => day % step == 0).Select(day => new DateTime(y, m, day)).ToArray();
                        RenderedMonth plain = MonthRenderer.Render(year, month, options.WithFirstDayOfWeek(firstDayOfWeek));
                        RenderedMonth withStates = MonthRenderer.Render(year, month, options.WithFirstDayOfWeek(firstDayOfWeek), marked, Open);

                        if (Difference(plain, withStates, marked) is string difference)
                        {
                            problems.Add($"{year}-{month:00} from {firstDayOfWeek}, every {step}: {difference}");
                        }
                    }
                }
            }
        }

        Assert.That(problems, Is.Empty);
    }

    private static string? Difference(RenderedMonth plain, RenderedMonth withStates, DateTime[] marked)
    {
        if (withStates.Lines.Count != plain.Lines.Count)
        {
            return "a different number of lines";
        }
        if (withStates.Lines[0].Text != plain.Lines[0].Text || withStates.Lines[1].Text != plain.Lines[1].Text)
        {
            return "the title or the labels changed";
        }
        for (int index = 2; index < plain.Lines.Count; index++)
        {
            RenderedLine before = plain.Lines[index];
            RenderedLine after = withStates.Lines[index];
            if (after.Text.Length != before.Text.Length || after.Text.EndsWith(' '))
            {
                return $"line {index} is <{after.Text}>";
            }
            if (after.Segments.Count != before.Segments.Count)
            {
                return $"line {index} has a different number of segments";
            }
            for (int position = 0; position < before.Segments.Count; position++)
            {
                RenderedSegment was = before.Segments[position];
                RenderedSegment now = after.Segments[position];
                bool isMarked = was.Date.HasValue && marked.Contains(was.Date.Value);
                if (now.Kind != was.Kind || now.Date != was.Date || now.DayOfWeek != was.DayOfWeek || now.Text.Length != was.Text.Length)
                {
                    return $"line {index}, segment {position} is {Rendered.Describe(now)}";
                }
                if (!isMarked && (now.Text != was.Text || now.State != null))
                {
                    return $"an unmarked segment changed: {Rendered.Describe(now)}";
                }
                if (isMarked && (now.State != Open || now.Text.Trim() != "*" + was.Text.Trim()))
                {
                    return $"a marked segment is {Rendered.Describe(now)}";
                }
            }
        }
        return null;
    }

    [Test]
    public void AMarkedLastDay_EndsItsLine()
    {
        RenderedMonth rendered = MonthRenderer.Render(2023, 5, ThreeWide, new[] { new DateTime(2023, 5, 31) }, Open);

        Assert.That(rendered.Lines[^1].Text, Is.EqualTo(" 29  30 *31"));
    }

    [Test]
    public void TheWholeMonthMarked()
    {
        IEnumerable<DateTime> everyDay = Enumerable.Range(1, 28).Select(day => new DateTime(2021, 2, day));

        Assert.That(Rendered.Lines(MonthRenderer.Render(2021, 2, ThreeWide, everyDay, Open)).Skip(2), Is.EqualTo(new[]
        {
            " *1  *2  *3  *4  *5  *6  *7",
            " *8  *9 *10 *11 *12 *13 *14",
            "*15 *16 *17 *18 *19 *20 *21",
            "*22 *23 *24 *25 *26 *27 *28",
        }));
    }

    [Test]
    public void MarkersWork_WithNoGapBetweenColumns_AndWithEveryFirstDayOfWeek()
    {
        MonthRenderOptions options = ThreeWide.WithColumnGap(0).WithFirstDayOfWeek(DayOfWeek.Sunday);

        Assert.That(MonthRenderer.Render(2023, 2, options, February(1, 14), Open).Lines[2].Text, Is.EqualTo("          *1  2  3  4"));
        Assert.That(MonthRenderer.Render(2023, 2, options, February(1, 14), Open).Lines[4].Text, Is.EqualTo(" 12 13*14 15 16 17 18"));
    }
}
