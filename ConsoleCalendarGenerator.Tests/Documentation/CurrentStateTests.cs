using System.Text.RegularExpressions;
using ConsoleCalendarGenerator.Tests.Support;

namespace ConsoleCalendarGenerator.Tests.Documentation;

/// <summary>
/// The documents describe the repository as it is now: all five corrections of version 2.0 made, the modern API
/// built, and the version 2.0.1 with no suffix: the source of the release. These tests look for what went stale
/// when each of those became true.
///
/// <para>
/// "Active" documents are the ones that say how things are: the four public documents, CLAUDE.md, the
/// maintenance notes, and the comments in the library's source, which become its XML documentation. The log of
/// decisions is different. It records what was decided and when, superseded decisions included, so the old
/// rules are still written there, each marked as superseded. The tests of what 1.0.0 did, in Historical/,
/// state the old behavior on purpose and are not documents at all.
/// </para>
/// <para>
/// What is checked is state and structure: a status, a version number, a row of a table. A sentence that was
/// retired is looked for only where it was a claim about the present that is now false.
/// </para>
/// </summary>
[TestFixture]
public class CurrentStateTests
{
    private const string Decisions = "docs/ai-context/decisions.md";

    private static readonly string[] MaintenanceNotes =
        Repository.FilesWithExtension(".md").Where(file => file.StartsWith("docs/ai-context/", StringComparison.Ordinal)).Append("CLAUDE.md").ToArray();

    private static readonly string[] LibrarySources =
        Repository.FilesWithExtension(".cs", ".csproj").Where(file => file.StartsWith("ConsoleCalendarGenerator/", StringComparison.Ordinal)).ToArray();

    /// <summary>Everything that says how things are. The decision log is listed apart.</summary>
    private static IEnumerable<string> ActiveDocuments() =>
        Repository.PublicDocuments.Concat(MaintenanceNotes.Where(file => file != Decisions)).Concat(LibrarySources);

    private static string ReleaseVersion()
    {
        string project = Repository.Read("ConsoleCalendarGenerator/ConsoleCalendarGenerator.csproj");
        Assert.That(project, Does.Not.Contain("<VersionSuffix>"), "the repository holds a release");
        return Regex.Match(project, "<VersionPrefix>([^<]+)</VersionPrefix>").Groups[1].Value;
    }

    [Test]
    public void TheActiveDocuments_AreTheOnesExpected()
    {
        Assert.That(MaintenanceNotes, Has.Length.EqualTo(9), "eight maintenance notes and CLAUDE.md");
        Assert.That(LibrarySources, Has.Length.GreaterThan(15));
        Assert.That(ActiveDocuments(), Has.No.Member(Decisions));
    }

    // ---- versions ----

    /// <summary>
    /// The repository holds a release, so no active document names a development version. The earlier numbers
    /// stay in two places that are history on purpose: the decision log, and the table of completed phases in
    /// the roadmap.
    /// </summary>
    [TestCaseSource(nameof(ActiveDocuments))]
    public void NoActiveDocument_NamesADevelopmentVersion(string document)
    {
        string text = Repository.Read(document);
        if (document == "docs/ai-context/roadmap.md")
        {
            int completed = text.IndexOf("\n## Completed\n", StringComparison.Ordinal);
            int next = text.IndexOf("\n## ", completed + 1, StringComparison.Ordinal);
            text = text.Remove(completed, next - completed);
        }

        Assert.That(Regex.Matches(text, @"\b\d+\.\d+\.\d+-(dev|rc|preview|beta|alpha)\b").Select(match => match.Value).Distinct(), Is.Empty);
    }

    [Test]
    public void TheReleaseVersion_IsNamedWhereAMaintainerLooksForIt()
    {
        Assert.That(ReleaseVersion(), Is.EqualTo("2.0.1"));
        foreach (string document in new[] { "CLAUDE.md", "CHANGELOG.md", "README.md", "docs/ai-context/packaging-and-releases.md", "docs/ai-context/compatibility.md" })
        {
            Assert.That(Repository.Read(document), Ordinal.Contains(ReleaseVersion()), document);
        }
    }

