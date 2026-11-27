using System.Text.RegularExpressions;
using ConsoleCalendarGenerator.Tests.Support;

namespace ConsoleCalendarGenerator.Tests.Documentation;

/// <summary>
/// Facts the public documents state, checked against the repository: which documents exist, which frameworks
/// and versions they name, and which commands they tell a reader to run. The wording is not tested.
/// </summary>
[TestFixture]
public class PublicDocumentationTests
{
    private static readonly string[] Documents = Repository.PublicDocuments;

    private static readonly string[] ProjectFiles =
    {
        "ConsoleCalendarGenerator/ConsoleCalendarGenerator.csproj",
        "ConsoleCalendarGeneratorExample/ConsoleCalendarGeneratorExample.csproj",
        "ConsoleCalendarGenerator.Tests/ConsoleCalendarGenerator.Tests.csproj",
    };

    private static string[] TargetFrameworksOf(string projectFile) =>
        Regex.Match(Repository.Read(projectFile), @"<TargetFrameworks?>([^<]+)</TargetFrameworks?>").Groups[1].Value
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    // ---- which documents exist ----

    /// <summary>
    /// The complete list. A document added anywhere else, such as a second copy of the readme beside the
    /// library, fails here.
    /// </summary>
    [Test]
    public void TheMarkdownFilesOfTheRepository_AreExactlyThese()
    {
        string[] outsideTheMaintenanceNotes = Repository.FilesWithExtension(".md")
            .Where(file => !file.StartsWith("docs/ai-context/", StringComparison.Ordinal))
            .ToArray();

        Assert.That(outsideTheMaintenanceNotes, Is.EqualTo(new[]
        {
            "CHANGELOG.md",
            "CLAUDE.md",
            "CONTRIBUTING.md",
            "ConsoleCalendarGenerator.Tests/Parity/Campertron/README.md",
            "README.md",
            "SECURITY.md",
        }));
    }

    [Test]
    public void TheLibraryFolder_HoldsOnlySourceAndItsProjectFile()
    {
        Assert.That(Repository.HasFolder("ConsoleCalendarGenerator/docs"), Is.False, "the copy of the readme and the 1.0.0 screenshots are removed");
        Assert.That(Repository.FilesWithExtension(".md", ".png", ".txt").Where(file => file.StartsWith("ConsoleCalendarGenerator/", StringComparison.Ordinal)), Is.Empty);
    }

    [Test]
    public void ThePackageReadme_IsTheRootReadme()
    {
        string project = Repository.Read(ProjectFiles[0]);

        Assert.That(project, Ordinal.Contains("<PackageReadmeFile>README.md</PackageReadmeFile>"));
        Assert.That(project, Ordinal.Contains("<None Include=\"..\\README.md\" Pack=\"true\""));
    }

    // ---- frameworks ----

    [TestCaseSource(nameof(Documents))]
    public void EveryTargetFrameworkNamed_IsOneAProjectOfTheRepositoryTargets(string document)
    {
        string[] targeted = ProjectFiles.SelectMany(TargetFrameworksOf).Distinct().ToArray();

        IEnumerable<string> named = Regex.Matches(Repository.Read(document), @"\bnet(standard|coreapp)?\d+(\.\d+)+\b").Select(match => match.Value).Distinct();

        Assert.That(named, Is.SubsetOf(targeted));
    }

    [Test]
    public void TheReadme_NamesBothBuildsOfTheLibrary()
    {
        string readme = Repository.Read("README.md");

        Assert.That(TargetFrameworksOf(ProjectFiles[0]), Is.EquivalentTo(new[] { "net10.0", "netstandard2.1" }));
        foreach (string framework in TargetFrameworksOf(ProjectFiles[0]))
        {
            Assert.That(readme, Ordinal.Contains("`" + framework + "`"));
        }
    }

    // ---- versions ----

    /// <summary>
    /// 1.0.0 is the only version that exists, so it is the only complete version number a document for users
    /// names. The version being developed is called 2.0 and nothing more exact, and no other version is named.
    /// </summary>
    [TestCase("README.md")]
    [TestCase("CONTRIBUTING.md")]
    [TestCase("SECURITY.md")]
    public void TheOnlyVersionsNamed_AreThePublishedOne_AndTheOneInDevelopment(string document)
    {
        string prose = Markdown.WithoutLinks(Repository.Read(document));

        Assert.That(Regex.Matches(prose, @"\b\d+\.\d+\.\d+(-[\w.]+)?").Select(match => match.Value).Distinct(), Is.SubsetOf(new[] { "1.0.0" }));
        Assert.That(Regex.Matches(prose, @"\b(version|v),?\s?(\d+\.\d+)(?![.\d])", RegexOptions.IgnoreCase).Select(match => match.Groups[2].Value).Distinct(),
            Is.SubsetOf(new[] { "2.0" }));
    }

