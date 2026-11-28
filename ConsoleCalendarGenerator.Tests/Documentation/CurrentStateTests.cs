using System.Text.RegularExpressions;
using ConsoleCalendarGenerator.Tests.Support;

namespace ConsoleCalendarGenerator.Tests.Documentation;

/// <summary>
/// The documents describe the repository as it is now: all five corrections of version 2.0 made, the modern API
/// built, and the version 2.0.1 with no suffix: the source of a release that is on GitHub and on NuGet. These
/// tests look for what went stale when each of those became true.
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
    /// that it is not released, and that the repository has one workflow and nothing that can publish; and,
    /// since 2.0.1 was published to NuGet, that it is not there, that the publication has not run, that
    /// nuget.org has 1.0.0 alone, and that its changelog heading has no date; and, since 1.0.0 was deprecated,
    /// that it is not, that it is supported, and that a question about it is open.
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
        @"2\.0\.1 is (released on GitHub and is )?not on NuGet",
        @"set up and (\*\*)?(has )?not run",
        @"(it|workflow) has (not|never) run",
        @"nuget\.org has 1\.0\.0 (only|and nothing else)",
        @"1\.0\.0 only \(verified",
        @"The publication to NuGet is not authorized",
        @"\[2\.0\.1\] - Unreleased",
        @"until a 2\.x version is published",
        @"the version that goes to NuGet",
        @"1\.0\.0 is (listed and )?not deprecated",
        @"neither (is|of them is) deprecated|, neither deprecated",
        @"1\.0\.0 (stays|is)( on NuGet,)? listed( and|,) supported",
        @"(is|stays) listed and supported",
        @"One question is open",
        @"Q-15 (remains|stays|is) open",
        @"Whether 1\.0\.0 is deprecated( afterwards)? is open",
        @"deprecating it is a (separate )?decision that has not been made",
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

    // ---- the release is published ----

    private static readonly string[] NotesAndPublicDocuments = Repository.PublicDocuments.Concat(MaintenanceNotes).ToArray();

    /// <summary>
    /// 2.0.1 is on nuget.org. The two places a maintainer reads first say so in the one form that cannot be
    /// mistaken, the address of the package, and the notes on releases name the run that published it.
    /// </summary>
    [Test]
    public void TheNotes_SayThatThisReleaseIsOnNuGet()
    {
        string page = "https://www.nuget.org/packages/ConsoleCalendarGenerator/" + ReleaseVersion();

        Assert.That(Repository.Read("CLAUDE.md"), Ordinal.Contains(page));
        Assert.That(Repository.Read("docs/ai-context/packaging-and-releases.md"), Ordinal.Contains(page));
        Assert.That(Repository.Read("docs/ai-context/packaging-and-releases.md"), Does.Match(@"publish-nuget\.yml[^\n]*attempt 1|attempt 1[^\n]*publish-nuget\.yml"),
            "the one run that published, in its first attempt");
    }

    /// <summary>
    /// No question is open. How the publication is authenticated was settled by the decision that set up
    /// Trusted Publishing, and whether 1.0.0 is deprecated by the decision that deprecated it. A question that
    /// is open has a section of its own in the notes; a settled one has a line that names its decision.
    /// </summary>
    [Test]
    public void NoQuestionIsOpen()
    {
        string questions = Regex.Replace(Repository.Read("docs/ai-context/open-questions.md"), @"\s+", " ");
        string decisions = Repository.Read(Decisions);

        Assert.That(Repository.Read("docs/ai-context/open-questions.md").Split('\n').Where(line => line.StartsWith("## ", StringComparison.Ordinal)), Is.Empty,
            "no question has a section, so none is open");
        Assert.That(questions, Does.Match(@"Q-14 \([^)]*\) is settled[^Q]*D-88"), "authentication: settled by the decision on Trusted Publishing");
        Assert.That(questions, Does.Match(@"Q-15 \([^)]*\) is settled[^Q]*D-92"), "the deprecation of 1.0.0: settled by the decision that made it");
        Assert.That(StatusOf(decisions, "D-88"), Ordinal.Contains("Q-14"));
        Assert.That(StatusOf(decisions, "D-92"), Ordinal.Contains("Q-15"));
    }

    /// <summary>
    /// The roadmap records every step of the release as done, the deprecation of 1.0.0 last, says that the
    /// release program is complete, and has nothing left: no numbered step under what remains, and no next
    /// action. A later version is not planned here.
    /// </summary>
    [Test]
    public void TheRoadmap_RecordsTheReleaseProgramAsComplete_AndHasNothingLeft()
    {
        string roadmap = Repository.Read("docs/ai-context/roadmap.md");
        string completed = Between(roadmap, "\n## Completed\n", "\n## Remaining sequence\n");
        string remaining = Between(roadmap, "\n## Remaining sequence\n", "\n## Next action\n");
        string next = roadmap.Substring(roadmap.IndexOf("\n## Next action\n", StringComparison.Ordinal));
        string[] phases = completed.Split('\n').Where(line => line.StartsWith("| ", StringComparison.Ordinal)).Select(line => line.Split('|')[1].Trim()).ToArray();

        Assert.That(phases, Is.SupersetOf(new[]
        {
            "NuGet Trusted Publishing", "NuGet publication of " + ReleaseVersion(), "Post-publication update", "Deprecation of 1.0.0",
        }));
        Assert.That(remaining.Split('\n').Where(line => Regex.IsMatch(line, @"^\d+\. ")), Is.Empty, "no step is left");
        Assert.That(Regex.Replace(remaining, @"\s+", " "), Does.Match(@"(?i)release program.{0,60}\bis complete\b"));
        Assert.That(next, Does.Match(@"(?m)^\*\*None\.\*\*"), "and there is no next action");
        Assert.That(Regex.Matches(roadmap, @"\b[23]\.\d+\.\d+\b").Select(match => match.Value).Distinct(), Is.SubsetOf(new[] { "2.0.0", ReleaseVersion() }),
            "no later version is planned here");
    }

    private static string Between(string text, string start, string end)
    {
        int from = text.IndexOf(start, StringComparison.Ordinal);
        int to = text.IndexOf(end, from + 1, StringComparison.Ordinal);
        Assert.That(from, Is.GreaterThanOrEqualTo(0), start.Trim());
        Assert.That(to, Is.GreaterThan(from), end.Trim());
        return text.Substring(from, to - from);
    }

    /// <summary>
    /// 1.0.0 is deprecated on nuget.org and stays listed: the two places a maintainer reads first say both, and
    /// the notes on releases record the deprecation as it was made, with its reason, the version it recommends
    /// and the message consumers are shown.
    /// </summary>
    [Test]
    public void TheNotes_SayThatTheFirstVersionIsDeprecatedAndStillListed_AndRecommendThisRelease()
    {
        const string message = "Version 2.0.1 is the current supported release and retains the original ConsoleCalendar API.";
        string releases = Repository.Read("docs/ai-context/packaging-and-releases.md");
        string section = Between(releases, "\n### The deprecation of 1.0.0\n", "\n## ");

        foreach (string document in new[] { "CLAUDE.md", "docs/ai-context/packaging-and-releases.md" })
        {
            string text = Regex.Replace(Repository.Read(document), @"\s+", " ");
            Assert.That(text, Does.Match(@"(?i)\b1\.0\.0\b.{0,250}?\bis deprecated\b.{0,120}?\bstays listed\b"),
                document + " says that 1.0.0 is deprecated and that it stays listed");
        }
        Assert.That(section, Does.Match(@"(?m)^\| Reason \| Legacy\b"));
        Assert.That(section, Does.Match(@"(?m)^\| Alternate \| `ConsoleCalendarGenerator`, version " + Regex.Escape(ReleaseVersion()) + @" \|$"));
        Assert.That(section, Does.Match(@"(?m)^\| Listed \| yes\b"));
        Assert.That(section, Ordinal.Contains("| Message | " + message + " |"), "the message, as it was entered");
    }

    /// <summary>
    /// What no document says, because none of it is so: that 1.0.0 is unlisted, deleted or removed; that it has
    /// critical bugs, which is the other reason nuget.org offers and was not given; and that 2.0.1, the current
    /// version, is deprecated. A sentence that says a thing is not so, or was not done, states nothing.
    /// </summary>
    [Test]
    public void NoDocument_SaysThatTheFirstVersionIsGoneOrFaulty_OrThatThisReleaseIsDeprecated()
    {
        string release = Regex.Escape(ReleaseVersion());
        var stated = new List<string>();
        foreach (string document in NotesAndPublicDocuments)
        {
            stated.AddRange(Repository.Read(document).Split('\n')
                .Where(line => Regex.IsMatch(line,
                        @"(?i)\b1\.0\.0 (is|was|has been) (now )?(unlisted|delisted|deleted|removed)\b|\b" + release + @" (is|was|has been) (now )?deprecated\b|\b2\.x (line )?is deprecated\b")
                    && !Regex.IsMatch(line, @"(?i)\bnot\b|\bnever\b|\bno\b|\bnor\b|\bneither\b|\bwhether\b|\bif\b|\?"))
                .Select(line => document + ": " + line.Trim()));
            stated.AddRange(Regex.Matches(Repository.Read(document), @"(?i)critical bugs?").Select(match => document + ": " + match.Value));
        }

        Assert.That(stated, Is.Empty);
    }

    /// <summary>
    /// The publication used a key that lasted an hour and was never written anywhere. Nothing that has the form
    /// of a credential is in a document, a workflow or a script: no key of nuget.org, no token of GitHub, no
    /// signed token, no private key.
    /// </summary>
    [Test]
    public void NoCredentialIsWrittenDown()
    {
        var found = new List<string>();
        foreach (string file in Repository.FilesWithExtension(".md", ".yml", ".yaml", ".sh", ".txt", ".json", ".config", ".props"))
        {
            found.AddRange(Regex.Matches(Repository.Read(file),
                    @"\boy2[a-z0-9]{43}\b|\bgh[pousr]_[A-Za-z0-9]{30,}|\bgithub_pat_[A-Za-z0-9_]{20,}|\beyJ[A-Za-z0-9_-]{10,}\.[A-Za-z0-9_-]{10,}\.|-----BEGIN [A-Z ]*PRIVATE KEY-----")
                .Select(match => file + ": " + match.Value.Substring(0, 8) + "..."));
        }

        Assert.That(found, Is.Empty);
    }

    /// <summary>
    /// Campertron adopts the package in its own project. These notes say that it is not changed from here; they
    /// give no command or project change to carry out there.
    /// </summary>
    [Test]
    public void NoNote_GivesAStepToCarryOutInCampertron()
    {
        var steps = new List<string>();
        foreach (string document in MaintenanceNotes)
        {
            steps.AddRange(Repository.Read(document).Split('\n')
                .Where(line => line.Contains("Campertron", StringComparison.Ordinal)
                    && Regex.IsMatch(line, @"\bdotnet add\b|<PackageReference|\bgit (clone|commit|push|switch|checkout)\b|\bgh pr\b"))
                .Select(line => document + ": " + line.Trim()));
        }

        Assert.That(steps, Is.Empty);
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
