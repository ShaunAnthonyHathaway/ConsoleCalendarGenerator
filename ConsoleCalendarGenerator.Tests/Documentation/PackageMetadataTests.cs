using System.Text.RegularExpressions;
using ConsoleCalendarGenerator.Tests.Support;

namespace ConsoleCalendarGenerator.Tests.Documentation;

/// <summary>
/// What the package says about itself on nuget.org: its description, its tags and where its release notes are.
/// These are read from the project file. They describe what the package lets an application do, name the
/// original ConsoleCalendar API as part of it, and say nothing about the state of a release.
/// The version identity is checked in VersionIdentityTests, and the packed result by eng/verify-package.sh.
/// </summary>
[TestFixture]
public class PackageMetadataTests
{
    private const string RepositoryPage = "https://github.com/ShaunAnthonyHathaway/ConsoleCalendarGenerator";

    private static readonly string Project = Repository.Read("ConsoleCalendarGenerator/ConsoleCalendarGenerator.csproj");

    private static string Property(string name)
    {
        MatchCollection matches = Regex.Matches(Project, "<" + name + ">([^<]*)</" + name + ">");
        Assert.That(matches, Has.Count.EqualTo(1), name);
        return matches[0].Groups[1].Value;
    }

    [Test]
    public void TheDescription_SaysWhatThePackageDoes_AndNamesTheOriginalApi()
    {
        string description = Property("Description");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(description, Does.Match("(?i)month calendars"), "what the package is for");
            Assert.That(description, Does.Match("(?i)render"), "text rendering");
            Assert.That(description, Does.Match("(?i)structured"), "the structured result");
            Assert.That(description, Ordinal.Contains("ConsoleCalendar"), "the original API, by name");
            Assert.That(description, Does.Not.Match("(?i)year calendars"), "the structured API renders months");
            Assert.That(description, Does.Not.Match(@"(?i)published|released|preview|beta|\d+\.\d+"), "nothing about a version or the state of a release");
            Assert.That(description, Has.Length.InRange(80, 400), "one short paragraph");
            Assert.That(description, Does.Not.Contain("\n"));
        }
    }

    [Test]
    public void TheTags_AreSingleWords_AndStillFindThePackage()
    {
        string[] tags = Property("PackageTags").Split(';');

        Assert.That(tags, Is.Unique);
        Assert.That(tags, Is.All.Match("^[a-z]+$"));
        Assert.That(tags, Is.SupersetOf(new[] { "console", "calendar", "terminal", "cli", "date" }), "the tags the package had before");
    }

    /// <summary>
    /// The release notes are the changelog of this repository, on the default branch, for the release as well.
    /// The changelog is a document of current policy: it is given the date of the release when the release is
    /// published, which a copy at the tag could never show.
    /// </summary>
    [Test]
    public void TheReleaseNotes_AreTheChangelogOfThisRepository()
    {
        string releaseNotes = Property("PackageReleaseNotes");

        Assert.That(releaseNotes, Is.EqualTo(RepositoryPage + "/blob/master/CHANGELOG.md"));
        Assert.That(Repository.HasFile("CHANGELOG.md"));
        Assert.That(releaseNotes, Ordinal.DoesNotContain(DocumentationLinkTests.ReleaseTag));
    }

    [Test]
    public void TheIdentityOfThePackage_IsWhatItWas()
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(Property("PackageId"), Is.EqualTo("ConsoleCalendarGenerator"));
            Assert.That(Property("Authors"), Is.EqualTo("Shaun Hathaway"));
            Assert.That(Property("PackageLicenseExpression"), Is.EqualTo("MIT"));
            Assert.That(Property("PackageProjectUrl"), Is.EqualTo(RepositoryPage));
            Assert.That(Property("RepositoryUrl"), Is.EqualTo(RepositoryPage));
            Assert.That(Property("PackageReadmeFile"), Is.EqualTo("README.md"));
            Assert.That(Project, Does.Not.Match("<PackageReference|<ProjectReference"), "the package has no dependencies");
        }
    }
}