    /// <summary>Wherever the README names the version in development, it has said that it is not published.</summary>
    [Test]
    public void TheReadme_SaysTheVersionInDevelopmentIsNotPublished_BeforeItSaysAnythingElseAboutIt()
    {
        string readme = Repository.Read("README.md");
        int firstMention = readme.IndexOf("2.0", StringComparison.Ordinal);

        Assert.That(firstMention, Is.GreaterThan(0));
        Assert.That(readme.Substring(firstMention, 80), Ordinal.Contains("not published yet"));
        Assert.That(readme, Does.Not.Match(@"(?i)2\.0 (is|was|has been) (published|released|available)|install[^\n]*2\.0"));
    }

    // ---- the corrections of the legacy class ----

    /// <summary>
    /// The constructor colors are corrected (D-57), and both documents a user reads say so: what each argument
    /// colors now, what it colored in 1.0.0, and how a program keeps what it drew. Neither still describes the
    /// old assignment as what happens.
    /// </summary>
    [TestCase("README.md", "### Changes from 1.0.0")]
    [TestCase("CHANGELOG.md", "### Breaking changes")]
    public void TheConstructorCorrection_IsDescribedForUsers(string document, string heading)
    {
        string text = Repository.Read(document);
        string section = SectionOf(text, heading);

        using (Assert.EnterMultipleScope())
        {
            foreach (string parameter in new[] { "TitleColor", "DateColor", "SelectedDateColor", "DayOfWeekColor" })
            {
                Assert.That(section, Ordinal.Contains("`" + parameter + "`"), parameter);
            }
            Assert.That(section, Ordinal.Contains("1.0.0"));
            Assert.That(section, Ordinal.Contains("trade places"), "what a program with four arguments does to keep its appearance");
            Assert.That(section, Ordinal.Contains("parameterless constructor"), "the form that is the same in both versions");
            Assert.That(section, Ordinal.Contains("not a defined `ConsoleColor`"), "the failure that moved");
            Assert.That(text, Does.Not.Match("sets the weekday row's color, not the title's|kept for compatibility"));
        }
    }

