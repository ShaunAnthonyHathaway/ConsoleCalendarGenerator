using System.Text.RegularExpressions;
using ConsoleCalendarGenerator.Tests.Support;

namespace ConsoleCalendarGenerator.Tests.Documentation;

/// <summary>
/// The links and images of the public documents point at things that exist. Nothing here uses the network: a
/// link into this repository is checked against the files of the checkout, and other addresses are only
/// required to be https.
///
/// README.md is also the readme of the NuGet package, and nuget.org is stricter than GitHub: it shows no raw
/// HTML, it empties links that are not absolute (links to a heading of the page are kept), and it shows images
/// only from a short list of hosts, of which raw.githubusercontent.com is one.
/// </summary>
[TestFixture]
public class DocumentationLinkTests
{
    private const string RepositoryPage = "https://github.com/ShaunAnthonyHathaway/ConsoleCalendarGenerator";
    private const string RawFiles = "https://raw.githubusercontent.com/ShaunAnthonyHathaway/ConsoleCalendarGenerator/master/";

    /// <summary>Pages of the repository on GitHub that are not files.</summary>
    private static readonly string[] RepositoryPages = { RepositoryPage, RepositoryPage + "/issues", RepositoryPage + "/commits/master" };

    private static readonly string[] Documents = Repository.PublicDocuments;

    private static IEnumerable<MarkdownLink> LinksOf(string document) => Markdown.Links(Repository.Read(document));

    [TestCaseSource(nameof(Documents))]
    public void EveryLink_PointsAtSomethingThatExists(string document)
    {
        IReadOnlyList<string> anchors = Markdown.HeadingAnchors(Repository.Read(document));
        string folder = document.Contains('/') ? document.Substring(0, document.LastIndexOf('/') + 1) : string.Empty;

        foreach (MarkdownLink link in LinksOf(document))
        {
            string target = link.Target;
            string what = document + ": " + target;
            if (target.StartsWith('#'))
            {
                Assert.That(anchors, Has.Member(target.Substring(1)), what + " is not a heading of the document");
            }
            else if (target.StartsWith(RepositoryPage + "/blob/master/", StringComparison.Ordinal))
            {
                Assert.That(Repository.HasFile(target.Substring((RepositoryPage + "/blob/master/").Length)), what + " is not a file of the repository");
            }
            else if (target.StartsWith(RepositoryPage + "/tree/master/", StringComparison.Ordinal))
            {
                Assert.That(Repository.HasFolder(target.Substring((RepositoryPage + "/tree/master/").Length)), what + " is not a folder of the repository");
            }
            else if (target.StartsWith(RawFiles, StringComparison.Ordinal))
            {
                Assert.That(Repository.HasFile(target.Substring(RawFiles.Length)), what + " is not a file of the repository");
            }
            else if (target.StartsWith(RepositoryPage, StringComparison.Ordinal))
            {
                Assert.That(RepositoryPages, Has.Member(target), what + " is not a known page of the repository");
            }
            else if (target.Contains("://", StringComparison.Ordinal))
            {
                Assert.That(target, Does.StartWith("https://").Using(StringComparison.Ordinal), what + " is not https");
            }
            else
            {
                Assert.That(Repository.HasFile(folder + target) || Repository.HasFolder(folder + target), what + " is not a file or a folder of the repository");
            }
        }
    }

    [TestCaseSource(nameof(Documents))]
    public void NoAddressIsLeftBare(string document)
    {
        string remaining = Markdown.WithoutLinks(Repository.Read(document));

        Assert.That(Regex.Matches(remaining, @"\b(https?://|www\.)\S*").Select(match => match.Value), Is.Empty);
    }

    [TestCaseSource(nameof(Documents))]
    public void EveryLinkHasText(string document)
    {
        Assert.That(LinksOf(document).Where(link => string.IsNullOrWhiteSpace(link.Label)).Select(link => link.Target), Is.Empty);
    }

    [Test]
    public void TheReadme_HasLinksToTheOtherPublicDocuments()
    {
        string[] targets = LinksOf("README.md").Select(link => link.Target).ToArray();

        foreach (string document in new[] { "CHANGELOG.md", "CONTRIBUTING.md", "SECURITY.md", "LICENSE" })
        {
            Assert.That(targets, Has.Member(RepositoryPage + "/blob/master/" + document));
        }
    }

