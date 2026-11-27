using System.Globalization;

namespace ConsoleCalendarGenerator.Tests.Support;

/// <summary>Finds the day cells that were written in the selected-date color.</summary>
internal static class SelectedDays
{
    /// <summary>The day numbers written in <paramref name="selectedColor"/>, in the order they were written.</summary>
    public static IReadOnlyList<int> Of(Captured captured, ConsoleColor selectedColor) =>
        ByMonth(captured, selectedColor).Select(cell => cell.Day).ToList();

    /// <summary>
    /// The same with the position of the month in the output: 1 for the first month written, 2 for the second.
    /// </summary>
    public static IReadOnlyList<(int Month, int Day)> ByMonth(Captured captured, ConsoleColor selectedColor)
    {
        var cells = new List<(int Month, int Day)>();
        int month = 0;
        foreach (WriteEvent e in captured.Events)
        {
            if (e.Payload == LegacyReference.WeekdayHeader)
            {
                month++;
            }
            else if (month > 0 && e.Foreground == selectedColor
                && int.TryParse(e.Payload.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out int day))
            {
                cells.Add((month, day));
            }
        }
        return cells;
    }
}
