using ConsoleCalendarGenerator.Tests.Support;

namespace ConsoleCalendarGenerator.Tests.Documentation;

/// <summary>
/// The example application on a terminal. Like the other terminal tests these need Linux with the util-linux
/// "script" program and a terminal database entry for xterm-256color, are in the
/// <see cref="Categories.Terminal"/> category and ignore themselves where they cannot run.
/// </summary>
[TestFixture]
[Category(Categories.Terminal)]
public class ExampleProgramTerminalTests
{
    private static ChildResult RunOnPseudoTerminal()
    {
        if (!OperatingSystem.IsLinux() || ChildProcess.ScriptProgram == null)
        {
            Assert.Ignore("Needs Linux with the util-linux script program.");
        }
        ChildResult result = ChildProcess.RunProgram(ExampleProgram.AssemblyPath, onPseudoTerminal: true);
        Assert.That(result.ExitCode, Is.Zero, result.StandardError);
        return result;
    }

    [Test]
    public void OnATerminal_TheTextIsTheExpectedOutput_WithColorAdded()
    {
        ExampleProgram.AssumeTheMonthNamesTheExpectedOutputWasWrittenWith();

        ChildResult result = RunOnPseudoTerminal();

        Assert.That(TerminalOutput.ColorSequence.Matches(result.StandardOutput), Has.Count.GreaterThan(30));
        Assert.That(TerminalOutput.WithoutTerminalControl(result.StandardOutput), Is.EqualTo(ExampleProgram.ExpectedOutputOnThisMachine));
    }

    /// <summary>Nothing but color changes is sent after the program's first line: no clearing, no cursor movement.</summary>
    [Test]
    public void OnATerminal_TheProgramSendsOnlyColorChanges()
    {
        ChildResult result = RunOnPseudoTerminal();

        // What precedes the first heading is the .NET runtime preparing the terminal, not the program.
        string fromTheProgram = result.StandardOutput.Substring(result.StandardOutput.IndexOf("== A month ==", StringComparison.Ordinal));
        string withoutColorChanges = TerminalOutput.ColorSequence.Replace(fromTheProgram, string.Empty);

        Assert.That(withoutColorChanges, Ordinal.DoesNotContain(TerminalOutput.EscapeCharacter));
    }

    [Test]
    public void OnATerminal_TheLastColorChange_GivesBackTheDefaultColors()
    {
        ChildResult result = RunOnPseudoTerminal();

        string last = TerminalOutput.ColorSequence.Matches(result.StandardOutput)[^1].Value;

        Assert.That(last, Is.EqualTo(TerminalOutput.EscapeCharacter + "[39;49m").Or.EqualTo(TerminalOutput.EscapeCharacter + "[0m"));
    }
}