    // ---- the links that change when a version is released ----

    /// <summary>The tag a released 2.0 package will be built from. It does not exist yet.</summary>
    internal const string ReleaseTag = "v2.0.0";

    /// <summary>
    /// Every link of the README that points into this repository, and what becomes of it when version 2.0 is
    /// released (D-68). The README is packed into the package and shown on nuget.org for as long as that
    /// version exists, so a link whose target is part of the version, the image, a file, a folder of code, must
    /// then point at the tag of the release, where it cannot change. A link to a document that states the
    /// project's current policy stays on the default branch, because the current policy is what a reader of
    /// any version should get. Until the tag exists, all of them point at the default branch.
    /// </summary>
    internal static readonly (string Target, bool PointsAtTheTagOnceReleased)[] ReadmeLinksIntoTheRepository =
    {
        (RawFiles + "docs/images/month-with-theme.png", true),
        (RepositoryPage + "/blob/master/global.json", true),
        (RepositoryPage + "/tree/master/ConsoleCalendarGeneratorExample", true),
        (RepositoryPage + "/blob/master/CHANGELOG.md", false),
        (RepositoryPage + "/blob/master/CONTRIBUTING.md", false),
        (RepositoryPage + "/blob/master/SECURITY.md", false),
        (RepositoryPage + "/blob/master/LICENSE", false),
        (RepositoryPage + "/issues", false),
    };

    /// <summary>What a link that is part of the version becomes in the README of the released package.</summary>
    internal static string AtTheReleaseTag(string target) => target.Replace("/master/", "/" + ReleaseTag + "/", StringComparison.Ordinal);

    /// <summary>
    /// A link added to the README has to be put in the list above, which is where it is decided whether the
    /// release moves it to the tag. A link that is in neither group fails here.
    /// </summary>
    [Test]
    public void EveryLinkOfTheReadmeIntoTheRepository_IsClassifiedForTheRelease()
    {
        IEnumerable<string> intoTheRepository = LinksOf("README.md")
            .Select(link => link.Target)
            .Where(target => target.StartsWith(RepositoryPage, StringComparison.Ordinal) || target.StartsWith("https://raw.githubusercontent.com/", StringComparison.Ordinal))
            .Distinct();

        Assert.That(intoTheRepository, Is.EquivalentTo(ReadmeLinksIntoTheRepository.Select(link => link.Target)));
    }

    /// <summary>
    /// The tag does not exist while the repository builds a development version, so nothing may point at it
    /// yet: such a link would lead nowhere. When the version suffix is removed to prepare the release, this
    /// test fails, and the links marked above are the ones to change in that same change.
    /// </summary>
    [Test]
    public void WhileTheVersionIsADevelopmentVersion_NoLinkPointsAtTheReleaseTag()
    {
        string project = Repository.Read("ConsoleCalendarGenerator/ConsoleCalendarGenerator.csproj");
        Assert.That(project, Ordinal.Contains("<VersionSuffix>dev</VersionSuffix>"),
            "The version is no longer a development version. Point the links marked in ReadmeLinksIntoTheRepository at the release tag, and replace this test with one that requires it.");

        foreach (string document in Documents)
        {
            IEnumerable<string> atTheTag = LinksOf(document)
                .Select(link => link.Target)
                .Where(target => target.StartsWith(RepositoryPage, StringComparison.Ordinal) || target.StartsWith("https://raw.githubusercontent.com/", StringComparison.Ordinal))
                .Where(target => target.Contains("/" + ReleaseTag, StringComparison.Ordinal));
            Assert.That(atTheTag, Is.Empty, document);
        }
        Assert.That(project, Ordinal.DoesNotContain(ReleaseTag));
        foreach ((string target, bool _) in ReadmeLinksIntoTheRepository.Where(link => link.PointsAtTheTagOnceReleased))
        {
            Assert.That(target, Ordinal.Contains("/master/"), target);
            Assert.That(AtTheReleaseTag(target), Is.Not.EqualTo(target).And.Contains("/" + ReleaseTag + "/"), "the tag form is known");
        }
    }

