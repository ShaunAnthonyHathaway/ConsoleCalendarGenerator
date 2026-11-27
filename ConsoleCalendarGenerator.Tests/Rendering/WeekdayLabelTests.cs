using System.Globalization;
using ConsoleCalendarGenerator.Tests.Support;

namespace ConsoleCalendarGenerator.Tests.Rendering;

/// <summary>
/// The labels of the weekday columns: the two built-in styles, labels supplied by the caller, and how a label
/// is fitted into a cell. Widths are counted in text elements, so a character outside the basic plane or a
/// letter with combining marks counts as one and is never split.
/// </summary>
[TestFixture]
public class WeekdayLabelTests
{
    /// <summary>Labels that say which weekday they belong to, in the required order: Sunday first.</summary>
    private static readonly string[] SundayFirst = { "Su0", "Mo1", "Tu2", "We3", "Th4", "Fr5", "Sa6" };

    private static string LabelLine(MonthRenderOptions options) => MonthRenderer.Render(2023, 5, options).Lines[1].Text;

    private static string[] LabelCells(MonthRenderOptions options) => MonthRenderer.Render(2023, 5, options).Lines[1].Segments
        .Where(segment => segment.Kind == RenderedSegmentKind.WeekdayLabel)
        .Select(segment => segment.Text)
        .ToArray();

    // Text that cannot be typed safely into every editor is built from code points.
    private static readonly string Smiley = char.ConvertFromUtf32(0x1F600);
    private static readonly string Star = char.ConvertFromUtf32(0x1F31F);
    private static readonly string EAcute = "e" + (char)0x0301;

    // ---- built-in styles ----

    [Test]
    public void TheShortestStyle_OfTheInvariantCulture_IsTwoLetters()
    {
        Assert.That(LabelLine(MonthRenderOptions.Default.WithWeekdayLabelStyle(WeekdayLabelStyle.Shortest)), Is.EqualTo("Mo Tu We Th Fr Sa Su"));
    }

    [Test]
    public void TheAbbreviatedStyle_OfTheInvariantCulture_IsThreeLetters()
    {
        MonthRenderOptions options = MonthRenderOptions.Default.WithWeekdayLabelStyle(WeekdayLabelStyle.Abbreviated).WithCellWidth(3);

        Assert.That(LabelLine(options), Is.EqualTo("Mon Tue Wed Thu Fri Sat Sun"));
    }

    [TestCase(DayOfWeek.Sunday, "Su Mo Tu We Th Fr Sa")]
    [TestCase(DayOfWeek.Monday, "Mo Tu We Th Fr Sa Su")]
    [TestCase(DayOfWeek.Tuesday, "Tu We Th Fr Sa Su Mo")]
    [TestCase(DayOfWeek.Wednesday, "We Th Fr Sa Su Mo Tu")]
    [TestCase(DayOfWeek.Thursday, "Th Fr Sa Su Mo Tu We")]
    [TestCase(DayOfWeek.Friday, "Fr Sa Su Mo Tu We Th")]
    [TestCase(DayOfWeek.Saturday, "Sa Su Mo Tu We Th Fr")]
    public void TheLabels_StartWithTheFirstDayOfWeek(DayOfWeek firstDayOfWeek, string expected)
    {
        Assert.That(LabelLine(MonthRenderOptions.Default.WithFirstDayOfWeek(firstDayOfWeek)), Is.EqualTo(expected));
    }

    [TestCase(-1)]
    [TestCase(2)]
    [TestCase(int.MaxValue)]
    public void AStyleThatIsNotDefined_IsRejected(int value)
    {
        var style = (WeekdayLabelStyle)value;

        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(
            () => MonthRenderOptions.Default.WithWeekdayLabelStyle(style))!;

        Assert.That(exception.ParamName, Is.EqualTo("weekdayLabelStyle"));
        Assert.That(exception.ActualValue, Is.EqualTo(style));
    }

    // ---- fitting a label into a cell ----

    [Test]
    public void ALabelShorterThanTheCell_IsRightAligned_LikeTheDays()
    {
        RenderedMonth rendered = MonthRenderer.Render(2023, 5, MonthRenderOptions.Default.WithCellWidth(4));

        Assert.That(rendered.Lines[1].Text, Is.EqualTo("  Mo   Tu   We   Th   Fr   Sa   Su"));
        Assert.That(rendered.Lines[2].Text, Is.EqualTo("   1    2    3    4    5    6    7"));
    }

    /// <summary>The grid keeps its shape whatever the culture's names are: a longer name is cut, never widened.</summary>
    [Test]
    public void ALabelLongerThanTheCell_IsCutToTheCellWidth()
    {
        MonthRenderOptions abbreviated = MonthRenderOptions.Default.WithWeekdayLabelStyle(WeekdayLabelStyle.Abbreviated);

        Assert.That(LabelLine(abbreviated), Is.EqualTo("Mo Tu We Th Fr Sa Su"), "Mon, Tue ... cut to two");
        Assert.That(LabelLine(MonthRenderOptions.Default.WithWeekdayLabels(SundayFirst)), Is.EqualTo("Mo Tu We Th Fr Sa Su"), "Mo1, Tu2 ... cut to two");
        Assert.That(LabelLine(MonthRenderOptions.Default.WithWeekdayLabels(SundayFirst).WithCellWidth(3)), Is.EqualTo("Mo1 Tu2 We3 Th4 Fr5 Sa6 Su0"));
    }

