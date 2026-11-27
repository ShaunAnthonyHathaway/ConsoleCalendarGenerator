using System;
using System.IO;

namespace ConsoleCalendarGenerator;

/// <summary>Writes a rendered month to the console, in the colors of a theme.</summary>
/// <remarks>
/// <para>
/// This is the only part of the modern API that uses the console. Text goes to <see cref="Console.Out"/> and
/// colors are set through <see cref="Console.ForegroundColor"/>; no escape sequences are written by this
/// library. Whether a color reaches the screen is decided by the runtime and the terminal: when output is
/// redirected, for example, the runtime does not turn colors into escape sequences, and the text arrives plain.
/// </para>
/// <para>
/// To write without the console, or to any other <see cref="TextWriter"/>, use <see cref="MonthWriter"/>.
/// </para>
/// </remarks>
public static class ConsoleMonthWriter
{
    private static readonly object Gate = new object();

    /// <summary>
    /// Writes every line of a rendered month to the console, each followed by the console's line terminator,
    /// with every segment in the color the theme gives it.
    /// </summary>
    /// <param name="month">The rendered month.</param>
    /// <param name="theme">
    /// The colors to use. With <see cref="CalendarTheme.None"/> no color is read or changed and only the text is
    /// written.
    /// </param>
    /// <remarks>
    /// <para>
    /// A segment the theme has a color for is written in that color. Every other segment, and every line
    /// terminator, is written in the foreground color the console had when the call began. Only the foreground
    /// color is ever set. Before the call returns, or fails, the foreground and background colors the console
    /// reported when the call began are put back.
    /// </para>
    /// <para>
    /// Where the console's foreground color had not been set, as is usual on Linux and macOS, it cannot be put
    /// back as a value; the colors are reset instead and a background color the caller had set is set again.
    /// </para>
    /// <para>
    /// Calls to this method from several threads do not mix their output: one month is written completely
    /// before the next begins. Nothing can stop other code that writes to the console at the same time from
    /// appearing in between.
    /// </para>
    /// <para>
    /// If writing fails, the exception is passed on after the colors have been put back, and what was written
    /// before it stays written.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="month"/> or <paramref name="theme"/> is <see langword="null"/>.
    /// </exception>
    public static void Write(RenderedMonth month, CalendarTheme theme)
    {
        if (month == null)
        {
            throw new ArgumentNullException(nameof(month));
        }
        if (theme == null)
        {
            throw new ArgumentNullException(nameof(theme));
        }

        lock (Gate)
        {
            TextWriter output = Console.Out;
            // The caller's colors, read the first time a color is about to be changed and not before.
            bool callerColorsKnown = false;
            ConsoleColor callerForeground = default;
            ConsoleColor callerBackground = default;
            // The theme color currently in effect; null while the caller's own color is.
            ConsoleColor? active = null;
            try
            {
                foreach (RenderedLine line in month.Lines)
                {
                    foreach (RenderedSegment segment in line.Segments)
                    {
                        ConsoleColor? wanted = theme.GetColor(segment);
                        if (wanted != active)
                        {
                            if (wanted.HasValue)
                            {
                                if (!callerColorsKnown)
                                {
                                    callerForeground = Console.ForegroundColor;
                                    callerBackground = Console.BackgroundColor;
                                    callerColorsKnown = true;
                                }
                                Console.ForegroundColor = wanted.Value;
                            }
                            else
                            {
                                Restore(callerForeground, callerBackground);
                            }
                            active = wanted;
                        }
                        output.Write(segment.Text);
                    }
                    if (active.HasValue)
                    {
                        Restore(callerForeground, callerBackground);
                        active = null;
                    }
                    output.WriteLine();
                }
            }
            finally
            {
                if (active.HasValue)
                {
                    Restore(callerForeground, callerBackground);
                }
            }
        }
    }

    private static void Restore(ConsoleColor foreground, ConsoleColor background)
    {
        if (IsDefined(foreground))
        {
            Console.ForegroundColor = foreground;
            return;
        }
        // The foreground was the terminal's own default, which has no ConsoleColor value to set. ResetColor
        // brings it back, and takes the background with it, so a background the caller had set is set again.
        Console.ResetColor();
        if (IsDefined(background))
        {
            Console.BackgroundColor = background;
        }
    }

    private static bool IsDefined(ConsoleColor color) => color >= ConsoleColor.Black && color <= ConsoleColor.White;
}
