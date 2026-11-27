namespace ConsoleCalendarGenerator;

/// <summary>Specifies where the title of a rendered month is placed relative to the grid of days.</summary>
public enum TitleAlignment
{
    /// <summary>The title starts at the left edge of the grid.</summary>
    Left = 0,

    /// <summary>
    /// The title is centered over the grid. When the space left over cannot be divided evenly, the extra space
    /// goes to the right of the title. A title as wide as the grid or wider starts at the left edge.
    /// </summary>
    Center = 1,
}
