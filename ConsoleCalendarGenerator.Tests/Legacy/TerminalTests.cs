using System.Text.RegularExpressions;
using ConsoleCalendarGenerator.Tests.Support;

namespace ConsoleCalendarGenerator.Tests.Legacy;

/// <summary>
/// Color on a terminal. These tests depend on the machine: on a terminal database entry for xterm-256color and,
/// for the pseudo-terminal tests, on Linux with the util-linux "script" program. They are in the
/// <see cref="Categories.Terminal"/> category, are left out of the normal run and ignore themselves where the
/// machine cannot run them.
///
/// The escape sequences themselves are written by the .NET runtime, not by the library, so the tests do not
/// compare them byte for byte. They check that color sequences are present where the runtime emits them, absent
/// where it does not, and that the text between them is exactly the plain calendar.
/// </summary>
[TestFixture]
[Category(Categories.Terminal)]
public class TerminalTests
{
    private static readonly Regex ColorSequence = new(@"\u001b\[[0-9;]*m");
    private static readonly Regex AnyEscapeSequence = new(@"\u001b\[[0-?]*[ -/]*[@-~]|\u001b[=>]");

    [Test]
    public void RedirectedOutput_WithTheRuntimesColorOptIn_CarriesColorSequencesAroundTheSameText()
    {
        if (OperatingSystem.IsWindows())
        {
            Assert.Ignore("On Windows console colors are not escape sequences in the output stream.");
        }

        ChildResult result = ChildProcess.RunRedirected(allowAnsiWhenRedirected: true);

        Assert.That(result.ExitCode, Is.Zero, result.StandardError);
        Assert.That(ColorSequence.Matches(result.StandardOutput), Has.Count.GreaterThan(28), "at least one color change per day");
        Assert.That(AnyEscapeSequence.Replace(result.StandardOutput, string.Empty), Is.EqualTo(ChildScenario.PlainText("\n")));
    }

    [Test]
    public void OnAPseudoTerminal_TheCalendarIsColored_AndTheTextIsUnchanged()
    {
        RequirePseudoTerminal();

        ChildResult result = ChildProcess.RunOnPseudoTerminal(noColor: false);

        Assert.That(result.ExitCode, Is.Zero, result.StandardError);
        Assert.That(ColorSequence.Matches(result.StandardOutput), Has.Count.GreaterThan(28), "at least one color change per day");
        Assert.That(WithoutTerminalControl(result.StandardOutput), Is.EqualTo(ChildScenario.PlainText("\n")));
    }

    /// <summary>The library goes through Console's color API, so the runtime's NO_COLOR handling applies to it.</summary>
    [Test]
    public void OnAPseudoTerminal_WithNoColorSet_NoColorSequencesAreWritten()
    {
        RequirePseudoTerminal();

        ChildResult result = ChildProcess.RunOnPseudoTerminal(noColor: true);

        Assert.That(result.ExitCode, Is.Zero, result.StandardError);
        Assert.That(ColorSequence.Matches(result.StandardOutput), Is.Empty);
        Assert.That(WithoutTerminalControl(result.StandardOutput), Is.EqualTo(ChildScenario.PlainText("\n")));
    }

    // A terminal turns "\n" into "\r\n"; removing the carriage returns gives back what the program wrote.
    private static string WithoutTerminalControl(string output) =>
        AnyEscapeSequence.Replace(output, string.Empty).Replace("\r", string.Empty);

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
