using System.Globalization;
using ConsoleCalendarGenerator.Tests.Migration;
using ConsoleCalendarGenerator.Tests.Support;

namespace ConsoleCalendarGenerator.Tests.Historical;

/// <summary>
/// What version 1.0.0 did in the five places that version 2.0 corrects, written out against the frozen source
/// of 1.0.0. These are statements about history. They stay as they are when the library is corrected, and they
/// are what a migration note can point to for "before".
///
/// <para>
/// Nothing here is the library's class: <c>Published100.ConsoleCalendar</c> is the frozen one.
/// </para>
/// </summary>
[TestFixture]
public class Published100BehaviorTests
{
    private const ConsoleColor TitleArgument = ConsoleColor.Red;
    private const ConsoleColor DateArgument = ConsoleColor.Green;
    private const ConsoleColor SelectedDateArgument = ConsoleColor.Magenta;
    private const ConsoleColor DayOfWeekArgument = ConsoleColor.DarkYellow;

    private static readonly List<DateTime> NoDates = new();

    // ---- 1. constructor colors ----

    [Test]
    public void In100_TheTitleColorArgumentColorsTheWeekdayRow_AndTheTitleStaysYellow()
    {
        foreach (Published100.ConsoleCalendar calendar in new[]
        {
            new Published100.ConsoleCalendar(TitleArgument),
            new Published100.ConsoleCalendar(TitleArgument, DateArgument),
            new Published100.ConsoleCalendar(TitleArgument, DateArgument, SelectedDateArgument),
        })
        {
            Assert.That(calendar.GetDayOfWeekColor(), Is.EqualTo(TitleArgument));
            Assert.That(calendar.GetTitleColor(), Is.EqualTo(ConsoleColor.Yellow));
        }
    }

