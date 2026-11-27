using CalendarGenerator;
using ConsoleCalendarGenerator.Tests.Support;

namespace ConsoleCalendarGenerator.Tests.Legacy;

/// <summary>
/// Line endings. Every line is ended with Console.WriteLine, which uses the console writer's newline, except
/// the last row of days: that one is ended with a literal "\n". Where the writer's newline is "\r\n", as on
/// Windows, the output therefore mixes both.
/// </summary>
[TestFixture]
public class LineEndingTests
{
    private static string Month(int month, int year, string newLine) =>
        ConsoleCapture.Run(() => new ConsoleCalendar().GenerateMonth(month, year), newLine: newLine).Text;

    [Test]
    public void WithACarriageReturnLineFeedWriter_TheLastRowStillEndsWithABareLineFeed()
    {
        Assert.That(Month(5, 2023, "\r\n"), Is.EqualTo(
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
    public void WithACarriageReturnLineFeedWriter_AMonthEndingOnSunday_HasTheBareLineFeedOnItsFirstBlankLine()
    {
        Assert.That(Month(4, 2023, "\r\n"), Is.EqualTo(
            "    April 2023\r\n" +
            "Mo Tu We Th Fr Sa Su\r\n" +
            "                1  2\r\n" +
            " 3  4  5  6  7  8  9\r\n" +
            "10 11 12 13 14 15 16\r\n" +
            "17 18 19 20 21 22 23\r\n" +
            "24 25 26 27 28 29 30\r\n" +
            "\n" +
            "\r\n"));
    }

    [Test]
    public void WithALineFeedWriter_EveryLineEndsWithALineFeed()
    {
        string text = Month(5, 2023, "\n");

        Assert.That(text, Ordinal.DoesNotContain("\r"));
        Assert.That(text.Count(c => c == '\n'), Is.EqualTo(8));
    }

    [TestCase("\n")]
    [TestCase("\r\n")]
    [TestCase("<EOL>")]
    public void TheWritersNewlineIsUsedForEveryLine_ExceptTheLiteralLineFeedAfterTheLastDay(string newLine)
    {
        Assert.That(Month(2, 2024, newLine), Is.EqualTo(LegacyReference.Month(2024, 2, "February", newLine)));
        Assert.That(Month(2, 2021, newLine), Is.EqualTo(LegacyReference.Month(2021, 2, "February", newLine)), "a month ending on a Sunday");
    }

    /// <summary>What a default writer produces on the machine running the tests.</summary>
    [Test]
    public void WithThePlatformsDefaultNewline_OnlyTheLiteralLineFeedDiffersFromIt()
    {
        string text = Month(5, 2023, Environment.NewLine);

        Assert.That(text, Is.EqualTo(LegacyReference.Month(2023, 5, "May", Environment.NewLine)));
        Assert.That(text, Does.EndWith("29 30 31\n" + Environment.NewLine).Using(StringComparison.Ordinal));
    }

    [Test]
    [Platform(Include = "Win", Reason = "Windows is where the default newline is \\r\\n.")]
    public void OnWindows_TheDefaultNewlineGivesMixedLineEndings()
    {
        string text = Month(5, 2023, Environment.NewLine);

        Assert.That(Environment.NewLine, Is.EqualTo("\r\n"));
        Assert.That(text, Does.StartWith("     May 2023\r\nMo Tu We Th Fr Sa Su\r\n").Using(StringComparison.Ordinal));
        Assert.That(text, Does.EndWith("22 23 24 25 26 27 28\r\n29 30 31\n\r\n").Using(StringComparison.Ordinal));
    }

    [Test]
    [Platform(Exclude = "Win", Reason = "Linux and macOS are where the default newline is \\n.")]
    public void OnLinuxAndMacOS_TheDefaultNewlineGivesLineFeedsOnly()
    {
        Assert.That(Environment.NewLine, Is.EqualTo("\n"));
        Assert.That(Month(5, 2023, Environment.NewLine), Ordinal.DoesNotContain("\r"));
    }
}
