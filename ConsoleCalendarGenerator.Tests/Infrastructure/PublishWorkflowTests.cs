using System.Text.RegularExpressions;
using ConsoleCalendarGenerator.Tests.Support;

namespace ConsoleCalendarGenerator.Tests.Infrastructure;

/// <summary>
/// The workflow that publishes to nuget.org, read as text. It is the one file of the repository that can
/// publish, so what these tests protect is everything that keeps it from publishing by accident, publishing
/// something else, or publishing before it was checked: it is started by hand and by nothing else; it accepts
/// the approved publication only; one job may ask for an identity, and only after every check; the key is a
/// temporary one that reaches one step; nothing is built; and what was pushed is checked afterwards.
///
/// <para>
/// These tests cannot run the workflow, and nothing may: starting it is a publication. What they can do is
/// read every job and step and work out which steps run when one of them fails, which is how GitHub runs a
/// job. The checks themselves are run by eng/publication-preflight-selftest.sh, which a test here starts.
/// </para>
/// </summary>
[TestFixture]
public class PublishWorkflowTests
{
    private const string Workflow = ".github/workflows/publish-nuget.yml";
    private const string LoginAction = "NuGet/login@8d196754b4036150537f80ac539e15c2f1028841";

    private static readonly string[] IdentityOptions = { "--version \"$VERSION\"", "--tag \"$TAG\"", "--release-id \"$RELEASE_ID\"", "--release-commit \"$RELEASE_COMMIT\"" };

    /// <summary>The workflow without its comments: lines that are comments, and what follows a "#" after a value.</summary>
    private static readonly string Text = string.Join("\n", Repository.Read(Workflow).Split('\n')
        .Where(line => !line.TrimStart().StartsWith('#'))
        .Select(line => Regex.Replace(line, @"\s+#.*$", string.Empty)));

    private static readonly string Facts = Repository.Read("eng/publication.sh");

    /// <summary>A value the publication scripts state, such as approved_version.</summary>
    private static string Approved(string name) =>
        Regex.Match(Facts, @"(?m)^approved_" + name + @"=""(?<value>[^""]+)""$") is { Success: true } match
            ? match.Groups["value"].Value
            : throw new InvalidOperationException("eng/publication.sh does not state approved_" + name);

    /// <summary>The lines below a key that are indented further than it, up to the next key at its level.</summary>
    private static string Block(string text, string key, int indent)
    {
        string[] lines = text.Split('\n');
        string start = new string(' ', indent) + key + ":";
        int first = Array.FindIndex(lines, line => line == start || line.StartsWith(start + " ", StringComparison.Ordinal));
        Assert.That(first, Is.GreaterThanOrEqualTo(0), "no '" + start + "'");
        IEnumerable<string> body = lines.Skip(first + 1).TakeWhile(line => line.Length == 0 || line.Length - line.TrimStart().Length > indent);
        return string.Join("\n", body);
    }

    /// <summary>The keys directly below a block whose own key is indented by <paramref name="indent"/>.</summary>
    private static string[] Keys(string block, int indent) =>
        Regex.Matches(block, @"(?m)^ {" + indent + @"}(?<key>[A-Za-z_][A-Za-z0-9_-]*):").Select(match => match.Groups["key"].Value).ToArray();

    private static string Job(string name) => Block(Block(Text, "jobs", 0), name, 2);

    /// <summary>The steps of a job, in order, each as its text.</summary>
    private static string[] Steps(string job) =>
        Regex.Split(Block(job, "steps", 4), @"(?m)^      - ").Where(step => step.Trim().Length > 0).ToArray();

    private static int IndexOfStep(string[] steps, string what, Func<string, bool> isIt)
    {
        int[] found = Enumerable.Range(0, steps.Length).Where(index => isIt(steps[index])).ToArray();
        Assert.That(found, Has.Length.EqualTo(1), "exactly one step is " + what);
        return found[0];
    }

    private static bool IsPreflight(string step) => step.Contains("bash eng/publication-preflight.sh", StringComparison.Ordinal);

    private static bool IsLogin(string step) => step.Contains("uses: NuGet/login@", StringComparison.Ordinal);

