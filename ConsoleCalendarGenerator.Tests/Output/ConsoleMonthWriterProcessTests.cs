using ConsoleCalendarGenerator.Tests.Support;

namespace ConsoleCalendarGenerator.Tests.Output;

/// <summary>
/// The real console streams. These tests start a separate process (see Program.cs) that writes one month with
/// <see cref="ConsoleMonthWriter"/> and a theme full of colors, and look at what arrives on standard output.
/// </summary>
[TestFixture]
public class ConsoleMonthWriterProcessTests
{
    /// <summary>
    /// Redirected to a pipe, as when a program's output is captured or sent to a file: the text arrives plain,
    /// with the platform's line terminator, although the theme has four colors. On Linux and macOS the runtime
    /// does not turn colors into escape sequences when output is redirected; on Windows colors were never part
    /// of the stream.
    /// </summary>
    [Test]
    public void RedirectedStandardOutput_ReceivesThePlainTextOnly()
    {
        ChildResult result = ChildProcess.RunRedirected(command: Program.WriteModernCommand);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.ExitCode, Is.Zero, result.StandardError);
            Assert.That(result.StandardError, Is.Empty);
            Assert.That(result.StandardOutput, Is.EqualTo(ModernChildScenario.PlainText(Environment.NewLine)));
            Assert.That(result.StandardOutput, Ordinal.DoesNotContain(TerminalOutput.EscapeCharacter));
        }
    }

    [Test]
    [Platform(Include = "Win", Reason = "Windows is where the console's line terminator is \\r\\n.")]
    public void OnWindows_EveryLineEndsWithCarriageReturnLineFeed_WithNoneMixedIn()
    {
        ChildResult result = ChildProcess.RunRedirected(command: Program.WriteModernCommand);

        Assert.That(result.StandardOutput, Is.EqualTo(ModernChildScenario.PlainText("\r\n")));
        Assert.That(result.StandardOutput.Replace("\r\n", string.Empty), Ordinal.DoesNotContain("\n"), "unlike the legacy class, no bare line feed");
    }

    [Test]
    [Platform(Exclude = "Win", Reason = "Linux and macOS are where the console's line terminator is \\n.")]
    public void OnLinuxAndMacOS_EveryLineEndsWithALineFeed()
    {
        ChildResult result = ChildProcess.RunRedirected(command: Program.WriteModernCommand);

        Assert.That(result.StandardOutput, Is.EqualTo(ModernChildScenario.PlainText("\n")));
    }
}
