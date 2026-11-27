using System.Reflection;
using ConsoleCalendarGenerator.Tests.Support;

namespace ConsoleCalendarGenerator.Tests.Themes;

/// <summary>
/// A theme as a plain function from a segment to an optional color, and as an immutable value. Nothing here
/// writes anything or touches the console.
/// </summary>
[TestFixture]
public class CalendarThemeTests
{
    private static readonly DayState Open = new("open", "*");
    private static readonly DayState Closed = new("closed", "x");

    /// <summary>February 2023 with the 14th open and the 15th closed, an indent, and a title suffix: every kind of segment.</summary>
    private static RenderedMonth Month() => MonthRenderer.Render(
        2023, 2,
        MonthRenderOptions.Default.WithCellWidth(3).WithIndent("  ").WithTitleSuffix(" (legend)").WithTitleAlignment(TitleAlignment.Center).WithColumnGap(2),
        new[]
        {
            new KeyValuePair<DateTime, DayState>(new DateTime(2023, 2, 14), Open),
            new KeyValuePair<DateTime, DayState>(new DateTime(2023, 2, 15), Closed),
        });

    private static IEnumerable<RenderedSegment> Segments() => Month().Lines.SelectMany(line => line.Segments);

    private static RenderedSegment The(int day) => Segments().Single(segment => segment.Date == new DateTime(2023, 2, day));

    private static readonly CalendarTheme Full = CalendarTheme.None
        .WithTitleColor(ConsoleColor.Yellow)
        .WithTitleSuffixColor(ConsoleColor.DarkGray)
        .WithWeekdayLabelColor(ConsoleColor.Cyan)
        .WithDayColor(ConsoleColor.White)
        .WithStateColor("open", ConsoleColor.Green);

    // ---- no colors ----

