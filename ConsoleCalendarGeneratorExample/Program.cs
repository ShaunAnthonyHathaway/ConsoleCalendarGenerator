using ConsoleCalendarGenerator;
using ConsoleCalendarGeneratorExample;

// A short tour of ConsoleCalendarGenerator: run it with "dotnet run --project ConsoleCalendarGeneratorExample".
//
// It needs no input, uses fixed dates and explicit cultures, and prints plain text when its output is
// redirected. Each step is one method in Examples.cs, and README.md shows the same code.
//
// The examples supply their weekday labels, so the program prints the same text on every operating system.
// The one exception is "The culture's own day names", which prints the names .NET has for French on this
// machine: two letters on Linux and macOS, one letter on Windows.

Heading("A month");
MonthRenderOptions options = Examples.QuickStart();

Heading("The culture's own day names");
MonthRenderOptions french = Examples.Culture();

Heading("Labels of your own");
Examples.OwnLabels(french);

Heading("Another first day of the week");
Examples.FirstDayOfWeek(options);

Heading("Marked dates");
RenderedMonth marked = Examples.MarkedDates(options);

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
