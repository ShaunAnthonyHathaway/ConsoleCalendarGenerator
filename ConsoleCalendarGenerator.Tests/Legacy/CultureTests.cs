using System.Globalization;
using CalendarGenerator;
using ConsoleCalendarGenerator.Tests.Support;

namespace ConsoleCalendarGenerator.Tests.Legacy;

/// <summary>
/// Culture. Only the month name follows the culture, and it follows CultureInfo.CurrentCulture. The weekday row,
/// the first day of the week, the day grid and the year do not.
///
/// The tests that need exact strings use cultures built in the test, so they do not depend on the operating
/// system's culture data. The tests that use real cultures compare with what that culture reports at run time.
/// </summary>
[TestFixture]
public class CultureTests
{
    private static string Month(int month, int year, CultureInfo culture, CultureInfo? uiCulture = null) =>
        ConsoleCapture.Run(() => new ConsoleCalendar().GenerateMonth(month, year), culture: culture, uiCulture: uiCulture).Text;

    [Test]
    public void TheMonthName_ComesFromCurrentCulture()
    {
        CultureInfo culture = TestCultures.WithMonthNames(month => "Mois" + month);

        Assert.That(Month(5, 2023, culture), Does.StartWith("    Mois5 2023\n").Using(StringComparison.Ordinal));
        Assert.That(Month(12, 2023, culture), Does.StartWith("    Mois12 2023\n").Using(StringComparison.Ordinal));
    }

    [Test]
    public void CurrentUICulture_DoesNotAffectTheMonthName()
    {
        CultureInfo other = TestCultures.WithMonthNames(month => "Mois" + month);

        string text = Month(5, 2023, culture: CultureInfo.InvariantCulture, uiCulture: other);

        Assert.That(text, Does.StartWith("     May 2023\n").Using(StringComparison.Ordinal));
    }

    [Test]
    public void TheWeekdayRow_StaysEnglishAndMondayFirst_WhateverTheCultureSays()
    {
        CultureInfo culture = TestCultures.WithForeignWeek();

        string text = Month(2, 2023, culture);

        Assert.That(culture.DateTimeFormat.FirstDayOfWeek, Is.EqualTo(DayOfWeek.Saturday));
        Assert.That(text, Is.EqualTo(LegacyReference.Month(2023, 2, "February")));
        Assert.That(text.Split('\n')[1], Is.EqualTo("Mo Tu We Th Fr Sa Su"));
        Assert.That(text.Split('\n')[2], Is.EqualTo("       1  2  3  4  5"), "1 February 2023, a Wednesday, is in the third column");
    }

    [TestCase(1)]
    [TestCase(99)]
    [TestCase(2023)]
    [TestCase(9999)]
    public void TheYear_IsTheArgumentWrittenAsAPlainNumber(int year)
    {
        Assert.That(Month(1, year, CultureInfo.InvariantCulture).Split('\n')[0], Is.EqualTo("    January " + year));
    }

    // ---- real cultures ----

    [TestCase("en-US")]
    [TestCase("en-GB")]
    [TestCase("de-DE")]
    [TestCase("fr-FR")]
    [TestCase("es-ES")]
    [TestCase("ja-JP")]
    [TestCase("th-TH")]
    [TestCase("fa-IR")]
    public void InARealCulture_OnlyTheMonthNameInTheTitleChanges(string cultureName)
    {
        CultureInfo culture = RequireCulture(cultureName);

        foreach (int month in new[] { 1, 5, 9, 12 })
        {
            string name = culture.DateTimeFormat.GetMonthName(month);

            Assert.That(Month(month, 2023, culture), Is.EqualTo(LegacyReference.Month(2023, month, name)),
                $"{cultureName}, month {month}: the culture's name for month number {month}, then the unchanged English grid");
        }
    }

    /// <summary>
    /// The month number is looked up in the culture's own calendar while the grid stays Gregorian. In a culture
    /// whose calendar is not Gregorian the title therefore names a different month than the one drawn. This is
    /// the behavior of 1.0.0, recorded here so that it is changed deliberately or not at all.
    /// </summary>
    [TestCase("fa-IR")]
    public void InACultureWithAnotherCalendar_TheTitleNamesThatCalendarsMonthOfTheSameNumber(string cultureName)
    {
        CultureInfo culture = RequireCulture(cultureName);
        Assume.That(culture.DateTimeFormat.Calendar, Is.Not.InstanceOf<GregorianCalendar>(), cultureName + " uses the Gregorian calendar here.");
        var firstOfSeptember = new DateTime(2023, 9, 1);

        string text = Month(9, 2023, culture);
        string title = text.Split('\n')[0];

        Assert.That(title, Is.EqualTo(LegacyReference.Title(culture.DateTimeFormat.GetMonthName(9), 2023)));
        Assert.That(title, Ordinal.DoesNotContain(firstOfSeptember.ToString("MMMM", culture)),
            "the title is not the culture's name for the month that contains 1 September 2023");
        Assert.That(text.Substring(text.IndexOf('\n') + 1), Is.EqualTo(GridOf(LegacyReference.Month(2023, 9, "September"))),
            "the grid is Gregorian September 2023");
    }

    [TestCase("th-TH")]
    [TestCase("fa-IR")]
    public void InACultureWithAnotherCalendar_TheYearIsStillTheGregorianArgument(string cultureName)
    {
        CultureInfo culture = RequireCulture(cultureName);
        Assume.That(culture.DateTimeFormat.Calendar, Is.Not.InstanceOf<GregorianCalendar>(), cultureName + " uses the Gregorian calendar here.");

        string title = Month(9, 2023, culture).Split('\n')[0];

        Assert.That(title, Does.EndWith(" 2023").Using(StringComparison.Ordinal));
        Assert.That(new DateTime(2023, 9, 1).ToString("yyyy", culture), Is.Not.EqualTo("2023"), "the culture itself writes another year");
    }

    private static string GridOf(string month) => month.Substring(month.IndexOf('\n') + 1);

    private static CultureInfo RequireCulture(string name)
    {
        CultureInfo? culture = TestCultures.TryGet(name);
        Assume.That(culture, Is.Not.Null, "The culture " + name + " is not available on this machine.");
        return culture!;
    }
}
