using System.Globalization;
using ConsoleCalendarGenerator.Tests.Migration;
using ConsoleCalendarGenerator.Tests.Support;

namespace ConsoleCalendarGenerator.Tests.Historical;

/// <summary>
/// The constructors of version 1.0.0, complete: which element each argument colored, what was drawn in which
/// color, and when a color that is not defined made the drawing fail. Everything here runs the frozen source
/// of 1.0.0 and states what it did. Version 2.0 corrects the constructors (D-57); these tests stay as they are,
/// as the record of what was corrected. The corrected behavior is in Legacy/ConstructorColorTests.cs, and the
/// two side by side in Migration/ConstructorColorMigrationTests.cs.
/// </summary>
[TestFixture]
public class Published100ConstructorTests
{
    private const ConsoleColor TitleArgument = ConsoleColor.Red;
    private const ConsoleColor DateArgument = ConsoleColor.Green;
    private const ConsoleColor SelectedDateArgument = ConsoleColor.Magenta;
    private const ConsoleColor DayOfWeekArgument = ConsoleColor.DarkYellow;

    private static readonly ConsoleColor Undefined = (ConsoleColor)99;

    private const string Title = "   February 2023\n";
    private const string WeekdayRow = "Mo Tu We Th Fr Sa Su\n";
    private const string DaysBeforeThe14th = "       1  2  3  4  5\n 6  7  8  9 10 11 12\n13";

    /// <summary>February 2023 with the 14th selected, through the constructor that takes these colors.</summary>
    private static LegacyObservation Drawn(params ConsoleColor[] constructorArguments) => LegacyObservation.Of(
        LegacySubject.Published100,
        new LegacyScenario("February 2023")
        {
            ConstructorArguments = constructorArguments,
            Draw = LegacyDraw.MonthWithDates,
            Month = 2,
            Year = 2023,
            Dates = new[] { new DateTime(2023, 2, 14) },
        },
        CultureInfo.InvariantCulture);

    // ---- the colors a calendar holds ----

    [Test]
    public void In100_TheParameterlessConstructor_HoldsYellowCyanWhiteBlue()
    {
        Assert.That(Drawn().StoredColors, Is.EqualTo(
            new LegacyStoredColors(Title: ConsoleColor.Yellow, DayOfWeek: ConsoleColor.Cyan, Date: ConsoleColor.White, SelectedDate: ConsoleColor.Blue)));
    }

    [Test]
    public void In100_OneArgument_TheTitleColorArgumentIsHeldAsTheWeekdayColor_AndTheTitleStaysYellow()
    {
        Assert.That(Drawn(TitleArgument).StoredColors, Is.EqualTo(
            new LegacyStoredColors(Title: ConsoleColor.Yellow, DayOfWeek: TitleArgument, Date: ConsoleColor.White, SelectedDate: ConsoleColor.Blue)));
    }

    [Test]
    public void In100_TwoArguments_TheTitleColorArgumentIsHeldAsTheWeekdayColor_AndTheTitleStaysYellow()
    {
        Assert.That(Drawn(TitleArgument, DateArgument).StoredColors, Is.EqualTo(
            new LegacyStoredColors(Title: ConsoleColor.Yellow, DayOfWeek: TitleArgument, Date: DateArgument, SelectedDate: ConsoleColor.Blue)));
    }

    [Test]
    public void In100_ThreeArguments_TheTitleColorArgumentIsHeldAsTheWeekdayColor_AndTheTitleStaysYellow()
    {
        Assert.That(Drawn(TitleArgument, DateArgument, SelectedDateArgument).StoredColors, Is.EqualTo(
            new LegacyStoredColors(Title: ConsoleColor.Yellow, DayOfWeek: TitleArgument, Date: DateArgument, SelectedDate: SelectedDateArgument)));
    }

    [Test]
    public void In100_FourArguments_TheTitleColorAndTheDayOfWeekColorArgumentsAreHeldAsEachOthersColor()
    {
        Assert.That(Drawn(TitleArgument, DateArgument, SelectedDateArgument, DayOfWeekArgument).StoredColors, Is.EqualTo(
            new LegacyStoredColors(Title: DayOfWeekArgument, DayOfWeek: TitleArgument, Date: DateArgument, SelectedDate: SelectedDateArgument)));
    }

    // ---- what is drawn in which color ----

