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
/// The repository builds a development version. <see cref="DevelopmentVersion"/> and
/// <see cref="AssemblyAndFileVersion"/> are changed here in the same change that changes them in the project
/// file, which for the final version is the change that prepares the release.
/// </para>
/// </summary>
[TestFixture]
public class VersionIdentityTests
{
    private const string DevelopmentVersion = "2.0.0-dev";
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
    /// The informational version is the development version, followed by the commit it was built from wherever
    /// the build could see one. That is every build from a Git checkout, which is what Source Link needs too.
    /// </summary>
    [Test]
    public void TheInformationalVersion_IsTheDevelopmentVersion_WithTheCommit()
    {
        Assert.That(PackageVersion, Is.EqualTo(DevelopmentVersion));
        Assert.That(InformationalVersion, Does.Match("^" + Regex.Escape(DevelopmentVersion) + @"(\+[0-9a-f]{40})?$"));
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
            Assert.That(Property("VersionPrefix") + "-" + Property("VersionSuffix"), Is.EqualTo(DevelopmentVersion));
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
    /// A note that names a development version names the one the repository builds. decisions.md is left out:
    /// it keeps superseded decisions as they were written.
    /// </summary>
    [TestCaseSource(nameof(MaintenanceNotes))]
    public void EveryDevelopmentVersionAMaintenanceNoteNames_IsTheOneTheRepositoryBuilds(string note)
    {
        Assert.That(Regex.Matches(Repository.Read(note), @"\b\d+\.\d+\.\d+-dev\b").Select(match => match.Value).Distinct(),
            Is.SubsetOf(new[] { DevelopmentVersion }));
    }

    [Test]
    public void TheNotesThatDescribeTheBuild_NameTheDevelopmentVersion_AndTheAssemblyVersion()
    {
        foreach (string note in new[] { "CLAUDE.md", "docs/ai-context/packaging-and-releases.md", "docs/ai-context/compatibility.md" })
        {
            Assert.That(Repository.Read(note), Ordinal.Contains("`" + DevelopmentVersion + "`"), note);
            Assert.That(Repository.Read(note), Ordinal.Contains("`" + AssemblyAndFileVersion + "`"), note);
        }
    }
}
