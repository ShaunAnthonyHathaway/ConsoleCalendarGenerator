using ConsoleCalendarGenerator.Tests.Support;

namespace ConsoleCalendarGenerator.Tests.Legacy;

/// <summary>
/// The real console streams. These tests start a separate process (see Program.cs) that draws one calendar to
/// its own Console.Out, with standard output redirected to a pipe, and look at the bytes that arrive. That is
/// what a program sees when it captures or pipes the output of an application using the library.
/// </summary>
[TestFixture]
public class RedirectedOutputTests
{
    [Test]
    public void RedirectedStandardOutput_ReceivesThePlainTextOnly()
    {
        ChildResult result = ChildProcess.RunRedirected();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.ExitCode, Is.Zero, result.StandardError);
            Assert.That(result.StandardError, Is.Empty);
            Assert.That(result.StandardOutput, Is.EqualTo(ChildScenario.PlainText(Environment.NewLine)));
            Assert.That(result.StandardOutput, Ordinal.DoesNotContain("\u001b"), "no color sequences although four colors are configured");
        }
    }

    /// <summary>
    /// On Windows every line ends with a carriage return and a line feed, the last row of days included. Version
    /// 1.0.0 ended that row with a bare line feed; what it sent is kept in
    /// Historical/Published100RedirectedOutputTests.cs.
    /// </summary>
    [Test]
    [Platform(Include = "Win", Reason = "Windows is where the console's newline is \\r\\n.")]
    public void OnWindows_RedirectedStandardOutput_EndsEveryLineWithCarriageReturnLineFeed()
    {
        ChildResult result = ChildProcess.RunRedirected();

        Assert.That(result.StandardOutput, Is.EqualTo(
            "   February 2023\r\n" +
            "Mo Tu We Th Fr Sa Su\r\n" +
            "       1  2  3  4  5\r\n" +
            " 6  7  8  9 10 11 12\r\n" +
            "13 14 15 16 17 18 19\r\n" +
            "20 21 22 23 24 25 26\r\n" +
            "27 28\r\n" +
            "\r\n"));
        Assert.That(result.StandardOutput.Replace("\r\n", string.Empty), Ordinal.DoesNotContain("\n"), "no bare line feed is left");
    }

    [Test]
    [Platform(Exclude = "Win", Reason = "Linux and macOS are where the console's newline is \\n.")]
    public void OnLinuxAndMacOS_RedirectedStandardOutput_HasLineFeedsOnly()
    {
        ChildResult result = ChildProcess.RunRedirected();

        Assert.That(result.StandardOutput, Is.EqualTo(Golden.Lines(
            "   February 2023",
            "Mo Tu We Th Fr Sa Su",
            "       1  2  3  4  5",
            " 6  7  8  9 10 11 12",
            "13 14 15 16 17 18 19",
            "20 21 22 23 24 25 26",
            "27 28",
            "")));
    }

    /// <summary>Guards the harness: started without its command, the test assembly draws nothing.</summary>
    [Test]
    public void TheChildProcess_WithoutItsCommand_WritesNothingToStandardOutput()
    {
        ChildResult result = ChildProcess.RunWithoutCommand();

        Assert.That(result.ExitCode, Is.EqualTo(64));
        Assert.That(result.StandardOutput, Is.Empty);
    }
}
