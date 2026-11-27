using System;
using System.IO;

namespace ConsoleCalendarGenerator;

/// <summary>Writes a rendered month as plain text to a <see cref="TextWriter"/>.</summary>
/// <remarks>
/// This class never uses the console and never changes a color. To write to the console with colors, use
/// <see cref="ConsoleMonthWriter"/>.
/// </remarks>
public static class MonthWriter
{
    /// <summary>Writes every line of a rendered month to a writer, each followed by the writer's line terminator.</summary>
    /// <param name="month">The rendered month.</param>
    /// <param name="writer">
    /// The writer to write to. Its <see cref="TextWriter.NewLine"/> ends every line, the last one included. The
    /// writer is not flushed, closed, disposed or changed.
    /// </param>
    /// <remarks>
    /// <para>
    /// The text written is exactly the <see cref="RenderedLine.Text"/> of each line. Unlike
    /// <see cref="RenderedMonth.ToText()"/>, which puts a terminator only between lines, this ends the last line
    /// as well, as writing lines to a stream or a console normally does.
    /// </para>
    /// <para>
    /// If the writer throws, the exception is passed on and the lines written before it stay written.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="month"/> or <paramref name="writer"/> is <see langword="null"/>.
    /// </exception>
    public static void Write(RenderedMonth month, TextWriter writer)
    {
        if (month == null)
        {
            throw new ArgumentNullException(nameof(month));
        }
        if (writer == null)
        {
            throw new ArgumentNullException(nameof(writer));
        }
        foreach (RenderedLine line in month.Lines)
        {
            writer.WriteLine(line.Text);
        }
    }
}
