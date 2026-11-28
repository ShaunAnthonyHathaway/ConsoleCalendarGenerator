using ConsoleCalendarGenerator.Tests.Support;

namespace ConsoleCalendarGenerator.Tests.Documentation;

/// <summary>
/// The code and the output shown in README.md are the code and the output of the example application.
///
/// Examples.cs is the source of truth for the code: every C# block of the README must be, character for
/// character, one of the examples marked in that file. expected-output.txt is the source of truth for the
/// output: every output block must be lines of it, and <see cref="ExampleProgramTests"/> checks that the
/// program really prints that file. To change an example, change Examples.cs and copy it to the README.
/// </summary>
[TestFixture]
public class ReadmeExamplesTests
{
    private static IReadOnlyList<FencedBlock> Blocks => Markdown.FencedBlocks(Repository.Read("README.md"));

    private static IEnumerable<FencedBlock> CodeBlocks => Blocks.Where(block => block.Language == "csharp");

    /// <summary>Text blocks that are commands to type; the other text blocks are output.</summary>
    private static bool IsCommands(FencedBlock block) =>
        block.Text.Split('\n').All(line => line.StartsWith("dotnet ", StringComparison.Ordinal));

    /// <summary>A block may begin with using directives and an empty line; the example follows.</summary>
    private static (string[] Usings, string Code) Split(FencedBlock block)
    {
        string[] lines = block.Text.Split('\n');
        int usings = lines.TakeWhile(line => line.StartsWith("using ", StringComparison.Ordinal) && !line.StartsWith("using var ", StringComparison.Ordinal)).Count();
        if (usings == 0)
        {
            return (Array.Empty<string>(), block.Text);
        }
        Assert.That(lines[usings], Is.Empty, "an empty line separates the using directives from the example");
        return (lines.Take(usings).ToArray(), string.Join("\n", lines.Skip(usings + 1)));
    }

    [Test]
    public void EveryBlock_IsCSharpOrText()
    {
        Assert.That(Blocks.Select(block => block.Language).Distinct(), Is.EquivalentTo(new[] { "csharp", "text" }));
    }

    [Test]
    public void EveryCodeBlock_IsAnExampleOfTheExampleProgram_CharacterForCharacter()
    {
        IReadOnlyDictionary<string, string> examples = ExampleProgram.Examples();

        foreach (FencedBlock block in CodeBlocks)
        {
            string code = Split(block).Code;
            Assert.That(examples.Values, Has.Member(code),
                "This block of README.md is not one of the examples in " + ExampleProgram.ExamplesFile + ":\n" + code);
        }
    }

    [Test]
    public void EveryExampleOfTheProgram_IsShownInTheReadme_Once()
    {
        string[] shown = CodeBlocks.Select(block => Split(block).Code).ToArray();

        foreach (KeyValuePair<string, string> example in ExampleProgram.Examples())
        {
            Assert.That(shown.Count(code => code == example.Value), Is.EqualTo(1), "example " + example.Key);
        }
    }

    [Test]
    public void TheProgramHasTheNineExamples()
    {
        Assert.That(ExampleProgram.Examples().Keys, Is.EqualTo(new[]
        {
            "QuickStart", "FirstDayOfWeek", "Localized", "MarkedDates", "Colors", "AnyTextWriter", "Segments", "Layout", "OriginalApi",
        }));
    }

    /// <summary>
    /// The first example shows the using directives a reader needs, and they are exactly the ones Examples.cs
    /// compiles with (besides the implicit ones of a console project, which the README says it assumes).
    /// </summary>
    [Test]
    public void TheFirstCodeBlock_ShowsExactlyTheUsingDirectivesTheExamplesCompileWith()
    {
        (string[] usings, string code) = Split(CodeBlocks.First());

        Assert.That(usings, Is.EqualTo(ExampleProgram.UsingDirectives));
        Assert.That(usings, Is.Not.Empty);
        Assert.That(code, Is.EqualTo(ExampleProgram.Examples()["QuickStart"]));
        Assert.That(CodeBlocks.Skip(1).Select(block => Split(block).Usings), Is.All.Empty, "the directives are shown once");
    }

