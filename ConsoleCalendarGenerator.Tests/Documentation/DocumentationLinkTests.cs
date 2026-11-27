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
