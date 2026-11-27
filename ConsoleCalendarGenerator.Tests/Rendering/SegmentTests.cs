using System.Collections;
using System.Reflection;
using ConsoleCalendarGenerator.Tests.Support;

namespace ConsoleCalendarGenerator.Tests.Rendering;

/// <summary>
/// The segments of a rendered month: which kinds appear, in which order, with which text, weekday and date, and
/// that nothing about them can be changed. Segments are described as <c>Kind[text] weekday date</c>.
/// </summary>
[TestFixture]
public class SegmentTests
{
    /// <summary>April 2023 starts on a Saturday, so its first week has five empty cells. Indented with "> ".</summary>
    private static RenderedMonth IndentedApril2023() =>
        MonthRenderer.Render(2023, 4, MonthRenderOptions.Default.WithIndent("> ").WithTitleSuffix(" *"));

    // ---- exact segments ----

    [Test]
    public void TheTitleLine_IsIndent_Padding_Title_AndSuffix()
    {
        Assert.That(Rendered.Describe(IndentedApril2023().Lines[0]), Is.EqualTo(new[]
        {
            "Indent[> ]",
            "Padding[    ]",
            "Title[2023 April]",
            "TitleSuffix[ *]",
        }));
    }

    [Test]
    public void TheWeekdayLabelsLine_IsIndent_AndSevenLabelCellsWithGapsBetween()
    {
        Assert.That(Rendered.Describe(IndentedApril2023().Lines[1]), Is.EqualTo(new[]
        {
            "Indent[> ]",
            "WeekdayLabel[Mo] Monday",
            "Gap[ ]",
            "WeekdayLabel[Tu] Tuesday",
            "Gap[ ]",
            "WeekdayLabel[We] Wednesday",
            "Gap[ ]",
            "WeekdayLabel[Th] Thursday",
            "Gap[ ]",
            "WeekdayLabel[Fr] Friday",
            "Gap[ ]",
            "WeekdayLabel[Sa] Saturday",
            "Gap[ ]",
            "WeekdayLabel[Su] Sunday",
        }));
    }

    [Test]
    public void TheFirstWeekLine_HasEmptyCellsBeforeThe1st()
    {
        Assert.That(Rendered.Describe(IndentedApril2023().Lines[2]), Is.EqualTo(new[]
        {
            "Indent[> ]",
            "EmptyDay[  ] Monday",
            "Gap[ ]",
            "EmptyDay[  ] Tuesday",
            "Gap[ ]",
            "EmptyDay[  ] Wednesday",
            "Gap[ ]",
            "EmptyDay[  ] Thursday",
            "Gap[ ]",
            "EmptyDay[  ] Friday",
            "Gap[ ]",
            "Day[ 1] Saturday 2023-04-01",
            "Gap[ ]",
            "Day[ 2] Sunday 2023-04-02",
        }));
    }

    [Test]
    public void AFullWeekLine_IsSevenDayCells()
    {
        Assert.That(Rendered.Describe(IndentedApril2023().Lines[4]), Is.EqualTo(new[]
        {
            "Indent[> ]",
            "Day[10] Monday 2023-04-10",
            "Gap[ ]",
            "Day[11] Tuesday 2023-04-11",
            "Gap[ ]",
            "Day[12] Wednesday 2023-04-12",
            "Gap[ ]",
            "Day[13] Thursday 2023-04-13",
            "Gap[ ]",
            "Day[14] Friday 2023-04-14",
            "Gap[ ]",
            "Day[15] Saturday 2023-04-15",
            "Gap[ ]",
            "Day[16] Sunday 2023-04-16",
        }));
    }

    /// <summary>The cells after the last day are not rendered at all, so the line ends with the last day.</summary>
    [Test]
    public void TheLastWeekLine_EndsWithTheLastDay_WithNoCellsAfterIt()
    {
        RenderedMonth may = MonthRenderer.Render(2023, 5, MonthRenderOptions.Default);

        Assert.That(Rendered.Describe(may.Lines[^1]), Is.EqualTo(new[]
        {
            "Day[29] Monday 2023-05-29",
            "Gap[ ]",
            "Day[30] Tuesday 2023-05-30",
            "Gap[ ]",
            "Day[31] Wednesday 2023-05-31",
        }));
    }

