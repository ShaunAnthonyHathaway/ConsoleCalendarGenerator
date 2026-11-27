using System.Reflection;
using System.Text.RegularExpressions;
using CalendarGenerator;
using ConsoleCalendarGenerator.Tests.Support;

namespace ConsoleCalendarGenerator.Tests;

/// <summary>
/// Which version the library says it is. The suite runs once against each build of the library (see
/// <see cref="LibraryBuildTests"/>), so each of these tests is a statement about both.
///
/// <para>
/// The repository holds the release 2.0.0: the version has no suffix. <see cref="ReleaseVersion"/> and
/// <see cref="AssemblyAndFileVersion"/> are changed here in the same change that changes them in the project
/// file. Every build of this source says it is 2.0.0; which one build is the release is not decided by the
/// source, and is recorded outside the repository.
/// </para>
/// </summary>
[TestFixture]
public class VersionIdentityTests
{
    private const string ReleaseVersion = "2.0.0";
    private const string AssemblyAndFileVersion = "2.0.0.0";
    private const string PublishedVersion = "1.0.0";

    private const string ProjectFile = "ConsoleCalendarGenerator/ConsoleCalendarGenerator.csproj";

    private static readonly Assembly Library = typeof(ConsoleCalendar).Assembly;

    private static string InformationalVersion =>
        Library.GetCustomAttribute<AssemblyInformationalVersionAttribute>()!.InformationalVersion;

    /// <summary>The package version the assembly was built as: its informational version without the commit.</summary>
    private static string PackageVersion => InformationalVersion.Split('+')[0];

    private static string Property(string name) =>
        Regex.Match(Repository.Read(ProjectFile), "<" + name + ">([^<]*)</" + name + ">").Groups[1].Value;

    // ---- the assembly ----

    [Test]
    public void TheAssemblyVersion_IsTheMajorVersionAlone()
    {
        Assert.That(Library.GetName().Version, Is.EqualTo(new Version(AssemblyAndFileVersion)));
        Assert.That(Library.GetName().Version, Is.EqualTo(new Version(2, 0, 0, 0)));
    }

    [Test]
    public void TheFileVersion_IsTheSame()
    {
        Assert.That(Library.GetCustomAttribute<AssemblyFileVersionAttribute>()!.Version, Is.EqualTo(AssemblyAndFileVersion));
    }

    /// <summary>
    /// The informational version is the release version, followed by the commit it was built from wherever
    /// the build could see one. That is every build from a Git checkout, which is what Source Link needs too.
    /// It has no suffix: nothing between the version and the commit.
    /// </summary>
    [Test]
    public void TheInformationalVersion_IsTheReleaseVersion_WithTheCommit()
    {
        Assert.That(PackageVersion, Is.EqualTo(ReleaseVersion));
        Assert.That(InformationalVersion, Does.Match("^" + Regex.Escape(ReleaseVersion) + @"(\+[0-9a-f]{40})?$"));
        Assert.That(InformationalVersion, Does.Not.Contain("-"), "no development or prerelease suffix");
    }

    /// <summary>
    /// The rule that does not depend on the version: the major version of the assembly is the major version of
    /// the package, and the rest of the assembly version is zero. A package of one major version with an
    /// assembly of another fails here, whichever of the two was forgotten.
    /// </summary>
    [Test]
    public void TheMajorVersionOfTheAssembly_IsTheMajorVersionOfThePackage()
    {
        Version assembly = Library.GetName().Version!;
        int packageMajor = int.Parse(PackageVersion.Split('.')[0], System.Globalization.CultureInfo.InvariantCulture);

        Assert.That(assembly.Major, Is.EqualTo(packageMajor));
        Assert.That((assembly.Minor, assembly.Build, assembly.Revision), Is.EqualTo((0, 0, 0)));
        Assert.That(new Version(Library.GetCustomAttribute<AssemblyFileVersionAttribute>()!.Version).Major, Is.EqualTo(packageMajor));
    }

