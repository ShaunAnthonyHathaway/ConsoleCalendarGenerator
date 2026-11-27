using System.Globalization;

namespace ConsoleCalendarGenerator.Tests.Support;

/// <summary>
/// What a culture calls the months, in its own calendar and in the Gregorian calendar, worked out here and not
/// taken from the library. The names are whatever the runtime and the operating system have for the culture,
/// so a test compares with these and never spells a name out.
/// </summary>
internal static class CultureCalendars
{
    /// <summary>True when the months the culture names are those of the Gregorian calendar.</summary>
    public static bool NamesGregorianMonths(CultureInfo culture) => culture.DateTimeFormat.Calendar is GregorianCalendar;

    /// <summary>The culture's name for month number <paramref name="month"/> of its own calendar. This is what 1.0.0 put in the title.</summary>
    public static string OwnMonthName(CultureInfo culture, int month) => culture.DateTimeFormat.GetMonthName(month);

    /// <summary>
    /// The culture's name for a month of the Gregorian calendar, or null when the culture offers no Gregorian
    /// calendar. For a culture whose calendar is Gregorian that is its own name. For another culture it is read
    /// from a fresh, writable copy of the whole culture whose calendar is set to the Gregorian one, by formatting
    /// the first day of that month: another route than the library's, which copies the date formats only and
    /// asks them for the name.
    /// </summary>
    public static string? GregorianMonthName(CultureInfo culture, int month)
    {
        if (NamesGregorianMonths(culture))
        {
            return OwnMonthName(culture, month);
        }
        // The culture's own kind of Gregorian calendar before the other kinds, which some systems offer and
        // which name the months in English or French.
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
        return new DateTime(2023, month, 1).ToString("MMMM", copy.DateTimeFormat);
    }

    /// <summary>
    /// The cultures the tests of the title name one by one: the invariant culture, cultures whose calendar is
    /// Gregorian, cultures whose calendar is not on the systems known, and cultures given one of the other
    /// calendars they offer (<c>name+CalendarType</c>), which are the same on every system that has the culture.
    /// Which calendar a culture has by itself is the system's business: <c>ar-SA</c> counts in the Um Al Qura
    /// calendar on some and in the Gregorian one on others. No test assumes it.
    /// </summary>
    public static readonly string[] Focused =
    {
        "invariant", "en-US", "de-DE", "fr-FR", "ja-JP", "th-TH", "fa-IR", "ar-SA", "he-IL", "ps-AF",
        "ar-SA+UmAlQuraCalendar", "ar-SA+HijriCalendar", "he-IL+HebrewCalendar", "ja-JP+JapaneseCalendar",
        "fa-IR+HijriCalendar", "fa-IR+GregorianCalendar", "th-TH+GregorianCalendar", "ko-KR+KoreanCalendar",
        "zh-TW+TaiwanCalendar",
    };

    /// <summary>
    /// The culture a label of <see cref="Focused"/> stands for, or null when this machine does not have it.
    /// </summary>
    public static CultureInfo? Resolve(string label)
    {
        if (label == "invariant")
        {
            return CultureInfo.InvariantCulture;
        }
        int plus = label.IndexOf('+', StringComparison.Ordinal);
        if (plus < 0)
        {
            return TestCultures.TryGet(label);
        }
        string calendarType = label.Substring(plus + 1);
        CultureInfo? culture = TestCultures.TryGet(label.Substring(0, plus));
        Calendar? calendar = culture?.OptionalCalendars.FirstOrDefault(optional => optional.GetType().Name == calendarType);
        if (culture == null || calendar == null)
        {
            return null;
        }
        var copy = (CultureInfo)culture.Clone();
        copy.DateTimeFormat.Calendar = calendar;
        return copy;
    }

    /// <summary>True when the culture's own name for some month number is not its name for that Gregorian month.</summary>
    public static bool NamesOtherMonths(CultureInfo culture) =>
        Enumerable.Range(1, 12).Any(month => !string.Equals(OwnMonthName(culture, month), GregorianMonthName(culture, month), StringComparison.Ordinal));

    /// <summary>
    /// A writable copy of a culture of the operating system whose dates use one of the calendars the culture
    /// offers besides its default, or null when the machine has no such culture or the culture does not offer
    /// that calendar. This is how a program makes, for example, an Arabic culture count in the Um Al Qura
    /// calendar, and it gives the tests a calendar that is not Gregorian on every system.
    /// </summary>
    public static CultureInfo? WithCalendar<TCalendar>(string cultureName)
        where TCalendar : Calendar => Resolve(cultureName + "+" + typeof(TCalendar).Name);

    /// <summary>
    /// The first culture this machine has whose dates use a calendar that is not Gregorian and whose months
    /// are not the Gregorian ones, made by giving a culture one of its other calendars; null when there is none
    /// (with globalization-invariant mode, for example).
    /// </summary>
    public static CultureInfo? AnyThatNamesOtherMonths() => LabelOfAnyThatNamesOtherMonths() is string label ? Resolve(label) : null;

    /// <summary>The label of that culture, for <see cref="Resolve"/>, or null.</summary>
    public static string? LabelOfAnyThatNamesOtherMonths() =>
        new[] { "he-IL+HebrewCalendar", "ar-SA+UmAlQuraCalendar", "ar-SA+HijriCalendar", "fa-IR+HijriCalendar", "fa-IR" }
            .FirstOrDefault(label => Resolve(label) is CultureInfo culture && !NamesGregorianMonths(culture) && NamesOtherMonths(culture));

    /// <summary>Everything about a culture's dates that drawing a month must leave as it was.</summary>
    public static string StateOf(CultureInfo culture) => string.Join("|", new[]
    {
        culture.Name,
        culture.IsReadOnly.ToString(),
        culture.Calendar.GetType().Name,
        culture.DateTimeFormat.Calendar.GetType().Name,
        culture.DateTimeFormat.IsReadOnly.ToString(),
        string.Join(",", culture.DateTimeFormat.MonthNames),
        string.Join(",", culture.DateTimeFormat.MonthGenitiveNames),
        string.Join(",", culture.DateTimeFormat.DayNames),
        culture.DateTimeFormat.FirstDayOfWeek.ToString(),
        culture.DateTimeFormat.YearMonthPattern,
        string.Join(",", culture.OptionalCalendars.Select(calendar => calendar.GetType().Name)),
    });
}

/// <summary>
/// A culture that says which calendars it offers, for the cases no culture of an operating system is known to
/// show: one that offers no Gregorian calendar, and one that offers several kinds of it. Its dates and its
/// names are those of the culture it is made from.
/// </summary>
internal sealed class CultureOffering : CultureInfo
{
    private readonly DateTimeFormatInfo _format;
    private readonly Calendar[] _offered;

    public CultureOffering(CultureInfo culture, params Calendar[] offered)
        : base(culture.Name)
    {
        _format = culture.DateTimeFormat;
        _offered = offered;
    }

    public override DateTimeFormatInfo DateTimeFormat
    {
        get => _format;
        set => throw new NotSupportedException();
    }

    public override Calendar Calendar => _format.Calendar;

    public override Calendar[] OptionalCalendars => (Calendar[])_offered.Clone();
}