    /// <summary>
    /// The checklist for preparing the release, in the maintenance notes, names every link that has to change
    /// and what it changes to, so that the list a person follows and the list this test knows are the same.
    /// </summary>
    [Test]
    public void TheReleaseChecklist_NamesEveryLinkThatMovesToTheTag_AndWhatItBecomes()
    {
        string notes = Repository.Read("docs/ai-context/packaging-and-releases.md");

        foreach ((string target, bool moves) in ReadmeLinksIntoTheRepository)
        {
            Assert.That(notes, Ordinal.Contains(target), "the checklist does not name " + target);
            if (moves)
            {
                Assert.That(notes, Ordinal.Contains(AtTheReleaseTag(target)), "the checklist does not say what " + target + " becomes");
            }
            else if (AtTheReleaseTag(target) != target)
            {
                Assert.That(notes, Ordinal.DoesNotContain(AtTheReleaseTag(target)), target + " stays on the default branch");
            }
        }
    }

    // ---- what nuget.org needs of the package readme ----

    [Test]
    public void TheReadme_HasOnlyAbsoluteLinks_AndLinksToItsOwnHeadings()
    {
        IEnumerable<string> others = LinksOf("README.md")
            .Select(link => link.Target)
            .Where(target => !target.StartsWith("https://", StringComparison.Ordinal) && !target.StartsWith('#'));

        Assert.That(others, Is.Empty, "nuget.org empties a relative link");
    }

    [Test]
    public void TheReadme_HasNoRawHtml()
    {
        string prose = Markdown.WithoutCode(Repository.Read("README.md"));

        Assert.That(Regex.Matches(prose, @"</?[A-Za-z][^>\n]*>|<!--").Select(match => match.Value), Is.Empty, "nuget.org does not render HTML");
    }

    [Test]
    public void EveryImageOfTheReadme_IsAFileOfThisRepository_ServedFromAHostNuGetShows()
    {
        MarkdownLink[] images = LinksOf("README.md").Where(link => link.IsImage).ToArray();

        Assert.That(images, Is.Not.Empty);
        foreach (MarkdownLink image in images)
        {
            Assert.That(image.Target, Does.StartWith(RawFiles + "docs/images/").Using(StringComparison.Ordinal));
            Assert.That(Repository.HasFile(image.Target.Substring(RawFiles.Length)), image.Target);
        }
    }

    [Test]
    public void EveryImageOfTheReadme_HasAlternativeTextThatDescribesIt()
    {
        foreach (MarkdownLink image in LinksOf("README.md").Where(link => link.IsImage))
        {
            Assert.That(image.Label.Split(' ', StringSplitOptions.RemoveEmptyEntries), Has.Length.GreaterThanOrEqualTo(8), image.Target);
            Assert.That(image.Label, Ordinal.DoesNotContain(".png"));
        }
    }

    [Test]
    public void OnlyTheReadmeHasImages()
    {
        foreach (string document in Documents.Where(document => document != "README.md"))
        {
            Assert.That(LinksOf(document).Where(link => link.IsImage), Is.Empty, document);
        }
    }

    /// <summary>No image is kept that nothing shows, and the screenshots of version 1.0.0 are gone.</summary>
    [Test]
    public void TheImagesOfTheRepository_AreExactlyTheOnesTheReadmeShows()
    {
        IEnumerable<string> shown = LinksOf("README.md")
            .Where(link => link.IsImage)
            .Select(link => link.Target.Substring(RawFiles.Length))
            .OrderBy(path => path, StringComparer.Ordinal);

        Assert.That(Repository.FilesWithExtension(".png", ".jpg", ".jpeg", ".gif", ".svg", ".webp", ".bmp", ".ico"), Is.EqualTo(shown));
    }

    [Test]
    public void TheImages_AreOfAReasonableSize()
    {
        foreach (string image in Repository.FilesWithExtension(".png"))
        {
            byte[] bytes = File.ReadAllBytes(Repository.PathOf(image));
            // A PNG file begins with an eight-byte signature and the IHDR chunk, which holds the width and the
            // height as big-endian numbers at offsets 16 and 20.
            int width = (bytes[16] << 24) | (bytes[17] << 16) | (bytes[18] << 8) | bytes[19];
            int height = (bytes[20] << 24) | (bytes[21] << 16) | (bytes[22] << 8) | bytes[23];

            Assert.That(bytes.Length, Is.LessThan(200_000), image);
            Assert.That(width, Is.InRange(300, 900), image + " width");
            Assert.That(height, Is.InRange(150, 900), image + " height");
        }
    }
}
