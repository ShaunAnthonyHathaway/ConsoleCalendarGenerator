using System.Text.RegularExpressions;
using ConsoleCalendarGenerator.Tests.Support;

namespace ConsoleCalendarGenerator.Tests.Infrastructure;

/// <summary>
/// The scripts that put a release together and check it, read as text, and the state of the repository around
/// them. The scripts themselves run on every build, in the workflow. What is checked here is what must stay
/// true about them: no script can publish, and of the two workflows only the one made for it can; no file of a
/// release is kept in the repository; and the two reports the upgrade test compares differ in the five
/// corrections and in nothing else.
/// </summary>
[TestFixture]
public class ReleaseToolingTests
{
    /// <summary>The one file of the repository that can publish. <see cref="PublishWorkflowTests"/> holds it to its rules.</summary>
    private const string PublicationWorkflow = ".github/workflows/publish-nuget.yml";

    private static readonly string[] ScriptsAndWorkflows = Repository.FilesWithExtension(".sh", ".yml", ".yaml", ".ps1", ".cmd", ".bat")
        .Where(file => file.StartsWith("eng/", StringComparison.Ordinal) || file.StartsWith(".github/", StringComparison.Ordinal))
        .ToArray();

    private static readonly string[] EverythingButThePublicationWorkflow = ScriptsAndWorkflows.Where(file => file != PublicationWorkflow).ToArray();

    [Test]
    public void TheScriptsOfARelease_AreThere()
    {
        Assert.That(ScriptsAndWorkflows, Is.SupersetOf(new[]
        {
            "eng/common.sh", "eng/release-bundle.sh", "eng/verify-release-bundle.sh", "eng/release-bundle-tamper-test.sh", "eng/direct-reference-smoke.sh",
            "eng/upgrade-smoke.sh", "eng/release-links.sh", "eng/release-state.sh", "eng/verify-package.sh", "eng/consumer-smoke.sh",
            "eng/verify-published-package.sh", "eng/verify-published-package-selftest.sh", "eng/designate-candidate.sh", "eng/release-notes.sh",
            "eng/publication.sh", "eng/publication-preflight.sh", "eng/publication-checksums.sh", "eng/publication-verify.sh",
            "eng/publication-preflight-selftest.sh", PublicationWorkflow,
        }));
    }

    /// <summary>
    /// No script, and no workflow but the one made for it, pushes a package, logs in to nuget.org, creates or
    /// uploads to a release, creates or pushes a tag, pushes commits, creates an attestation, or starts a
    /// workflow. The publication workflow is the one exception: it holds the one command that publishes, runs
    /// only when it is started by hand, and starting it is a person's explicit decision, every time.
    /// </summary>
    [TestCaseSource(nameof(EverythingButThePublicationWorkflow))]
    public void NothingButThePublicationWorkflowCanPublish(string file)
    {
        string commands = string.Join("\n", Repository.Read(file).Split('\n').Where(line => !line.TrimStart().StartsWith('#')));

        Assert.That(Regex.Matches(commands,
                @"\bnuget(\.exe)?\s+push\b|\bdotnet\s+nuget\s+push\b|\bgh\s+release\s+(create|upload|edit|delete)\b|\bgh\s+attestation\b|actions/attest|"
                + @"\bgit\s+(-C\s+\S+\s+)?(push|commit|tag\s+(?!--list))|softprops/|NuGet/login|nuget/setup-nuget|\bcurl\b[^\n]*\s-X\s*(POST|PUT|DELETE)|"
                + @"\bgh\s+workflow\s+run\b|\bgh\s+run\s+rerun\b|\bgh\s+api\b[^\n]*\s(-X|--method)\b|workflow_dispatch:|id-token",
                RegexOptions.IgnoreCase).Select(match => match.Value), Is.Empty);
    }

    /// <summary>
    /// The exception is exactly one file, and it is there: were the publication workflow renamed, the test above
    /// would start to refuse it, and the policy on nuget.org, which names the file, would no longer match.
    /// </summary>
    [Test]
    public void ThePublicationWorkflow_IsTheOneFileThatCanPublish()
    {
        Assert.That(ScriptsAndWorkflows, Has.Member(PublicationWorkflow));
        Assert.That(ScriptsAndWorkflows.Where(file => Regex.IsMatch(Repository.Read(file), @"(?m)^[^#\n]*\bdotnet\s+nuget\s+push\b")), Is.EqualTo(new[] { PublicationWorkflow }));
    }