    [Test]
    public void In100_WithFourArguments_TheTitleAndTheDayOfWeekArgumentsColorEachOthersElement()
    {
        var calendar = new Published100.ConsoleCalendar(TitleArgument, DateArgument, SelectedDateArgument, DayOfWeekArgument);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(calendar.GetTitleColor(), Is.EqualTo(DayOfWeekArgument));
            Assert.That(calendar.GetDayOfWeekColor(), Is.EqualTo(TitleArgument));
            Assert.That(calendar.GetDateColor(), Is.EqualTo(DateArgument));
            Assert.That(calendar.GetSelectedDateColor(), Is.EqualTo(SelectedDateArgument));
        }
    }

    [Test]
    [Platform(Exclude = "Win", Reason = ConsoleState.ColorsObservableOnUnixOnly)]
    public void In100_WithFourArguments_TheSwapIsWhatIsDrawn()
    {
        var calendar = new Published100.ConsoleCalendar(TitleArgument, DateArgument, SelectedDateArgument, DayOfWeekArgument);

        Captured captured = ConsoleCapture.Run(() => calendar.GenerateMonth(2, 2023, NoDates));

        Assert.That(captured.ForegroundRuns.Take(2), Is.EqualTo(new[]
        {
            (DayOfWeekArgument, "   February 2023\n"),
            (TitleArgument, "Mo Tu We Th Fr Sa Su\n"),
        }));
    }

    // ---- 2. a null list ----

    [Test]
    public void In100_ANullList_ThrowsNullReferenceException_BeforeAnythingIsWritten()
    {
        Captured month = ConsoleCapture.TryRun(() => new Published100.ConsoleCalendar().GenerateMonth(2, 2023, null!));
        Captured year = ConsoleCapture.TryRun(() => new Published100.ConsoleCalendar().GenerateYear(2023, null!));

        foreach (Captured captured in new[] { month, year })
        {
            Assert.That(captured.Exception, Is.TypeOf<NullReferenceException>());
            Assert.That(captured.Events, Is.Empty);
        }
    }

    [Test]
    public void In100_ANullList_IsReportedBeforeAnInvalidMonthOrYear()
    {
        Assert.That(ConsoleCapture.TryRun(() => new Published100.ConsoleCalendar().GenerateMonth(13, 2023, null!)).Exception, Is.TypeOf<NullReferenceException>());
        Assert.That(ConsoleCapture.TryRun(() => new Published100.ConsoleCalendar().GenerateMonth(1, 0, null!)).Exception, Is.TypeOf<NullReferenceException>());
        Assert.That(ConsoleCapture.TryRun(() => new Published100.ConsoleCalendar().GenerateMonth(13, 2023, NoDates)).Exception, Is.TypeOf<ArgumentOutOfRangeException>());
    }

    // ---- 3. the end of the last row ----

    [Test]
    public void In100_TheLastRowOfDays_EndsWithABareLineFeed_WhateverTheConsolesTerminatorIs()
    {
        string text = ConsoleCapture.Run(() => new Published100.ConsoleCalendar().GenerateMonth(5, 2023), newLine: "\r\n").Text;

        Assert.That(text, Is.EqualTo(
            "     May 2023\r\n" +
            "Mo Tu We Th Fr Sa Su\r\n" +
            " 1  2  3  4  5  6  7\r\n" +
            " 8  9 10 11 12 13 14\r\n" +
            "15 16 17 18 19 20 21\r\n" +
            "22 23 24 25 26 27 28\r\n" +
            "29 30 31\n" +
            "\r\n"));
    }

    [Test]
    public void In100_TheBareLineFeed_IsACallOfItsOwn_FollowedByOneLineEnd()
    {
        Captured captured = ConsoleCapture.Run(() => new Published100.ConsoleCalendar().GenerateMonth(5, 2023));

        Assert.That(captured.Transcript, Does.EndWith("[ 30][ 31][\\n]<NL>").Using(StringComparison.Ordinal));
    }

    // ---- 4. the blank line after a month ----

    [Test]
    public void In100_AMonthThatEndsOnASunday_IsFollowedByTwoBlankLines()
    {
        string text = ConsoleCapture.Run(() => new Published100.ConsoleCalendar().GenerateMonth(4, 2023)).Text;

        Assert.That(text, Is.EqualTo(Golden.Lines(
            "    April 2023",
            "Mo Tu We Th Fr Sa Su",
            "                1  2",
            " 3  4  5  6  7  8  9",
            "10 11 12 13 14 15 16",
            "17 18 19 20 21 22 23",
            "24 25 26 27 28 29 30",
            "",
            "")));
    }

    [Test]
    public void In100_AnyOtherMonth_IsFollowedByOneBlankLine()
    {
        string text = ConsoleCapture.Run(() => new Published100.ConsoleCalendar().GenerateMonth(5, 2023)).Text;

        Assert.That(text, Does.EndWith("29 30 31\n\n").Using(StringComparison.Ordinal));
        Assert.That(text, Does.Not.EndWith("\n\n\n"));
    }

    // ---- 5. the title under a culture with another calendar ----

    /// <summary>
    /// Under a culture whose own calendar is not the Gregorian one, the title is that calendar's name for the
    /// month of the same number, which is not the month the grid shows. The names come from the system, so the
    /// test compares with what the culture reports and assumes only that the culture exists.
    /// </summary>
    [Test]
    public void In100_UnderACultureWithAnotherCalendar_TheTitleNamesThatCalendarsMonthOfTheSameNumber()
    {
        CultureInfo? persian = TestCultures.TryGet("fa-IR");
        Assume.That(persian, Is.Not.Null, "The culture fa-IR is not available on this machine.");
        Assume.That(persian!.DateTimeFormat.Calendar, Is.Not.InstanceOf<GregorianCalendar>(), "fa-IR uses the Gregorian calendar here.");
        string ownName = persian.DateTimeFormat.GetMonthName(9);
        string gregorianName = new DateTime(2023, 9, 1).ToString("MMMM", GregorianDataOf(persian));
        Assume.That(ownName, Is.Not.EqualTo(gregorianName), "The two calendars of fa-IR name their ninth month alike here.");

        string[] lines = ConsoleCapture.Run(() => new Published100.ConsoleCalendar().GenerateMonth(9, 2023), culture: persian).Text.Split('\n');

        Assert.That(lines[0], Is.EqualTo(LegacyReference.Title(ownName, 2023)));
        Assert.That(lines[0], Ordinal.DoesNotContain(gregorianName));
        Assert.That(lines[2], Is.EqualTo("             1  2  3"), "the grid is Gregorian September 2023, which begins on a Friday");
    }

    /// <summary>The culture's names for the months of the Gregorian calendar.</summary>
    private static DateTimeFormatInfo GregorianDataOf(CultureInfo culture)
    {
        var format = (DateTimeFormatInfo)culture.DateTimeFormat.Clone();
        format.Calendar = culture.OptionalCalendars.OfType<GregorianCalendar>().First();
        return format;
    }

    // ---- the frozen source also does what is not being corrected ----

    [Test]
    public void In100_ATwoDigitDayOnAMonday_IsWrittenAsANumber_AndEveryOtherCellAsText()
    {
        LegacyObservation observation = LegacyObservation.Capture(() => new Published100.ConsoleCalendar().GenerateMonth(5, 2023));
        string[] calls = observation.Calls.Select(call => call.Method + "[" + call.Text + "]").ToArray();

        Assert.That(string.Concat(calls), Ordinal.Contains("S[ 14]L[]I[15]S[ 16]"));
        Assert.That(observation.Calls.Where(call => call.Method == "I").Select(call => call.Text), Is.EqualTo(new[] { "15", "22", "29" }));
        Assert.That(observation.Calls.Select(call => call.Method).Distinct(), Is.EquivalentTo(new[] { "LS", "S", "I", "L" }));
    }
}