    [Test]
    public void TheThemeWithNoColors_HasNone()
    {
        CalendarTheme none = CalendarTheme.None;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(none.TitleColor, Is.Null);
            Assert.That(none.TitleSuffixColor, Is.Null);
            Assert.That(none.WeekdayLabelColor, Is.Null);
            Assert.That(none.DayColor, Is.Null);
            Assert.That(none.StateColors, Is.Empty);
            Assert.That(CalendarTheme.None, Is.SameAs(none));
            Assert.That(Segments().Select(none.GetColor), Is.All.Null);
        }
    }

    [Test]
    public void TheMonthUsedHere_HasEveryKindOfSegment()
    {
        Assert.That(Segments().Select(segment => segment.Kind).Distinct(), Is.EquivalentTo(Enum.GetValues<RenderedSegmentKind>()));
    }

    // ---- what each color applies to ----

    [Test]
    public void EachKindOfSegment_GetsItsOwnColor()
    {
        var expected = new Dictionary<RenderedSegmentKind, ConsoleColor?>
        {
            [RenderedSegmentKind.Indent] = null,
            [RenderedSegmentKind.Padding] = null,
            [RenderedSegmentKind.Title] = ConsoleColor.Yellow,
            [RenderedSegmentKind.TitleSuffix] = ConsoleColor.DarkGray,
            [RenderedSegmentKind.WeekdayLabel] = ConsoleColor.Cyan,
            [RenderedSegmentKind.Gap] = null,
            [RenderedSegmentKind.EmptyDay] = null,
        };

        foreach (RenderedSegment segment in Segments().Where(segment => segment.Kind != RenderedSegmentKind.Day))
        {
            Assert.That(Full.GetColor(segment), Is.EqualTo(expected[segment.Kind]), Rendered.Describe(segment));
        }
    }

    [Test]
    public void ADayWithoutAState_GetsTheDayColor()
    {
        Assert.That(Full.GetColor(The(13)), Is.EqualTo(ConsoleColor.White));
        Assert.That(CalendarTheme.None.WithStateColor("open", ConsoleColor.Green).GetColor(The(13)), Is.Null);
    }

    [Test]
    public void ADayInAStateWithAColor_GetsThatColor()
    {
        Assert.That(Full.GetColor(The(14)), Is.EqualTo(ConsoleColor.Green));
    }

    /// <summary>The 15th is closed, and the theme has no color for "closed".</summary>
    [Test]
    public void ADayInAStateWithoutAColor_FallsBackToTheDayColor()
    {
        Assert.That(Full.GetColor(The(15)), Is.EqualTo(ConsoleColor.White));
        Assert.That(Full.WithDayColor(null).GetColor(The(15)), Is.Null);
    }

    [Test]
    public void TheTitleSuffix_DoesNotTakeTheColorOfTheTitle()
    {
        CalendarTheme titleOnly = CalendarTheme.None.WithTitleColor(ConsoleColor.Red);

        Assert.That(titleOnly.GetColor(Segments().Single(segment => segment.Kind == RenderedSegmentKind.Title)), Is.EqualTo(ConsoleColor.Red));
        Assert.That(titleOnly.GetColor(Segments().Single(segment => segment.Kind == RenderedSegmentKind.TitleSuffix)), Is.Null);
    }

    [Test]
    public void EachRoleColor_SetsOnlyItsOwnProperty()
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(Describe(CalendarTheme.None.WithTitleColor(ConsoleColor.Red)), Is.EqualTo("Red|-|-|-|"));
            Assert.That(Describe(CalendarTheme.None.WithTitleSuffixColor(ConsoleColor.Red)), Is.EqualTo("-|Red|-|-|"));
            Assert.That(Describe(CalendarTheme.None.WithWeekdayLabelColor(ConsoleColor.Red)), Is.EqualTo("-|-|Red|-|"));
            Assert.That(Describe(CalendarTheme.None.WithDayColor(ConsoleColor.Red)), Is.EqualTo("-|-|-|Red|"));
            Assert.That(Describe(CalendarTheme.None.WithStateColor("open", ConsoleColor.Red)), Is.EqualTo("-|-|-|-|open=Red"));
            Assert.That(Describe(Full), Is.EqualTo("Yellow|DarkGray|Cyan|White|open=Green"));
        }
    }

    private static string Describe(CalendarTheme theme) => string.Join("|",
        theme.TitleColor?.ToString() ?? "-", theme.TitleSuffixColor?.ToString() ?? "-", theme.WeekdayLabelColor?.ToString() ?? "-",
        theme.DayColor?.ToString() ?? "-",
        string.Join(",", theme.StateColors.OrderBy(entry => entry.Key, StringComparer.Ordinal).Select(entry => entry.Key + "=" + entry.Value)));

    // ---- states are found by name ----

    [Test]
    public void AStateColor_AppliesToEveryStateOfThatName_WhicheverInstanceItIs()
    {
        var another = new DayState("open", "*");
        RenderedMonth rendered = MonthRenderer.Render(2023, 2, MonthRenderOptions.Default.WithCellWidth(3), new[] { new DateTime(2023, 2, 1) }, another);

        RenderedSegment first = rendered.Lines.SelectMany(line => line.Segments).Single(segment => segment.State != null);

        Assert.That(first.State, Is.Not.SameAs(Open));
        Assert.That(Full.GetColor(first), Is.EqualTo(ConsoleColor.Green));
    }

    /// <summary>Names are compared character by character: "Open" is not "open".</summary>
    [TestCase("Open")]
    [TestCase("OPEN")]
    [TestCase("open ")]
    [TestCase(" open")]
    public void StateNames_AreCaseSensitive_AndExact(string otherName)
    {
        CalendarTheme theme = CalendarTheme.None.WithDayColor(ConsoleColor.White).WithStateColor(otherName, ConsoleColor.Red);

        Assert.That(theme.GetColor(The(14)), Is.EqualTo(ConsoleColor.White), "the open day does not get the color of '" + otherName + "'");
        Assert.That(theme.StateColors.ContainsKey("open"), Is.False);
        Assert.That(theme.StateColors[otherName], Is.EqualTo(ConsoleColor.Red));
    }

    [Test]
    public void TwoNamesThatDifferOnlyInCase_AreTwoEntries()
    {
        CalendarTheme theme = CalendarTheme.None.WithStateColor("open", ConsoleColor.Green).WithStateColor("Open", ConsoleColor.Red);

        Assert.That(Describe(theme), Is.EqualTo("-|-|-|-|Open=Red,open=Green"));
    }

    /// <summary>A theme can name states that a month does not use; they make no difference.</summary>
    [Test]
    public void AColorForAStateThatIsNotInTheMonth_IsHarmless()
    {
        CalendarTheme theme = Full.WithStateColor("never used", ConsoleColor.Magenta);

        Assert.That(Segments().Select(theme.GetColor), Is.EqualTo(Segments().Select(Full.GetColor)));
    }

    [Test]
    public void SettingAStateColorAgain_ReplacesIt()
    {
        CalendarTheme theme = Full.WithStateColor("open", ConsoleColor.Red);

        Assert.That(theme.StateColors, Has.Count.EqualTo(1));
        Assert.That(theme.GetColor(The(14)), Is.EqualTo(ConsoleColor.Red));
        Assert.That(Full.GetColor(The(14)), Is.EqualTo(ConsoleColor.Green), "the theme it was made from is unchanged");
    }

    [Test]
    public void SettingAStateColorToNull_RemovesIt()
    {
        CalendarTheme theme = Full.WithStateColor("closed", ConsoleColor.Red).WithStateColor("open", null);

        Assert.That(Describe(theme), Is.EqualTo("Yellow|DarkGray|Cyan|White|closed=Red"));
        Assert.That(theme.GetColor(The(14)), Is.EqualTo(ConsoleColor.White));
        Assert.That(Describe(theme.WithStateColor("not there", null)), Is.EqualTo(Describe(theme)), "removing a name that has no color changes nothing");
    }

    // ---- colors that are accepted and refused ----

    [Test]
    public void EveryConsoleColor_IsAccepted([Values] ConsoleColor color)
    {
        CalendarTheme theme = CalendarTheme.None.WithTitleColor(color).WithTitleSuffixColor(color).WithWeekdayLabelColor(color)
            .WithDayColor(color).WithStateColor("open", color);

        Assert.That(Describe(theme), Is.EqualTo($"{color}|{color}|{color}|{color}|open={color}"));
    }

    [Test]
    public void ANullColor_ClearsARole()
    {
        CalendarTheme cleared = Full.WithTitleColor(null).WithTitleSuffixColor(null).WithWeekdayLabelColor(null).WithDayColor(null);

        Assert.That(Describe(cleared), Is.EqualTo("-|-|-|-|open=Green"));
    }

    /// <summary>A value that is not a console color is refused when the theme is built, not later when it is used.</summary>
    [TestCase(-1)]
    [TestCase(16)]
    [TestCase(int.MaxValue)]
    [TestCase(int.MinValue)]
    public void AnUndefinedColor_IsRejected_ByEveryMethod(int value)
    {
        var color = (ConsoleColor)value;

        foreach (Func<CalendarTheme> attempt in new Func<CalendarTheme>[]
        {
            () => CalendarTheme.None.WithTitleColor(color),
            () => CalendarTheme.None.WithTitleSuffixColor(color),
            () => CalendarTheme.None.WithWeekdayLabelColor(color),
            () => CalendarTheme.None.WithDayColor(color),
            () => CalendarTheme.None.WithStateColor("open", color),
        })
        {
            ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => attempt())!;

            Assert.That(exception.ParamName, Is.EqualTo("color"));
            Assert.That(exception.ActualValue, Is.EqualTo(color));
        }
    }

    // ---- state names that are refused ----

    [Test]
    public void ANullStateName_IsRejected()
    {
        Assert.That(Assert.Throws<ArgumentNullException>(() => CalendarTheme.None.WithStateColor(null!, ConsoleColor.Red))!.ParamName, Is.EqualTo("stateName"));
    }

    /// <summary>The same rules as for the name of a state, so that a theme cannot hold a name no state can have.</summary>
    [TestCase("")]
    [TestCase(" ")]
    [TestCase("\t")]
    [TestCase("two\nlines")]
    [TestCase("\r")]
    public void AStateNameThatNoStateCanHave_IsRejected(string name)
    {
        Assert.That(Assert.Throws<ArgumentException>(() => CalendarTheme.None.WithStateColor(name, ConsoleColor.Red))!.ParamName, Is.EqualTo("stateName"));
        Assert.That(Assert.Throws<ArgumentException>(() => CalendarTheme.None.WithStateColor(name, null))!.ParamName, Is.EqualTo("stateName"));
    }

    [Test]
    public void TheStateName_IsCheckedBeforeTheColor()
    {
        Assert.That(Assert.Throws<ArgumentNullException>(() => CalendarTheme.None.WithStateColor(null!, (ConsoleColor)99))!.ParamName, Is.EqualTo("stateName"));
    }

    [Test]
    public void ANullSegment_IsRejected()
    {
        Assert.That(Assert.Throws<ArgumentNullException>(() => Full.GetColor(null!))!.ParamName, Is.EqualTo("segment"));
    }

    // ---- immutability ----

    [Test]
    public void EveryChange_ReturnsANewTheme_AndLeavesTheOriginalAsItWas()
    {
        string before = Describe(Full);

        CalendarTheme[] changed =
        {
            Full.WithTitleColor(ConsoleColor.Blue), Full.WithTitleSuffixColor(ConsoleColor.Blue), Full.WithWeekdayLabelColor(ConsoleColor.Blue),
            Full.WithDayColor(ConsoleColor.Blue), Full.WithStateColor("open", ConsoleColor.Blue), Full.WithStateColor("new", ConsoleColor.Blue),
            Full.WithStateColor("open", null),
        };

        Assert.That(changed, Has.None.SameAs(Full));
        Assert.That(changed.Select(Describe), Has.None.EqualTo(before));
        Assert.That(Describe(Full), Is.EqualTo(before));
        Assert.That(Describe(CalendarTheme.None), Is.EqualTo("-|-|-|-|"));
    }

    [Test]
    public void TheStateColors_CannotBeChanged_ThroughTheDictionaryInterfaces()
    {
        IReadOnlyDictionary<string, ConsoleColor> colors = Full.StateColors;

        Assert.That(colors, Is.Not.InstanceOf<Dictionary<string, ConsoleColor>>());
        if (colors is IDictionary<string, ConsoleColor> dictionary)
        {
            Assert.That(dictionary.IsReadOnly, Is.True);
            Assert.That(() => dictionary["open"] = ConsoleColor.Red, Throws.TypeOf<NotSupportedException>());
            Assert.That(() => dictionary.Add("new", ConsoleColor.Red), Throws.TypeOf<NotSupportedException>());
            Assert.That(() => dictionary.Remove("open"), Throws.TypeOf<NotSupportedException>());
            Assert.That(() => dictionary.Clear(), Throws.TypeOf<NotSupportedException>());
        }
        Assert.That(Describe(Full), Is.EqualTo("Yellow|DarkGray|Cyan|White|open=Green"));
    }

    [Test]
    public void TheStateColorsOfOneTheme_AreNotSharedWithAThemeMadeFromIt()
    {
        IReadOnlyDictionary<string, ConsoleColor> before = Full.StateColors;

        CalendarTheme derived = Full.WithStateColor("closed", ConsoleColor.Red);

        Assert.That(before, Has.Count.EqualTo(1));
        Assert.That(Full.StateColors, Is.SameAs(before));
        Assert.That(derived.StateColors, Has.Count.EqualTo(2));
    }

    [Test]
    public void ATheme_IsSealed_WithReadOnlyPropertiesAndNoPublicConstructorsOrFields()
    {
        Type type = typeof(CalendarTheme);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(type.IsSealed, Is.True);
            Assert.That(type.GetConstructors(), Is.Empty);
            Assert.That(type.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static), Is.Empty);
            Assert.That(type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static).Where(property => property.CanWrite), Is.Empty);
        }
    }

    // ---- a theme changes nothing else ----

    [Test]
    public void AskingAThemeForColors_DoesNotChangeTheRenderedMonth()
    {
        RenderedMonth month = Month();
        string before = Rendered.Describe(month);

        foreach (RenderedSegment segment in month.Lines.SelectMany(line => line.Segments))
        {
            _ = Full.GetColor(segment);
        }

        Assert.That(Rendered.Describe(month), Is.EqualTo(before));
        Assert.That(month.ToText(), Ordinal.DoesNotContain(((char)27).ToString()), "no escape sequence is ever part of the text");
    }

    [Test]
    public void TheSameSegment_AlwaysGetsTheSameColor_FromAnyThread()
    {
        RenderedSegment[] segments = Segments().ToArray();
        ConsoleColor?[] expected = segments.Select(Full.GetColor).ToArray();
        int wrong = 0;

        Parallel.For(0, 4000, new ParallelOptions { MaxDegreeOfParallelism = 8 }, index =>
        {
            if (Full.GetColor(segments[index % segments.Length]) != expected[index % segments.Length])
            {
                Interlocked.Increment(ref wrong);
            }
        });

        Assert.That(wrong, Is.Zero);
    }
}
