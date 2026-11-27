using System;
using System.Collections.Generic;
using System.Linq;

namespace ConsoleCalendarGenerator;

/// <summary>
/// A month rendered as text: lines made of segments that say what each piece of text is.
/// </summary>
/// <remarks>
/// <para>
/// The result can be used in two ways. <see cref="ToText()"/> gives the plain text. <see cref="Lines"/> gives the
/// structure, for code that styles, marks or writes the month without parsing that text.
/// </para>
/// <para>
/// Instances are immutable, can be used from several threads at once, and are created only by
/// <see cref="MonthRenderer"/>.
/// </para>
/// </remarks>
public sealed class RenderedMonth
{
    private const string LineFeed = "\n";
    private const string CarriageReturnLineFeed = "\r\n";

    internal RenderedMonth(MonthLayout layout, IReadOnlyList<RenderedLine> lines)
    {
        Layout = layout;
        Lines = lines;
    }

    /// <summary>Gets the layout that was rendered: the year, the month, the first day of the week and the cells.</summary>
    public MonthLayout Layout { get; }

    /// <summary>
    /// Gets the lines from top to bottom: one <see cref="RenderedLineKind.Title"/> line, one
    /// <see cref="RenderedLineKind.WeekdayLabels"/> line, then one <see cref="RenderedLineKind.Week"/> line for
    /// each week of <see cref="Layout"/>.
    /// </summary>
    public IReadOnlyList<RenderedLine> Lines { get; }

    /// <summary>Returns the month as text with a line feed (<c>"\n"</c>) between lines, on every platform.</summary>
    /// <returns>The text of every line, joined by <c>"\n"</c>. There is no line terminator after the last line.</returns>
    public string ToText() => ToText(LineFeed);

    /// <summary>Returns the month as text with the given line terminator between lines.</summary>
    /// <param name="newLine">The text to put between lines: <c>"\n"</c> or <c>"\r\n"</c>.</param>
    /// <returns>The text of every line, joined by <paramref name="newLine"/>. There is no line terminator after the last line.</returns>
    /// <remarks>
    /// To join the lines with anything else, join the <see cref="RenderedLine.Text"/> of <see cref="Lines"/>.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="newLine"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="newLine"/> is neither <c>"\n"</c> nor <c>"\r\n"</c>.</exception>
    public string ToText(string newLine)
    {
        if (newLine == null)
        {
            throw new ArgumentNullException(nameof(newLine));
        }
        if (newLine != LineFeed && newLine != CarriageReturnLineFeed)
        {
            throw new ArgumentException("The line terminator must be \"\\n\" or \"\\r\\n\".", nameof(newLine));
        }
        return string.Join(newLine, Lines.Select(line => line.Text));
    }

    /// <summary>Returns the same text as <see cref="ToText()"/>.</summary>
    /// <returns>The text of every line, joined by <c>"\n"</c>.</returns>
    public override string ToString() => ToText();
}
