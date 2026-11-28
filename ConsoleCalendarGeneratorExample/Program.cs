using ConsoleCalendarGenerator;
using ConsoleCalendarGeneratorExample;

// A short tour of ConsoleCalendarGenerator. Run it with:
//
//     dotnet run --project ConsoleCalendarGeneratorExample
//
// The program uses fixed dates and explicit cultures, takes no input, and prints plain text when its output
// is redirected. Each step is a method in Examples.cs, and README.md shows the same code.

Heading("Quick start");
MonthRenderOptions options = Examples.QuickStart();

Heading("First day of the week");
Examples.FirstDayOfWeek(options);

Heading("Culture and labels");
Examples.Localized();

Heading("Marked dates");
RenderedMonth marked = Examples.MarkedDates(options);

Heading("Console colors");
Examples.Colors(marked);

Heading("Any TextWriter");
Examples.AnyTextWriter(marked);

Heading("Segments");
Examples.Segments(marked);

Heading("Layout");
Examples.Layout();

Heading("ConsoleCalendar");
Examples.OriginalApi();

static void Heading(string text)
{
    Console.WriteLine();
    Console.WriteLine("== " + text + " ==");
}