    private static bool IsPush(string step) => Regex.IsMatch(step, @"\bdotnet\s+nuget\s+push\b");

    /// <summary>
    /// Which steps of a job run when one of them fails, as GitHub decides it: a step runs when every step before
    /// it succeeded, and after a failure only a step whose condition says "always()" or "failure()" still runs.
    /// A step marked continue-on-error would not count as failed, so no step may be marked that way.
    /// </summary>
    private static bool[] RunsWhenStepFails(string[] steps, int failing)
    {
        var runs = new bool[steps.Length];
        for (int index = 0; index < steps.Length; index++)
        {
            Match condition = Regex.Match(steps[index], @"(?m)^\s*if:\s*(?<condition>.+)$");
            runs[index] = index <= failing
                || (condition.Success && Regex.IsMatch(condition.Groups["condition"].Value, @"always\(\)|failure\(\)"));
        }
        return runs;
    }

    [Test]
    public void ItIsStartedByHand_AndByNothingElse()
    {
        Assert.That(Keys(Block(Text, "on", 0), 2), Is.EqualTo(new[] { "workflow_dispatch" }), "the one trigger");
        Assert.That(Regex.Matches(Text, @"(?m)^\s*(push|pull_request|pull_request_target|schedule|release|create|workflow_run|workflow_call|repository_dispatch|deployment|registry_package|tags|branches)\s*:")
            .Select(match => match.Value.Trim()), Is.Empty, "no push, pull request, tag, release, schedule or other workflow starts it");
    }

    [Test]
    public void ItsInputsAreTheApprovedPublication_AndEveryOneIsRequired()
    {
        string inputs = Block(Block(Block(Text, "on", 0), "workflow_dispatch", 2), "inputs", 4);

        Assert.That(Keys(inputs, 6), Is.EqualTo(new[] { "version", "tag", "release_id", "release_commit" }));
        foreach ((string input, string fact) in new[] { ("version", "version"), ("tag", "tag"), ("release_id", "release_id"), ("release_commit", "release_commit") })
        {
            string definition = Block(inputs, input, 6);
            Assert.That(definition, Does.Match(@"(?m)^\s*required: true$"), input);
            Assert.That(definition, Does.Match(@"(?m)^\s*type: string$"), input);
            Assert.That(Regex.Match(definition, @"(?m)^\s*default: ""(?<value>[^""]+)""$").Groups["value"].Value, Is.EqualTo(Approved(fact)),
                "the default of " + input + " is the value eng/publication.sh requires, so that the two cannot drift apart");
        }
        Assert.That(Regex.Matches(Text, @"\binputs\.(?<name>[a-z_]+)").Select(match => match.Groups["name"].Value).Distinct(),
            Is.EquivalentTo(new[] { "version", "tag", "release_id", "release_commit" }), "and no input switches a check off or a mode on");
    }

    [Test]
    public void TheApprovedPublication_IsTheVersionThisRepositoryHolds_AndItsFourFiles()
    {
        string project = Repository.Read("ConsoleCalendarGenerator/ConsoleCalendarGenerator.csproj");
        string version = Approved("version");

        Assert.That(project, Ordinal.Contains("<VersionPrefix>" + version + "</VersionPrefix>"));
        Assert.That(Approved("tag"), Is.EqualTo("v" + version));
        Assert.That(Approved("file_version"), Is.EqualTo(version + ".0"));
        Assert.That(Approved("assembly_version"), Is.EqualTo(version.Split('.')[0] + ".0.0.0"));
        Assert.That(Approved("repository"), Is.EqualTo("ShaunAnthonyHathaway/ConsoleCalendarGenerator"));
        Assert.That(Approved("workflow"), Is.EqualTo(Workflow));
        Assert.That(Approved("ref"), Is.EqualTo("refs/heads/master"));
        Assert.That(Approved("release_commit"), Does.Match("^[0-9a-f]{40}$"));
        Assert.That(Approved("tag_object"), Does.Match("^[0-9a-f]{40}$"));
        Assert.That(Approved("nuget_user"), Does.Match("^[A-Za-z0-9._-]+$"), "a user name of nuget.org, not an e-mail address");

        string[] checksums = Regex.Match(Facts, @"(?m)^approved_checksums=""(?<lines>[^""]+)""$").Groups["lines"].Value.Split('\n');
        Assert.That(checksums.Select(line => Regex.Replace(line, "^[0-9a-f]{64}  ", string.Empty)), Is.EqualTo(new[]
        {
            "ConsoleCalendarGenerator-" + version + "-binaries.zip", "ConsoleCalendarGenerator." + version + ".nupkg",
            "ConsoleCalendarGenerator." + version + ".snupkg", "SHA256SUMS.txt",
        }), "four lines as sha256sum writes them, in the order of the names");
    }

