using System.Reflection;
using System.Text.RegularExpressions;
using ConsoleCalendarGenerator.Tests.Support;
using ConsoleCalendarGeneratorExample;

namespace ConsoleCalendarGenerator.Tests.Documentation;

/// <summary>
/// The example application: it runs unattended, prints exactly what expected-output.txt says, and is written
/// the way the README recommends. The README's own code and output are compared with it in
/// <see cref="ReadmeExamplesTests"/>.
/// </summary>
[TestFixture]
public class ExampleProgramTests
{
    [Test]
    public void WithItsOutputRedirected_ItPrintsTheExpectedOutput_AndExitsWithoutInput()
    {
        // Standard input is closed as soon as the program starts, and the run has a time limit: a program
        // that waited for a key or a line would fail here.
        ExampleProgram.AssumeTheCultureDataTheExpectedOutputWasWrittenWith();

        ChildResult result = ChildProcess.RunProgram(ExampleProgram.AssemblyPath);

        Assert.That(result.ExitCode, Is.Zero, result.StandardError);
        Assert.That(result.StandardError, Is.Empty);
        Assert.That(WithLineFeeds(result.StandardOutput), Is.EqualTo(ExampleProgram.ExpectedOutput));
    }

    [Test]
    public void RedirectedOutput_HasNoEscapeSequences()
    {
        ChildResult result = ChildProcess.RunProgram(ExampleProgram.AssemblyPath);

        Assert.That(result.ExitCode, Is.Zero, result.StandardError);
        Assert.That(result.StandardOutput, Is.Not.Empty);
        Assert.That(result.StandardOutput, Ordinal.DoesNotContain(TerminalOutput.EscapeCharacter));
    }

    /// <summary>
    /// Every culture the program uses is named in its code. On Linux the variables below change the culture a
    /// .NET process starts with; elsewhere they change nothing and the test only repeats the run.
    /// </summary>
    [TestCase("de_DE.UTF-8")]
    [TestCase("ja_JP.UTF-8")]
    [TestCase("C")]
    public void TheOutput_DoesNotDependOnTheCultureOfTheMachine(string locale)
    {
        ExampleProgram.AssumeTheCultureDataTheExpectedOutputWasWrittenWith();
        var environment = new Dictionary<string, string> { ["LANG"] = locale, ["LC_ALL"] = locale };

        ChildResult result = ChildProcess.RunProgram(ExampleProgram.AssemblyPath, environment: environment);

        Assert.That(result.ExitCode, Is.Zero, result.StandardError);
        Assert.That(WithLineFeeds(result.StandardOutput), Is.EqualTo(ExampleProgram.ExpectedOutput));
    }

    [Test]
    public void TheExpectedOutput_IsPlainAsciiText()
    {
        Assert.That(ExampleProgram.ExpectedOutput.Where(c => c != '\n' && (c < ' ' || c > '~')), Is.Empty);
    }

    [Test]
    public void TheExpectedOutput_FitsANarrowTerminal()
    {
        Assert.That(ExampleProgram.ExpectedOutput.Split('\n').Max(line => line.Length), Is.LessThanOrEqualTo(60));
    }

    /// <summary>
    /// Examples.cs is compiled into this test assembly as well as into the program, so its code compiles against
    /// the build of the library these tests run against: net10.0 in the normal run, netstandard2.1 in the other.
    /// </summary>
    [Test]
    public void EveryExample_IsCompiledAgainstTheLibraryBuildUnderTest()
    {
        string[] compiled = typeof(Examples)
            .GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .Select(method => method.Name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        Assert.That(compiled, Is.EqualTo(ExampleProgram.Examples().Keys.OrderBy(name => name, StringComparer.Ordinal)),
            "one method per marked example, each named like its example");
        Assert.That(typeof(Examples).Assembly, Is.SameAs(typeof(ExampleProgramTests).Assembly));
    }

    [Test]
    public void TheProgram_RunsEveryExample()
    {
        string program = Repository.Read(ExampleProgram.ProgramFile);

        foreach (string name in ExampleProgram.Examples().Keys)
        {
            Assert.That(program, Ordinal.Contains("Examples." + name + "("), name + " is never called");
        }
    }

    // ---- how the example is written ----

    private static readonly string[] SourceFiles = { ExampleProgram.ProgramFile, ExampleProgram.ExamplesFile };

    [TestCaseSource(nameof(SourceFiles))]
    public void TheSource_NeverWaitsForInput_AndNeverClearsOrResetsTheTerminal(string file)
    {
        string source = Repository.Read(file);

        foreach (string forbidden in new[] { "ReadKey", "ReadLine", "Console.Read(", "Console.In", "Console.Clear", "ResetColor", "SetCursorPosition", "KeyAvailable" })
        {
            Assert.That(source, Ordinal.DoesNotContain(forbidden));
        }
    }

    [TestCaseSource(nameof(SourceFiles))]
    public void TheSource_WritesNoEscapeSequencesOfItsOwn(string file)
    {
        string source = Repository.Read(file);

        Assert.That(source, Ordinal.DoesNotContain(TerminalOutput.EscapeCharacter));
        Assert.That(Regex.IsMatch(source, @"\\u001[bB]|\\x1[bB]|\\e|\(char\)\s*(27|0x1[bB])"), Is.False, "an escape character written as code");
    }

    [TestCaseSource(nameof(SourceFiles))]
    public void TheSource_UsesFixedDates_NotTheClock(string file)
    {
        string source = Repository.Read(file);

        foreach (string forbidden in new[] { "DateTime.Now", "DateTime.Today", "DateTime.UtcNow", "DateTimeOffset.Now", "DateTimeOffset.UtcNow", "TimeProvider", "Environment.TickCount", "Random" })
        {
            Assert.That(source, Ordinal.DoesNotContain(forbidden));
        }
    }

    /// <summary>
    /// The legacy constructors that take colors assign them to other elements than their names say. The example
    /// must not present them as the way to use the class.
    /// </summary>
    [TestCaseSource(nameof(SourceFiles))]
    public void TheSource_CreatesTheLegacyCalendar_OnlyWithItsParameterlessConstructor(string file)
    {
        MatchCollection creations = Regex.Matches(Repository.Read(file), @"new\s+(CalendarGenerator\.)?ConsoleCalendar\s*\(([^)]*)\)");

        Assert.That(creations.Select(creation => creation.Groups[2].Value.Trim()), Is.All.Empty);
        if (file == ExampleProgram.ExamplesFile)
        {
            Assert.That(creations, Has.Count.EqualTo(1), "the example shows the legacy class once");
        }
    }

    [Test]
    public void TheExampleNeverReadsTheCurrentCulture_ExceptToSetItForTheLegacyClass()
    {
        string source = Repository.Read(ExampleProgram.ExamplesFile);

        MatchCollection uses = Regex.Matches(source, @"Current(UI)?Culture[^\n]*");

        Assert.That(uses.Select(use => use.Value), Is.EqualTo(new[] { "CurrentCulture = CultureInfo.GetCultureInfo(\"en-US\");" }));
        Assert.That(Repository.Read(ExampleProgram.ProgramFile), Does.Not.Match("CultureInfo|Current(UI)?Culture"));
    }

    private static string WithLineFeeds(string output) => output.Replace("\r\n", "\n", StringComparison.Ordinal);
}
