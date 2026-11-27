using CalendarGenerator;
using ConsoleCalendarGenerator.Tests.Support;

namespace ConsoleCalendarGenerator.Tests.Legacy;

/// <summary>
/// Which color each constructor argument controls: the one it is named for. This is the behavior of version
/// 2.0, the first of its corrections (D-57). An element that has no argument keeps its default: a yellow title,
/// a cyan weekday row, white days and blue selected days.
///
/// Version 1.0.0 did otherwise: the argument named TitleColor colored the weekday row, the title stayed
/// yellow, and with four arguments DayOfWeekColor colored the title. What 1.0.0 did is recorded, unchanged,
/// against its frozen source in Historical/Published100ConstructorTests.cs, and the two are held side by side
/// in Migration/ConstructorColorMigrationTests.cs.
/// </summary>
[TestFixture]
public class ConstructorColorTests
{
    private const ConsoleColor TitleArgument = ConsoleColor.Red;
    private const ConsoleColor DateArgument = ConsoleColor.Green;
    private const ConsoleColor SelectedDateArgument = ConsoleColor.Magenta;
    private const ConsoleColor DayOfWeekArgument = ConsoleColor.DarkYellow;

    private static readonly List<DateTime> Fourteenth = new() { new DateTime(2023, 2, 14) };

    // ---- the colors a calendar holds ----

    [Test]
    public void DefaultConstructor_UsesYellowTitle_CyanWeekdays_WhiteDates_BlueSelection()
    {
        AssertColors(new ConsoleCalendar(),
            title: ConsoleColor.Yellow, weekdays: ConsoleColor.Cyan, dates: ConsoleColor.White, selected: ConsoleColor.Blue);
    }

    [Test]
    public void OneArgument_TitleColorColorsTheTitle_AndEverythingElseKeepsItsDefault()
    {
        AssertColors(new ConsoleCalendar(TitleArgument),
            title: TitleArgument, weekdays: ConsoleColor.Cyan, dates: ConsoleColor.White, selected: ConsoleColor.Blue);
    }

    [Test]
    public void TwoArguments_TitleColorColorsTheTitle_DateColorTheDates_AndTheRestKeepTheirDefaults()
    {
        AssertColors(new ConsoleCalendar(TitleArgument, DateArgument),
            title: TitleArgument, weekdays: ConsoleColor.Cyan, dates: DateArgument, selected: ConsoleColor.Blue);
    }

    [Test]
    public void ThreeArguments_ColorTheTitle_TheDatesAndTheSelectedDates_AndTheWeekdayRowStaysCyan()
    {
        AssertColors(new ConsoleCalendar(TitleArgument, DateArgument, SelectedDateArgument),
            title: TitleArgument, weekdays: ConsoleColor.Cyan, dates: DateArgument, selected: SelectedDateArgument);
    }

    [Test]
    public void FourArguments_EachColorTheElementTheyAreNamedFor()
    {
        AssertColors(new ConsoleCalendar(TitleArgument, DateArgument, SelectedDateArgument, DayOfWeekArgument),
            title: TitleArgument, weekdays: DayOfWeekArgument, dates: DateArgument, selected: SelectedDateArgument);
    }

    /// <summary>The same with the arguments passed by name, in another order: the name decides, not the position.</summary>
    [Test]
    public void NamedArguments_ColorTheElementOfTheirName()
    {
        AssertColors(
            new ConsoleCalendar(DayOfWeekColor: DayOfWeekArgument, SelectedDateColor: SelectedDateArgument, DateColor: DateArgument, TitleColor: TitleArgument),
            title: TitleArgument, weekdays: DayOfWeekArgument, dates: DateArgument, selected: SelectedDateArgument);
    }

