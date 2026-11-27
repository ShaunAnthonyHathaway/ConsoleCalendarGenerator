using System.Globalization;
using ConsoleCalendarGenerator.Tests.Support;

namespace ConsoleCalendarGenerator.Tests.Migration;

/// <summary>
/// The correction of the title (D-61), as an exact change to the list of calls that 1.0.0 made on the console's
/// writer. It is the whole of what the migration tests allow a title to differ by.
///
/// <para>
/// 1.0.0 wrote the title with one <c>WriteLine(string)</c>: the indent, the current culture's name for month
/// number <c>Month</c> in its own calendar, a space and the year. Version 2.0 writes the same call with the
/// culture's name for that month of the Gregorian calendar, and the indent that the three steps give that name.
/// Nothing else about the call changes, and no other call does.
/// </para>
/// <para>
/// Both names are worked out here from the culture, by <see cref="CultureCalendars"/>, which does not use the
/// library. A title is changed only when it is, character for character, the title 1.0.0 wrote for the month
/// that is due; anything else where a title belongs is refused with an error. Under a culture whose dates use
/// the Gregorian calendar the calls are returned as they are, without being looked at.
/// </para>
/// </summary>
internal static class GregorianTitle
{
    /// <summary>
    /// The name version 2.0 puts in the title: the culture's name for the Gregorian month. For a culture that
    /// offers no Gregorian calendar, of which none is known, it is the invariant name.
    /// </summary>
    public static string MonthName(CultureInfo culture, int month) =>
        CultureCalendars.GregorianMonthName(culture, month) ?? CultureInfo.InvariantCulture.DateTimeFormat.GetMonthName(month);

    /// <summary>The title 1.0.0 wrote: the name of that month number in the culture's own calendar.</summary>
    public static string TitleIn100(CultureInfo culture, int month, int year) =>
        Historical.Published100Text.Title(CultureCalendars.OwnMonthName(culture, month), year);

    /// <summary>The title version 2.0 writes.</summary>
    public static string TitleIn20(CultureInfo culture, int month, int year) => LegacyReference.Title(MonthName(culture, month), year);

    /// <summary>
    /// The calls of 1.0.0 with every title replaced by the title of 2.0. <paramref name="months"/> are the
    /// months the calls draw, in order; fewer may have been drawn when the drawing failed on the way. Each month
    /// begins with two <c>WriteLine(string)</c> calls, the title and the weekday row, and nothing else is
    /// written with that method, which is how a title is found.
    /// </summary>
    public static IReadOnlyList<RecordedCall> Corrected(IReadOnlyList<RecordedCall> calls, CultureInfo culture, int year, IReadOnlyList<int> months)
    {
        if (CultureCalendars.NamesGregorianMonths(culture))
        {
            return calls;
        }

        var corrected = new List<RecordedCall>(calls.Count);
        bool changed = false;
        int linesOfText = 0;
        for (int index = 0; index < calls.Count; index++)
        {
            RecordedCall call = calls[index];
            if (call.Method != "LS")
            {
                corrected.Add(call);
                continue;
            }

            bool isTitle = linesOfText % 2 == 0;
            int drawn = linesOfText / 2;
            linesOfText++;
            if (!isTitle)
            {
                if (call.Text != Historical.Published100Text.WeekdayHeader)
                {
                    throw new InvalidOperationException("Call " + (index + 1) + " is where the weekday row belongs and is not the weekday row.");
                }
                corrected.Add(call);
                continue;
            }
            if (drawn >= months.Count)
            {
                throw new InvalidOperationException("Call " + (index + 1) + " is a title, and more months are drawn than the scenario has.");
            }

            int month = months[drawn];
            if (call.Text != TitleIn100(culture, month, year))
            {
                throw new InvalidOperationException("Call " + (index + 1) + " is where the title of month " + month + " belongs and is not the title 1.0.0 wrote for it.");
            }
            string title = TitleIn20(culture, month, year);
            changed |= title != call.Text;
            corrected.Add(call with { Text = title });
        }
        return changed ? corrected : calls;
    }
}
