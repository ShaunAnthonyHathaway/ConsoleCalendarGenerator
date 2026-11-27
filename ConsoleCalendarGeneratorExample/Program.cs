using ConsoleCalendarGenerator;
using ConsoleCalendarGeneratorExample;

// A short tour of ConsoleCalendarGenerator: run it with "dotnet run --project ConsoleCalendarGeneratorExample".
//
// It needs no input, uses fixed dates and explicit cultures, and prints plain text when its output is
// redirected. Each step is one method in Examples.cs, and README.md shows the same code.
//
// The month and day names come from .NET's culture data, so they can differ between operating systems: on
// Windows, for example, the shortest day names are single letters.

Heading("A month");
Examples.QuickStart();

Heading("Another language");
Examples.Culture();

Heading("Another first day of the week");
Examples.FirstDayOfWeek();

Heading("Labels of your own");
Examples.OwnLabels();

Heading("Marked dates");
RenderedMonth marked = Examples.MarkedDates();

Heading("Colors");
Examples.Colors(marked);

Heading("Any TextWriter");
Examples.AnyTextWriter(marked);

Heading("What each piece of text is");
Examples.Segments(marked);

Heading("Layout only");
Examples.Layout();

Heading("The legacy API");
Examples.Legacy();

static void Heading(string text)
{
    Console.WriteLine();
    Console.WriteLine("== " + text + " ==");
}
