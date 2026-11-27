using CalendarGenerator;
using ConsoleCalendarGenerator.Tests.Support;

namespace ConsoleCalendarGenerator.Tests.Legacy;

/// <summary>
/// How the library drives the console: the individual writes, the color that is in effect for each of them, and
/// what it leaves behind. The text goes to Console.Out, which these tests replace; the colors are set on the
/// console itself, whatever Console.Out is.
/// </summary>
[TestFixture]
public class ConsoleBehaviorTests
{
    private static readonly List<DateTime> Fourteenth = new() { new DateTime(2023, 2, 14) };

    // ---- on every platform: the writes ----

    /// <summary>
    /// The title and the weekday row are one WriteLine each. Every day, and every empty cell before the 1st, is a
    /// separate Write. A row ends with an empty WriteLine after Sunday, and the last row with an empty WriteLine
    /// after the last day; one more empty WriteLine is the blank line after the month.
    /// </summary>
    [Test]
    public void AMonth_IsWrittenAsOneLinePerHeading_AndOneWritePerCell()
    {
        Captured captured = ConsoleCapture.Run(() => new ConsoleCalendar().GenerateMonth(2, 2023));

        Assert.That(captured.Transcript, Is.EqualTo(
            "[   February 2023]<NL>" +
            "[Mo Tu We Th Fr Sa Su]<NL>" +
            "[   ][   ][ 1][  2][  3][  4][  5]<NL>" +
            "[ 6][  7][  8][  9][ 10][ 11][ 12]<NL>" +
            "[13][ 14][ 15][ 16][ 17][ 18][ 19]<NL>" +
            "[20][ 21][ 22][ 23][ 24][ 25][ 26]<NL>" +
            "[27][ 28]<NL><NL>"));
    }

    [Test]
    public void AMonthEndingOnSunday_EndsItsLastRowOnce_ThenWritesTheBlankLine_LikeAnyMonth()
    {
        Captured captured = ConsoleCapture.Run(() => new ConsoleCalendar().GenerateMonth(2, 2021));

        Assert.That(captured.Transcript, Does.EndWith("[22][ 23][ 24][ 25][ 26][ 27][ 28]<NL><NL>").Using(StringComparison.Ordinal));
        Assert.That(captured.Transcript, Does.Not.EndWith("<NL><NL><NL>"));
    }

    /// <summary>No call ever passes a line feed as text: every line end is the writer's own.</summary>
    [Test]
    public void NoWrite_ContainsALineFeed()
    {
        Captured captured = ConsoleCapture.Run(() => new ConsoleCalendar().GenerateYear(2023, Fourteenth));

        Assert.That(captured.Events.Where(written => written.Payload.Contains('\n') || written.Payload.Contains('\r')), Is.Empty);
        Assert.That(captured.Events.Count(written => written.EndsLine && written.Payload.Length == 0), Is.EqualTo(53 + 10 + 12),
            "one line end after each of the 53 Sundays of 2023, one after the last day of the ten months that do not end on a Sunday, and one blank line for each of the twelve months");
    }

    [Test]
    public void TheLibrary_WritesNoEscapeSequencesOfItsOwn()
    {
        Captured captured = ConsoleCapture.Run(() =>
            new ConsoleCalendar(ConsoleColor.Red, ConsoleColor.Green, ConsoleColor.Magenta, ConsoleColor.DarkYellow).GenerateYear(2023, Fourteenth));

        Assert.That(captured.Text, Ordinal.DoesNotContain("\u001b"));
        Assert.That(captured.Text.Where(char.IsControl).Distinct(), Is.EqualTo(new[] { '\n' }));
    }

    // ---- where colors can be read back: the color in effect for each write ----

    [Test]
    [Platform(Exclude = "Win", Reason = ConsoleState.ColorsObservableOnUnixOnly)]
    public void DefaultColors_TitleYellow_WeekdaysCyan_DatesWhite_SelectedDayBlue()
    {
        Captured captured = ConsoleCapture.Run(() => new ConsoleCalendar().GenerateMonth(2, 2023, Fourteenth));

        Assert.That(captured.ForegroundRuns, Is.EqualTo(new[]
        {
            (ConsoleColor.Yellow, "   February 2023\n"),
            (ConsoleColor.Cyan, "Mo Tu We Th Fr Sa Su\n"),
            (ConsoleColor.White, "       1  2  3  4  5\n 6  7  8  9 10 11 12\n13"),
            // The space in front of a day belongs to that day's write and is written in its color.
            (ConsoleColor.Blue, " 14"),
            (ConsoleColor.White, " 15 16 17 18 19\n20 21 22 23 24 25 26\n27 28\n\n"),
        }));
    }

