using System.Globalization;

namespace ConsoleCalendarGenerator;

/// <summary>
/// Measures and cuts text in text elements, the runtime's unit for one user-perceived character
/// (<see cref="StringInfo"/>), so that a surrogate pair or a letter with its combining marks is never split.
/// A text element is not a promise about how many columns a terminal uses to show it.
/// </summary>
internal static class TextElements
{
    private const char NextLine = (char)0x0085;
    private const char LineSeparator = (char)0x2028;
    private const char ParagraphSeparator = (char)0x2029;

    public static int Count(string text) => new StringInfo(text).LengthInTextElements;

    /// <summary>
    /// Returns the text right-aligned in <paramref name="width"/> text elements: padded with spaces on the left
    /// when it is shorter, cut to its first <paramref name="width"/> text elements when it is longer.
    /// </summary>
    public static string FitRight(string text, int width)
    {
        var info = new StringInfo(text);
        int count = info.LengthInTextElements;
        if (count > width)
        {
            return info.SubstringByTextElements(0, width);
        }
        return count < width ? new string(' ', width - count) + text : text;
    }

    /// <summary>
    /// Whether the text contains a character that ends a line: CR, LF, form feed, next line (U+0085), line
    /// separator (U+2028) or paragraph separator (U+2029). These are the characters .NET treats as newlines.
    /// </summary>
    public static bool ContainsLineBreak(string text)
    {
        foreach (char character in text)
        {
            switch (character)
            {
                case '\r':
                case '\n':
                case '\f':
                case NextLine:
                case LineSeparator:
                case ParagraphSeparator:
                    return true;
            }
        }
        return false;
    }
}
