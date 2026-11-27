using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using ConsoleCalendarGenerator.Tests.Support;

namespace ConsoleCalendarGenerator.Tests.Migration;

/// <summary>
/// The two texts that the smoke script compares the sample program of 1.0.0 with
/// (eng/consumer-smoke/OriginalSample): what it writes with the published 1.0.0 package, recorded from a run of
/// that package, and what it writes with version 2.0. The script can only run the program where it has a
/// pseudo-terminal. These tests need none: they put the calendars the program draws through the frozen source
/// of 1.0.0 and through the library, add the program's own prompts, and compare with the two files.
///
/// <para>
/// So the file recorded from the published package is also evidence about the frozen source, which writes
/// exactly that; and the two files differ in the one way 2.0 means them to.
/// </para>
/// </summary>
[TestFixture]
public class OriginalSampleTextTests
{
    private const string TextOf100 = "eng/consumer-smoke/OriginalSample/expected-text-1.0.0.txt";
    private const string TextOf20 = "eng/consumer-smoke/OriginalSample/expected-text-2.0.txt";

    /// <summary>The SHA-256 of the text recorded from the published 1.0.0. The file is historical and never changes.</summary>
    private const string Sha256OfTheTextOf100 = "ad83e16e54f45f7de0a48bf885379c085178e815b444329638fab8b368d9eb6d";

    // What the program itself writes between its calendars: a prompt, the key the runtime echoes when it is
    // pressed (the smoke script presses x), and a form feed that begins clearing the screen. The two escape
    // sequences after the form feed are not text.
    private const string Pause = "\n\nPress any key for next example...\nx\f";
    private const string End = "\n\nPress any key to terminate...\nx";

    /// <summary>The text of the calendars of the sample whose names begin a certain way, drawn by one implementation.</summary>
    private static string Drawn(LegacySubject subject, string calendar) => string.Concat(
        MigrationMatrix.OriginalSample
            .Where(scenario => scenario.Name.StartsWith(calendar, StringComparison.Ordinal))
            .Select(scenario => LegacyObservation.Of(subject, scenario, CultureInfo.InvariantCulture).Text));

    /// <summary>Everything the program writes, as text: its four sets of calendars with its prompts between them.</summary>
    private static string TheWholeProgram(LegacySubject subject) =>
        Drawn(subject, "the first calendar") + Pause
        + Drawn(subject, "the second calendar, month") + Pause
        + Drawn(subject, "the second calendar, the year") + Pause
        + Drawn(subject, "the calendar with four colors") + End;

    [Test]
    public void TheTextRecordedFromThePublished100_IsNotChanged()
    {
        string sha256 = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(Repository.Read(TextOf100)))).ToLowerInvariant();

        Assert.That(sha256, Is.EqualTo(Sha256OfTheTextOf100), TextOf100 + " is historical evidence, recorded from the published package, and is never edited.");
    }

    /// <summary>The published package and the frozen source write the same text for this program.</summary>
    [Test]
    public void TheTextRecordedFromThePublished100_IsWhatTheFrozenSourceOf100Writes()
    {
        Assert.That(TheWholeProgram(LegacySubject.Published100), Is.EqualTo(Repository.Read(TextOf100)));
    }

    [Test]
    public void TheTextExpectedOf20_IsWhatTheLibraryWrites()
    {
        Assert.That(TheWholeProgram(LegacySubject.Library), Is.EqualTo(Repository.Read(TextOf20)));
    }

    /// <summary>
    /// The two texts differ in one way: after a month whose last day is a Sunday, 1.0.0 has two blank lines and
    /// 2.0 has one (D-60). The program draws six such months: April 2023 four times and December 2023 twice.
    /// Nothing else differs, because the text shows no colors
    /// (D-57), the program passes no null list (D-58), and the last row of a month ends with the same character
    /// either way where the terminator is a line feed (D-59).
    /// </summary>
    [Test]
    public void TheTwoTexts_DifferOnlyInTheSecondBlankLineAfterAMonthThatEndsOnASunday()
    {
        string[] linesOf100 = Repository.Read(TextOf100).Split('\n');
        string[] linesOf20 = Repository.Read(TextOf20).Split('\n');

        var corrected = new List<string>();
        int removed = 0;
        for (int index = 0; index < linesOf100.Length; index++)
        {
            bool afterAFullWeek = index > 0 && linesOf100[index - 1].Length == 20 && linesOf100[index - 1].All(c => c == ' ' || char.IsAsciiDigit(c));
            if (afterAFullWeek && linesOf100[index].Length == 0)
            {
                // The first blank line directly after a full week: the one 2.0 does not write.
                removed++;
                continue;
            }
            corrected.Add(linesOf100[index]);
        }

        Assert.That(linesOf20, Is.EqualTo(corrected));
        Assert.That(removed, Is.EqualTo(6));
        Assert.That(MigrationMatrix.OriginalSample.Sum(scenario => MonthsEndingOnSunday(scenario)), Is.EqualTo(6), "the months the program draws that end on a Sunday");
    }

    private static int MonthsEndingOnSunday(LegacyScenario scenario)
    {
        IEnumerable<int> months = scenario.Draw is LegacyDraw.Year or LegacyDraw.YearWithDates ? Enumerable.Range(1, 12) : new[] { scenario.Month };
        return months.Count(month => new DateTime(scenario.Year, month, DateTime.DaysInMonth(scenario.Year, month)).DayOfWeek == DayOfWeek.Sunday);
    }

    [Test]
    public void TheSampleDraws34Months_WithEachKindOfConstructor()
    {
        Assert.That(MigrationMatrix.OriginalSample, Has.Count.EqualTo(12), "five months, five months, a year and a year");
        Assert.That(MigrationMatrix.OriginalSample.Sum(scenario => scenario.Draw is LegacyDraw.Year or LegacyDraw.YearWithDates ? 12 : 1), Is.EqualTo(34));
        Assert.That(MigrationMatrix.OriginalSample.Select(scenario => scenario.ConstructorArguments.Count).Distinct(), Is.EquivalentTo(new[] { 0, 1, 4 }));
    }
}