    [Test]
    public void TheWorkflowReadsTheRepository_AndOnlyThePublishingJobMayAskForAnIdentity()
    {
        Assert.That(Text, Does.Match(@"(?m)^permissions:\n  contents: read\n\n"), "the default of every job is to read the contents and nothing else");
        Assert.That(Regex.Matches(Text, @"(?m)^\s*permissions:"), Has.Count.EqualTo(2), "one block for the workflow and one for the job that publishes");
        Assert.That(Keys(Block(Job("publish"), "permissions", 4), 6), Is.EqualTo(new[] { "contents", "id-token" }));
        Assert.That(Block(Job("publish"), "permissions", 4), Is.EqualTo("      contents: read\n      id-token: write"));
        Assert.That(Regex.Matches(Text, @":\s*write\b"), Has.Count.EqualTo(1), "id-token is the only thing any job may write");
        Assert.That(Regex.Matches(Text, @"(?m)write-all|^\s*(packages|actions|attestations|pull-requests|security-events|deployments|checks|statuses|issues|discussions|pages)\s*:")
            .Select(match => match.Value.Trim()), Is.Empty);
        foreach (string job in new[] { "preflight", "verify" })
        {
            Assert.That(Job(job), Ordinal.DoesNotContain("permissions:"), job + " keeps the default");
            Assert.That(Job(job), Ordinal.DoesNotContain("id-token"), job);
        }
    }

    [Test]
    public void OnlyThePublishingJobUsesTheEnvironment_WhichIsTheApprovedOne()
    {
        Assert.That(Regex.Matches(Text, @"(?m)^\s*environment:"), Has.Count.EqualTo(1));
        Assert.That(Job("publish"), Does.Match(@"(?m)^    environment: " + Regex.Escape(Approved("environment")) + "$"));
        Assert.That(Approved("environment"), Is.EqualTo("nuget-production"), "the name the policy on nuget.org is bound to");
        Assert.That(Regex.Matches(Text, @"\bvars\.(?<name>[A-Z_]+)").Select(match => match.Groups["name"].Value).Distinct(), Is.EqualTo(new[] { "NUGET_USER" }),
            "the user name for nuget.org is the one variable, and it is not a secret");
    }

    [Test]
    public void TheThreeJobsRunOneAfterTheOther_AndNoneIsEverSkippedOver()
    {
        Assert.That(Keys(Block(Text, "jobs", 0), 2), Is.EqualTo(new[] { "preflight", "publish", "verify" }));
        Assert.That(Job("preflight"), Ordinal.DoesNotContain("needs:"));
        Assert.That(Job("publish"), Does.Match(@"(?m)^    needs: preflight$"), "nothing is published unless the preflight job passed");
        Assert.That(Job("verify"), Does.Match(@"(?m)^    needs: publish$"), "and what was pushed is checked after the push");
        foreach (string job in new[] { "preflight", "publish", "verify" })
        {
            Assert.That(Job(job), Does.Not.Match(@"(?m)^    if:"), job + " has no condition of its own");
            Assert.That(Job(job), Does.Match(@"(?m)^    runs-on: ubuntu-latest$"), job);
            Assert.That(Job(job), Does.Match(@"(?m)^    timeout-minutes: \d+$"), job + " has a time limit");
        }
        Assert.That(Regex.Matches(Text, @"continue-on-error|strategy:|matrix:").Select(match => match.Value), Is.Empty,
            "a failure is a failure, and each job runs once");
        Assert.That(Text, Does.Match(@"(?m)^concurrency:\n  group: publish-nuget\n  cancel-in-progress: false$"), "one publication at a time, never cancelled half way");
    }

