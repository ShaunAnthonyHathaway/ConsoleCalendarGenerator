using System.Globalization;
using System.Reflection;
using System.Text.RegularExpressions;

namespace ConsoleCalendarGenerator.Tests.Support;

/// <summary>
/// The example application in ConsoleCalendarGeneratorExample: its source, the examples marked in it, what it
/// is expected to print, and the built program.
/// </summary>
internal static class ExampleProgram
{
    public const string ExamplesFile = "ConsoleCalendarGeneratorExample/Examples.cs";
    public const string ProgramFile = "ConsoleCalendarGeneratorExample/Program.cs";
    public const string ExpectedOutputFile = "ConsoleCalendarGeneratorExample/expected-output.txt";

    private static readonly Regex Opening = new(@"^\s*// <(\w+)>$");
    private static readonly Regex Closing = new(@"^\s*// </(\w+)>$");
    private static readonly Regex UsingDirective = new(@"^using [\w.]+;$");

    /// <summary>What the program prints when its output is redirected, with "\n" line ends.</summary>
    public static string ExpectedOutput => Repository.Read(ExpectedOutputFile);

    /// <summary>The using directives at the top of Examples.cs.</summary>
    public static IReadOnlyList<string> UsingDirectives =>
        Repository.Read(ExamplesFile).Split('\n').Where(line => UsingDirective.IsMatch(line)).ToArray();

    /// <summary>
    /// The examples of Examples.cs by name: the lines between a "// &lt;Name&gt;" comment and its
    /// "// &lt;/Name&gt;" comment, without the indentation of the comment.
    /// </summary>
    public static IReadOnlyDictionary<string, string> Examples()
    {
        var examples = new Dictionary<string, string>(StringComparer.Ordinal);
        string? name = null;
        string indent = string.Empty;
        var lines = new List<string>();
        foreach (string line in Repository.Read(ExamplesFile).Split('\n'))
        {
            Match opening = Opening.Match(line);
            Match closing = Closing.Match(line);
            if (opening.Success)
            {
                if (name != null)
                {
                    throw new FormatException("Example " + opening.Groups[1].Value + " begins inside example " + name + ".");
                }
                name = opening.Groups[1].Value;
                indent = line.Substring(0, line.Length - line.TrimStart().Length);
                lines.Clear();
            }
            else if (closing.Success)
            {
                if (closing.Groups[1].Value != name)
                {
                    throw new FormatException("The end of example " + closing.Groups[1].Value + " does not match its beginning.");
                }
                examples.Add(name, string.Join("\n", lines));
                name = null;
            }
            else if (name != null)
            {
                if (line.Length > 0 && !line.StartsWith(indent, StringComparison.Ordinal))
                {
                    throw new FormatException("A line of example " + name + " is indented less than its opening comment.");
                }
                lines.Add(line.Length > 0 ? line.Substring(indent.Length) : line);
            }
        }
        if (name != null)
        {
            throw new FormatException("Example " + name + " has no end.");
        }
        return examples;
    }

    /// <summary>The heading under which the program prints a month labeled with the culture's own day names.</summary>
    public const string CultureDerivedHeading = "== The culture's own day names ==";

    /// <summary>
    /// The weekday row of that month as this machine's culture data gives it. It is the one line of the
    /// program's output that is allowed to differ between systems: .NET's shortest French day names are two
    /// letters on Linux and macOS and one letter on Windows.
    /// </summary>
    public static string CultureDerivedWeekdayRowOnThisMachine =>
        MonthRenderer.Render(2027, 3, MonthRenderOptions.Default.WithCulture(CultureInfo.GetCultureInfo("fr-FR"))).Lines[1].Text;

    /// <summary>Where that row is in the output: two lines below its heading, after the title.</summary>
    public static int CultureDerivedWeekdayRowIndex(string[] lines) => Array.IndexOf(lines, CultureDerivedHeading) + 2;

    /// <summary>
    /// What the program must print on this machine: expected-output.txt, which was written on Linux, with the
    /// one culture-derived weekday row replaced by what this machine's culture data gives. Every other line is
    /// the same on every system, because every other example supplies its weekday labels.
    /// </summary>
    public static string ExpectedOutputOnThisMachine
    {
        get
        {
            string[] lines = ExpectedOutput.Split('\n');
            lines[CultureDerivedWeekdayRowIndex(lines)] = CultureDerivedWeekdayRowOnThisMachine;
            return string.Join("\n", lines);
        }
    }

    /// <summary>
    /// The titles of the expected output contain the English and the French name of March, which come from the
    /// culture data of the machine. They are the same on Linux, macOS and Windows. A test that compares real
    /// output with the expected output is inconclusive, not failed, on a machine that has other names, such as
    /// one that runs .NET without globalization data.
    /// </summary>
    public static void AssumeTheMonthNamesTheExpectedOutputWasWrittenWith()
    {
        foreach ((string culture, string title) in new[] { ("en-US", "March 2027"), ("fr-FR", "mars 2027") })
        {
            RenderedMonth month = MonthRenderer.Render(2027, 3, MonthRenderOptions.Default.WithCulture(CultureInfo.GetCultureInfo(culture)));
            Assume.That(month.Lines[0].Text.Trim(), Is.EqualTo(title),
                "The culture data of this machine gives another month name for " + culture + " than the expected output was written with.");
        }
    }

    /// <summary>The built program. It is built with the solution, in the configuration these tests were built in.</summary>
    public static string AssemblyPath
    {
        get
        {
            string configuration = typeof(ExampleProgram).Assembly.GetCustomAttribute<AssemblyConfigurationAttribute>()?.Configuration
                ?? throw new InvalidOperationException("The test assembly does not say which configuration it was built in.");
            string path = Repository.PathOf("ConsoleCalendarGeneratorExample/bin/" + configuration + "/net10.0/ConsoleCalendarGeneratorExample.dll");
            if (!File.Exists(path))
            {
                throw new FileNotFoundException("The example program has not been built. Build the solution, or the test project, first.", path);
            }
            return path;
        }
    }
}
