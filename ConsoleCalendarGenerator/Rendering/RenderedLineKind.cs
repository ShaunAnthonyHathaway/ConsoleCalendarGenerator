namespace ConsoleCalendarGenerator;

/// <summary>Specifies what a line of a <see cref="RenderedMonth"/> shows.</summary>
public enum RenderedLineKind
{
    /// <summary>The title of the month.</summary>
    Title = 0,

    /// <summary>The labels of the seven weekday columns.</summary>
    WeekdayLabels = 1,

    /// <summary>One week of days.</summary>
    Week = 2,
}
