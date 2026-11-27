using ConsoleCalendarGenerator.Tests.Support;

namespace ConsoleCalendarGenerator.Tests.Historical;

/// <summary>
/// What version 1.0.0 sent to the real standard output of a process, redirected: the frozen source of 1.0.0,
/// drawing a month in a child process. On Windows, where the console's line terminator is "\r\n", the last row
/// of days ended with a bare "\n", so the output had two kinds of line end. Version 2.0 corrects that (D-59);
/// these tests stay as the record. The corrected behavior is in Legacy/RedirectedOutputTests.cs.
/// </summary>
[TestFixture]
public class Published100RedirectedOutputTests
{
    private static ChildResult Run() => ChildProcess.RunRedirected(command: Program.RenderPublished100Command);

    [Test]
    public void In100_RedirectedStandardOutput_ReceivedThePlainText_WithABareLineFeedAfterTheLastRow()
    {
        ChildResult result = Run();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.ExitCode, Is.Zero, result.StandardError);
            Assert.That(result.StandardError, Is.Empty);
            Assert.That(result.StandardOutput, Is.EqualTo(ChildScenario.PlainTextOfPublished100(Environment.NewLine)));
        }
    }

    [Test]
    [Platform(Include = "Win", Reason = "Windows is where the console's newline is \\r\\n.")]
    public void In100_OnWindows_RedirectedStandardOutput_HadMixedLineEndings()
    {
        Assert.That(Run().StandardOutput, Is.EqualTo(
            "   February 2023\r\n" +
            "Mo Tu We Th Fr Sa Su\r\n" +
            "       1  2  3  4  5\r\n" +
            " 6  7  8  9 10 11 12\r\n" +
            "13 14 15 16 17 18 19\r\n" +
            "20 21 22 23 24 25 26\r\n" +
            "27 28\n" +
            "\r\n"));
    }

    [Test]
    [Platform(Exclude = "Win", Reason = "Linux and macOS are where the console's newline is \\n.")]
    public void In100_OnLinuxAndMacOS_RedirectedStandardOutput_HadLineFeedsOnly()
    {
        Assert.That(Run().StandardOutput, Is.EqualTo(Golden.Lines(
            "   February 2023",
            "Mo Tu We Th Fr Sa Su",
            "       1  2  3  4  5",
            " 6  7  8  9 10 11 12",
            "13 14 15 16 17 18 19",
            "20 21 22 23 24 25 26",
            "27 28",
            "")));
    }
}