    [Test]
    public void TheActions_AreTheReviewedOnes_AtTheCommitsCiUses()
    {
        string[] used = Regex.Matches(Text, @"(?m)^\s*(-\s+)?uses: (?<action>\S+)$").Select(match => match.Groups["action"].Value).Distinct().ToArray();
        string ci = Repository.Read(".github/workflows/ci.yml");

        Assert.That(used.Select(action => action.Split('@')[0]), Is.EquivalentTo(new[] { "actions/checkout", "actions/setup-dotnet", "NuGet/login" }));
        Assert.That(used, Has.Member(LoginAction), "NuGet/login v1.2.0, the commit that was read before it was pinned");
        Assert.That(Repository.Read(Workflow), Ordinal.Contains("uses: " + LoginAction + " # v1.2.0"));
        foreach (string action in used.Where(action => !action.StartsWith("NuGet/", StringComparison.Ordinal)))
        {
            Assert.That(ci, Ordinal.Contains("uses: " + action + " #"), action + " is pinned to the commit ci.yml uses");
        }
        Assert.That(Regex.Matches(Text, @"uses: actions/checkout@[0-9a-f]{40}\n\s+with:\n\s+persist-credentials: false"), Has.Count.EqualTo(3),
            "no checkout leaves its token in the repository's configuration");
    }

    [Test]
    public void NothingIsBuiltOrPacked_AndTheOnlyDotnetCommandIsThePush()
    {
        string[] commands = Regex.Matches(Text, @"\bdotnet\s+[a-z][a-z-]*(\s+[a-z][a-z-]*)?").Select(match => Regex.Replace(match.Value, @"\s+", " ")).ToArray();

        Assert.That(commands, Is.EqualTo(new[] { "dotnet nuget push" }), "the files that are published are the files of the release, never a build");
        Assert.That(Regex.Matches(Text, @"\.sln\b|\.csproj\b|\bmsbuild\b|eng/release-bundle\.sh|artifacts/").Select(match => match.Value), Is.Empty);
        Assert.That(Regex.Matches(Text, @"bash (""\$GITHUB_WORKSPACE/)?(?<script>eng/[a-z-]+\.sh)").Select(match => match.Groups["script"].Value).Distinct(),
            Is.EquivalentTo(new[] { "eng/publication-preflight.sh", "eng/publication-checksums.sh", "eng/publication-verify.sh" }),
            "the scripts it runs are the three that check a publication");
    }

    [Test]
    public void ThePush_IsOneCommand_ForTheApprovedPackage_ToNuGetOrg()
    {
        string version = Approved("version");
        MatchCollection pushes = Regex.Matches(Text, @"(?m)^.*\bdotnet\s+nuget\s+push\b.*$");

        Assert.That(pushes, Has.Count.EqualTo(1), "one command in the whole repository publishes");
        string push = pushes[0].Value;
        Assert.That(push, Ordinal.Contains("\"$RUNNER_TEMP/release/ConsoleCalendarGenerator." + version + ".nupkg\""), "the package the checks downloaded, by its exact name");
        Assert.That(push, Ordinal.Contains("--source https://api.nuget.org/v3/index.json"));
        Assert.That(push, Ordinal.Contains("--api-key \"$NUGET_API_KEY\""));
        Assert.That(Regex.Matches(push, @"--skip-duplicate|--no-symbols|--no-service-endpoint|--disable-buffering|--force|\*").Select(match => match.Value), Is.Empty,
            "no option that hides a version that is already there, and no pattern that could match another file");
        Assert.That(Regex.Matches(Text, @"(?i)\bdotnet\s+nuget\s+delete\b|\bnuget(\.exe)?\s+delete\b|deprecat|\bset\s+-[a-z]*x|xtrace|ACTIONS_STEP_DEBUG|ACTIONS_RUNNER_DEBUG").Select(match => match.Value), Is.Empty,
            "nothing deletes, unlists or deprecates a version, and nothing traces the commands of a step that has the key");
    }

