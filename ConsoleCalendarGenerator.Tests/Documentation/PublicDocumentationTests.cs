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
    /// The one exception is the number of the development package, which the document for contributors gives,
    /// because contributors build it. It is the number the project file has.
    /// </summary>
    [TestCase("README.md")]
    [TestCase("CONTRIBUTING.md", "2.0.0-dev")]
    [TestCase("SECURITY.md")]
    public void TheOnlyVersionsNamed_AreThePublishedOne_AndTheOneInDevelopment(string document, params string[] alsoAllowed)
    {
        string prose = Markdown.WithoutLinks(Repository.Read(document));

        Assert.That(Regex.Matches(prose, @"\b\d+\.\d+\.\d+(-[\w.]+)?").Select(match => match.Value).Distinct(), Is.SubsetOf(new[] { "1.0.0" }.Concat(alsoAllowed)));
        Assert.That(alsoAllowed, Is.SubsetOf(new[] { DevelopmentVersion() }), "a development version a document names is the one the repository builds");
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

    // ---- the state of the project ----

    /// <summary>The version the project file gives the packages built here, such as <c>2.0.0-dev</c>.</summary>
    private static string DevelopmentVersion()
    {
        string project = Repository.Read(ProjectFiles[0]);
        return Regex.Match(project, "<VersionPrefix>([^<]+)</VersionPrefix>").Groups[1].Value + "-"
            + Regex.Match(project, "<VersionSuffix>([^<]+)</VersionSuffix>").Groups[1].Value;
    }

    /// <summary>
    /// What the README says about where things stand, each a fact that is true today: one published version,
    /// the next one in development and not published, and the modern API not in the published package.
    /// </summary>
    [Test]
    public void TheReadme_SaysWhatIsPublished_AndWhatIsNot()
    {
        string status = SectionOf(Repository.Read("README.md"), "## Status");
        string installation = SectionOf(Repository.Read("README.md"), "## Installation");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(status, Ordinal.Contains("https://www.nuget.org/packages/ConsoleCalendarGenerator/1.0.0"), "the published version, with its page");
            Assert.That(status, Ordinal.Contains("only published version"));
            Assert.That(status, Ordinal.Contains("not published yet"));
            Assert.That(status, Ordinal.Contains("not in the published package"), "the modern API");
            Assert.That(status, Ordinal.Contains("](#migrating-from-100-to-20)"), "the way to the migration section");
            Assert.That(installation, Ordinal.Contains("1.0.0"), "what the command installs today");
            Assert.That(installation, Ordinal.Contains("legacy API"));
            Assert.That(Markdown.FencedBlocks(installation).Select(block => block.Text.Trim()), Is.EqualTo(new[] { "dotnet add package ConsoleCalendarGenerator" }),
                "one command, with no version: it installs what is published");
        }
    }

    /// <summary>
    /// Nothing a user reads says more than is true: that the next version is out, can be installed, is stable
    /// or is ready for release, or that the development package can be had from anywhere.
    /// </summary>
    [TestCaseSource(nameof(Documents))]
    public void NoDocumentClaimsARelease_OrThatOneIsReady(string document)
    {
        string text = Repository.Read(document);

        Assert.That(Regex.Matches(text, @"(?i)\b2\.0(\.0)? (is|was|has been) (now )?(published|released|available|stable|out)\b|release[- ]ready|ready (for|to) release|--version 2\.|--prerelease|is now available")
            .Select(match => match.Value), Is.Empty);
        if (text.Contains(DevelopmentVersion(), StringComparison.Ordinal))
        {
            Assert.That(text, Does.Match(Regex.Escape(DevelopmentVersion()) + @"[^\n]*(\n[^\n]*)?not published"),
                document + ": where the development package is named, it is said to be unpublished");
        }
    }

    [Test]
    public void TheChangelog_KeepsTheNextVersionUnreleased_AndUndated()
    {
        string unreleased = SectionOf(Repository.Read("CHANGELOG.md"), "## [Unreleased]");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(unreleased, Ordinal.Contains("not released"));
            Assert.That(unreleased, Ordinal.Contains("`" + DevelopmentVersion() + "`"), "the number of what the repository builds");
            Assert.That(Regex.Matches(unreleased, @"\b20\d\d-\d\d-\d\d\b").Select(match => match.Value), Is.Empty, "no release date");
            Assert.That(Repository.Read("CHANGELOG.md").Split('\n').Where(line => line.StartsWith("### ", StringComparison.Ordinal)).Take(4),
                Is.EqualTo(new[] { "### Added", "### Changed", "### Breaking changes", "### Compatibility" }));
        }
    }

    [Test]
    public void TheSecurityDocument_SaysWhichVersionIsSupported_AndThatThereIsNoPrivateChannelYet()
    {
        string security = Repository.Read("SECURITY.md");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(SectionOf(security, "## Supported versions"), Ordinal.Contains("1.0.0"));
            Assert.That(SectionOf(security, "## Supported versions"), Ordinal.Contains("not published"), "the version in development");
            Assert.That(SectionOf(security, "## Reporting a problem"), Ordinal.Contains("does not have a private channel"),
                "true while private vulnerability reporting is off; this sentence changes in the change that turns it on");
            Assert.That(security, Does.Not.Match(@"[\w.+-]+@[\w-]+\.[\w.]+"), "no e-mail address");
        }
    }

    // ---- the legacy class and the migration from 1.0.0 ----

    private const string MigrationHeading = "## Migrating from 1.0.0 to 2.0";

    /// <summary>
    /// The five corrections of version 2.0 (D-57 to D-61), each with the facts a user needs to recognize it:
    /// the names of what is involved, in code where there is code. Both documents a user reads must state them
    /// where they list the changes. The wording around the facts is free.
    /// </summary>
    private static readonly (string Correction, string[] Facts)[] Corrections =
    {
        ("constructor colors", new[] { "`TitleColor`", "`DateColor`", "`SelectedDateColor`", "`DayOfWeekColor`", "trade places", "parameterless constructor", "not a defined `ConsoleColor`" }),
        ("null list", new[] { "`ArgumentNullException`", "`NullReferenceException`", "`dates`", "`GenerateMonth", "`GenerateYear", "empty list" }),
        ("line endings", new[] { "line terminator", "line feed", "Windows", "LF and CRLF" }),
        ("blank line", new[] { "exactly one blank line", "Sunday" }),
        ("Gregorian title", new[] { "names the Gregorian month", "own calendar", "`fa-IR`", "nearly every culture", "year in the title is still the number" }),
    };

    private static IEnumerable<TestCaseData> CorrectionsInBothDocuments() =>
        from document in new[] { ("README.md", MigrationHeading), ("CHANGELOG.md", "### Breaking changes") }
        from correction in Corrections
        select new TestCaseData(document.Item1, document.Item2, correction.Correction).SetArgDisplayNames(document.Item1, correction.Correction);

    [TestCaseSource(nameof(CorrectionsInBothDocuments))]
    public void EachOfTheFiveCorrections_IsDescribedForUsers(string document, string heading, string correction)
    {
        string section = SectionOf(Repository.Read(document), heading);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(section, Ordinal.Contains("1.0.0"), "what it was");
            foreach (string fact in Corrections.Single(entry => entry.Correction == correction).Facts)
            {
                Assert.That(section, Ordinal.Contains(fact), correction);
            }
        }
    }

    /// <summary>Five, and each once: the README as a numbered list, the changelog as five entries.</summary>
    [Test]
    public void TheFiveCorrections_AreListedOnceEach_AndThereAreFive()
    {
        string changed = SectionOf(Repository.Read("README.md"), "### What changed");
        string breaking = SectionOf(Repository.Read("CHANGELOG.md"), "### Breaking changes");

        string[] numbered = Regex.Matches(changed, @"(?m)^(\d)\. \*\*([^*]+)\*\*").Select(match => match.Groups[1].Value).ToArray();
        string[] entries = Regex.Matches(breaking, @"(?m)^- \*\*([^*]+)\*\*").Select(match => match.Groups[1].Value).ToArray();

        Assert.That(numbered, Is.EqualTo(new[] { "1", "2", "3", "4", "5" }));
        Assert.That(entries, Has.Length.EqualTo(5));
        Assert.That(entries, Is.Unique);
        Assert.That(SectionOf(Repository.Read("README.md"), MigrationHeading), Does.Match("five corrections"));
        Assert.That(breaking, Does.Match("five behaviors"));
    }

    /// <summary>
    /// What 1.0.0 did is told in one place of the README, the migration section, where it is told as the past.
    /// Nowhere else does the README mention the old behavior, so nothing there can read as what happens now.
    /// </summary>
    [TestCase("`NullReferenceException`")]
    [TestCase("bare line feed")]
    [TestCase("followed by two")]
    [TestCase("stayed yellow")]
    [TestCase("the month of the same number")]
    [TestCase("LF and CRLF")]
    public void WhatTheOldVersionDid_IsToldInTheMigrationSectionOfTheReadme_AndNowhereElse(string fact)
    {
        string readme = Repository.Read("README.md");
        string migration = SectionOf(readme, MigrationHeading);

        Assert.That(migration, Ordinal.Contains(fact));
        Assert.That(readme.Replace(migration, string.Empty, StringComparison.Ordinal), Ordinal.DoesNotContain(fact));
    }

    /// <summary>The other half of a migration: what a program can keep relying on.</summary>
    [Test]
    public void TheReadme_SaysWhatDidNotChange()
    {
        string unchanged = SectionOf(Repository.Read("README.md"), "### What did not change");

        using (Assert.EnterMultipleScope())
        {
            foreach (string fact in new[]
            {
                "signature", "parameter name", "`CalendarGenerator`", "`ConsoleCalendar`", "obsolete", "`GenerateYear`", "`Get`", "`Set`",
                "year, month and day", "`Mo Tu We Th Fr Sa Su`", "Monday", "directly to the console", "resetting its colors", "no lock", "modern API",
            })
            {
                Assert.That(unchanged, Ordinal.Contains(fact));
            }
        }
    }

    /// <summary>The legacy class is described as kept and supported, not as something on its way out.</summary>
    [Test]
    public void TheReadme_DescribesTheLegacyClassAsKeptAndSupported()
    {
        string legacy = SectionOf(Repository.Read("README.md"), "## Legacy API");

        Assert.That(legacy, Ordinal.Contains("supports it"));
        Assert.That(legacy, Ordinal.Contains("not obsolete"));
        Assert.That(legacy, Ordinal.Contains("without being rebuilt"), "a program compiled against 1.0.0 loads the new assembly");
        Assert.That(Repository.Read("README.md"), Does.Not.Match(@"(?i)\bdeprecated\b|will be removed"));
    }

    /// <summary>
    /// The text of a section: from its heading to the next heading of the same or a higher level, so that a
    /// section includes its subsections.
    /// </summary>
    private static string SectionOf(string text, string heading)
    {
        int start = text.IndexOf("\n" + heading + "\n", StringComparison.Ordinal);
        Assert.That(start, Is.GreaterThan(0), heading);
        int level = heading.TakeWhile(character => character == '#').Count();
        Match next = Regex.Match(text.Substring(start + 1), @"\n#{1," + level + @"} ");
        return next.Success ? text.Substring(start, next.Index + 1) : text.Substring(start);
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
