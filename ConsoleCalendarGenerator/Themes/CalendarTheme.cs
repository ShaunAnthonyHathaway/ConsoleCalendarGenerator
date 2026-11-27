using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace ConsoleCalendarGenerator;

/// <summary>
/// The console colors of a rendered month: an optional color for the title, the title suffix, the weekday
/// labels and the days, and for the days in each named state.
/// </summary>
/// <remarks>
/// <para>
/// A theme maps what a segment is to a color and does nothing else. It does not change the text of a rendered
/// month, writes nothing, and knows nothing about the console or the terminal. A color that is
/// <see langword="null"/> means "no color of the theme": the text is written in whatever color the destination
/// already has.
/// </para>
/// <para>
/// Only foreground colors are supported. Indents, padding, gaps and empty cells have no color.
/// </para>
/// <para>
/// Instances are immutable. Start from <see cref="None"/> and call the <c>With</c> methods; each returns a new
/// instance and leaves the one it was called on unchanged.
/// </para>
/// </remarks>
public sealed class CalendarTheme
{
    private static readonly IReadOnlyDictionary<string, ConsoleColor> NoStateColors =
        new ReadOnlyDictionary<string, ConsoleColor>(new Dictionary<string, ConsoleColor>(StringComparer.Ordinal));

    private CalendarTheme(
        ConsoleColor? titleColor,
        ConsoleColor? titleSuffixColor,
        ConsoleColor? weekdayLabelColor,
        ConsoleColor? dayColor,
        IReadOnlyDictionary<string, ConsoleColor> stateColors)
    {
        TitleColor = titleColor;
        TitleSuffixColor = titleSuffixColor;
        WeekdayLabelColor = weekdayLabelColor;
        DayColor = dayColor;
        StateColors = stateColors;
    }

    /// <summary>Gets the theme with no colors at all. Writing with it changes no color.</summary>
    public static CalendarTheme None { get; } = new CalendarTheme(null, null, null, null, NoStateColors);

    /// <summary>Gets the color of the title, or <see langword="null"/> for none.</summary>
    public ConsoleColor? TitleColor { get; }

    /// <summary>
    /// Gets the color of the title suffix, or <see langword="null"/> for none. The suffix does not take the
    /// color of the title.
    /// </summary>
    public ConsoleColor? TitleSuffixColor { get; }

    /// <summary>Gets the color of the weekday labels, or <see langword="null"/> for none.</summary>
    public ConsoleColor? WeekdayLabelColor { get; }

    /// <summary>
    /// Gets the color of the days, or <see langword="null"/> for none. It applies to every day that has no
    /// state, and to a day whose state has no color in <see cref="StateColors"/>.
    /// </summary>
    public ConsoleColor? DayColor { get; }

    /// <summary>
    /// Gets the colors of the days in particular states, by the name of the state. Names are compared character
    /// by character, so they are case-sensitive. The dictionary is read-only.
    /// </summary>
    public IReadOnlyDictionary<string, ConsoleColor> StateColors { get; }

    /// <summary>Returns a theme with a different color for the title.</summary>
    /// <param name="color">The color, or <see langword="null"/> for none.</param>
    /// <returns>A new theme with <see cref="TitleColor"/> set.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="color"/> is not a defined <see cref="ConsoleColor"/>.</exception>
    public CalendarTheme WithTitleColor(ConsoleColor? color) =>
        new CalendarTheme(Validated(color), TitleSuffixColor, WeekdayLabelColor, DayColor, StateColors);

    /// <summary>Returns a theme with a different color for the title suffix.</summary>
    /// <param name="color">The color, or <see langword="null"/> for none.</param>
    /// <returns>A new theme with <see cref="TitleSuffixColor"/> set.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="color"/> is not a defined <see cref="ConsoleColor"/>.</exception>
    public CalendarTheme WithTitleSuffixColor(ConsoleColor? color) =>
        new CalendarTheme(TitleColor, Validated(color), WeekdayLabelColor, DayColor, StateColors);