    /// <summary>A constructor does what the parameterless constructor and the setters of the same names do.</summary>
    [Test]
    public void EveryConstructor_HoldsTheColorsThatTheSettersOfTheSameNamesWouldSet()
    {
        var bySetters = new ConsoleCalendar();
        bySetters.SetTitleColor(TitleArgument);
        AssertSameColors(new ConsoleCalendar(TitleArgument), bySetters);
        bySetters.SetDateColor(DateArgument);
        AssertSameColors(new ConsoleCalendar(TitleArgument, DateArgument), bySetters);
        bySetters.SetSelectedDateColor(SelectedDateArgument);
        AssertSameColors(new ConsoleCalendar(TitleArgument, DateArgument, SelectedDateArgument), bySetters);
        bySetters.SetDayOfWeekColor(DayOfWeekArgument);
        AssertSameColors(new ConsoleCalendar(TitleArgument, DateArgument, SelectedDateArgument, DayOfWeekArgument), bySetters);
    }

    // ---- what is drawn in which color ----

    [Test]
    [Platform(Exclude = "Win", Reason = ConsoleState.ColorsObservableOnUnixOnly)]
    public void FourArguments_TheTitleIsDrawnInTheTitleArgument_AndTheWeekdayRowInTheDayOfWeekArgument()
    {
        var calendar = new ConsoleCalendar(TitleArgument, DateArgument, SelectedDateArgument, DayOfWeekArgument);

        Captured captured = ConsoleCapture.Run(() => calendar.GenerateMonth(2, 2023, Fourteenth));

        Assert.That(captured.ForegroundRuns, Is.EqualTo(new[]
        {
            (TitleArgument, "   February 2023\n"),
            (DayOfWeekArgument, "Mo Tu We Th Fr Sa Su\n"),
            (DateArgument, "       1  2  3  4  5\n 6  7  8  9 10 11 12\n13"),
            (SelectedDateArgument, " 14"),
            (DateArgument, " 15 16 17 18 19\n20 21 22 23 24 25 26\n27 28\n\n"),
        }));
    }

    [Test]
    [Platform(Exclude = "Win", Reason = ConsoleState.ColorsObservableOnUnixOnly)]
    public void ThreeArguments_TheTitleIsDrawnInTheTitleArgument_AndTheWeekdayRowInCyan()
    {
        var calendar = new ConsoleCalendar(TitleArgument, DateArgument, SelectedDateArgument);

        Captured captured = ConsoleCapture.Run(() => calendar.GenerateMonth(2, 2023, Fourteenth));

        Assert.That(captured.ForegroundRuns, Is.EqualTo(new[]
        {
            (TitleArgument, "   February 2023\n"),
            (ConsoleColor.Cyan, "Mo Tu We Th Fr Sa Su\n"),
            (DateArgument, "       1  2  3  4  5\n 6  7  8  9 10 11 12\n13"),
            (SelectedDateArgument, " 14"),
            (DateArgument, " 15 16 17 18 19\n20 21 22 23 24 25 26\n27 28\n\n"),
        }));
    }

    [Test]
    [Platform(Exclude = "Win", Reason = ConsoleState.ColorsObservableOnUnixOnly)]
    public void OneArgument_TheTitleIsDrawnInTheTitleArgument_AndTheRestInTheDefaultColors()
    {
        Captured captured = ConsoleCapture.Run(() => new ConsoleCalendar(TitleArgument).GenerateMonth(2, 2023, Fourteenth));

        Assert.That(captured.ForegroundRuns, Is.EqualTo(new[]
        {
            (TitleArgument, "   February 2023\n"),
            (ConsoleColor.Cyan, "Mo Tu We Th Fr Sa Su\n"),
            (ConsoleColor.White, "       1  2  3  4  5\n 6  7  8  9 10 11 12\n13"),
            (ConsoleColor.Blue, " 14"),
            (ConsoleColor.White, " 15 16 17 18 19\n20 21 22 23 24 25 26\n27 28\n\n"),
        }));
    }

