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
    /// <summary>
    /// The complete output, on every system. Every example but one supplies its weekday labels, so the text is
    /// the same on Linux, macOS and Windows; the one culture-derived weekday row is compared with what this
    /// machine's culture data gives.
    /// </summary>
    [Test]
    public void WithItsOutputRedirected_ItPrintsTheExpectedOutput_AndExitsWithoutInput()
    {
        ExampleProgram.AssumeTheMonthNamesTheExpectedOutputWasWrittenWith();

        // Standard input is closed as soon as the program starts, and the run has a time limit: a program
        // that waited for a key or a line would fail here.
        ChildResult result = ChildProcess.RunProgram(ExampleProgram.AssemblyPath);

        Assert.That(result.ExitCode, Is.Zero, result.StandardError);
        Assert.That(result.StandardError, Is.Empty);
        Assert.That(WithLineFeeds(result.StandardOutput), Is.EqualTo(ExampleProgram.ExpectedOutputOnThisMachine));
    }

    /// <summary>
    /// The first example is what the README's quick start shows. It supplies its labels, so its text is in
    /// expected-output.txt exactly as every system prints it, with nothing replaced.
    /// </summary>
    [Test]
    public void ThePrimaryExample_PrintsTheSameTextOnEverySystem()
    {
        ExampleProgram.AssumeTheMonthNamesTheExpectedOutputWasWrittenWith();

        ChildResult result = ChildProcess.RunProgram(ExampleProgram.AssemblyPath);

        string[] primary = Section(WithLineFeeds(result.StandardOutput), "== A month ==");
        Assert.That(primary, Is.EqualTo(Section(ExampleProgram.ExpectedOutput, "== A month ==")));
        Assert.That(primary, Is.EqualTo(new[]
        {
            "     March 2027",
            "Mo Tu We Th Fr Sa Su",
            " 1  2  3  4  5  6  7",
            " 8  9 10 11 12 13 14",
            "15 16 17 18 19 20 21",
            "22 23 24 25 26 27 28",
            "29 30 31",
        }));
    }

    /// <summary>
    /// The one example that takes its labels from the culture prints what the runtime has for that culture on
    /// this machine, whatever that is. The expected row is built here from the culture's day names directly,
    /// not taken from the renderer and not written down.
    /// </summary>
    [Test]
    public void TheCultureDerivedExample_PrintsTheDayNamesOfThisMachine()
    {
        System.Globalization.DateTimeFormatInfo french = System.Globalization.CultureInfo.GetCultureInfo("fr-FR").DateTimeFormat;
        DayOfWeek[] mondayFirst = { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday };
        string[] names = mondayFirst.Select(day => french.GetShortestDayName(day)).ToArray();
        Assume.That(names.All(name => name.Length is 1 or 2 && name.All(char.IsAsciiLetter)),
            "This machine's shortest French day names are not one or two plain letters, so the row cannot be built this simply.");
        string expectedRow = string.Join(" ", names.Select(name => name.PadLeft(2)));

        ChildResult result = ChildProcess.RunProgram(ExampleProgram.AssemblyPath);

        string[] lines = WithLineFeeds(result.StandardOutput).Split('\n');
        Assert.That(lines[ExampleProgram.CultureDerivedWeekdayRowIndex(lines)], Is.EqualTo(expectedRow));
        Assert.That(ExampleProgram.CultureDerivedWeekdayRowOnThisMachine, Is.EqualTo(expectedRow));
    }

    /// <summary>
    /// Only that row may depend on the system: every other weekday row of the expected output is made of labels
    /// an example supplies. This fails if another example starts taking its labels from a culture.
    /// </summary>
    [Test]
    public void EveryOtherWeekdayRow_IsMadeOfLabelsAnExampleSupplies()
    {
        string source = Repository.Read(ExampleProgram.ExamplesFile);
        string[] lines = ExampleProgram.ExpectedOutput.Split('\n');
        int derived = ExampleProgram.CultureDerivedWeekdayRowIndex(lines);

        // A weekday row is the line below a title; in this output the titles are the lines that name March.
        int[] weekdayRows = Enumerable.Range(1, lines.Length - 1)
            .Where(index => lines[index - 1].Contains("March 2027", StringComparison.Ordinal) || lines[index - 1].Contains("mars 2027", StringComparison.Ordinal))
            .ToArray();

        Assert.That(weekdayRows, Has.Length.EqualTo(8), "ten headings, of which the segments and the layout print no month");
        Assert.That(weekdayRows, Has.Member(derived));
        foreach (int index in weekdayRows.Where(index => index != derived))
        {
            string[] labels = lines[index].Split(' ', StringSplitOptions.RemoveEmptyEntries);
            Assert.That(labels, Has.Length.EqualTo(7), lines[index]);
            foreach (string label in labels)
            {
                // Supplied in Examples.cs as a string, or part of the fixed English row of the legacy class.
                Assert.That(source.Contains("\"" + label + "\"", StringComparison.Ordinal) || "Mo Tu We Th Fr Sa Su".Contains(label, StringComparison.Ordinal),
                    "the label " + label + " in: " + lines[index]);
            }
        }
        Assert.That(Regex.Matches(source, @"\.WithWeekdayLabels\("), Has.Count.EqualTo(2), "the quick start and the French labels");
    }

    /// <summary>
    /// Every culture the program uses is named in its code, so the culture a process starts with changes
    /// nothing. On Linux the variables below change that culture; elsewhere they change nothing and the test
    /// only repeats the run. The comparison is with a run on the same machine, so it holds on every system.
    /// </summary>
    [TestCase("de_DE.UTF-8")]
    [TestCase("ja_JP.UTF-8")]
    [TestCase("C")]
    public void TheOutput_DoesNotDependOnTheCultureTheProcessStartsWith(string locale)
    {
        var environment = new Dictionary<string, string> { ["LANG"] = locale, ["LC_ALL"] = locale };

        ChildResult usual = ChildProcess.RunProgram(ExampleProgram.AssemblyPath);
        ChildResult result = ChildProcess.RunProgram(ExampleProgram.AssemblyPath, environment: environment);

        Assert.That(result.ExitCode, Is.Zero, result.StandardError);
        Assert.That(usual.StandardOutput, Is.Not.Empty);
        Assert.That(result.StandardOutput, Is.EqualTo(usual.StandardOutput));
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

    /// <summary>The lines the program prints under a heading, up to the empty line that ends them.</summary>
    private static string[] Section(string output, string heading)
    {
        string[] lines = output.Split('\n');
        return lines.Skip(Array.IndexOf(lines, heading) + 1).TakeWhile(line => line.Length > 0).ToArray();
    }
}