    /// <summary>1.0.0 is published. Nothing built here may say it is that version, in any of its three version numbers.</summary>
    [Test]
    public void NoVersionOfTheAssembly_IsThePublishedOne()
    {
        Assert.That(PackageVersion, Is.Not.EqualTo(PublishedVersion));
        Assert.That(Library.GetName().Version, Is.Not.EqualTo(new Version(1, 0, 0, 0)));
        Assert.That(Library.GetCustomAttribute<AssemblyFileVersionAttribute>()!.Version, Does.Not.StartWith("1."));
    }

    // ---- the project file ----

    [Test]
    public void TheProjectFile_SetsTheVersionsTheAssemblyHas()
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(Property("VersionPrefix"), Is.EqualTo(ReleaseVersion));
            Assert.That(Repository.Read(ProjectFile), Does.Not.Match("<VersionSuffix>|<Version>|<PackageVersion>"), "no suffix, and the version is stated once");
            Assert.That(Property("AssemblyVersion"), Is.EqualTo(AssemblyAndFileVersion));
            Assert.That(Property("FileVersion"), Is.EqualTo("$(VersionPrefix).0"), "the file version follows the version prefix");
            Assert.That(Property("PackageId"), Is.EqualTo("ConsoleCalendarGenerator"));
        }
    }

    /// <summary>
    /// The public API of every package built here is compared with the published 1.0.0, parameter names
    /// included, and nothing is excused: there is no suppression file and no rule is turned off. Version 2.0
    /// changes behavior, never a signature, so it needs no exception.
    /// </summary>
    [Test]
    public void PackageValidation_ComparesWithThePublishedVersion_AndNothingIsSuppressed()
    {
        string project = Repository.Read(ProjectFile);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(Property("EnablePackageValidation"), Is.EqualTo("true"));
            Assert.That(Property("PackageValidationBaselineVersion"), Is.EqualTo(PublishedVersion));
            Assert.That(Property("ApiCompatEnableRuleCannotChangeParameterName"), Is.EqualTo("true"));
            Assert.That(project, Does.Not.Match("ApiCompatSuppress|ApiCompatGenerateSuppressionFile|ApiCompatExclude|NoWarn|PackageValidationBaselinePath"));
            Assert.That(Repository.FilesWithExtension(".xml").Where(file => file.Contains("Suppression", StringComparison.OrdinalIgnoreCase)), Is.Empty);
        }
    }

    // ---- the maintenance notes ----

    private static IEnumerable<string> MaintenanceNotes() =>
        Repository.FilesWithExtension(".md")
            .Where(file => file == "CLAUDE.md" || file.StartsWith("docs/ai-context/", StringComparison.Ordinal))
            .Where(file => file != "docs/ai-context/decisions.md");

    /// <summary>
    /// The repository no longer builds a development version, so no note says it does. Two places keep the
    /// earlier numbers on purpose, as history: the log of decisions, and the table of completed phases in the
    /// roadmap, which say what each phase did when it was done.
    /// </summary>
    [TestCaseSource(nameof(MaintenanceNotes))]
    public void NoMaintenanceNote_StillNamesADevelopmentVersion(string note)
    {
        string text = Repository.Read(note);
        if (note == "docs/ai-context/roadmap.md")
        {
            int completed = text.IndexOf("\n## Completed\n", StringComparison.Ordinal);
            int next = text.IndexOf("\n## ", completed + 1, StringComparison.Ordinal);
            Assert.That(completed, Is.GreaterThan(0).And.LessThan(next));
            text = text.Remove(completed, next - completed);
        }

        Assert.That(Regex.Matches(text, @"\b\d+\.\d+\.\d+-dev\b").Select(match => match.Value).Distinct(), Is.Empty);
    }

    [Test]
    public void TheNotesThatDescribeTheBuild_NameTheReleaseVersion_AndTheAssemblyVersion()
    {
        foreach (string note in new[] { "CLAUDE.md", "docs/ai-context/packaging-and-releases.md", "docs/ai-context/compatibility.md" })
        {
            Assert.That(Repository.Read(note), Ordinal.Contains("`" + ReleaseVersion + "`"), note);
            Assert.That(Repository.Read(note), Ordinal.Contains("`" + AssemblyAndFileVersion + "`"), note);
        }
    }
}