    [Test]
    public void ACharacterOutsideTheBasicPlane_CountsAsOne_AndIsNeverSplit()
    {
        // Each of these is two UTF-16 code units and one text element.
        string[] labels = { Smiley, Smiley + Star, Smiley + Star + Smiley, "a" + Smiley, Smiley + "a", "ab", Star };

        string[] cells = LabelCells(MonthRenderOptions.Default.WithWeekdayLabels(labels).WithFirstDayOfWeek(DayOfWeek.Sunday));

        Assert.That(cells, Is.EqualTo(new[]
        {
            " " + Smiley,
            Smiley + Star,
            Smiley + Star,
            "a" + Smiley,
            Smiley + "a",
            "ab",
            " " + Star,
        }));
        foreach (string cell in cells)
        {
            Assert.That(new StringInfo(cell).LengthInTextElements, Is.EqualTo(2), cell);
            Assert.That(char.IsHighSurrogate(cell[^1]), Is.False, "no cell ends in half a character");
        }
    }

    [Test]
    public void ALetterWithACombiningMark_CountsAsOne_AndKeepsItsMark()
    {
        string[] labels = { EAcute, EAcute + "x", EAcute + "xy", "x" + EAcute + "y", "xy" + EAcute, "abc", "ab" };

        string[] cells = LabelCells(MonthRenderOptions.Default.WithWeekdayLabels(labels).WithFirstDayOfWeek(DayOfWeek.Sunday));

        Assert.That(cells, Is.EqualTo(new[]
        {
            " " + EAcute,
            EAcute + "x",
            EAcute + "x",
            "x" + EAcute,
            "xy",
            "ab",
            "ab",
        }));
    }

    // ---- labels supplied by the caller ----

    [TestCase(DayOfWeek.Sunday, "Su0 Mo1 Tu2 We3 Th4 Fr5 Sa6")]
    [TestCase(DayOfWeek.Monday, "Mo1 Tu2 We3 Th4 Fr5 Sa6 Su0")]
    [TestCase(DayOfWeek.Wednesday, "We3 Th4 Fr5 Sa6 Su0 Mo1 Tu2")]
    [TestCase(DayOfWeek.Saturday, "Sa6 Su0 Mo1 Tu2 We3 Th4 Fr5")]
    public void SuppliedLabels_AreGivenSundayFirst_AndShownFromTheFirstDayOfWeek(DayOfWeek firstDayOfWeek, string expected)
    {
        MonthRenderOptions options = MonthRenderOptions.Default.WithWeekdayLabels(SundayFirst).WithCellWidth(3);

        Assert.That(LabelLine(options.WithFirstDayOfWeek(firstDayOfWeek)), Is.EqualTo(expected));
        // The order of the two calls makes no difference.
        Assert.That(LabelLine(MonthRenderOptions.Default.WithFirstDayOfWeek(firstDayOfWeek).WithCellWidth(3).WithWeekdayLabels(SundayFirst)), Is.EqualTo(expected));
    }

    [Test]
    public void EachSuppliedLabel_IsOnTheCellOfItsOwnWeekday()
    {
        foreach (DayOfWeek firstDayOfWeek in ReferenceCalendar.AllDaysOfWeek)
        {
            RenderedMonth rendered = MonthRenderer.Render(2023, 5,
                MonthRenderOptions.Default.WithWeekdayLabels(SundayFirst).WithCellWidth(3).WithFirstDayOfWeek(firstDayOfWeek));

            foreach (RenderedSegment label in rendered.Lines[1].Segments.Where(segment => segment.Kind == RenderedSegmentKind.WeekdayLabel))
            {
                Assert.That(label.Text, Is.EqualTo(SundayFirst[(int)label.DayOfWeek!.Value]));
            }
        }
    }

    [Test]
    public void TheOptions_ExposeTheSuppliedLabels_ReadOnly()
    {
        MonthRenderOptions options = MonthRenderOptions.Default.WithWeekdayLabels(SundayFirst);

        Assert.That(options.WeekdayLabels, Is.EqualTo(SundayFirst));
        Assert.That(options.WeekdayLabels, Is.Not.SameAs(SundayFirst).And.Not.InstanceOf<Array>());
        Assert.That(() => ((IList<string>)options.WeekdayLabels!)[0] = "changed", Throws.TypeOf<NotSupportedException>());
        Assert.That(((ICollection<string>)options.WeekdayLabels!).IsReadOnly, Is.True);
    }

