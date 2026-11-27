using CalendarGenerator;
using ConsoleCalendarGenerator.Tests.Support;

namespace ConsoleCalendarGenerator.Tests.Legacy;

/// <summary>
/// Which color each constructor argument ends up controlling in 1.0.0.
///
/// This is characterized behavior, kept for 1.x compatibility. It is not the intended design: the parameter
/// called TitleColor colors the weekday row, the title stays Yellow unless four arguments are given, and with
/// four arguments DayOfWeekColor colors the title. Programs and the README screenshots already depend on these
/// results, so they must not change before a 2.0 release. Do not "fix" these tests.
/// </summary>
[TestFixture]
public class ConstructorColorTests
{
    private const ConsoleColor TitleArgument = ConsoleColor.Red;
    private const ConsoleColor DateArgument = ConsoleColor.Green;
    private const ConsoleColor SelectedDateArgument = ConsoleColor.Magenta;
    private const ConsoleColor DayOfWeekArgument = ConsoleColor.DarkYellow;

    [Test]
    public void DefaultConstructor_UsesYellowTitle_CyanWeekdays_WhiteDates_BlueSelection()
    {
        AssertColors(new ConsoleCalendar(),
            title: ConsoleColor.Yellow, weekdays: ConsoleColor.Cyan, dates: ConsoleColor.White, selected: ConsoleColor.Blue);
    }

    [Test]
    public void OneArgument_Characterized1xBehavior_TitleColorArgumentColorsTheWeekdayRow_AndTheTitleStaysYellow()
    {
        AssertColors(new ConsoleCalendar(TitleArgument),
            title: ConsoleColor.Yellow, weekdays: TitleArgument, dates: ConsoleColor.White, selected: ConsoleColor.Blue);
    }

    [Test]
    public void TwoArguments_Characterized1xBehavior_TitleColorArgumentColorsTheWeekdayRow_AndTheTitleStaysYellow()
    {
        AssertColors(new ConsoleCalendar(TitleArgument, DateArgument),
            title: ConsoleColor.Yellow, weekdays: TitleArgument, dates: DateArgument, selected: ConsoleColor.Blue);
    }

    [Test]
    public void ThreeArguments_Characterized1xBehavior_TitleColorArgumentColorsTheWeekdayRow_AndTheTitleStaysYellow()
    {
        AssertColors(new ConsoleCalendar(TitleArgument, DateArgument, SelectedDateArgument),
            title: ConsoleColor.Yellow, weekdays: TitleArgument, dates: DateArgument, selected: SelectedDateArgument);
    }

    [Test]
    public void FourArguments_Characterized1xBehavior_TitleAndDayOfWeekArgumentsAreSwapped()
    {
        AssertColors(new ConsoleCalendar(TitleArgument, DateArgument, SelectedDateArgument, DayOfWeekArgument),
            title: DayOfWeekArgument, weekdays: TitleArgument, dates: DateArgument, selected: SelectedDateArgument);
    }

    /// <summary>The same swap, seen in what is drawn rather than in what the getters return.</summary>
    [Test]
    [Platform(Exclude = "Win", Reason = ConsoleState.ColorsObservableOnUnixOnly)]
    public void FourArguments_Characterized1xBehavior_TheTitleIsDrawnInTheDayOfWeekArgument_AndTheWeekdayRowInTheTitleArgument()
    {
        var calendar = new ConsoleCalendar(TitleArgument, DateArgument, SelectedDateArgument, DayOfWeekArgument);

        Captured captured = ConsoleCapture.Run(() => calendar.GenerateMonth(2, 2023, new List<DateTime> { new DateTime(2023, 2, 14) }));

        Assert.That(captured.ForegroundRuns, Is.EqualTo(new[]
        {
            (DayOfWeekArgument, "   February 2023\n"),
            (TitleArgument, "Mo Tu We Th Fr Sa Su\n"),
            (DateArgument, "       1  2  3  4  5\n 6  7  8  9 10 11 12\n13"),
            (SelectedDateArgument, " 14"),
            (DateArgument, " 15 16 17 18 19\n20 21 22 23 24 25 26\n27 28\n\n"),
        }));
    }

    [Test]
    [Platform(Exclude = "Win", Reason = ConsoleState.ColorsObservableOnUnixOnly)]
    public void OneArgument_Characterized1xBehavior_TheWeekdayRowIsDrawnInTheTitleArgument_AndTheTitleInYellow()
    {
        Captured captured = ConsoleCapture.Run(() => new ConsoleCalendar(TitleArgument).GenerateMonth(2, 2023));

        Assert.That(captured.ForegroundRuns.Take(2), Is.EqualTo(new[]
        {
            (ConsoleColor.Yellow, "   February 2023\n"),
            (TitleArgument, "Mo Tu We Th Fr Sa Su\n"),
        }));
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
}