    // ---- the corrections ----

    /// <summary>
    /// In the decision log each of the five corrections is marked as implemented, and the earlier rules they
    /// replaced are marked as superseded, so that nobody reads an old rule as a standing one.
    /// </summary>
    [Test]
    public void TheDecisionLog_MarksTheFiveCorrectionsImplemented_AndTheOldRulesSuperseded()
    {
        string decisions = Repository.Read(Decisions);

        using (Assert.EnterMultipleScope())
        {
            foreach (string correction in new[] { "D-57", "D-58", "D-59", "D-60", "D-61" })
            {
                Assert.That(StatusOf(decisions, correction), Ordinal.Contains("Implemented"), correction);
                Assert.That(StatusOf(decisions, correction), Does.Not.Match("(?i)not implemented"), correction);
            }
            foreach (string oldRule in new[] { "D-02", "D-03", "D-09" })
            {
                Assert.That(StatusOf(decisions, oldRule), Ordinal.Contains("Superseded"), oldRule);
            }
        }
    }

    /// <summary>The status of a decision: its <c>Status</c> item, up to the next item of the list.</summary>
    private static string StatusOf(string decisions, string identifier)
    {
        int heading = decisions.IndexOf("\n### " + identifier + ": ", StringComparison.Ordinal);
        Assert.That(heading, Is.GreaterThan(0), identifier);
        int status = decisions.IndexOf("- **Status:**", heading, StringComparison.Ordinal);
        int next = decisions.IndexOf("\n- **", status + 1, StringComparison.Ordinal);
        return decisions.Substring(status, next - status);
    }

    /// <summary>The table of the five corrections in the compatibility notes says "yes" five times.</summary>
    [Test]
    public void TheCompatibilityNotes_ListAllFiveCorrectionsAsMade()
    {
        string[] rows = Repository.Read("docs/ai-context/compatibility.md").Split('\n')
            .Where(line => Regex.IsMatch(line, @"^\| \d \| \[D-\d\d\]"))
            .ToArray();

        Assert.That(rows, Has.Length.EqualTo(5));
        Assert.That(rows.Select(row => Regex.Match(row, @"\[(D-\d\d)\]").Groups[1].Value), Is.EqualTo(new[] { "D-57", "D-58", "D-59", "D-60", "D-61" }));
        Assert.That(rows, Is.All.Contains("**Yes**"));
    }

    // ---- claims about the present that are no longer true ----

    /// <summary>
    /// Sentences that were true once and said how things are. Each is something an earlier state of the
    /// repository wrote about itself: that the old behavior holds in every 1.x version, that a correction is
    /// still to come, that the modern layers are not built, that the release version is undecided; and, since
    /// 2.0.1 was tagged and released on GitHub and its publication to NuGet was set up, that it has no tag,
    /// that it is not released, and that the repository has one workflow and nothing that can publish.
    /// </summary>
    private static readonly string[] RetiredClaims =
    {
        @"every 1\.x version",
        @"1\.1\.0",
        @"sets the weekday row's color, not the title's",
        @"can therefore name a different month",
        @"Four (of the five )?are (implemented|done)",
        @"Only the title (\(D-61\) )?is still",
        @"one correction still to be made",
        @"\| Not yet \|",
        @"apart from its first layer, not\s+built",
        @"final (version )?number is not set",
        @"modern API (is|was) not (yet )?(implemented|built)",
        @"builds an? (unpublished )?development (version|package)",
        @"\[Unreleased\]",
        @"version suffix is removed only",
        @"no tag .v2\.0\.1.",
        @"It is not released",
        @"Nothing of 2\.0\.1 is released",
        @"(is|was) the only workflow",
        @"There is (still )?no publication workflow",
        @"Nothing in the repository can (publish|push)",
    };

