namespace ConsoleCalendarGenerator;

/// <summary>Specifies what a segment of a <see cref="RenderedLine"/> is.</summary>
/// <remarks>
/// The kind says what a piece of text means, not how it should look. Code that styles or writes a rendered month
/// decides from the kind, without reading the text.
/// </remarks>
public enum RenderedSegmentKind
{
    /// <summary>The indent from <see cref="MonthRenderOptions.Indent"/>, at the start of a line.</summary>
    Indent = 0,

    /// <summary>Spaces that position the title. They belong to no cell.</summary>
    Padding = 1,

    /// <summary>The formatted title of the month, without the suffix.</summary>
    Title = 2,

    /// <summary>The text from <see cref="MonthRenderOptions.TitleSuffix"/>, directly after the title.</summary>
    TitleSuffix = 3,

    /// <summary>The cell that labels one weekday column. Its text is the label, padded to the cell width.</summary>
    WeekdayLabel = 4,

    /// <summary>The spaces between two neighboring columns.</summary>
    Gap = 5,

    /// <summary>A cell with no day in it. Its text is spaces.</summary>
    EmptyDay = 6,

    /// <summary>A cell that holds a day of the month. Its text is the day number, padded to the cell width.</summary>
    Day = 7,
}