    [Test]
    public void ChangingTheSuppliedCollectionAfterwards_DoesNotChangeTheOptions()
    {
        string[] array = (string[])SundayFirst.Clone();
        var list = new List<string>(SundayFirst);
        MonthRenderOptions fromArray = MonthRenderOptions.Default.WithWeekdayLabels(array).WithCellWidth(3);
        MonthRenderOptions fromList = MonthRenderOptions.Default.WithWeekdayLabels(list).WithCellWidth(3);

        array[1] = "XXX";
        list[1] = "XXX";
        list.Add("extra");

        Assert.That(LabelLine(fromArray), Is.EqualTo("Mo1 Tu2 We3 Th4 Fr5 Sa6 Su0"));
        Assert.That(LabelLine(fromList), Is.EqualTo("Mo1 Tu2 We3 Th4 Fr5 Sa6 Su0"));
        Assert.That(fromArray.WeekdayLabels, Is.EqualTo(SundayFirst));
    }

    /// <summary>A lazily evaluated sequence is read exactly once, when the options are created.</summary>
    [Test]
    public void ASequence_IsEnumeratedOnce()
    {
        int enumerations = 0;
        IEnumerable<string> Sequence()
        {
            enumerations++;
            foreach (string label in SundayFirst)
            {
                yield return label;
            }
        }

        MonthRenderOptions options = MonthRenderOptions.Default.WithWeekdayLabels(Sequence());
        _ = MonthRenderer.Render(2023, 5, options);
        _ = MonthRenderer.Render(2023, 6, options);

        Assert.That(enumerations, Is.EqualTo(1));
    }

    [Test]
    public void TheLastCallWins_BetweenAStyleAndSuppliedLabels()
    {
        MonthRenderOptions supplied = MonthRenderOptions.Default.WithCellWidth(3)
            .WithWeekdayLabelStyle(WeekdayLabelStyle.Abbreviated).WithWeekdayLabels(SundayFirst);
        MonthRenderOptions styled = supplied.WithWeekdayLabelStyle(WeekdayLabelStyle.Abbreviated);

        Assert.That(LabelLine(supplied), Is.EqualTo("Mo1 Tu2 We3 Th4 Fr5 Sa6 Su0"));
        Assert.That(supplied.WeekdayLabelStyle, Is.EqualTo(WeekdayLabelStyle.Abbreviated), "kept, but not used while labels are supplied");
        Assert.That(LabelLine(styled), Is.EqualTo("Mon Tue Wed Thu Fri Sat Sun"));
        Assert.That(styled.WeekdayLabels, Is.Null);
    }

    [Test]
    public void ALabelOfSpaces_IsAllowed_AndKeptAsText()
    {
        string[] labels = { "Su", "  ", "Tu", "We", "Th", "Fr", "Sa" };

        RenderedMonth rendered = MonthRenderer.Render(2023, 5, MonthRenderOptions.Default.WithWeekdayLabels(labels).WithFirstDayOfWeek(DayOfWeek.Tuesday));

        Assert.That(rendered.Lines[1].Text, Is.EqualTo("Tu We Th Fr Sa Su   "), "the last label is the caller's two spaces");
        Assert.That(rendered.Lines[1].Segments[^1].Kind, Is.EqualTo(RenderedSegmentKind.WeekdayLabel));
    }

    // ---- invalid labels ----

    [Test]
    public void ANullCollection_IsRejected()
    {
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => MonthRenderOptions.Default.WithWeekdayLabels(null!))!;

        Assert.That(exception.ParamName, Is.EqualTo("weekdayLabels"));
    }

    [TestCase(0)]
    [TestCase(1)]
    [TestCase(6)]
    [TestCase(8)]
    [TestCase(14)]
    public void ACollectionThatDoesNotHoldExactlySevenLabels_IsRejected(int count)
    {
        AssertRejected(Enumerable.Repeat("x", count).ToArray());
    }

    [Test]
    public void ANullLabel_IsRejected()
    {
        AssertRejected(new[] { "Su", "Mo", null!, "We", "Th", "Fr", "Sa" });
    }

    [Test]
    public void AnEmptyLabel_IsRejected()
    {
        AssertRejected(new[] { "Su", "Mo", "Tu", string.Empty, "Th", "Fr", "Sa" });
    }

    /// <summary>
    /// Line feed, carriage return, form feed, next line (U+0085), line separator (U+2028) and paragraph
    /// separator (U+2029): the characters .NET treats as the end of a line.
    /// </summary>
    [TestCase(0x000A)]
    [TestCase(0x000D)]
    [TestCase(0x000C)]
    [TestCase(0x0085)]
    [TestCase(0x2028)]
    [TestCase(0x2029)]
    public void ALabelWithALineBreak_IsRejected(int codePoint)
    {
        AssertRejected(new[] { "Su", "Mo", "Tu", "We", "T" + (char)codePoint + "h", "Fr", "Sa" });
        AssertRejected(new[] { "Su", "Mo", "Tu", "We", "Th", "Fr", ((char)codePoint).ToString() });
    }

    private static void AssertRejected(string[] labels)
    {
        // Exactly ArgumentException: not ArgumentNullException, not ArgumentOutOfRangeException.
        ArgumentException exception = Assert.Throws<ArgumentException>(() => MonthRenderOptions.Default.WithWeekdayLabels(labels))!;

        Assert.That(exception.ParamName, Is.EqualTo("weekdayLabels"));
    }
}
