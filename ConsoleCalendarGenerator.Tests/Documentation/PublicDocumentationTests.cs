using System.Reflection;
using System.Text.RegularExpressions;
using ConsoleCalendarGenerator.Tests.Support;

namespace ConsoleCalendarGenerator.Tests.Documentation;

/// <summary>
/// Facts the public documents state, checked against the repository: which documents exist, which frameworks,
/// versions and API names they mention, which commands they tell a reader to run, and what they say about
/// where each version is available. Headings, order and wording are free, with one exception: a short list of
/// phrases that the project does not use in public (D-84), which is checked word for word.
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

    /// <summary>The version on NuGet, and the version released on GitHub alone.</summary>
    private const string OnNuGet = "1.0.0";
    private const string OnGitHubOnly = "2.0.0";

    private static string[] TargetFrameworksOf(string projectFile) =>
        Regex.Match(Repository.Read(projectFile), @"<TargetFrameworks?>([^<]+)</TargetFrameworks?>").Groups[1].Value
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    /// <summary>The version the project file gives every build of this source. It has no suffix: it is a release.</summary>
    private static string ReleaseVersion()
    {
        string project = Repository.Read(ProjectFiles[0]);
        Assert.That(project, Does.Not.Contain("<VersionSuffix>"), "the repository holds a release, not a development version");
        return Regex.Match(project, "<VersionPrefix>([^<]+)</VersionPrefix>").Groups[1].Value;
    }

    /// <summary>The notes the GitHub release of this version carries.</summary>
    private static string ReleaseNotesFile => "docs/release-notes/" + ReleaseVersion() + ".md";

    /// <summary>Everything a user reads as prose: the four documents and the notes of the release.</summary>
    private static IEnumerable<string> Prose => Documents.Append(ReleaseNotesFile);

    /// <summary>
    /// Everything public that is text: the prose, the example program with what it prints, and the description
    /// of the package (as a pseudo-file, read from the project).
    /// </summary>
    private static IEnumerable<string> PublicFiles =>
        Prose.Concat(new[] { ExampleProgram.ProgramFile, ExampleProgram.ExamplesFile, ExampleProgram.ExpectedOutputFile });

    private static string PackageDescription =>
        Regex.Match(Repository.Read(ProjectFiles[0]), "<Description>([^<]*)</Description>").Groups[1].Value;

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
            ReleaseNotesFile,
        }));
    }

    [Test]
    public void TheLibraryFolder_HoldsOnlySourceAndItsProjectFile()
    {
        Assert.That(Repository.HasFolder("ConsoleCalendarGenerator/docs"), Is.False);
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

    [TestCaseSource(nameof(Prose))]
    public void EveryTargetFrameworkNamed_IsOneAProjectOfTheRepositoryTargets(string document)
    {
        string[] targeted = ProjectFiles.SelectMany(TargetFrameworksOf).Distinct().ToArray();

        IEnumerable<string> named = Regex.Matches(Repository.Read(document), @"\bnet(standard|coreapp)?\d+(\.\d+)+\b").Select(match => match.Value).Distinct();

        Assert.That(named, Is.SubsetOf(targeted));
    }

    [TestCase("README.md")]
    [TestCase("release notes")]
    public void BothBuildsOfTheLibrary_AreNamed(string document)
    {
        string text = Repository.Read(document == "release notes" ? ReleaseNotesFile : document);

        Assert.That(TargetFrameworksOf(ProjectFiles[0]), Is.EquivalentTo(new[] { "net10.0", "netstandard2.1" }));
        foreach (string framework in TargetFrameworksOf(ProjectFiles[0]))
        {
            Assert.That(text, Ordinal.Contains("`" + framework + "`"));
        }
    }

    // ---- versions ----

    /// <summary>
    /// Three versions exist as far as a user is concerned: the one on NuGet, the one released on GitHub, and
    /// the release this source is. No document names any other, and none names a development or prerelease
    /// version.
    /// </summary>
    [TestCaseSource(nameof(Prose))]
    public void TheOnlyVersionsNamed_AreTheTwoReleasedOnes_AndThisRelease(string document)
    {
        string prose = Markdown.WithoutLinks(Repository.Read(document));
        // The changelog also gives the assembly version and the file version, which have four parts.
        prose = Regex.Replace(prose, @"\b\d+\.\d+\.\d+\.\d+\b", string.Empty);

        Assert.That(Regex.Matches(prose, @"\b\d+\.\d+\.\d+(-[\w.]+)?").Select(match => match.Value).Distinct(),
            Is.SubsetOf(new[] { OnNuGet, OnGitHubOnly, ReleaseVersion() }));
        // "-binaries" follows the version in the name of the archive; nothing else follows a version with a hyphen.
        Assert.That(Repository.Read(document), Does.Not.Match(@"\d+\.\d+\.\d+-(?!binaries\.zip)[A-Za-z]"), "no development or prerelease version");
    }

    /// <summary>
    /// The README is packed into the package and describes that package: it names this release, and the
    /// version on NuGet only where it explains what changed since. It has no reason to name another version.
    /// </summary>
    [Test]
    public void TheReadme_NamesThisRelease_AndTheVersionItExplainsTheMoveFrom()
    {
        string readme = Markdown.WithoutLinks(Repository.Read("README.md"));

        Assert.That(Regex.Matches(readme, @"\b\d+\.\d+\.\d+\b").Select(match => match.Value).Distinct(), Is.EquivalentTo(new[] { ReleaseVersion(), OnNuGet }));
        Assert.That(readme, Ordinal.Contains("ConsoleCalendarGenerator " + ReleaseVersion()), "which release the page documents");
    }

    /// <summary>One installation command, and it names the version of the package this README is packed into.</summary>
    [Test]
    public void TheInstallationCommand_NamesThisRelease()
    {
        string[] installs = Markdown.FencedBlocks(Repository.Read("README.md"))
            .SelectMany(block => block.Text.Split('\n'))
            .Where(line => line.Contains("add package", StringComparison.Ordinal))
            .ToArray();

        Assert.That(installs, Is.EqualTo(new[] { "dotnet add package ConsoleCalendarGenerator --version " + ReleaseVersion() }));
    }

    [Test]
    public void TheRepositoryHoldsARelease_NotADevelopmentVersion()
    {
        string project = Repository.Read(ProjectFiles[0]);

        Assert.That(project, Does.Not.Match(@"<VersionSuffix>|<Version>\d"));
        Assert.That(ReleaseVersion(), Does.Match(@"^\d+\.\d+\.\d+$"));
        Assert.That(ReleaseVersion(), Is.Not.EqualTo(OnNuGet).And.Not.EqualTo(OnGitHubOnly), "a released version is never built again");
    }

    // ---- where each version is available ----

    /// <summary>
    /// What the documents may say about availability, as it stands: 1.0.0 is on NuGet, 2.0.0 was released on
    /// GitHub and never on NuGet, and this release is not published anywhere yet. So nothing says this release
    /// is published, released or available, and no line connects 2.0.0 with NuGet except to say that it was
    /// not published there. This test is edited when this release is published.
    /// </summary>
    [TestCaseSource(nameof(Prose))]
    public void NoDocumentSaysThatThisReleaseIsPublished_OrThatTheGitHubReleaseIsOnNuGet(string document)
    {
        string text = Repository.Read(document);
        string release = Regex.Escape(ReleaseVersion());

        using (Assert.EnterMultipleScope())
        {
            Assert.That(Regex.Matches(text, @"(?i)(?<!until )\b" + release + @" (is|was|has been) (now )?(published|released|available|out|on nuget)\b|is now available|now on nuget|--prerelease")
                .Select(match => match.Value), Is.Empty, "this release is not described as published");
            Assert.That(text.Split('\n').Where(line => line.Contains(OnGitHubOnly, StringComparison.Ordinal)
                    && line.Contains("NuGet", StringComparison.OrdinalIgnoreCase)
                    && !line.Contains("not published to NuGet", StringComparison.Ordinal)),
                Is.Empty, OnGitHubOnly + " is not described as being on NuGet");
        }
    }

    /// <summary>
    /// The changelog has one section for each version, newest first, each with a link to where that version
    /// is. This release has no date until it is published: its heading says "Unreleased" until then, and a
    /// date is never guessed. The section of 2.0.0 says where that version was distributed.
    /// </summary>
    [Test]
    public void TheChangelog_HasASectionForEachVersion_AndThisReleaseIsDatedOnlyOncePublished()
    {
        string changelog = Repository.Read("CHANGELOG.md");
        string[] sections = changelog.Split('\n').Where(line => line.StartsWith("## ", StringComparison.Ordinal)).ToArray();
        const string releases = "https://github.com/ShaunAnthonyHathaway/ConsoleCalendarGenerator/releases/tag/v";

        using (Assert.EnterMultipleScope())
        {
            Assert.That(sections, Has.Length.EqualTo(3));
            Assert.That(sections[0], Does.Match(@"^## \[" + Regex.Escape(ReleaseVersion()) + @"\] - (Unreleased|\d{4}-\d{2}-\d{2})$"));
            Assert.That(sections[1], Is.EqualTo("## [" + OnGitHubOnly + "] - 2026-11-27"));
            Assert.That(sections[2], Is.EqualTo("## [" + OnNuGet + "] - 2023-06-25"));
            Assert.That(changelog, Ordinal.Contains("\n[" + ReleaseVersion() + "]: " + releases + ReleaseVersion() + "\n"));
            Assert.That(changelog, Ordinal.Contains("\n[" + OnGitHubOnly + "]: " + releases + OnGitHubOnly + "\n"));
            Assert.That(changelog, Ordinal.Contains("\n[" + OnNuGet + "]: https://www.nuget.org/packages/ConsoleCalendarGenerator/" + OnNuGet + "\n"));
            Assert.That(changelog, Ordinal.DoesNotContain("[Unreleased]"));

            string github = SectionOf(changelog, sections[1]);
            Assert.That(github, Ordinal.Contains("GitHub Releases"));
            Assert.That(github, Ordinal.Contains("not published to NuGet"));

            string thisRelease = Regex.Replace(SectionOf(changelog, sections[0]), @"\s+", " ");
            Assert.That(thisRelease, Ordinal.Contains("public API are the same as in " + OnGitHubOnly), "what this release does not change");
        }
    }

    /// <summary>
    /// `SECURITY.md` is a document of current policy: it is not packed into the package, every link to it
    /// points at the default branch, and it is edited when the facts change. It has a row for each version
    /// that says where the version is, and it sends reports to GitHub's private vulnerability reporting.
    /// </summary>
    [Test]
    public void TheSecurityDocument_SaysWhereEachVersionIs_AndSendsReportsToThePrivateChannel()
    {
        string security = Repository.Read("SECURITY.md");
        string[] rows = SectionOf(security, "## Supported versions").Split('\n').Where(line => Regex.IsMatch(line, @"^\| \d")).ToArray();
        string reporting = Regex.Replace(SectionOf(security, "## Reporting a problem"), @"\s+", " ");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(rows.Select(row => Regex.Match(row, @"^\| (\d+\.\d+\.\d+) \|").Groups[1].Value), Is.EqualTo(new[] { ReleaseVersion(), OnGitHubOnly, OnNuGet }));
            Assert.That(rows[1], Ordinal.Contains("GitHub Releases"));
            Assert.That(rows[2], Ordinal.Contains("NuGet"));
            Assert.That(rows[2], Ordinal.Contains("until " + ReleaseVersion() + " is published on NuGet"), "how long the version on NuGet is supported");
            Assert.That(security, Ordinal.Contains("Published versions are never changed"));

            // The route for a report: the form of GitHub's private vulnerability reporting for this repository.
            Assert.That(reporting, Ordinal.Contains("](https://github.com/ShaunAnthonyHathaway/ConsoleCalendarGenerator/security/advisories/new)"));
            Assert.That(reporting, Ordinal.Contains("private vulnerability reporting"));
            Assert.That(reporting, Ordinal.Contains("**Security** tab"), "the way there through the page, for a link that moves");
            Assert.That(reporting, Ordinal.Contains("do not put the details of a vulnerability in a public issue"));
            Assert.That(security, Does.Not.Match("(?i)open an issue|/issues"), "no route through a public issue");
            Assert.That(security, Does.Not.Match(@"[\w.+-]+@[\w-]+\.[\w.]+"), "no e-mail address");
            Assert.That(reporting, Does.Not.Match(@"(?i)within \d+|\d+ (hours|days|business days)|we will respond|guarantee[sd]? (a|to)"), "no promise of a response time");
        }
    }

    // ---- moving from 1.0.0 ----

    /// <summary>The section of the README about moving from 1.0.0, whatever it is called: its heading names that version.</summary>
    private static string MigrationSection(string readme)
    {
        string[] headings = readme.Split('\n').Where(line => Regex.IsMatch(line, @"^## .*\b1\.0\.0\b")).ToArray();
        Assert.That(headings, Has.Length.EqualTo(1), "one section of the README is about moving from 1.0.0");
        return SectionOf(readme, headings[0]);
    }

    /// <summary>The changelog's list of what version 2 changed: the "Changed" part of the section of 2.0.0.</summary>
    private static string ChangedIn200(string changelog)
    {
        string section = SectionOf(changelog, "## [" + OnGitHubOnly + "] - 2026-11-27");
        return SectionOf(section, "### Changed");
    }

    /// <summary>
    /// The five behaviors version 2 updated (D-57 to D-61), each with the names a user needs to recognize it:
    /// in code where there is code. The README's section on moving from 1.0.0 and the changelog's section of
    /// 2.0.0 both state them. The wording around the names is free.
    /// </summary>
    private static readonly (string Behavior, string[] Names)[] Behaviors =
    {
        ("constructor colors", new[] { "`TitleColor`", "`DateColor`", "`SelectedDateColor`", "`DayOfWeekColor`" }),
        ("null list", new[] { "`ArgumentNullException`", "`NullReferenceException`", "`dates`", "`GenerateMonth`", "`GenerateYear`" }),
        ("line endings", new[] { "line terminator", "line feed" }),
        ("blank line", new[] { "one blank line", "Sunday" }),
        ("Gregorian title", new[] { "Gregorian", "`fa-IR`" }),
    };

    private static IEnumerable<TestCaseData> BehaviorsInBothDocuments() =>
        from document in new[] { "README.md", "CHANGELOG.md" }
        from behavior in Behaviors
        select new TestCaseData(document, behavior.Behavior).SetArgDisplayNames(document, behavior.Behavior);

    [TestCaseSource(nameof(BehaviorsInBothDocuments))]
    public void EachOfTheFiveUpdatedBehaviors_IsDescribedForUsers(string document, string behavior)
    {
        string text = Repository.Read(document);
        string section = Regex.Replace(document == "README.md" ? MigrationSection(text) : ChangedIn200(text), @"\s+", " ");

        foreach (string name in Behaviors.Single(entry => entry.Behavior == behavior).Names)
        {
            Assert.That(section, Ordinal.Contains(name), behavior);
        }
    }

    /// <summary>Five, and each once: the README as a numbered list, the changelog as five entries.</summary>
    [Test]
    public void TheFiveUpdatedBehaviors_AreListedOnceEach_AndThereAreFive()
    {
        string migration = MigrationSection(Repository.Read("README.md"));
        string changed = ChangedIn200(Repository.Read("CHANGELOG.md"));

        string[] numbered = Regex.Matches(migration, @"(?m)^(\d)\. \*\*([^*]+)\*\*").Select(match => match.Groups[1].Value).ToArray();
        string[] titles = Regex.Matches(migration, @"(?m)^\d\. \*\*([^*]+)\*\*").Select(match => match.Groups[1].Value).ToArray();
        string[] entries = Regex.Matches(changed.Substring(0, changed.IndexOf("Also changed", StringComparison.Ordinal)), @"(?m)^- \*\*([^*]+)\*\*")
            .Select(match => match.Groups[1].Value).ToArray();

        Assert.That(numbered, Is.EqualTo(new[] { "1", "2", "3", "4", "5" }));
        Assert.That(titles, Is.Unique);
        Assert.That(entries, Is.EqualTo(titles), "the changelog lists the same five, under the same names");
        Assert.That(migration, Does.Match(@"\bfive\b"));
    }

    /// <summary>
    /// What 1.0.0 did is told in one place of the README, the section on moving from it. Nowhere else does the
    /// README go back to that version, so the rest of the page describes the library as it is.
    /// </summary>
    [Test]
    public void TheReadme_SpeaksOfTheEarlierVersion_OnlyWhereItExplainsTheMove()
    {
        string readme = Repository.Read("README.md");
        string elsewhere = readme.Replace(MigrationSection(readme), string.Empty, StringComparison.Ordinal);

        Assert.That(elsewhere, Ordinal.DoesNotContain(OnNuGet));
        Assert.That(elsewhere, Ordinal.DoesNotContain("`NullReferenceException`"));
    }

    // ---- the API the documents name ----

    /// <summary>
    /// Every name of the API that the README or the release notes mention in code exists: a public type of the
    /// library, a public member, an enumeration value, or a parameter of a public method. A name that was
    /// renamed, removed or mistyped fails here. Names of .NET itself are listed below.
    /// </summary>
    [TestCase("README.md")]
    [TestCase("release notes")]
    public void EveryApiNameMentionedInCode_Exists(string document)
    {
        string text = Repository.Read(document == "release notes" ? ReleaseNotesFile : document);
        Assembly library = typeof(MonthLayout).Assembly;
        const BindingFlags all = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

        var known = new HashSet<string>(StringComparer.Ordinal)
        {
            // .NET's own names, and the two prefixes the README uses for groups of methods.
            "ConsoleColor", "DayOfWeek", "TextWriter", "Console.Out", "ArgumentNullException", "NullReferenceException", "Get", "Set", "With",
        };
        foreach (Type type in library.GetExportedTypes())
        {
            known.Add(type.Name);
            known.Add(type.FullName!);
            foreach (MemberInfo member in type.GetMembers(all))
            {
                known.Add(member.Name);
                known.Add(type.Name + "." + member.Name);
                if (member is MethodBase method)
                {
                    foreach (ParameterInfo parameter in method.GetParameters())
                    {
                        known.Add(parameter.Name!);
                    }
                }
            }
        }

        // Inline code that is one identifier, or identifiers joined by dots, and starts with a capital letter.
        // File names, commands, literals and code with arguments are not names of the API and are left alone.
        string prose = Regex.Replace(text, "(?s)```.*?```", string.Empty);
        string[] names = Regex.Matches(prose, @"`([A-Z][A-Za-z]+(?:\.[A-Z][A-Za-z]+)*)(\(\))?`")
            .Select(match => match.Groups[1].Value)
            .Distinct()
            .ToArray();

        Assert.That(names, Has.Length.GreaterThan(5));
        Assert.That(names.Where(name => !known.Contains(name)), Is.Empty, "names in code that are not part of the API");
    }

    /// <summary>
    /// The original API is described as part of the library, by its full name, with what it does: the section
    /// of the README about it names both of its drawing methods, and nothing public calls it deprecated.
    /// </summary>
    [Test]
    public void TheReadme_DescribesTheOriginalApi_ByNameAndByWhatItDoes()
    {
        string readme = Repository.Read("README.md");
        string[] headings = readme.Split('\n').Where(line => Regex.IsMatch(line, @"^## .*\bConsoleCalendar\b")).ToArray();
        Assert.That(headings, Has.Length.EqualTo(1), "one section of the README is about ConsoleCalendar");
        string section = SectionOf(readme, headings[0]);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(section, Ordinal.Contains("`CalendarGenerator.ConsoleCalendar`"));
            Assert.That(section, Ordinal.Contains("`GenerateMonth`"));
            Assert.That(section, Ordinal.Contains("`GenerateYear`"));
            foreach (string document in Prose)
            {
                Assert.That(Repository.Read(document), Does.Not.Match(@"(?i)\bdeprecated\b|\bobsolete\b|will be removed"), document);
            }
        }
    }

    /// <summary>
    /// The notes of the release link into this repository only: to files at the tag of the release, which
    /// exist in this checkout, and to the changelog on the default branch. They have no heading of the first
    /// level, because the release has a title of its own.
    /// </summary>
    [Test]
    public void TheReleaseNotes_LinkToFilesOfThisRelease_AndNameItsFourFiles()
    {
        string notes = Repository.Read(ReleaseNotesFile);
        const string repositoryPage = "https://github.com/ShaunAnthonyHathaway/ConsoleCalendarGenerator/blob/";
        string[] targets = Regex.Matches(notes, @"\]\(([^)]+)\)").Select(match => match.Groups[1].Value).ToArray();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(targets, Is.Not.Empty);
            foreach (string target in targets)
            {
                string path = target.Split('#')[0];
                Assert.That(path, Does.StartWith(repositoryPage + "v" + ReleaseVersion() + "/").Or.EqualTo(repositoryPage + "master/CHANGELOG.md"), target);
                Assert.That(Repository.HasFile(path.Substring(path.IndexOf('/', repositoryPage.Length) + 1)), target + " is not a file of the repository");
            }
            foreach (string file in new[] { "ConsoleCalendarGenerator." + ReleaseVersion() + ".nupkg", "ConsoleCalendarGenerator." + ReleaseVersion() + ".snupkg", "ConsoleCalendarGenerator-" + ReleaseVersion() + "-binaries.zip", "SHA256SUMS.txt" })
            {
                Assert.That(notes, Ordinal.Contains("`" + file + "`"));
            }
            Assert.That(notes, Does.Not.Match(@"(?m)^# "));
        }
    }

    /// <summary>
    /// The text of a section: from its heading to the next heading of the same or a higher level, so that a
    /// section includes its subsections.
    /// </summary>
    private static string SectionOf(string text, string heading)
    {
        int start = text.IndexOf("\n" + heading + "\n", StringComparison.Ordinal);
        Assert.That(start, Is.GreaterThanOrEqualTo(0), heading);
        int level = heading.TakeWhile(character => character == '#').Count();
        Match next = Regex.Match(text.Substring(start + 1), @"\n#{1," + level + @"} ");
        return next.Success ? text.Substring(start, next.Index + 1) : text.Substring(start);
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

    // ---- what the public text must not contain ----

    /// <summary>
    /// The phrases the project does not use in public (D-84). They are the exact phrases that were rejected,
    /// not a list of everything that could be said badly: the tone of a document is judged by reading it.
    /// </summary>
    private static readonly string[] RejectedPhrases =
    {
        "got wrong", "broken", "defective", "bad implementation", "flawed", "outdated code", "old API", "legacy API only",
        "incorrect implementation", "workaround for mistakes", "if the command gives you", "until it appears", "not sure whether",
        "this page might describe", "not yet published", "is not on nuget.org yet", "were wrong", "corrects five",
    };

    private static string TextOf(string file) => file == "package description" ? PackageDescription : Repository.Read(file);

    private static IEnumerable<string> PublicText => PublicFiles.Append("package description");

    [TestCaseSource(nameof(PublicText))]
    public void NoRejectedPhrase(string file)
    {
        string text = TextOf(file);

        Assert.That(RejectedPhrases.Where(phrase => text.Contains(phrase, StringComparison.OrdinalIgnoreCase)), Is.Empty);
        Assert.That(text, Does.Not.Match(@"(?i)\blegacy\b"), "the public name of that API is the original API, or its type name");
    }

    /// <summary>
    /// Public text is about the library. How the project is run stays in the maintenance notes: no decision or
    /// question number, no pull request number, no workflow run, no release candidate, no audit, no count of
    /// tests, and nothing about the tools the project is maintained with.
    /// </summary>
    [TestCaseSource(nameof(PublicText))]
    public void NothingAboutHowTheProjectIsRun(string file)
    {
        string text = TextOf(file);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(Regex.Matches(text, @"\b[DQ]-\d\d\b|(?i)\bPR ?#?\d+|pull request #?\d+|workflow run|\bcandidate\b|\baudit|\bapproval\b|\bapproved\b").Select(match => match.Value),
                Is.Empty, "project management");
            Assert.That(Regex.Matches(text, @"(?i)\b[\d,]{2,} tests\b").Select(match => match.Value), Is.Empty, "a count of tests");
            Assert.That(Regex.Matches(text, @"\bAI\b|(?i)\bClaude\b|\bFable\b|Anthropic|\bLLM\b|coding agent|Copilot|ChatGPT").Select(match => match.Value), Is.Empty, "tooling");
        }
    }

    [TestCaseSource(nameof(PublicText))]
    public void NoPathOfOneMachine(string file)
    {
        Assert.That(Regex.Matches(TextOf(file), @"/home/|/Users/|/tmp/|/var/|[A-Za-z]:\\|~/").Select(match => match.Value), Is.Empty);
    }

    /// <summary>The package is .NET only, and it is documented on its own terms, not through one of its consumers.</summary>
    [TestCaseSource(nameof(PublicText))]
    public void NoOtherLanguage_AndNoConsumerOfThePackage(string file)
    {
        Assert.That(Regex.Matches(TextOf(file), @"python|\bpip\b|campertron", RegexOptions.IgnoreCase).Select(match => match.Value), Is.Empty);
    }

    [TestCaseSource(nameof(Prose))]
    public void NoHostedDocumentationSite(string document)
    {
        Assert.That(Repository.Read(document), Does.Not.Match(@"github\.io|GitHub Pages"));
    }
}