    [Test]
    public void TheKeyIsATemporaryOne_GivenToTheOneStepThatPushes()
    {
        string[] steps = Steps(Job("publish"));
        int login = IndexOfStep(steps, "the login", IsLogin);
        int push = IndexOfStep(steps, "the push", IsPush);

        Assert.That(Regex.Matches(Text, @"(?i)secrets\.|password|passphrase").Select(match => match.Value), Is.Empty, "no secret is stored, so none is read");
        Assert.That(steps[login], Does.Match(@"(?m)^\s*id: login$"));
        Assert.That(steps[login], Does.Match(@"(?m)^\s*user: \$\{\{ vars\.NUGET_USER \}\}$"), "the user name, from the environment's variable");
        Assert.That(Regex.Matches(Text, @"steps\.login\.outputs\.NUGET_API_KEY"), Has.Count.EqualTo(1), "the key is taken from the login once");
        Assert.That(steps[push], Does.Match(@"(?m)^\s*NUGET_API_KEY: \$\{\{ steps\.login\.outputs\.NUGET_API_KEY \}\}$"), "by the step that pushes, as an environment variable");
        foreach (string step in steps.Where((_, index) => index != push))
        {
            Assert.That(step, Ordinal.DoesNotContain("NUGET_API_KEY"), "no other step of the job has the key");
        }
        foreach (string job in new[] { "preflight", "verify" })
        {
            Assert.That(Job(job), Ordinal.DoesNotContain("NUGET_API_KEY"), job);
            Assert.That(Job(job), Ordinal.DoesNotContain("NuGet/login"), job);
            Assert.That(Job(job), Does.Not.Match(@"\bdotnet\s+nuget\b"), job);
        }
        Assert.That(Regex.Matches(Text, @"(?m)^    outputs:|GITHUB_OUTPUT|GITHUB_ENV|upload-artifact|actions/cache").Select(match => match.Value.Trim()), Is.Empty,
            "nothing carries a value from the job that publishes to another job, a later run or a stored file");
        Assert.That(Regex.Matches(steps[push], @"\b(echo|printf|cat|tee)\b[^\n]*\$NUGET_API_KEY").Select(match => match.Value),
            Is.EqualTo(new[] { "printf '%s\\n' \"$NUGET_API_KEY" }), "the key is written nowhere: the one printf feeds the search for it in the output of the push");
        Assert.That(steps[push], Ordinal.Contains("rm -f \"$RUNNER_TEMP/push.log\""), "and that output is deleted if the key is in it");
    }

    [Test]
    public void TheTokenOfTheRun_OnlyReadsTheRelease_ForTheStepsThatCheck()
    {
        Assert.That(Regex.Matches(Text, @"github\.token|GITHUB_TOKEN|GH_TOKEN").Select(match => match.Value).Distinct(), Is.EquivalentTo(new[] { "github.token", "GH_TOKEN" }));
        foreach (string job in new[] { "preflight", "publish", "verify" })
        {
            foreach (string step in Steps(Job(job)))
            {
                bool checks = Regex.IsMatch(step, @"bash eng/publication-(preflight|verify)\.sh");
                Assert.That(step.Contains("GH_TOKEN: ${{ github.token }}", StringComparison.Ordinal), Is.EqualTo(checks),
                    "the token is given to the steps that read the release, and to no other: " + step.Split('\n')[0]);
            }
        }
    }