    /// <summary>
    /// The first example names its culture, so the title is not the invariant one, and supplies its weekday
    /// labels, Sunday first, so the text is the same on every operating system.
    /// </summary>
    [Test]
    public void TheFirstExample_ChoosesACultureAndItsWeekdayLabelsExplicitly()
    {
        string code = Split(CodeBlocks.First()).Code;

        Assert.That(code, Ordinal.Contains(".WithCulture(CultureInfo.GetCultureInfo(\"en-US\"))"));
        Assert.That(code, Ordinal.Contains(".WithWeekdayLabels(new[] { \"Su\", \"Mo\", \"Tu\", \"We\", \"Th\", \"Fr\", \"Sa\" })"));
    }

    /// <summary>
    /// The output block right after the first example is the first section of the expected output: what the
    /// quick start prints on every system.
    /// </summary>
    [Test]
    public void TheFirstOutputBlock_IsWhatThePrimaryExamplePrints()
    {
        string firstOutput = Blocks.First(block => block.Language == "text" && !IsCommands(block)).Text;

        Assert.That(ExampleProgram.ExpectedOutput, Does.StartWith("\n== Quick start ==\n" + firstOutput + "\n\n").Using(StringComparison.Ordinal));
    }

    /// <summary>
    /// Every example that starts from the default options names its culture and supplies its weekday labels,
    /// so what the README shows is what every operating system prints. The other examples build on those.
    /// </summary>
    [Test]
    public void EveryExampleThatStartsFromTheDefaults_NamesItsCultureAndSuppliesItsLabels()
    {
        KeyValuePair<string, string>[] fromTheDefaults = ExampleProgram.Examples()
            .Where(example => example.Value.Contains("MonthRenderOptions.Default", StringComparison.Ordinal))
            .ToArray();

        Assert.That(fromTheDefaults.Select(example => example.Key), Is.EqualTo(new[] { "QuickStart", "Localized" }));
        foreach (KeyValuePair<string, string> example in fromTheDefaults)
        {
            Assert.That(example.Value, Ordinal.Contains(".WithCulture(CultureInfo.GetCultureInfo(\""), example.Key);
            Assert.That(example.Value, Ordinal.Contains(".WithWeekdayLabels(new[] { \""), example.Key);
        }
    }

    [Test]
    public void EveryOutputBlock_IsWhatTheExampleProgramPrints()
    {
        string expected = "\n" + ExampleProgram.ExpectedOutput;
        FencedBlock[] outputs = Blocks.Where(block => block.Language == "text" && !IsCommands(block)).ToArray();

        Assert.That(outputs, Has.Length.GreaterThanOrEqualTo(7));
        foreach (FencedBlock output in outputs)
        {
            Assert.That(expected, Ordinal.Contains("\n" + output.Text + "\n"),
                "This output block of README.md is not in " + ExampleProgram.ExpectedOutputFile + ":\n" + output.Text);
        }
    }

    /// <summary>The marked month is shown as text, where the markers and their explanation can be seen without color.</summary>
    [Test]
    public void TheMarkedMonthShownAsOutput_HasItsMarkersAndItsLegend()
    {
        string marked = Blocks.Single(block => block.Language == "text" && block.Text.Contains("(* = holiday)", StringComparison.Ordinal)).Text;

        Assert.That(marked, Ordinal.Contains(" *5 "));
        Assert.That(marked, Ordinal.Contains("*17"));
        Assert.That(marked, Ordinal.Contains("*26"));
    }

    [Test]
    public void TheInstallationCommand_IsTheDotNetCommandForThePackage_WithItsVersion()
    {
        string version = System.Text.RegularExpressions.Regex.Match(
            Repository.Read("ConsoleCalendarGenerator/ConsoleCalendarGenerator.csproj"), "<VersionPrefix>([^<]+)</VersionPrefix>").Groups[1].Value;

        Assert.That(version, Does.Match(@"^\d+\.\d+\.\d+$"));
        Assert.That(Blocks.Where(IsCommands).Select(block => block.Text), Has.Member("dotnet add package ConsoleCalendarGenerator --version " + version));
    }
}