    /// <summary>The constructor only decides colors: the text and the single writes are those of any calendar.</summary>
    [Test]
    public void TheConstructorUsed_ChangesNoTextAndNoWrite()
    {
        string Transcript(ConsoleCalendar calendar) => ConsoleCapture.Run(() => calendar.GenerateMonth(2, 2023, Fourteenth)).Transcript;
        string plain = Transcript(new ConsoleCalendar());

        Assert.That(plain, Does.StartWith("[   February 2023]<NL>[Mo Tu We Th Fr Sa Su]<NL>").Using(StringComparison.Ordinal));
        Assert.That(Transcript(new ConsoleCalendar(TitleArgument)), Is.EqualTo(plain));
        Assert.That(Transcript(new ConsoleCalendar(TitleArgument, DateArgument)), Is.EqualTo(plain));
        Assert.That(Transcript(new ConsoleCalendar(TitleArgument, DateArgument, SelectedDateArgument)), Is.EqualTo(plain));
        Assert.That(Transcript(new ConsoleCalendar(TitleArgument, DateArgument, SelectedDateArgument, DayOfWeekArgument)), Is.EqualTo(plain));
    }

    // ---- an argument that is not a defined color ----

    // A constructor accepts any value and checks nothing. The value fails when the console is first asked to use
    // it, so the element an argument colors decides how much is written before the failure. With the correction
    // of the constructors that element changed for the first and the fourth argument, and the failure with it.

    private static readonly ConsoleColor Undefined = (ConsoleColor)99;

    private const string Title = "   February 2023\n";
    private const string WeekdayRow = "Mo Tu We Th Fr Sa Su\n";
    private const string DaysBeforeThe14th = "       1  2  3  4  5\n 6  7  8  9 10 11 12\n13";

    private static Captured Draw(ConsoleCalendar calendar) => ConsoleCapture.TryRun(() => calendar.GenerateMonth(2, 2023, Fourteenth));

    [Test]
    public void NoConstructor_RejectsAValueThatIsNotAColor()
    {
        Assert.That(new ConsoleCalendar(Undefined).GetTitleColor(), Is.EqualTo(Undefined));
        Assert.That(new ConsoleCalendar(TitleArgument, Undefined).GetDateColor(), Is.EqualTo(Undefined));
        Assert.That(new ConsoleCalendar(TitleArgument, DateArgument, Undefined).GetSelectedDateColor(), Is.EqualTo(Undefined));
        Assert.That(new ConsoleCalendar(TitleArgument, DateArgument, SelectedDateArgument, Undefined).GetDayOfWeekColor(), Is.EqualTo(Undefined));
    }

    [Test]
    public void AnUndefinedTitleColorArgument_FailsAtTheTitle_BeforeAnythingIsWritten()
    {
        foreach (ConsoleCalendar calendar in new[]
        {
            new ConsoleCalendar(Undefined),
            new ConsoleCalendar(Undefined, DateArgument),
            new ConsoleCalendar(Undefined, DateArgument, SelectedDateArgument),
            new ConsoleCalendar(Undefined, DateArgument, SelectedDateArgument, DayOfWeekArgument),
        })
        {
            Captured captured = Draw(calendar);

            Assert.That(captured.Exception, Is.TypeOf<ArgumentException>());
            Assert.That(captured.Events, Is.Empty);
        }
    }

    [Test]
    public void AnUndefinedDayOfWeekColorArgument_FailsAtTheWeekdayRow_AfterTheTitleIsWritten()
    {
        Captured captured = Draw(new ConsoleCalendar(TitleArgument, DateArgument, SelectedDateArgument, Undefined));

        Assert.That(captured.Exception, Is.TypeOf<ArgumentException>());
        Assert.That(captured.Text, Is.EqualTo(Title));
    }

