using System.Globalization;
using CalendarGenerator;

/// <summary>
/// Run with the argument <c>culture-audit</c>, the application draws every month of a year with the legacy
/// calendar under every culture the system has, and checks that the title is the culture's name for the
/// Gregorian month that is drawn. It then says which cultures there are, which is different on every system:
/// how many, how many count their dates in a calendar that is not Gregorian, and under how many of those the
/// title is therefore another one than version 1.0.0 wrote. Nothing here is compared with a fixed text.
/// </summary>
internal static class CultureAudit
{
    public static int Run()
    {
        CultureInfo[] cultures = CultureInfo.GetCultures(CultureTypes.AllCultures);
        var calendars = new SortedDictionary<string, int>(StringComparer.Ordinal);
        var differing = new List<string>();
        var withoutGregorian = new List<string>();
        var wrong = new List<string>();

        foreach (CultureInfo culture in cultures)
        {
            if (culture.DateTimeFormat.Calendar is not GregorianCalendar)
            {
                string calendar = culture.DateTimeFormat.Calendar.GetType().Name;
                calendars[calendar] = calendars.GetValueOrDefault(calendar) + 1;
                if (GregorianCopyOf(culture) == null)
                {
                    withoutGregorian.Add(culture.Name);
                }
            }
            if (Check(culture.Name, culture, wrong))
            {
                differing.Add(culture.Name);
            }
        }

        // Cultures given one of the other calendars they offer, as a program can do. These are the same on
        // every system that has the culture, whatever calendar the system gives the culture by itself.
        var given = new List<string>();
        foreach ((string name, Type calendarType) in new[]
        {
            ("ar-SA", typeof(UmAlQuraCalendar)), ("ar-SA", typeof(HijriCalendar)), ("he-IL", typeof(HebrewCalendar)),
            ("fa-IR", typeof(HijriCalendar)), ("ja-JP", typeof(JapaneseCalendar)), ("th-TH", typeof(ThaiBuddhistCalendar)),
        })
        {
            CultureInfo? culture = With(name, calendarType);
            if (culture == null)
            {
                continue;
            }
            string label = name + " with " + calendarType.Name;
            given.Add(label + (Check(label, culture, wrong) ? " (differs)" : " (same)"));
        }

        Console.WriteLine("cultures examined: " + cultures.Length);
        Console.WriteLine("whose dates use a calendar that is not Gregorian: " + calendars.Values.Sum()
            + (calendars.Count == 0 ? string.Empty : " (" + string.Join(", ", calendars.Select(entry => entry.Value + " " + entry.Key)) + ")"));
        Console.WriteLine("under which the legacy title differs from the one of 1.0.0: " + differing.Count
            + (differing.Count == 0 ? string.Empty : " (" + string.Join(", ", differing) + ")"));
        Console.WriteLine("that offer no Gregorian calendar: " + withoutGregorian.Count
            + (withoutGregorian.Count == 0 ? string.Empty : " (" + string.Join(", ", withoutGregorian) + ")"));
        Console.WriteLine("cultures given another of their calendars: " + given.Count
            + (given.Count == 0 ? string.Empty : " (" + string.Join(", ", given) + ")"));
        foreach (string line in wrong.Take(20))
        {
            Console.WriteLine("WRONG TITLE: " + line);
        }
        return wrong.Count == 0 ? 0 : 1;
    }

    /// <summary>
    /// Checks the twelve titles under a culture. Returns whether the name differs, for some month, from the one
    /// 1.0.0 wrote, which was the culture's name for that month number in its own calendar.
    /// </summary>
    private static bool Check(string label, CultureInfo culture, List<string> wrong)
    {
        CultureInfo? gregorian = GregorianCopyOf(culture);
        bool differs = false;
        for (int month = 1; month <= 12; month++)
        {
            string own = culture.DateTimeFormat.GetMonthName(month);
            string name = culture.DateTimeFormat.Calendar is GregorianCalendar ? own
                : gregorian != null ? new DateTime(2023, month, 1).ToString("MMMM", gregorian.DateTimeFormat)
                : CultureInfo.InvariantCulture.DateTimeFormat.GetMonthName(month);
            string expected = (name.Length <= 4 ? "     " : name.Length <= 7 ? "    " : "   ") + name + " 2023";
            string title = TitleOf(culture, month);
            if (title != expected)
            {
                wrong.Add(label + ", month " + month + ": expected [" + expected + "], drawn [" + title + "]");
            }
            differs |= name != own;
        }
        return differs;
    }

    /// <summary>A writable copy of the culture whose dates use the Gregorian calendar it offers, or null when it offers none.</summary>
    private static CultureInfo? GregorianCopyOf(CultureInfo culture)
    {
        Calendar? gregorian = culture.OptionalCalendars
            .OfType<GregorianCalendar>()
            .OrderBy(calendar => calendar.CalendarType == GregorianCalendarTypes.Localized ? 0 : 1)
            .FirstOrDefault();
        if (gregorian == null)
        {
            return null;
        }
        var copy = (CultureInfo)culture.Clone();
        copy.DateTimeFormat.Calendar = gregorian;
        return copy;
    }

    private static CultureInfo? With(string name, Type calendarType)
    {
        CultureInfo culture;
        try
        {
            culture = CultureInfo.GetCultureInfo(name);
        }
        catch (CultureNotFoundException)
        {
            return null;
        }
        Calendar? calendar = culture.OptionalCalendars.FirstOrDefault(optional => optional.GetType() == calendarType);
        if (calendar == null || culture.DateTimeFormat.Calendar.GetType() == calendarType)
        {
            return null;
        }
        var copy = (CultureInfo)culture.Clone();
        copy.DateTimeFormat.Calendar = calendar;
        return copy;
    }

    private static string TitleOf(CultureInfo culture, int month)
    {
        TextWriter console = Console.Out;
        CultureInfo current = CultureInfo.CurrentCulture;
        var text = new StringWriter { NewLine = "\n" };
        try
        {
            Console.SetOut(text);
            CultureInfo.CurrentCulture = culture;
            new ConsoleCalendar().GenerateMonth(month, 2023);
        }
        finally
        {
            Console.SetOut(console);
            CultureInfo.CurrentCulture = current;
        }
        string written = text.ToString();
        return written.Substring(0, written.IndexOf('\n'));
    }
}