    /// <summary>
    /// A package, an archive or a file of checksums in the repository would be a release file outside a
    /// release. They are made in ignored folders and in temporary ones, and never committed.
    /// </summary>
    [Test]
    public void NoFileOfARelease_IsKeptInTheRepository()
    {
        Assert.That(Repository.FilesWithExtension(".nupkg", ".snupkg", ".zip", ".dll", ".pdb", ".tgz", ".gz"), Is.Empty);
        Assert.That(Repository.FilesWithExtension(".txt").Where(file => file.EndsWith("SHA256SUMS.txt", StringComparison.OrdinalIgnoreCase)), Is.Empty);
    }

    /// <summary>
    /// The upgrade test compares what a program reports with 1.0.0 and with 2.0. The two expected reports state
    /// the migration exactly: everything under "[unchanged]" is the same in both, each of the five numbered
    /// lines under "[corrected in 2.0]" is different, and only the report of 2.0 has the modern API.
    /// </summary>
    [Test]
    public void TheTwoReportsOfTheUpgrade_DifferInTheFiveCorrections_AndInTheModernApi()
    {
        string before = Repository.Read("eng/consumer-smoke/MigrationProbe/expected-1.0.0.txt");
        string after = Repository.Read("eng/consumer-smoke/MigrationProbe/expected-2.0.txt");

        Assert.That(Section(before, "[unchanged]"), Is.Not.Empty);
        Assert.That(Section(after, "[unchanged]"), Is.EqualTo(Section(before, "[unchanged]")));
        for (int number = 1; number <= 5; number++)
        {
            string was = Section(before, "[corrected in 2.0]").Single(line => line.StartsWith(number + ". ", StringComparison.Ordinal));
            string now = Section(after, "[corrected in 2.0]").Single(line => line.StartsWith(number + ". ", StringComparison.Ordinal));
            Assert.That(now, Is.Not.EqualTo(was), "correction " + number);
        }
        Assert.That(Section(after, "[corrected in 2.0]").Count(line => Regex.IsMatch(line, @"^\d\. ")), Is.EqualTo(5));
        Assert.That(before, Ordinal.DoesNotContain("[new in 2.0]"));
        Assert.That(Section(after, "[new in 2.0]"), Is.Not.Empty);
        Assert.That(before.Split('\n')[0], Is.EqualTo("library: {LIBRARY}"), "the first line is filled in by the scripts");
        Assert.That(after.Split('\n')[0], Is.EqualTo("library: {LIBRARY}"));
    }

    /// <summary>The lines after a heading, up to the next empty line.</summary>
    private static string[] Section(string report, string heading) =>
        report.Split('\n').SkipWhile(line => line != heading).Skip(1).TakeWhile(line => line.Length > 0).ToArray();

    /// <summary>
    /// The program of the upgrade test starts from the published version, by its number, so that the upgrade
    /// is a real one; and the program that uses an assembly directly has no package reference at all.
    /// </summary>
    [Test]
    public void TheProgramOfTheUpgrade_StartsFromThePublishedVersion_AndTheDirectOneHasNoPackage()
    {
        string package = Repository.Read("eng/consumer-smoke/MigrationProbe/PackageConsumer.csproj");
        string direct = Repository.Read("eng/consumer-smoke/MigrationProbe/DirectConsumer.csproj");

        Assert.That(package, Ordinal.Contains("<PackageReference Include=\"ConsoleCalendarGenerator\" Version=\"1.0.0\" />"));
        Assert.That(direct, Ordinal.DoesNotContain("<PackageReference"));
        Assert.That(direct, Ordinal.Contains("<HintPath>$(LibraryAssemblyPath)</HintPath>"));
    }

    /// <summary>The names of the four files of a release are stated once, where every script takes them from.</summary>
    [Test]
    public void TheFourFilesOfARelease_AreNamedInOnePlace()
    {
        string common = Repository.Read("eng/common.sh");

        Assert.That(common, Ordinal.Contains("'ConsoleCalendarGenerator.%s.nupkg\\n'"));
        Assert.That(common, Ordinal.Contains("'ConsoleCalendarGenerator.%s.snupkg\\n'"));
        Assert.That(common, Ordinal.Contains("'ConsoleCalendarGenerator-%s-binaries.zip\\n'"));
        Assert.That(common, Ordinal.Contains("checksums_name=\"SHA256SUMS.txt\""));
    }
}
