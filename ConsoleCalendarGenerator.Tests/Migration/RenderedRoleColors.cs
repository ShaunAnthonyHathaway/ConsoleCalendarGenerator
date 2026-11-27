using System.Globalization;

namespace ConsoleCalendarGenerator.Tests.Migration;

/// <summary>
/// The color each element of one drawn month was written in, read from the calls of an observation: the title
/// is the first call, the weekday row the second, and every call after them whose text is a number is a day.
/// Only meaningful where colors can be read back, which is Linux and macOS.
/// </summary>
internal sealed class RenderedRoleColors
{
    private RenderedRoleColors(ConsoleColor title, ConsoleColor dayOfWeek, ConsoleColor[] dates, ConsoleColor[] selectedDates)
    {
        Title = title;
        DayOfWeek = dayOfWeek;
        Dates = dates;
        SelectedDates = selectedDates;
    }

    public ConsoleColor Title { get; }

    public ConsoleColor DayOfWeek { get; }

    /// <summary>The distinct colors of the days that are not selected. One color, unless something is wrong.</summary>
    public IReadOnlyList<ConsoleColor> Dates { get; }

    /// <summary>The distinct colors of the selected days.</summary>
    public IReadOnlyList<ConsoleColor> SelectedDates { get; }

    /// <param name="observation">One month, drawn to its end.</param>
    /// <param name="selectedDays">The days of that month that were selected.</param>
    public static RenderedRoleColors Of(LegacyObservation observation, params int[] selectedDays)
    {
        if (observation.Calls.Count < 3 || observation.Calls[0].Method != "LS" || observation.Calls[1].Method != "LS")
        {
            throw new ArgumentException("The observation is not a month drawn to its end.", nameof(observation));
        }
        var dates = new List<ConsoleColor>();
        var selected = new List<ConsoleColor>();
        foreach (Support.RecordedCall call in observation.Calls.Skip(2))
        {
            if (int.TryParse(call.Text.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out int day))
            {
                (selectedDays.Contains(day) ? selected : dates).Add(call.Foreground);
            }
        }
        return new RenderedRoleColors(
            observation.Calls[0].Foreground, observation.Calls[1].Foreground, dates.Distinct().ToArray(), selected.Distinct().ToArray());
    }
}
