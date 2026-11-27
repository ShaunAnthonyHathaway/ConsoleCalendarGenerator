using System;

namespace ConsoleCalendarGenerator;

/// <summary>
/// A state that a day of a rendered month can be in, such as "selected" or "closed": a name that identifies it
/// and a marker that shows it in the text.
/// </summary>
/// <remarks>
/// <para>
/// A state says what a day is, not how it looks. The marker makes the state visible in plain text, so that it
/// never depends on color alone. A color, if one is wanted, is given to the state by name in a
/// <see cref="CalendarTheme"/>.
/// </para>
/// <para>
/// Instances are immutable. Two states are the same kind of state when their names are equal, compared
/// character by character; <see cref="object.Equals(object)"/> is not overridden, so two separately created
/// instances with the same name are still different objects. A rendered day carries the instance it was given.
/// </para>
/// </remarks>
public sealed class DayState
{
    /// <summary>Creates a state.</summary>
    /// <param name="name">
    /// The name that identifies the state, for example to a <see cref="CalendarTheme"/>. It is compared
    /// character by character, so it is case-sensitive and does not depend on any culture. It must contain at
    /// least one character that is not white space, and no line break.
    /// </param>
    /// <param name="marker">
    /// The text placed directly before the day number in the cell of a day in this state, such as <c>"*"</c>.
    /// It must contain at least one character that is not white space, and no line break. A cell must be wide
    /// enough for the marker and a two-digit day; see <see cref="MonthRenderOptions.CellWidth"/>.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="name"/> or <paramref name="marker"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="name"/> or <paramref name="marker"/> is empty, consists only of white space or contains a
    /// line break. The name is checked first.
    /// </exception>
    public DayState(string name, string marker)
    {
        ValidateName(name, nameof(name));
        if (marker == null)
        {
            throw new ArgumentNullException(nameof(marker));
        }
        if (string.IsNullOrWhiteSpace(marker))
        {
            throw new ArgumentException("The marker of a state must contain a visible character.", nameof(marker));
        }
        if (TextElements.ContainsLineBreak(marker))
        {
            throw new ArgumentException("The marker of a state must not contain a line break.", nameof(marker));
        }
        Name = name;
        Marker = marker;
    }

    /// <summary>Gets the name that identifies the state. Names are compared character by character.</summary>
    public string Name { get; }

    /// <summary>Gets the text that marks a day in this state, placed directly before the day number.</summary>
    public string Marker { get; }

    /// <summary>Returns <see cref="Name"/>.</summary>
    /// <returns>The name of the state.</returns>
    public override string ToString() => Name;

    /// <summary>The rules for a state name, shared with the theme, which looks states up by name.</summary>
    internal static void ValidateName(string name, string parameterName)
    {
        if (name == null)
        {
            throw new ArgumentNullException(parameterName);
        }
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("The name of a state must contain a character that is not white space.", parameterName);
        }
        if (TextElements.ContainsLineBreak(name))
        {
            throw new ArgumentException("The name of a state must not contain a line break.", parameterName);
        }
    }
}