    /// <summary>
    /// The exception for a null list is corrected (D-58), and both documents say so: the exception thrown now,
    /// the parameter it names, and the one 1.0.0 threw.
    /// </summary>
    [TestCase("README.md", "### Changes from 1.0.0")]
    [TestCase("CHANGELOG.md", "### Breaking changes")]
    public void TheNullListCorrection_IsDescribedForUsers(string document, string heading)
    {
        string section = SectionOf(Repository.Read(document), heading);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(section, Ordinal.Contains("`ArgumentNullException`"));
            Assert.That(section, Ordinal.Contains("`NullReferenceException`"), "what 1.0.0 threw");
            Assert.That(section, Ordinal.Contains("`dates`"), "the parameter the exception names");
            Assert.That(section, Ordinal.Contains("`GenerateMonth"));
            Assert.That(section, Ordinal.Contains("`GenerateYear"));
            Assert.That(section, Ordinal.Contains("empty list"), "what to pass instead");
        }
    }

    /// <summary>
    /// How a month ends is corrected (D-59 and D-60), and both documents say so: one line terminator for every
    /// line, with what that means on Windows, and exactly one blank line after every month.
    /// </summary>
    [TestCase("README.md", "### Changes from 1.0.0")]
    [TestCase("CHANGELOG.md", "### Breaking changes")]
    public void TheEndOfMonthCorrections_AreDescribedForUsers(string document, string heading)
    {
        string section = SectionOf(Repository.Read(document), heading);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(section, Ordinal.Contains("line terminator"));
            Assert.That(section, Ordinal.Contains("bare line feed"), "what 1.0.0 wrote");
            Assert.That(section, Ordinal.Contains("no longer mixes LF and CRLF"), "what changes on Windows");
            Assert.That(section, Ordinal.Contains("Windows"));
            Assert.That(section, Ordinal.Contains("exactly one blank line"));
            Assert.That(section, Ordinal.Contains("Sunday"), "which months had two");
        }
    }

    /// <summary>The text from a third-level heading to the next heading of the same or a higher level.</summary>
    private static string SectionOf(string text, string heading)
    {
        int start = text.IndexOf("\n" + heading + "\n", StringComparison.Ordinal);
        Assert.That(start, Is.GreaterThan(0), heading);
        int end = text.IndexOf("\n## ", start, StringComparison.Ordinal);
        int nextSection = text.IndexOf("\n### ", start + 1, StringComparison.Ordinal);
        return text.Substring(start, (nextSection > 0 && nextSection < end ? nextSection : end) - start);
    }

    [Test]
    public void TheChangelog_ListsBreakingChanges_UnderTheUnreleasedVersionOnly()
    {
        string changelog = Repository.Read("CHANGELOG.md");
        int unreleased = changelog.IndexOf("\n## [Unreleased]", StringComparison.Ordinal);
        int published = changelog.IndexOf("\n## [1.0.0]", StringComparison.Ordinal);
        int breaking = changelog.IndexOf("\n### Breaking changes\n", StringComparison.Ordinal);

        Assert.That(breaking, Is.GreaterThan(unreleased).And.LessThan(published));
        Assert.That(changelog.IndexOf("\n### Breaking changes\n", breaking + 1, StringComparison.Ordinal), Is.EqualTo(-1));
    }

    [Test]
    public void TheChangelog_HasOnePublishedVersion_AndAnUnreleasedSection()
    {
        string[] sections = Repository.Read("CHANGELOG.md").Split('\n').Where(line => line.StartsWith("## ", StringComparison.Ordinal)).ToArray();

        Assert.That(sections, Is.EqualTo(new[] { "## [Unreleased]", "## [1.0.0] - 2023-06-25" }));
    }

    /// <summary>What the repository builds is a development version, which is why the documents call it unpublished.</summary>
    [Test]
    public void TheRepositoryBuildsADevelopmentVersion_NotThePublishedOne()
    {
        string project = Repository.Read(ProjectFiles[0]);

        Assert.That(project, Does.Match(@"<VersionSuffix>dev</VersionSuffix>|<Version>\d+\.\d+\.\d+-dev</Version>"));
        Assert.That(Repository.Read("README.md"), Ordinal.Contains("not published yet"));
    }

    // ---- commands ----

    /// <summary>The project files, solutions, scripts and project folders named in the commands exist.</summary>
    [TestCase("README.md")]
    [TestCase("CONTRIBUTING.md")]
    public void EveryFileNamedInACommand_Exists(string document)
    {
        string[] commands = Markdown.FencedBlocks(Repository.Read(document))
            .Where(block => block.Language == "text")
            .SelectMany(block => block.Text.Split('\n'))
            .Where(line => line.StartsWith("dotnet ", StringComparison.Ordinal) || line.StartsWith("bash ", StringComparison.Ordinal))
            .ToArray();

        Assert.That(commands, Is.Not.Empty);
        foreach (string command in commands)
        {
            string[] words = command.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < words.Length; i++)
            {
                if (Regex.IsMatch(words[i], @"\.(sln|csproj|sh)$"))
                {
                    Assert.That(Repository.HasFile(words[i]), command);
                }
                if (words[i] == "--project")
                {
                    Assert.That(Repository.HasFolder(words[i + 1]), command);
                }
            }
        }
    }

    // ---- what the documents must not contain ----

    private static IEnumerable<string> PublicFiles =>
        Documents.Concat(new[] { ExampleProgram.ProgramFile, ExampleProgram.ExamplesFile, ExampleProgram.ExpectedOutputFile });

    [TestCaseSource(nameof(PublicFiles))]
    public void NoPathOfOneMachine(string file)
    {
        Assert.That(Regex.Matches(Repository.Read(file), @"/home/|/Users/|/tmp/|/var/|[A-Za-z]:\\|~/").Select(match => match.Value), Is.Empty);
    }

    /// <summary>The package is .NET only, and it is documented on its own terms, not through one of its consumers.</summary>
    [TestCaseSource(nameof(PublicFiles))]
    public void NoOtherLanguage_AndNoConsumerOfThePackage(string file)
    {
        Assert.That(Regex.Matches(Repository.Read(file), @"python|\bpip\b|campertron", RegexOptions.IgnoreCase).Select(match => match.Value), Is.Empty);
    }

    [TestCaseSource(nameof(Documents))]
    public void NoHostedDocumentationSite(string document)
    {
        Assert.That(Repository.Read(document), Does.Not.Match(@"github\.io|GitHub Pages"));
    }
}
