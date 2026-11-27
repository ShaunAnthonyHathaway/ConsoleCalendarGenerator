using System.Text.RegularExpressions;

namespace ConsoleCalendarGenerator.Tests.Support;

/// <summary>A fenced code block: the word after the opening fence and the lines between the fences.</summary>
internal sealed record FencedBlock(string Language, string Text);

/// <summary>A link or an image: its text (the alternative text of an image) and where it points.</summary>
internal sealed record MarkdownLink(string Label, string Target, bool IsImage);

/// <summary>
/// Reads the few Markdown constructs the documentation tests need: fenced code blocks, links, images and
/// headings. It understands only the plain forms this repository's documents use, which the tests also require.
/// </summary>
internal static class Markdown
{
    private const string Fence = "```";
    private static readonly Regex InlineCode = new("`[^`\n]*`");
    private static readonly Regex InlineLink = new(@"(!?)\[([^\]]*)\]\(([^)\s]*)\)");
    private static readonly Regex LinkDefinition = new(@"^\[([^\]]+)\]:[ \t]*(\S+)[ \t]*$", RegexOptions.Multiline);
    private static readonly Regex Heading = new(@"^#{1,6}[ \t]+(.+?)[ \t]*$", RegexOptions.Multiline);

    public static IReadOnlyList<FencedBlock> FencedBlocks(string markdown)
    {
        var blocks = new List<FencedBlock>();
        string? language = null;
        var lines = new List<string>();
        foreach (string line in markdown.Split('\n'))
        {
            if (language == null)
            {
                if (line.StartsWith(Fence, StringComparison.Ordinal))
                {
                    language = line.Substring(Fence.Length).Trim();
                    lines.Clear();
                }
            }
            else if (line == Fence)
            {
                blocks.Add(new FencedBlock(language, string.Join("\n", lines)));
                language = null;
            }
            else
            {
                lines.Add(line);
            }
        }
        if (language != null)
        {
            throw new FormatException("A fenced code block is not closed.");
        }
        return blocks;
    }

    /// <summary>The document without its fenced blocks and without its inline code: the prose and the links.</summary>
    public static string WithoutCode(string markdown)
    {
        var kept = new List<string>();
        bool inBlock = false;
        foreach (string line in markdown.Split('\n'))
        {
            if (!inBlock && line.StartsWith(Fence, StringComparison.Ordinal))
            {
                inBlock = true;
            }
            else if (inBlock)
            {
                inBlock = line != Fence;
            }
            else
            {
                kept.Add(line);
            }
        }
        return InlineCode.Replace(string.Join("\n", kept), string.Empty);
    }

    /// <summary>Every link and image outside code, including the targets of reference-style definitions.</summary>
    public static IReadOnlyList<MarkdownLink> Links(string markdown)
    {
        string prose = WithoutCode(markdown);
        var links = new List<MarkdownLink>();
        foreach (Match match in InlineLink.Matches(prose))
        {
            links.Add(new MarkdownLink(match.Groups[2].Value, match.Groups[3].Value, match.Groups[1].Value == "!"));
        }
        foreach (Match match in LinkDefinition.Matches(prose))
        {
            links.Add(new MarkdownLink(match.Groups[1].Value, match.Groups[2].Value, IsImage: false));
        }
        return links;
    }

    /// <summary>The prose with every link, image and link definition taken out, to look for addresses left bare.</summary>
    public static string WithoutLinks(string markdown) =>
        LinkDefinition.Replace(InlineLink.Replace(WithoutCode(markdown), string.Empty), string.Empty);

    /// <summary>The anchor GitHub gives each heading: lower case, punctuation removed, spaces as hyphens.</summary>
    public static IReadOnlyList<string> HeadingAnchors(string markdown)
    {
        var anchors = new List<string>();
        foreach (Match match in Heading.Matches(WithoutCode(markdown)))
        {
            string text = match.Groups[1].Value.ToLowerInvariant();
            anchors.Add(new string(text.Where(c => char.IsLetterOrDigit(c) || c == ' ' || c == '-' || c == '_').ToArray()).Replace(' ', '-'));
        }
        return anchors;
    }
}