    [Test]
    public void AnUndefinedDateColorArgument_FailsAtTheFirstCell_AfterTheWeekdayRow()
    {
        foreach (ConsoleCalendar calendar in new[]
        {
            new ConsoleCalendar(TitleArgument, Undefined),
            new ConsoleCalendar(TitleArgument, Undefined, SelectedDateArgument),
            new ConsoleCalendar(TitleArgument, Undefined, SelectedDateArgument, DayOfWeekArgument),
        })
        {
            Captured captured = Draw(calendar);

            Assert.That(captured.Exception, Is.TypeOf<ArgumentException>());
            Assert.That(captured.Text, Is.EqualTo(Title + WeekdayRow));
        }
    }

    [Test]
    public void AnUndefinedSelectedDateColorArgument_FailsAtTheFirstSelectedDay()
    {
        foreach (ConsoleCalendar calendar in new[]
        {
            new ConsoleCalendar(TitleArgument, DateArgument, Undefined),
            new ConsoleCalendar(TitleArgument, DateArgument, Undefined, DayOfWeekArgument),
        })
        {
            Captured captured = Draw(calendar);

            Assert.That(captured.Exception, Is.TypeOf<ArgumentException>());
            Assert.That(captured.Text, Is.EqualTo(Title + WeekdayRow + DaysBeforeThe14th));
        }
    }

    [Test]
    public void AnUndefinedSelectedDateColorArgument_IsHarmless_WhileNoDayOfTheMonthIsSelected()
    {
        var calendar = new ConsoleCalendar(TitleArgument, DateArgument, Undefined);

        Captured captured = ConsoleCapture.Run(() => calendar.GenerateMonth(2, 2023));

        Assert.That(captured.Text, Is.EqualTo(LegacyReference.Month(2023, 2, "February")));
    }

    /// <summary>With several undefined arguments the first element drawn fails, and the others are never reached.</summary>
    [Test]
    public void WithSeveralUndefinedArguments_TheElementDrawnFirstFails()
    {
        Captured all = Draw(new ConsoleCalendar(Undefined, Undefined, Undefined, Undefined));
        Captured allButTheTitle = Draw(new ConsoleCalendar(TitleArgument, Undefined, Undefined, Undefined));

        Assert.That(all.Exception, Is.TypeOf<ArgumentException>());
        Assert.That(all.Events, Is.Empty, "the title is drawn first");
        Assert.That(allButTheTitle.Exception, Is.TypeOf<ArgumentException>());
        Assert.That(allButTheTitle.Text, Is.EqualTo(Title), "then the weekday row");
    }

    /// <summary>The failure is the one the same value gives through the setter of the same name.</summary>
    [Test]
    public void AnUndefinedArgument_FailsExactlyAsTheSameValueSetThroughTheSetter()
    {
        var bySetter = new ConsoleCalendar();
        bySetter.SetTitleColor(Undefined);

        Captured fromConstructor = Draw(new ConsoleCalendar(Undefined));
        Captured fromSetter = Draw(bySetter);

        Assert.That(fromConstructor.Exception, Is.TypeOf(fromSetter.Exception!.GetType()));
        Assert.That(fromConstructor.Exception!.Message, Is.EqualTo(fromSetter.Exception.Message));
        Assert.That(fromConstructor.Text, Is.EqualTo(fromSetter.Text));
    }

    private static void AssertColors(ConsoleCalendar calendar, ConsoleColor title, ConsoleColor weekdays, ConsoleColor dates, ConsoleColor selected)
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(calendar.GetTitleColor(), Is.EqualTo(title), "title");
            Assert.That(calendar.GetDayOfWeekColor(), Is.EqualTo(weekdays), "weekday row");
            Assert.That(calendar.GetDateColor(), Is.EqualTo(dates), "dates");
            Assert.That(calendar.GetSelectedDateColor(), Is.EqualTo(selected), "selected dates");
        }
    }

    private static void AssertSameColors(ConsoleCalendar calendar, ConsoleCalendar expected) =>
        AssertColors(calendar, expected.GetTitleColor(), expected.GetDayOfWeekColor(), expected.GetDateColor(), expected.GetSelectedDateColor());
}