    /// <summary>Returns a theme with a different color for the weekday labels.</summary>
    /// <param name="color">The color, or <see langword="null"/> for none.</param>
    /// <returns>A new theme with <see cref="WeekdayLabelColor"/> set.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="color"/> is not a defined <see cref="ConsoleColor"/>.</exception>
    public CalendarTheme WithWeekdayLabelColor(ConsoleColor? color) =>
        new CalendarTheme(TitleColor, TitleSuffixColor, Validated(color), DayColor, StateColors);

    /// <summary>Returns a theme with a different color for the days.</summary>
    /// <param name="color">The color, or <see langword="null"/> for none.</param>
    /// <returns>A new theme with <see cref="DayColor"/> set.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="color"/> is not a defined <see cref="ConsoleColor"/>.</exception>
    public CalendarTheme WithDayColor(ConsoleColor? color) =>
        new CalendarTheme(TitleColor, TitleSuffixColor, WeekdayLabelColor, Validated(color), StateColors);

    /// <summary>Returns a theme that gives the days in one state a color of their own.</summary>
    /// <param name="stateName">
    /// The <see cref="DayState.Name"/> of the state, compared character by character. The same rules apply as for
    /// the name of a state: at least one character that is not white space, and no line break.
    /// </param>
    /// <param name="color">
    /// The color, or <see langword="null"/> to remove the state's color so that its days use
    /// <see cref="DayColor"/> again.
    /// </param>
    /// <returns>
    /// A new theme with the entry for <paramref name="stateName"/> in <see cref="StateColors"/> set, replaced or
    /// removed. A color set earlier for the same name is replaced.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="stateName"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="stateName"/> is empty, consists only of white space or contains a line break.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="color"/> is not a defined <see cref="ConsoleColor"/>.</exception>
    public CalendarTheme WithStateColor(string stateName, ConsoleColor? color)
    {
        DayState.ValidateName(stateName, nameof(stateName));
        Validated(color);

        var stateColors = new Dictionary<string, ConsoleColor>(StringComparer.Ordinal);
        foreach (KeyValuePair<string, ConsoleColor> existing in StateColors)
        {
            stateColors.Add(existing.Key, existing.Value);
        }
        if (color.HasValue)
        {
            stateColors[stateName] = color.Value;
        }
        else
        {
            stateColors.Remove(stateName);
        }
        return new CalendarTheme(
            TitleColor, TitleSuffixColor, WeekdayLabelColor, DayColor, new ReadOnlyDictionary<string, ConsoleColor>(stateColors));
    }

    /// <summary>Returns the color this theme gives a segment of a rendered month.</summary>
    /// <param name="segment">The segment.</param>
    /// <returns>
    /// The color, or <see langword="null"/> when the theme has none for the segment. A
    /// <see cref="RenderedSegmentKind.Day"/> segment with a state gets the color of that state in
    /// <see cref="StateColors"/> if there is one, and <see cref="DayColor"/> otherwise. Indents, padding, gaps
    /// and empty cells never have a color.
    /// </returns>
    /// <remarks>
    /// This is the whole of what a theme does. Code that writes a rendered month somewhere other than the
    /// console can call it for each segment and apply the color in its own way.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="segment"/> is <see langword="null"/>.</exception>
    public ConsoleColor? GetColor(RenderedSegment segment)
    {
        if (segment == null)
        {
            throw new ArgumentNullException(nameof(segment));
        }
        switch (segment.Kind)
        {
            case RenderedSegmentKind.Title:
                return TitleColor;
            case RenderedSegmentKind.TitleSuffix:
                return TitleSuffixColor;
            case RenderedSegmentKind.WeekdayLabel:
                return WeekdayLabelColor;
            case RenderedSegmentKind.Day:
                return segment.State != null && StateColors.TryGetValue(segment.State.Name, out ConsoleColor stateColor)
                    ? stateColor
                    : DayColor;
            default:
                return null;
        }
    }

    private static ConsoleColor? Validated(ConsoleColor? color)
    {
        if (color.HasValue && (color.Value < ConsoleColor.Black || color.Value > ConsoleColor.White))
        {
            throw new ArgumentOutOfRangeException(nameof(color), color.Value, "The color is not a defined console color.");
        }
        return color;
    }
}