    [Test]
    public void WithoutAnIndent_AGap_OrSparePadding_ThoseSegmentsAreLeftOut()
    {
        // A 14-character grid and a 14-character title: nothing to pad, nothing between the cells, no indent.
        RenderedMonth rendered = MonthRenderer.Render(2023, 9, MonthRenderOptions.Default.WithColumnGap(0));

        Assert.That(Rendered.Describe(rendered.Lines[0]), Is.EqualTo(new[] { "Title[2023 September]" }));
        Assert.That(rendered.Lines[1].Text, Is.EqualTo("MoTuWeThFrSaSu"));
        Assert.That(rendered.Lines.SelectMany(line => line.Segments).Select(segment => segment.Kind).Distinct(), Is.EquivalentTo(new[]
        {
            RenderedSegmentKind.Title, RenderedSegmentKind.WeekdayLabel, RenderedSegmentKind.EmptyDay, RenderedSegmentKind.Day,
        }));
    }

    // ---- rules that hold for every segment ----

    private static IEnumerable<MonthRenderOptions> VariedOptions() => new[]
    {
        MonthRenderOptions.Default,
        MonthRenderOptions.Default.WithIndent("    ").WithCellWidth(3).WithTitleSuffix(" (* = open)").WithTitleAlignment(TitleAlignment.Left),
        MonthRenderOptions.Default.WithColumnGap(0).WithCellWidth(4).WithFirstDayOfWeek(DayOfWeek.Sunday),
        MonthRenderOptions.Default.WithColumnGap(3).WithWeekdayLabelStyle(WeekdayLabelStyle.Abbreviated).WithFirstDayOfWeek(DayOfWeek.Saturday),
        MonthRenderOptions.Default.WithWeekdayLabels(new[] { "S", "M", "T", "W", "T", "F", "S" }).WithIndent("\t"),
    };

    [Test]
    public void TheTextOfALine_IsExactlyItsSegmentsJoined()
    {
        foreach (MonthRenderOptions options in VariedOptions())
        {
            for (int month = 1; month <= 12; month++)
            {
                foreach (RenderedLine line in MonthRenderer.Render(2024, month, options).Lines)
                {
                    Assert.That(string.Concat(line.Segments.Select(segment => segment.Text)), Is.EqualTo(line.Text));
                    Assert.That(line.ToString(), Is.EqualTo(line.Text));
                }
            }
        }
    }

    [Test]
    public void NoSegment_IsEmpty_OrContainsALineBreak()
    {
        foreach (MonthRenderOptions options in VariedOptions())
        {
            for (int month = 1; month <= 12; month++)
            {
                foreach (RenderedSegment segment in MonthRenderer.Render(2024, month, options).Lines.SelectMany(line => line.Segments))
                {
                    Assert.That(segment.Text, Is.Not.Null.And.Not.Empty);
                    Assert.That(segment.Text.IndexOfAny(new[] { '\r', '\n' }), Is.EqualTo(-1));
                    Assert.That(segment.ToString(), Is.EqualTo(segment.Text));
                }
            }
        }
    }

    /// <summary>
    /// Which kinds carry a weekday and which carry a date: a day cell has both, an empty cell and a label cell
    /// have the weekday only, everything else has neither.
    /// </summary>
    [Test]
    public void OnlyCells_CarryAWeekday_AndOnlyDayCells_CarryADate()
    {
        foreach (MonthRenderOptions options in VariedOptions())
        {
            for (int month = 1; month <= 12; month++)
            {
                foreach (RenderedSegment segment in MonthRenderer.Render(2024, month, options).Lines.SelectMany(line => line.Segments))
                {
                    bool isCell = segment.Kind is RenderedSegmentKind.WeekdayLabel or RenderedSegmentKind.EmptyDay or RenderedSegmentKind.Day;

                    Assert.That(segment.DayOfWeek.HasValue, Is.EqualTo(isCell), Rendered.Describe(segment));
                    Assert.That(segment.Date.HasValue, Is.EqualTo(segment.Kind == RenderedSegmentKind.Day), Rendered.Describe(segment));
                    if (segment.Date is DateTime date)
                    {
                        Assert.That(date.DayOfWeek, Is.EqualTo(segment.DayOfWeek));
                        Assert.That((date.Year, date.Month), Is.EqualTo((2024, month)));
                        Assert.That(date.TimeOfDay, Is.EqualTo(TimeSpan.Zero));
                        Assert.That(date.Kind, Is.EqualTo(DateTimeKind.Unspecified));
                    }
                }
            }
        }
    }