    private static IEnumerable<TestCaseData> ActiveDocumentsAndRetiredClaims() =>
        ActiveDocuments().Select(document => new TestCaseData(document).SetArgDisplayNames(document));

    [TestCaseSource(nameof(ActiveDocumentsAndRetiredClaims))]
    public void NoActiveDocument_StillSaysWhatWasTrueBefore(string document)
    {
        // The changelog names the format it follows by a link whose address contains a version number.
        string text = document == "CHANGELOG.md" ? Markdown.WithoutLinks(Repository.Read(document)) : Repository.Read(document);

        Assert.That(RetiredClaims.Where(claim => Regex.IsMatch(text, claim, RegexOptions.IgnoreCase)), Is.Empty);
    }

    // ---- what is recorded of the repository's settings ----

    /// <summary>
    /// The ruleset for `master` on GitHub waits for three checks by name, and the names come from the workflow:
    /// the name of its job and the systems of its matrix. The ruleset is a setting and is in no file, so the
    /// notes record the names it requires; this test keeps that record and the workflow the same. If it fails
    /// because the workflow changed, the ruleset has to change with it, which needs approval: until it does,
    /// the checks it waits for never arrive and no pull request can be merged.
    /// </summary>
    [Test]
    public void TheChecksRecordedAsRequiredForMaster_AreTheChecksTheWorkflowProduces()
    {
        string workflow = Repository.Read(".github/workflows/ci.yml");
        string notes = Repository.Read("docs/ai-context/development.md");

        MatchCollection jobNames = Regex.Matches(workflow, @"(?m)^    name: (?<name>.+)$");
        Match matrix = Regex.Match(workflow, @"(?m)^        os: \[(?<systems>[^\]]+)\]$");
        Assert.That(jobNames, Has.Count.EqualTo(1), "one job, named once");
        Assert.That(matrix.Success, "one matrix of operating systems");
        string jobName = jobNames[0].Groups["name"].Value;
        Assert.That(jobName, Does.Contain("${{ matrix.os }}"));
        string[] produced = matrix.Groups["systems"].Value.Split(',').Select(system => jobName.Replace("${{ matrix.os }}", system.Trim(), StringComparison.Ordinal)).ToArray();

        string settings = notes[notes.IndexOf("## Repository settings\n", StringComparison.Ordinal)..];
        string[] recorded = Regex.Match(settings, "```text\n(?<names>[^`]+)```").Groups["names"].Value
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(produced, Is.EqualTo(new[] { "Build (ubuntu-latest)", "Build (windows-latest)", "Build (macos-latest)" }), "what the ruleset was created with");
            Assert.That(recorded, Is.EqualTo(produced), "the names in development.md");
            Assert.That(produced.Where(name => !Repository.Read("CLAUDE.md").Contains("`" + name + "`", StringComparison.Ordinal)), Is.Empty, "the names in CLAUDE.md");
        }
    }

    // ---- what the maintenance notes must not contain ----

    [TestCaseSource(nameof(MaintenanceNotes))]
    public void NoPathOfOneMachine_InTheMaintenanceNotes(string document)
    {
        Assert.That(Regex.Matches(Repository.Read(document), @"/home/|/Users/|/tmp/|[A-Za-z]:\\|~/").Select(match => match.Value), Is.Empty);
    }

    /// <summary>The project is .NET only: no document tells anyone to install or run another language's tools.</summary>
    [TestCaseSource(nameof(MaintenanceNotes))]
    public void NoInstructionForAnotherLanguage_InTheMaintenanceNotes(string document)
    {
        Assert.That(Regex.Matches(Repository.Read(document), @"\bpip3? install\b|\bpython3? [\w./-]+\.py\b|requirements\.txt|pyproject\.toml", RegexOptions.IgnoreCase).Select(match => match.Value), Is.Empty);
    }
}