    /// <summary>
    /// The order of the job that publishes, and what it means: every check has passed before the login, the
    /// login has succeeded before the push, and nothing between them can fail without stopping both.
    /// </summary>
    [Test]
    public void InTheJobThatPublishes_EveryCheckComesBeforeTheLogin_AndTheLoginBeforeThePush()
    {
        string[] steps = Steps(Job("publish"));
        int preflight = IndexOfStep(steps, "the preflight", IsPreflight);
        int login = IndexOfStep(steps, "the login", IsLogin);
        int push = IndexOfStep(steps, "the push", IsPush);

        Assert.That(login, Is.EqualTo(preflight + 1), "the login is the step right after the checks");
        Assert.That(push, Is.EqualTo(login + 1), "and the push the step right after the login: the key is used at once");
        foreach (string option in IdentityOptions.Concat(new[] { "--nuget-user \"$NUGET_USER\"", "--into \"$RUNNER_TEMP/release\"" }))
        {
            Assert.That(steps[preflight], Ordinal.Contains(option), "the checks are given " + option);
        }
        Assert.That(steps[preflight], Ordinal.DoesNotContain("--simulate"), "a simulation is never a step of a publication");
        Assert.That(steps[preflight], Ordinal.DoesNotContain("--bundle"), "the files are downloaded from the release, never brought along");
        Assert.That(steps[preflight], Ordinal.DoesNotContain("|| true"));
        Assert.That(steps[login], Does.Not.Match(@"(?m)^\s*if:"), "the login has no condition: it runs only when every step before it succeeded");
        Assert.That(steps[push], Does.Not.Match(@"(?m)^\s*if:"), "and so does the push");
        Assert.That(steps[push].IndexOf("bash eng/publication-checksums.sh \"$RUNNER_TEMP/release\"", StringComparison.Ordinal),
            Is.InRange(0, steps[push].IndexOf("dotnet nuget push", StringComparison.Ordinal)), "the checksums are compared once more, in the step that pushes, before the push");
        Assert.That(Regex.Matches(Text, @"defaults:\n  run:\n    shell: bash"), Has.Count.EqualTo(1), "every step runs in a shell that stops at the first command that fails");
    }

    /// <summary>
    /// A simulation of the job, for a failure at every step: whichever step fails, the login and the push that
    /// come after it do not run. The steps that do run after a failure are the summaries, which publish nothing.
    /// </summary>
    [Test]
    public void WhicheverStepFails_NeitherTheLoginNorThePushRunsAfterIt()
    {
        string[] steps = Steps(Job("publish"));
        int login = IndexOfStep(steps, "the login", IsLogin);
        int push = IndexOfStep(steps, "the push", IsPush);

        for (int failing = 0; failing < steps.Length; failing++)
        {
            bool[] runs = RunsWhenStepFails(steps, failing);
            if (failing < login)
            {
                Assert.That(runs[login], Is.False, "the login does not run after a failure of step " + (failing + 1));
            }
            if (failing < push)
            {
                Assert.That(runs[push], Is.False, "the push does not run after a failure of step " + (failing + 1));
            }
            for (int index = failing + 1; index < steps.Length; index++)
            {
                if (runs[index])
                {
                    Assert.That(steps[index], Does.StartWith("name: Summary").Using(StringComparison.Ordinal), "only a summary runs after a failure");
                    Assert.That(Regex.Matches(steps[index], @"\bdotnet\b|\buses:|NUGET_API_KEY|GH_TOKEN|eng/").Select(match => match.Value), Is.Empty,
                        "and a summary runs no tool of the repository, no action and has neither the key nor the token");
                }
            }
        }
        // The same for the jobs: the job that publishes needs the preflight, and has no condition that lets it run without it.
        Assert.That(Job("publish"), Does.Match(@"(?m)^    needs: preflight$"));
        Assert.That(Job("publish"), Does.Not.Match(@"(?m)^    if:"));
    }

    [Test]
    public void ThePreflightJob_RunsEveryCheck_AndPublishesNothing()
    {
        string[] steps = Steps(Job("preflight"));
        int preflight = IndexOfStep(steps, "the preflight", IsPreflight);

        foreach (string option in IdentityOptions)
        {
            Assert.That(steps[preflight], Ordinal.Contains(option));
        }
        Assert.That(steps[preflight], Ordinal.DoesNotContain("--simulate"));
        Assert.That(steps[preflight], Ordinal.DoesNotContain("--bundle"));
        Assert.That(Job("preflight"), Ordinal.DoesNotContain("environment:"));
    }