    /// <summary>The day is in <see cref="RenderedSegment.Date"/>; nothing has to be read back out of the text.</summary>
    [Test]
    public void TheDaySegments_AreTheDaysOfTheMonth_InOrder_OnceEach()
    {
        foreach (MonthRenderOptions options in VariedOptions())
        {
            RenderedMonth rendered = MonthRenderer.Render(2024, 2, options);

            DateTime[] dates = rendered.Lines.SelectMany(line => line.Segments)
                .Where(segment => segment.Kind == RenderedSegmentKind.Day)
                .Select(segment => segment.Date!.Value)
                .ToArray();

            Assert.That(dates, Is.EqualTo(Enumerable.Range(1, 29).Select(day => new DateTime(2024, 2, day))));
        }
    }

    [Test]
    public void EveryCell_IsExactlyAsWideAsTheCellWidth_AndEveryGapAsWideAsTheColumnGap()
    {
        foreach (MonthRenderOptions options in VariedOptions())
        {
            foreach (RenderedSegment segment in MonthRenderer.Render(2024, 3, options).Lines.SelectMany(line => line.Segments))
            {
                if (segment.DayOfWeek.HasValue)
                {
                    Assert.That(segment.Text, Has.Length.EqualTo(options.CellWidth), Rendered.Describe(segment));
                }
                if (segment.Kind == RenderedSegmentKind.Gap)
                {
                    Assert.That(segment.Text, Is.EqualTo(new string(' ', options.ColumnGap)));
                }
            }
        }
    }

    // ---- immutability ----

    [Test]
    public void TheLinesAndTheSegments_CannotBeChanged_ThroughAnyListInterface()
    {
        RenderedMonth rendered = IndentedApril2023();
        string before = Rendered.Describe(rendered);
        RenderedMonth other = MonthRenderer.Render(2024, 2, MonthRenderOptions.Default);

        AssertRejectsEveryChange(rendered.Lines, other.Lines[0]);
        foreach (RenderedLine line in rendered.Lines)
        {
            AssertRejectsEveryChange(line.Segments, other.Lines[0].Segments[0]);
        }

        Assert.That(Rendered.Describe(rendered), Is.EqualTo(before));
    }

    [Test]
    public void TheCollections_AreNotArraysOrLists()
    {
        RenderedMonth rendered = IndentedApril2023();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(rendered.Lines, Is.Not.InstanceOf<Array>().And.Not.InstanceOf<List<RenderedLine>>());
            foreach (RenderedLine line in rendered.Lines)
            {
                Assert.That(line.Segments, Is.Not.InstanceOf<Array>().And.Not.InstanceOf<List<RenderedSegment>>());
            }
        }
    }

    [Test]
    public void TheRenderedTypes_AreSealed_WithReadOnlyPropertiesAndNoPublicConstructorsOrFields()
    {
        foreach (Type type in new[] { typeof(RenderedMonth), typeof(RenderedLine), typeof(RenderedSegment), typeof(MonthRenderOptions) })
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(type.IsSealed, Is.True, type.Name + " is sealed");
                Assert.That(type.GetConstructors(), Is.Empty, type.Name + " has no public constructor");
                Assert.That(type.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static), Is.Empty, type.Name + " has no public field");
                Assert.That(type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
                    .Where(property => property.CanWrite).Select(property => property.Name), Is.Empty, type.Name + " has no settable property");
            }
        }
    }

    /// <summary>
    /// Tries every way of changing the collection that the generic and the non-generic list interfaces offer.
    /// Whichever of them the collection implements must say it is read-only and refuse each change.
    /// </summary>
    private static void AssertRejectsEveryChange<T>(IReadOnlyList<T> collection, T item)
    {
        T[] before = collection.ToArray();

        if (collection is ICollection<T> generic)
        {
            Assert.That(generic.IsReadOnly, Is.True);
            Assert.That(() => generic.Add(item), Throws.TypeOf<NotSupportedException>());
            Assert.That(() => generic.Remove(item), Throws.TypeOf<NotSupportedException>());
            Assert.That(() => generic.Clear(), Throws.TypeOf<NotSupportedException>());
        }
        if (collection is IList<T> genericList)
        {
            Assert.That(() => genericList[0] = item, Throws.TypeOf<NotSupportedException>());
            Assert.That(() => genericList.Insert(0, item), Throws.TypeOf<NotSupportedException>());
            Assert.That(() => genericList.RemoveAt(0), Throws.TypeOf<NotSupportedException>());
        }
        if (collection is IList list)
        {
            Assert.That(list.IsReadOnly, Is.True);
            Assert.That(() => list[0] = item, Throws.TypeOf<NotSupportedException>());
            Assert.That(() => list.Add(item), Throws.TypeOf<NotSupportedException>());
            Assert.That(() => list.Clear(), Throws.TypeOf<NotSupportedException>());
        }

        Assert.That(collection, Is.EqualTo(before), "the collection still holds what it held");
    }
}
