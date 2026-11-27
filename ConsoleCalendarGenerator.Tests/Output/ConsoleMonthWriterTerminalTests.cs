using ConsoleCalendarGenerator.Tests.Support;

namespace ConsoleCalendarGenerator.Tests.Output;

/// <summary>
/// Color on a terminal, for the modern console writer. Like the legacy terminal tests these depend on the
/// machine (a terminal database entry for xterm-256color and, for the pseudo-terminal tests, Linux with the
/// util-linux "script" program), are in the <see cref="Categories.Terminal"/> category, are left out of the
/// normal run and ignore themselves where they cannot run.
///
/// The escape sequences are written by the .NET runtime, not by the library, so they are not compared byte for
/// byte: the tests check that color sequences are present or absent and that the text between them is exactly
/// the plain month.
/// </summary>
[TestFixture]
[Category(Categories.Terminal)]
public class ConsoleMonthWriterTerminalTests
{
    [Test]
    public void RedirectedOutput_WithTheRuntimesColorOptIn_CarriesColorSequencesAroundTheSameText()
    {
        if (OperatingSystem.IsWindows())
        {
            Assert.Ignore("On Windows console colors are not escape sequences in the output stream.");
        }

        ChildResult result = ChildProcess.RunRedirected(allowAnsiWhenRedirected: true, command: Program.WriteModernCommand);

        Assert.That(result.ExitCode, Is.Zero, result.StandardError);
        Assert.That(TerminalOutput.ColorSequence.Matches(result.StandardOutput), Has.Count.GreaterThan(28), "at least one color change per day");
        Assert.That(TerminalOutput.WithoutEscapeSequences(result.StandardOutput), Is.EqualTo(ModernChildScenario.PlainText("\n")));
    }

    [Test]
    public void OnAPseudoTerminal_TheMonthIsColored_AndTheTextIsUnchanged()
    {
        RequirePseudoTerminal();

        ChildResult result = ChildProcess.RunOnPseudoTerminal(noColor: false, command: Program.WriteModernCommand);

        Assert.That(result.ExitCode, Is.Zero, result.StandardError);
        Assert.That(TerminalOutput.ColorSequence.Matches(result.StandardOutput), Has.Count.GreaterThan(28), "at least one color change per day");
        Assert.That(TerminalOutput.WithoutTerminalControl(result.StandardOutput), Is.EqualTo(ModernChildScenario.PlainText("\n")));
    }

    /// <summary>The writer goes through Console's color API, so the runtime's NO_COLOR handling applies to it.</summary>
    [Test]
    public void OnAPseudoTerminal_WithNoColorSet_NoColorSequencesAreWritten()
    {
        RequirePseudoTerminal();

        ChildResult result = ChildProcess.RunOnPseudoTerminal(noColor: true, command: Program.WriteModernCommand);

        Assert.That(result.ExitCode, Is.Zero, result.StandardError);
        Assert.That(TerminalOutput.ColorSequence.Matches(result.StandardOutput), Is.Empty);
        Assert.That(TerminalOutput.WithoutTerminalControl(result.StandardOutput), Is.EqualTo(ModernChildScenario.PlainText("\n")));
    }

    private static void RequirePseudoTerminal()
    {
        if (!OperatingSystem.IsLinux())
        {
            Assert.Ignore("The pseudo-terminal tests use the Linux 'script' program.");
        }
        if (ChildProcess.ScriptProgram == null)
        {
            Assert.Ignore("The 'script' program (util-linux) is not installed.");
        }
    }
}
