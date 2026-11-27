#if MODERN_API
using System.Globalization;
using ConsoleCalendarGenerator;

// The API that is new in version 2.0. This file compiles only against 2.0: the namespace does not exist in
// 1.0.0, which is part of what the upgrade test shows.
internal static class Modern
{
    public static void Run()
    {
        Console.WriteLine("[new in 2.0]");
        MonthRenderOptions options = MonthRenderOptions.Default
            .WithCulture(CultureInfo.InvariantCulture)
            .WithTitleFormat("MMMM yyyy")
            .WithCellWidth(3)
            .WithWeekdayLabels(new[] { "Su", "Mo", "Tu", "We", "Th", "Fr", "Sa" });
        RenderedMonth month = MonthRenderer.Render(2027, 3, options, new[] { new DateTime(2027, 3, 17) }, new DayState("marked", "*"));
        Console.WriteLine(month.ToText());
        Console.WriteLine("layout: " + MonthLayout.Create(2027, 3, DayOfWeek.Monday).Weeks.Count + " weeks");
        Console.WriteLine("marked days: " + month.Lines.SelectMany(line => line.Segments).Count(segment => segment.State != null));
    }
}
#endif
