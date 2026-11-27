using System.Globalization;
using ConsoleCalendarGenerator.Tests.Migration;
using ConsoleCalendarGenerator.Tests.Support;

namespace ConsoleCalendarGenerator.Tests.Legacy;

/// <summary>
/// The title on a real console, under a culture whose calendar is not the Gregorian one. A child process draws
/// September 2023 with the library, and another draws it with the frozen source of 1.0.0, both to the real
/// console streams. What reaches the terminal is the same byte for byte once the name in the title of 1.0.0 is
/// exchanged for the Gregorian one: the sequence that sets the title color before it, the reset after it, and
/// every sequence and character of the weekday row and the days. This is what a test in the same process cannot
/// see, because there a reset of the colors leaves no trace.
/// </summary>
[TestFixture]
[Category(Categories.Terminal)]
public class GregorianTitleTerminalTests
{
    private static IEnumerable<string> Labels() => new[] { "fa-IR", "ps-AF", "he-IL+HebrewCalendar", "ar-SA+UmAlQuraCalendar", "th-TH" };

    [TestCaseSource(nameof(Labels))]
    public void OnAPseudoTerminal_TheBytesAreThoseOf100_WithTheGregorianNameInTheTitle(string label)
    {
        if (!OperatingSystem.IsLinux())
        {
            Assert.Ignore("The pseudo-terminal tests use the Linux 'script' program.");
        }
        if (ChildProcess.ScriptProgram == null)
        {
            Assert.Ignore("The 'script' program (util-linux) is not installed.");
        }

        AssertTheSameApartFromTheName(
            label,
            ChildProcess.RunOnPseudoTerminal(noColor: false, command: Program.RenderPublished100UnderCultureCommand + label),
            ChildProcess.RunOnPseudoTerminal(noColor: false, command: Program.RenderUnderCultureCommand + label),
            lineEnd: "\r\n");
    }

    [TestCaseSource(nameof(Labels))]
    public void RedirectedWithTheRuntimesColorOptIn_TheBytesAreThoseOf100_WithTheGregorianNameInTheTitle(string label)
    {
        if (OperatingSystem.IsWindows())
        {
            Assert.Ignore("On Windows console colors are not escape sequences in the output stream.");
        }

        AssertTheSameApartFromTheName(
            label,
            ChildProcess.RunRedirected(allowAnsiWhenRedirected: true, command: Program.RenderPublished100UnderCultureCommand + label),
            ChildProcess.RunRedirected(allowAnsiWhenRedirected: true, command: Program.RenderUnderCultureCommand + label),
            lineEnd: "\n");
    }

    private static void AssertTheSameApartFromTheName(string label, ChildResult published, ChildResult library, string lineEnd)
    {
        CultureInfo? culture = CultureCalendars.Resolve(label);
        Assume.That(culture, Is.Not.Null, "This machine does not have " + label + ".");
        Assume.That(CultureCalendars.NamesGregorianMonths(culture!), Is.False, label + " uses the Gregorian calendar on this machine.");
        Assert.That(published.ExitCode, Is.Zero, published.StandardError);
        Assert.That(library.ExitCode, Is.Zero, library.StandardError);
        string title100 = GregorianTitle.TitleIn100(culture!, 9, 2023) + lineEnd;
        string title20 = GregorianTitle.TitleIn20(culture!, 9, 2023) + lineEnd;

        Assert.That(TerminalOutput.ColorSequence.Matches(library.StandardOutput), Has.Count.GreaterThan(30), "the output carries the colors");
        Assert.That(Occurrences(published.StandardOutput, title100), Is.EqualTo(1), "1.0.0 wrote its title once: " + title100.TrimEnd());
        Assert.That(Occurrences(library.StandardOutput, title20), Is.EqualTo(1), "2.0 writes its title once: " + title20.TrimEnd());
        Assert.That(library.StandardOutput, Is.EqualTo(published.StandardOutput.Replace(title100, title20, StringComparison.Ordinal)),
            "with the name exchanged, every byte is the one 1.0.0 wrote");
        Assert.That(library.StandardOutput == published.StandardOutput, Is.EqualTo(title100 == title20),
            "the output differs exactly when the two names do");
        Assert.That(TerminalOutput.WithoutTerminalControl(library.StandardOutput), Is.EqualTo(LegacyReference.Month(2023, 9, GregorianTitle.MonthName(culture!, 9))));
    }

    private static int Occurrences(string text, string part)
    {
        int count = 0;
        for (int index = text.IndexOf(part, StringComparison.Ordinal); index >= 0; index = text.IndexOf(part, index + part.Length, StringComparison.Ordinal))
        {
            count++;
        }
        return count;
    }
}
