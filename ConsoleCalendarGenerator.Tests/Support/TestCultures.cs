using System.Globalization;

namespace ConsoleCalendarGenerator.Tests.Support;

/// <summary>Cultures for tests that must not depend on the operating system's culture data.</summary>
internal static class TestCultures
{
    /// <summary>A copy of the invariant culture with the twelve month names replaced.</summary>
    public static CultureInfo WithMonthNames(Func<int, string> monthName)
    {
        var culture = (CultureInfo)CultureInfo.InvariantCulture.Clone();
        string[] names = new string[13];
        for (int month = 1; month <= 12; month++)
        {
            names[month - 1] = monthName(month);
        }
        names[12] = string.Empty;
        culture.DateTimeFormat.MonthNames = names;
        return culture;
    }

    /// <summary>
    /// A copy of the invariant culture whose week starts on Saturday and whose day names are not English.
    /// </summary>
    public static CultureInfo WithForeignWeek()
    {
        var culture = (CultureInfo)CultureInfo.InvariantCulture.Clone();
        culture.DateTimeFormat.FirstDayOfWeek = DayOfWeek.Saturday;
        culture.DateTimeFormat.DayNames = new[] { "Dimanche", "Lundi", "Mardi", "Mercredi", "Jeudi", "Vendredi", "Samedi" };
        culture.DateTimeFormat.AbbreviatedDayNames = new[] { "dim", "lun", "mar", "mer", "jeu", "ven", "sam" };
        culture.DateTimeFormat.ShortestDayNames = new[] { "di", "lu", "ma", "me", "je", "ve", "sa" };
        return culture;
    }

    /// <summary>
    /// The operating system's culture of that name, or null where it is not available (for example with
    /// globalization-invariant mode).
    /// </summary>
    public static CultureInfo? TryGet(string name)
    {
        try
        {
            CultureInfo culture = CultureInfo.GetCultureInfo(name);
            return string.Equals(culture.Name, name, StringComparison.OrdinalIgnoreCase) ? culture : null;
        }
        catch (CultureNotFoundException)
        {
            return null;
        }
    }
}
