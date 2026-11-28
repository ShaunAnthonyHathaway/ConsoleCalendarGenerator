using System.Text.RegularExpressions;
using ConsoleCalendarGenerator.Tests.Support;

namespace ConsoleCalendarGenerator.Tests.Infrastructure;

/// <summary>
/// The workflows of the repository, read as text. There are two. ci.yml builds and checks the repository on
/// every pull request and every push; what these tests protect is what makes that safe: it runs only code that
/// was reviewed, it can write nothing, it knows no secret, and nobody can start it by hand. publish-nuget.yml
/// publishes, is started by hand only, and has tests of its own in <see cref="PublishWorkflowTests"/>. What
/// holds for both is here: every action is pinned, and an action both use is the same commit in both.
///
/// <para>
/// An action referenced by a tag or a branch runs whatever that name points at on the day of the run. Only the
/// full SHA of a commit names code that cannot change, so every reference must be one, with the version it was
/// released as written beside it for the person who reads or updates it.
/// </para>
/// </summary>
[TestFixture]
public class WorkflowTests
{
    private const string Folder = ".github/workflows/";
    private const string Ci = Folder + "ci.yml";
    private const string Publication = Folder + "publish-nuget.yml";

    private static readonly string[] Workflows =
        Repository.FilesWithExtension(".yml", ".yaml").Where(file => file.StartsWith(Folder, StringComparison.Ordinal)).ToArray();

    /// <summary>The lines of a workflow that are not comments, without what follows a "#" after the value.</summary>
    private static string[] Lines(string workflow) =>
        Repository.Read(workflow).Split('\n').Where(line => !line.TrimStart().StartsWith('#')).ToArray();

    private static string WithoutComments(string workflow) =>
        string.Join("\n", Lines(workflow).Select(line => Regex.Replace(line, @"\s+#.*$", string.Empty)));

    private static string[] Uses(string workflow) =>
        Lines(workflow).Where(line => Regex.IsMatch(line, @"^\s*(-\s+)?uses:")).Select(line => line.Trim()).ToArray();

    [Test]
    public void ThereAreTwoWorkflows_TheOneThatChecksAndTheOneThatPublishes()
    {
        Assert.That(Workflows, Is.EqualTo(new[] { Ci, Publication }), "another workflow is a decision of its own");
    }

    [TestCaseSource(nameof(Workflows))]
    public void EveryAction_IsPinnedToTheFullShaOfACommit_WithItsVersionBesideIt(string workflow)
    {
        string[] uses = Uses(workflow);

        Assert.That(uses, Is.Not.Empty);
        foreach (string reference in uses)
        {
            Assert.That(reference, Does.Match(@"^(-\s+)?uses: [A-Za-z0-9_.-]+/[A-Za-z0-9_./-]+@[0-9a-f]{40} # v\d+\.\d+\.\d+$"),
                "not '<owner>/<repository>@<40 hexadecimal digits> # v<major>.<minor>.<patch>': " + reference);
        }
    }

    /// <summary>The actions that were reviewed. Another one is added here in the change that adds it.</summary>
    [TestCase(Ci, "actions/checkout", "actions/setup-dotnet", "actions/upload-artifact")]
    [TestCase(Publication, "actions/checkout", "actions/setup-dotnet", "NuGet/login")]
    public void TheActionsUsed_AreTheOnesThatWereReviewed(string workflow, params string[] reviewed)
    {
        IEnumerable<string> actions = Uses(workflow).Select(reference => Regex.Match(reference, @"uses: ([^@\s]+)").Groups[1].Value).Distinct();

        Assert.That(actions, Is.EquivalentTo(reviewed));
    }

    /// <summary>An action that both workflows use is one commit, so that a pin is moved in both or in neither.</summary>
    [Test]
    public void AnActionBothWorkflowsUse_IsTheSameCommitInBoth()
    {
        string[] pins = Workflows.SelectMany(Uses).Select(reference => Regex.Match(reference, @"uses: (\S+ # v\S+)$").Groups[1].Value).Distinct().ToArray();

        Assert.That(pins.Select(pin => pin.Split('@')[0]), Is.Unique, "one commit and one version for each action: " + string.Join(", ", pins));
    }

    [TestCase(Ci)]
    public void TheOnlyPermission_IsToReadTheRepository(string workflow)
    {
        string text = WithoutComments(workflow);

        Assert.That(Regex.Matches(text, @"(?m)^\s*permissions:"), Has.Count.EqualTo(1), "one permissions block, for the whole workflow");
        Assert.That(text, Does.Match(@"(?m)^permissions:\n  contents: read\n\n"), "and it grants reading the contents, nothing else");
        Assert.That(Regex.Matches(text, @"(?m):\s*write\b|write-all|id-token|attestations:").Select(match => match.Value), Is.Empty);
    }

    [TestCase(Ci)]
    public void ItRunsOnPullRequestsAndOnPushesToMaster_AndCannotBeStartedByHand(string workflow)
    {
        string text = WithoutComments(workflow);

        Assert.That(text, Does.Match(@"(?m)^on:\n  pull_request:\n  push:\n    branches: \[master\]\n\n"));
        Assert.That(Regex.Matches(text, @"workflow_dispatch|workflow_call|workflow_run|pull_request_target|repository_dispatch|schedule:|release:|\btags:").Select(match => match.Value), Is.Empty);
    }

    [TestCase(Ci)]
    public void ItUsesNoSecret_AndKeepsNoCredential(string workflow)
    {
        string text = WithoutComments(workflow);

        Assert.That(Regex.Matches(text, @"secrets\.|GITHUB_TOKEN|github\.token|API_KEY|api-key|password", RegexOptions.IgnoreCase).Select(match => match.Value), Is.Empty);
        Assert.That(text, Does.Match(@"uses: actions/checkout@[0-9a-f]{40}\n\s+with:\n\s+persist-credentials: false"), "the checkout does not leave its token in the repository's configuration");
    }

    [Test]
    public void ItBuildsOnTheThreeOperatingSystems_AndStillRunsEveryCheck()
    {
        string text = WithoutComments(Ci);

        Assert.That(text, Ordinal.Contains("os: [ubuntu-latest, windows-latest, macos-latest]"));
        Assert.That(text, Ordinal.Contains("--native-aot --published-sample"), "the sample of 1.0.0 is run with the published 1.0.0 on Linux");
        foreach (string script in new[]
        {
            "eng/verify-package.sh", "eng/consumer-smoke.sh", "eng/release-bundle.sh", "eng/release-bundle-tamper-test.sh",
            "eng/direct-reference-smoke.sh", "eng/upgrade-smoke.sh", "eng/verify-published-package-selftest.sh", "eng/release-state.sh",
        })
        {
            Assert.That(text, Ordinal.Contains("bash " + script), script);
            Assert.That(Repository.HasFile(script), script);
        }
        Assert.That(Regex.Matches(text, @"(?m)^\s+-warnaserror$"), Has.Count.GreaterThanOrEqualTo(3), "warnings still fail the build, the second test run and the pack");
    }
}