    [Test]
    public void TheVerifyJob_ChecksWhatNuGetServes_ForALimitedTime_AndRepairsNothing()
    {
        string[] steps = Steps(Job("verify"));
        int verify = IndexOfStep(steps, "the verification", step => step.Contains("bash eng/publication-verify.sh", StringComparison.Ordinal));

        foreach (string option in IdentityOptions)
        {
            Assert.That(steps[verify], Ordinal.Contains(option));
        }
        int minutes = int.Parse(Regex.Match(steps[verify], @"--wait (?<minutes>\d+)").Groups["minutes"].Value, System.Globalization.CultureInfo.InvariantCulture);
        int limit = int.Parse(Regex.Match(Job("verify"), @"(?m)^    timeout-minutes: (?<minutes>\d+)$").Groups["minutes"].Value, System.Globalization.CultureInfo.InvariantCulture);
        Assert.That(minutes, Is.InRange(10, 120), "nuget.org is given time to index the version, and not without end");
        Assert.That(limit, Is.GreaterThan(minutes), "and the job outlasts the wait, so that the checks after it can run");
        Assert.That(steps[verify], Ordinal.Contains("--native-aot"));
        Assert.That(Job("verify"), Ordinal.DoesNotContain("environment:"));
        Assert.That(Regex.Matches(Job("verify"), @"\bdotnet\s+[a-z]|\buses: NuGet/|\bcurl\b[^\n]*\s-X").Select(match => match.Value), Is.Empty,
            "after the push nothing pushes again and nothing takes the version back: the job runs the one script, which reads");
    }

    /// <summary>
    /// The scripts of the publication read GitHub and nuget.org and compare. None of them can publish, start a
    /// workflow, or change anything on GitHub: every call they make to GitHub is a read.
    /// </summary>
    [TestCase("eng/publication.sh")]
    [TestCase("eng/publication-preflight.sh")]
    [TestCase("eng/publication-checksums.sh")]
    [TestCase("eng/publication-verify.sh")]
    [TestCase("eng/publication-preflight-selftest.sh")]
    public void TheScriptsOfThePublication_ReadAndCompare_AndCannotPublishOrStartAnything(string script)
    {
        string commands = string.Join("\n", Repository.Read(script).Split('\n').Where(line => !line.TrimStart().StartsWith('#')));

        Assert.That(Regex.Matches(commands,
                @"(?i)\bnuget\s+push\b|NuGet/login|/api/v2/token|id-token|ACTIONS_ID_TOKEN|\bgh\s+(workflow|run|release|secret|variable|pr|repo)\b|"
                + @"\bgh\s+api\b[^\n]*\s(-X|--method|-f|-F|--field|--raw-field|--input)\b|\bcurl\b[^\n]*\s(-X|--request|-d|--data\S*|-T|--upload-file)\b|\bgit\s+(-C\s+\S+\s+)?(push|commit|tag\b)")
            .Select(match => match.Value), Is.Empty);
    }

    /// <summary>
    /// The self-test runs the preflight against recorded answers in which one thing is wrong at a time, and the
    /// preflight must refuse each. It needs no network and publishes nothing. It runs here on Linux, where the
    /// publication runs; where bash or jq is missing it is left out.
    /// </summary>
    [Test]
    [Platform(Include = "Linux", Reason = "The publication runs on Linux, and so does its self-test.")]
    public void ThePreflight_RefusesEveryWrongCase()
    {
        string? bash = new[] { "/bin/bash", "/usr/bin/bash" }.FirstOrDefault(File.Exists);
        if (bash == null || !new[] { "/usr/bin/jq", "/bin/jq", "/usr/local/bin/jq" }.Any(File.Exists))
        {
            Assert.Ignore("bash and jq are needed to run the self-test of the preflight.");
        }

        ChildResult result = ChildProcess.RunCommand(bash!, Repository.PathOf("eng/publication-preflight-selftest.sh"));

        Assert.That(result.ExitCode, Is.Zero, result.StandardOutput + result.StandardError);
        Assert.That(result.StandardOutput, Does.Match(@"The preflight refuses each of (4[0-9]|[5-9][0-9]) wrong cases for its own reason\."));
        Assert.That(result.StandardOutput, Ordinal.Contains("ok: refused, a version that nuget.org already lists"));
        Assert.That(result.StandardOutput, Ordinal.Contains("ok: refused, a run started by a push"));
        Assert.That(result.StandardOutput, Ordinal.Contains("ok: refused, a release that can be changed"));
        Assert.That(result.StandardOutput, Ordinal.Contains("ok: refused, a simulation in a workflow"));
    }
}