    /// <summary>The empty cells before the 1st are written in the color of the 1st.</summary>
    [Test]
    [Platform(Exclude = "Win", Reason = ConsoleState.ColorsObservableOnUnixOnly)]
    public void WhenTheFirstIsSelected_TheEmptyCellsBeforeItAreWrittenInTheSelectedColorToo()
    {
        Captured captured = ConsoleCapture.Run(() => new ConsoleCalendar().GenerateMonth(1, 2023, new List<DateTime> { new DateTime(2023, 1, 1) }));

        Assert.That(captured.ForegroundRuns, Is.EqualTo(new[]
        {
            (ConsoleColor.Yellow, "    January 2023\n"),
            (ConsoleColor.Cyan, "Mo Tu We Th Fr Sa Su\n"),
            (ConsoleColor.Blue, "                   1\n"),
            (ConsoleColor.White, " 2  3  4  5  6  7  8\n 9 10 11 12 13 14 15\n16 17 18 19 20 21 22\n23 24 25 26 27 28 29\n30 31\n\n"),
        }));
    }

    [Test]
    [Platform(Exclude = "Win", Reason = ConsoleState.ColorsObservableOnUnixOnly)]
    public void EveryWrite_IsMadeWithAColorSet_NeverInTheResetState()
    {
        Captured captured = ConsoleCapture.Run(() => new ConsoleCalendar().GenerateYear(2024, Fourteenth));

        Assert.That(captured.Events.Select(e => e.Foreground).Distinct(),
            Is.EquivalentTo(new[] { ConsoleColor.Yellow, ConsoleColor.Cyan, ConsoleColor.White }));
    }

    // ---- where colors can be read back: what the caller is left with ----

    [Test]
    [Platform(Exclude = "Win", Reason = ConsoleState.ColorsObservableOnUnixOnly)]
    public void AfterGenerateMonth_TheConsoleColorsAreReset_NotRestoredToTheCallersColors()
    {
        Captured captured = ConsoleCapture.Run(
            () => new ConsoleCalendar().GenerateMonth(2, 2023),
            foregroundBefore: ConsoleColor.DarkCyan,
            backgroundBefore: ConsoleColor.DarkBlue);

        Assert.That(captured.ForegroundAfter, Is.EqualTo(ConsoleState.Unset));
        Assert.That(captured.BackgroundAfter, Is.EqualTo(ConsoleState.Unset));
    }

    /// <summary>The reset after the title line also clears the background the caller had set.</summary>
    [Test]
    [Platform(Exclude = "Win", Reason = ConsoleState.ColorsObservableOnUnixOnly)]
    public void TheCallersBackgroundColor_SurvivesOnlyUntilTheEndOfTheTitleLine()
    {
        Captured captured = ConsoleCapture.Run(
            () => new ConsoleCalendar().GenerateMonth(2, 2023),
            backgroundBefore: ConsoleColor.DarkBlue);

        Assert.That(captured.BackgroundRuns, Is.EqualTo(new[]
        {
            (ConsoleColor.DarkBlue, "   February 2023\n"),
            (ConsoleState.Unset, "Mo Tu We Th Fr Sa Su\n" + "       1  2  3  4  5\n 6  7  8  9 10 11 12\n13 14 15 16 17 18 19\n20 21 22 23 24 25 26\n27 28\n\n"),
        }));
    }

    [Test]
    [Platform(Exclude = "Win", Reason = ConsoleState.ColorsObservableOnUnixOnly)]
    public void TheLibraryNeverSetsABackgroundColor()
    {
        Captured captured = ConsoleCapture.Run(() => new ConsoleCalendar().GenerateYear(2023, Fourteenth));

        Assert.That(captured.Events.Select(e => e.Background).Distinct(), Is.EqualTo(new[] { ConsoleState.Unset }));
    }

    /// <summary>
    /// Replacing Console.Out captures the text only. The library still changes the console's own colors, which
    /// is why a caller cannot get a fully "captured" calendar out of 1.0.0.
    /// </summary>
    [Test]
    [Platform(Exclude = "Win", Reason = ConsoleState.ColorsObservableOnUnixOnly)]
    public void WithConsoleOutReplaced_TheConsolesColorStateIsStillChanged()
    {
        Captured captured = ConsoleCapture.Run(
            () => new ConsoleCalendar().GenerateMonth(2, 2023),
            foregroundBefore: ConsoleColor.DarkCyan);

        Assert.That(captured.Events.Select(e => e.Foreground), Has.None.EqualTo(ConsoleColor.DarkCyan), "the colors changed while the text was captured");
        Assert.That(captured.ForegroundAfter, Is.Not.EqualTo(ConsoleColor.DarkCyan));
    }
}
