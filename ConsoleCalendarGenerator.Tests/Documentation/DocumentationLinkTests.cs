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
    /// <summary>The same address for the files as they are at the tag of the release.</summary>
    private const string RawFilesAtTheTag = "https://raw.githubusercontent.com/ShaunAnthonyHathaway/ConsoleCalendarGenerator/" + ReleaseTag + "/";

    /// <summary>
    /// Pages of the repository that are not files of it. The page of the release is where the changelog sends
    /// a reader for version 2.0.0; it exists from the moment the release is published on GitHub.
    /// </summary>
    private static readonly string[] RepositoryPages = { RepositoryPage, RepositoryPage + "/issues", RepositoryPage + "/releases/tag/" + ReleaseTag };

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
            // A link at the tag of the release. The tag is put on the commit that is the release, so what the
            // link names must be in this checkout; that is checked here, and the address has to have exactly one
            // of these three shapes. Whether the tag exists yet is not: until it does, a well-formed address
            // answers "not found", which is expected, and these tests never use the network.
            else if (target.StartsWith(RepositoryPage + "/blob/" + ReleaseTag + "/", StringComparison.Ordinal))
            {
                Assert.That(Repository.HasFile(target.Substring((RepositoryPage + "/blob/" + ReleaseTag + "/").Length)), what + " is not a file of the repository");
            }
            else if (target.StartsWith(RepositoryPage + "/tree/" + ReleaseTag + "/", StringComparison.Ordinal))
            {
                Assert.That(Repository.HasFolder(target.Substring((RepositoryPage + "/tree/" + ReleaseTag + "/").Length)), what + " is not a folder of the repository");
            }
            else if (target.StartsWith(RawFilesAtTheTag, StringComparison.Ordinal))
            {
                Assert.That(Repository.HasFile(target.Substring(RawFilesAtTheTag.Length)), what + " is not a file of the repository");
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
    /// any version should get. The list states every address as it is on the default branch; the repository
    /// now holds the release, so the README has the first group at the tag.
    /// </summary>
    internal static readonly (string Target, bool PointsAtTheTagOnceReleased)[] ReadmeLinksIntoTheRepository = ReadTheList();

    /// <summary>
    /// The list is a file, eng/release-links.txt, because a script reads it too: eng/release-links.sh makes the
    /// substitution when a release is prepared and rehearses it on every build. One list, read by both.
    /// </summary>
    internal const string ListOfLinks = "eng/release-links.txt";

    private static (string, bool)[] ReadTheList() => Repository.Read(ListOfLinks).Split('\n')
        .Where(line => line.Trim().Length > 0 && !line.StartsWith('#'))
        .Select(line => line.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        .Select(parts => parts.Length == 2 && parts[0] is "tag" or "master"
            ? (parts[1], parts[0] == "tag")
            : throw new FormatException(ListOfLinks + " has a line that is not 'tag <address>' or 'master <address>'."))
        .ToArray();

    /// <summary>The approved classification (D-68), so that an edit of the list is a visible decision.</summary>
    [Test]
    public void TheList_ClassifiesTheLinksAsApproved()
    {
        Assert.That(ReadmeLinksIntoTheRepository.Where(link => link.PointsAtTheTagOnceReleased).Select(link => link.Target), Is.EqualTo(new[]
        {
            RawFiles + "docs/images/month-with-theme.png",
            RepositoryPage + "/blob/master/global.json",
            RepositoryPage + "/tree/master/ConsoleCalendarGeneratorExample",
        }), "the image, the SDK file and the example program are part of a version");
        Assert.That(ReadmeLinksIntoTheRepository.Where(link => !link.PointsAtTheTagOnceReleased).Select(link => link.Target), Is.EqualTo(new[]
        {
            RepositoryPage + "/blob/master/CHANGELOG.md",
            RepositoryPage + "/blob/master/CONTRIBUTING.md",
            RepositoryPage + "/blob/master/SECURITY.md",
            RepositoryPage + "/blob/master/LICENSE",
            RepositoryPage + "/issues",
        }), "the documents of current policy stay on the default branch");
    }

    /// <summary>What a link that is part of the version becomes in the README of the released package.</summary>
    internal static string AtTheReleaseTag(string target) => target.Replace("/master/", "/" + ReleaseTag + "/", StringComparison.Ordinal);

    /// <summary>The address a listed link has in the README of this release.</summary>
    private static string InTheReadmeOfTheRelease((string Target, bool PointsAtTheTagOnceReleased) link) =>
        link.PointsAtTheTagOnceReleased ? AtTheReleaseTag(link.Target) : link.Target;

    /// <summary>
    /// A link added to the README has to be put in the list, which is where it is decided whether it belongs to
    /// the version or to current policy. A link that is in neither group fails here.
    /// </summary>
    [Test]
    public void EveryLinkOfTheReadmeIntoTheRepository_IsClassifiedForTheRelease()
    {
        IEnumerable<string> intoTheRepository = LinksOf("README.md")
            .Select(link => link.Target)
            .Where(target => target.StartsWith(RepositoryPage, StringComparison.Ordinal) || target.StartsWith("https://raw.githubusercontent.com/", StringComparison.Ordinal))
            .Distinct();

        Assert.That(intoTheRepository, Is.EquivalentTo(ReadmeLinksIntoTheRepository.Select(InTheReadmeOfTheRelease)));
    }

    /// <summary>
    /// The repository holds the release, so the substitution has been made: the three links that are part of
    /// the version point at the tag, exactly, and nothing else does. The links of current policy are on the
    /// default branch, in the README and in the other documents alike.
    /// </summary>
    [Test]
    public void TheLinksThatArePartOfTheVersion_PointAtTheTagOfTheRelease_AndNoOthersDo()
    {
        string project = Repository.Read("ConsoleCalendarGenerator/ConsoleCalendarGenerator.csproj");
        Assert.That(project, Does.Not.Contain("<VersionSuffix>"), "the repository holds a release");

        string[] inTheReadme = LinksOf("README.md").Select(link => link.Target).ToArray();
        foreach ((string target, bool moves) in ReadmeLinksIntoTheRepository)
        {
            if (moves)
            {
                Assert.That(target, Ordinal.Contains("/master/"), "the list states every address on the default branch");
                Assert.That(inTheReadme, Has.Member(AtTheReleaseTag(target)), target + " is not at the tag");
                Assert.That(inTheReadme, Has.No.Member(target), target + " is still on the default branch");
            }
            else
            {
                Assert.That(inTheReadme, Has.Member(target), target + " is not on the default branch");
            }
        }
        Assert.That(inTheReadme.Count(target => target.Contains("/" + ReleaseTag + "/", StringComparison.Ordinal)),
            Is.EqualTo(ReadmeLinksIntoTheRepository.Count(link => link.PointsAtTheTagOnceReleased)), "three links at the tag, and no more");

        // No other document, and not the package's own metadata, points at a file at the tag.
        foreach (string document in Documents.Where(document => document != "README.md"))
        {
            Assert.That(LinksOf(document).Select(link => link.Target).Where(target => Regex.IsMatch(target, "/(blob|tree)/" + Regex.Escape(ReleaseTag) + "/|" + Regex.Escape(RawFilesAtTheTag))),
                Is.Empty, document);
        }
        Assert.That(project, Ordinal.DoesNotContain(ReleaseTag));
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
            // The image is part of the version: it is served from the tag of the release, where it cannot change.
            Assert.That(image.Target, Does.StartWith(RawFilesAtTheTag + "docs/images/").Using(StringComparison.Ordinal));
            Assert.That(Repository.HasFile(image.Target.Substring(RawFilesAtTheTag.Length)), image.Target);
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
            .Select(link => link.Target.Substring(RawFilesAtTheTag.Length))
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