    private static IEnumerable<TestCaseData> DrawnColors()
    {
        yield return new TestCaseData(Array.Empty<ConsoleColor>(), ConsoleColor.Yellow, ConsoleColor.Cyan, ConsoleColor.White, ConsoleColor.Blue)
            .SetArgDisplayNames("no arguments");
        yield return new TestCaseData(new[] { TitleArgument }, ConsoleColor.Yellow, TitleArgument, ConsoleColor.White, ConsoleColor.Blue)
            .SetArgDisplayNames("one argument");
        yield return new TestCaseData(new[] { TitleArgument, DateArgument }, ConsoleColor.Yellow, TitleArgument, DateArgument, ConsoleColor.Blue)
            .SetArgDisplayNames("two arguments");
        yield return new TestCaseData(new[] { TitleArgument, DateArgument, SelectedDateArgument }, ConsoleColor.Yellow, TitleArgument, DateArgument, SelectedDateArgument)
            .SetArgDisplayNames("three arguments");
        yield return new TestCaseData(new[] { TitleArgument, DateArgument, SelectedDateArgument, DayOfWeekArgument }, DayOfWeekArgument, TitleArgument, DateArgument, SelectedDateArgument)
            .SetArgDisplayNames("four arguments");
    }

    [TestCaseSource(nameof(DrawnColors))]
    [Platform(Exclude = "Win", Reason = ConsoleState.ColorsObservableOnUnixOnly)]
    public void In100_TheMonthIsDrawnInTheColorsHeld(ConsoleColor[] arguments, ConsoleColor title, ConsoleColor weekdayRow, ConsoleColor dates, ConsoleColor selectedDates)
    {
        LegacyObservation observation = Drawn(arguments);
        RenderedRoleColors drawn = RenderedRoleColors.Of(observation, 14);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(observation.ExceptionType, Is.Null);
            Assert.That(drawn.Title, Is.EqualTo(title), "title");
            Assert.That(drawn.DayOfWeek, Is.EqualTo(weekdayRow), "weekday row");
            Assert.That(drawn.Dates, Is.EqualTo(new[] { dates }), "days that are not selected");
            Assert.That(drawn.SelectedDates, Is.EqualTo(new[] { selectedDates }), "the selected day");
            Assert.That((observation.ForegroundAfter, observation.BackgroundAfter), Is.EqualTo((ConsoleState.Unset, ConsoleState.Unset)), "the colors are reset");
        }
    }

    // ---- a color that is not defined ----

    // A value that is not a ConsoleColor is accepted by every constructor and fails later, when the console is
    // asked to use it. Which element an argument colors therefore decides how much is written first.

    [Test]
    public void In100_AnUndefinedTitleColorArgument_FailsAtTheWeekdayRow_AfterTheTitleIsWritten()
    {
        foreach (LegacyObservation observation in new[]
        {
            Drawn(Undefined),
            Drawn(Undefined, DateArgument),
            Drawn(Undefined, DateArgument, SelectedDateArgument),
            Drawn(Undefined, DateArgument, SelectedDateArgument, DayOfWeekArgument),
        })
        {
            Assert.That(observation.ExceptionType, Is.EqualTo("System.ArgumentException"));
            Assert.That(observation.Text, Is.EqualTo(Title));
        }
    }

    [Test]
    public void In100_AnUndefinedDayOfWeekColorArgument_FailsAtTheTitle_BeforeAnythingIsWritten()
    {
        LegacyObservation observation = Drawn(TitleArgument, DateArgument, SelectedDateArgument, Undefined);

        Assert.That(observation.ExceptionType, Is.EqualTo("System.ArgumentException"));
        Assert.That(observation.Calls, Is.Empty);
    }

    [Test]
    public void In100_AnUndefinedDateColorArgument_FailsAtTheFirstCell_AfterTheWeekdayRow()
    {
        foreach (LegacyObservation observation in new[]
        {
            Drawn(TitleArgument, Undefined),
            Drawn(TitleArgument, Undefined, SelectedDateArgument),
            Drawn(TitleArgument, Undefined, SelectedDateArgument, DayOfWeekArgument),
        })
        {
            Assert.That(observation.ExceptionType, Is.EqualTo("System.ArgumentException"));
            Assert.That(observation.Text, Is.EqualTo(Title + WeekdayRow));
        }
    }

    [Test]
    public void In100_AnUndefinedSelectedDateColorArgument_FailsAtTheFirstSelectedDay()
    {
        foreach (LegacyObservation observation in new[]
        {
            Drawn(TitleArgument, DateArgument, Undefined),
            Drawn(TitleArgument, DateArgument, Undefined, DayOfWeekArgument),
        })
        {
            Assert.That(observation.ExceptionType, Is.EqualTo("System.ArgumentException"));
            Assert.That(observation.Text, Is.EqualTo(Title + WeekdayRow + DaysBeforeThe14th));
        }
    }

    [Test]
    public void In100_WithEveryArgumentUndefined_TheTitleFailsFirst()
    {
        LegacyObservation observation = Drawn(Undefined, Undefined, Undefined, Undefined);

        Assert.That(observation.StoredColors, Is.EqualTo(new LegacyStoredColors(Undefined, Undefined, Undefined, Undefined)));
        Assert.That(observation.ExceptionType, Is.EqualTo("System.ArgumentException"));
        Assert.That(observation.Calls, Is.Empty);
    }
}
