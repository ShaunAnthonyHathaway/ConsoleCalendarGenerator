using CalendarGenerator;
using ConsoleCalendarGenerator.Tests.Support;

namespace ConsoleCalendarGenerator.Tests.Legacy;

/// <summary>The four color getters and setters, including what happens with values that are not real colors.</summary>
[TestFixture]
public class ColorAccessorTests
{
    private static readonly ConsoleColor[] AllColors = Enum.GetValues<ConsoleColor>();
    private static readonly ConsoleColor[] UndefinedColors = { (ConsoleColor)16, (ConsoleColor)99, (ConsoleColor)int.MaxValue };
    private static readonly List<DateTime> Fourteenth = new() { new DateTime(2023, 2, 14) };

    [TestCaseSource(nameof(AllColors))]
    public void EverySetter_StoresWhatItsGetterReturns(ConsoleColor color)
    {
        var calendar = new ConsoleCalendar();

        calendar.SetTitleColor(color);
        calendar.SetDayOfWeekColor(color);
        calendar.SetDateColor(color);
        calendar.SetSelectedDateColor(color);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(calendar.GetTitleColor(), Is.EqualTo(color));
            Assert.That(calendar.GetDayOfWeekColor(), Is.EqualTo(color));
            Assert.That(calendar.GetDateColor(), Is.EqualTo(color));
            Assert.That(calendar.GetSelectedDateColor(), Is.EqualTo(color));
        }
    }

    /// <summary>The setters each control the color their name says, in 1.0.0 as in 2.0.</summary>
    [Test]
    public void EachSetter_ChangesOnlyItsOwnColor()
    {
        var calendar = new ConsoleCalendar();

        calendar.SetTitleColor(ConsoleColor.DarkRed);
        Assert.That(Colors(calendar), Is.EqualTo((ConsoleColor.DarkRed, ConsoleColor.Cyan, ConsoleColor.White, ConsoleColor.Blue)));

        calendar.SetDayOfWeekColor(ConsoleColor.DarkGreen);
        Assert.That(Colors(calendar), Is.EqualTo((ConsoleColor.DarkRed, ConsoleColor.DarkGreen, ConsoleColor.White, ConsoleColor.Blue)));

        calendar.SetDateColor(ConsoleColor.DarkCyan);
        Assert.That(Colors(calendar), Is.EqualTo((ConsoleColor.DarkRed, ConsoleColor.DarkGreen, ConsoleColor.DarkCyan, ConsoleColor.Blue)));

        calendar.SetSelectedDateColor(ConsoleColor.DarkMagenta);
        Assert.That(Colors(calendar), Is.EqualTo((ConsoleColor.DarkRed, ConsoleColor.DarkGreen, ConsoleColor.DarkCyan, ConsoleColor.DarkMagenta)));
    }

    [Test]
    [Platform(Exclude = "Win", Reason = ConsoleState.ColorsObservableOnUnixOnly)]
    public void ColorsChangedWithSetters_AreUsedByTheNextRender()
    {
        var calendar = new ConsoleCalendar();
        calendar.SetTitleColor(ConsoleColor.DarkRed);
        calendar.SetDayOfWeekColor(ConsoleColor.DarkGreen);
        calendar.SetDateColor(ConsoleColor.DarkCyan);
        calendar.SetSelectedDateColor(ConsoleColor.DarkMagenta);

        Captured captured = ConsoleCapture.Run(() => calendar.GenerateMonth(2, 2023, Fourteenth));

        Assert.That(captured.ForegroundRuns.Select(run => run.Color), Is.EqualTo(new[]
        {
            ConsoleColor.DarkRed, ConsoleColor.DarkGreen, ConsoleColor.DarkCyan, ConsoleColor.DarkMagenta, ConsoleColor.DarkCyan,
        }));
    }

    [TestCaseSource(nameof(UndefinedColors))]
    public void Setters_AcceptValuesThatAreNotConsoleColors_WithoutThrowing(ConsoleColor undefined)
    {
        var calendar = new ConsoleCalendar();

        calendar.SetTitleColor(undefined);
        calendar.SetDayOfWeekColor(undefined);
        calendar.SetDateColor(undefined);
        calendar.SetSelectedDateColor(undefined);

        Assert.That(Colors(calendar), Is.EqualTo((undefined, undefined, undefined, undefined)));
    }

    [Test]
    public void Constructors_AcceptValuesThatAreNotConsoleColors_WithoutThrowing()
    {
        var undefined = (ConsoleColor)99;

        var calendar = new ConsoleCalendar(undefined, undefined, undefined, undefined);

        Assert.That(Colors(calendar), Is.EqualTo((undefined, undefined, undefined, undefined)));
    }

    // An undefined color only fails later, when the console is asked to use it. Whatever was written before that
    // point stays written.

    [Test]
    public void UndefinedTitleColor_FailsTheRenderWithArgumentException_BeforeAnythingIsWritten()
    {
        var calendar = new ConsoleCalendar();
        calendar.SetTitleColor((ConsoleColor)99);

        Captured captured = ConsoleCapture.TryRun(() => calendar.GenerateMonth(2, 2023));

        Assert.That(captured.Exception, Is.TypeOf<ArgumentException>());
        Assert.That(captured.Events, Is.Empty);
    }

    [Test]
    public void UndefinedWeekdayColor_FailsTheRenderAfterTheTitleLine()
    {
        var calendar = new ConsoleCalendar();
        calendar.SetDayOfWeekColor((ConsoleColor)99);

        Captured captured = ConsoleCapture.TryRun(() => calendar.GenerateMonth(2, 2023));

        Assert.That(captured.Exception, Is.TypeOf<ArgumentException>());
        Assert.That(captured.Text, Is.EqualTo("   February 2023\n"));
    }

    [Test]
    public void UndefinedDateColor_FailsTheRenderAfterTheWeekdayRow()
    {
        var calendar = new ConsoleCalendar();
        calendar.SetDateColor((ConsoleColor)99);

        Captured captured = ConsoleCapture.TryRun(() => calendar.GenerateMonth(2, 2023));

        Assert.That(captured.Exception, Is.TypeOf<ArgumentException>());
        Assert.That(captured.Text, Is.EqualTo("   February 2023\nMo Tu We Th Fr Sa Su\n"));
    }

    [Test]
    public void UndefinedSelectedDateColor_FailsTheRenderAtTheFirstSelectedDay()
    {
        var calendar = new ConsoleCalendar();
        calendar.SetSelectedDateColor((ConsoleColor)99);

        Captured captured = ConsoleCapture.TryRun(() => calendar.GenerateMonth(2, 2023, Fourteenth));

        Assert.That(captured.Exception, Is.TypeOf<ArgumentException>());
        Assert.That(captured.Text, Is.EqualTo(
            "   February 2023\n" +
            "Mo Tu We Th Fr Sa Su\n" +
            "       1  2  3  4  5\n" +
            " 6  7  8  9 10 11 12\n" +
            "13"));
    }

    [Test]
    public void UndefinedSelectedDateColor_IsHarmless_WhileNoDayOfTheMonthIsSelected()
    {
        var calendar = new ConsoleCalendar();
        calendar.SetSelectedDateColor((ConsoleColor)99);

        Captured captured = ConsoleCapture.Run(() => calendar.GenerateMonth(2, 2023, new List<DateTime> { new DateTime(2023, 3, 14) }));

        Assert.That(captured.Text, Is.EqualTo(LegacyReference.Month(2023, 2, "February")));
    }

    [Test]
    [Platform(Exclude = "Win", Reason = ConsoleState.ColorsObservableOnUnixOnly)]
    public void ARenderThatFailsPartWay_LeavesTheConsoleInTheLastColorItSet()
    {
        var calendar = new ConsoleCalendar();
        calendar.SetSelectedDateColor((ConsoleColor)99);

        Captured captured = ConsoleCapture.TryRun(() => calendar.GenerateMonth(2, 2023, Fourteenth));

        Assert.That(captured.Exception, Is.TypeOf<ArgumentException>());
        Assert.That(captured.ForegroundAfter, Is.EqualTo(ConsoleColor.White), "the date color of day 13 is still in effect");
    }

    private static (ConsoleColor Title, ConsoleColor Weekdays, ConsoleColor Dates, ConsoleColor Selected) Colors(ConsoleCalendar calendar) =>
        (calendar.GetTitleColor(), calendar.GetDayOfWeekColor(), calendar.GetDateColor(), calendar.GetSelectedDateColor());
}
