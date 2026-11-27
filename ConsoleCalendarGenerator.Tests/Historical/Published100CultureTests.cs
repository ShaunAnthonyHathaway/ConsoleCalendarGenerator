using System.Globalization;
using ConsoleCalendarGenerator.Tests.Migration;
using ConsoleCalendarGenerator.Tests.Support;

namespace ConsoleCalendarGenerator.Tests.Historical;

/// <summary>
/// The title and the culture in version 1.0.0, complete. The month name in the title was
/// <c>CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(Month)</c>: the name of month number
/// <c>Month</c> in the calendar of the current culture, whatever calendar that is. The grid below it was always
/// the Gregorian month. Under a culture whose calendar is the Gregorian one, as most are, the two agree. Under
/// any other, the title named a month of another calendar: under <c>fa-IR</c>, for September, the ninth month
/// of the Persian calendar.
///
/// <para>
/// Everything here runs the frozen source of 1.0.0. Version 2.0 takes the name from the culture's Gregorian
/// names (D-61); these tests stay as they are, as the record of what was corrected. The corrected behavior is
/// in Legacy/CultureTests.cs, and the two side by side in Migration/GregorianTitleMigrationTests.cs.
/// </para>
/// <para>
/// Month names come from the operating system's culture data, so no name is spelled out here. Every expectation
/// is computed from the culture at run time, and a culture a machine does not have makes its test inconclusive.
/// </para>
/// </summary>
[TestFixture]
public class Published100CultureTests
{
    private static LegacyObservation Month(int month, int year, CultureInfo culture, CultureInfo? uiCulture = null) => LegacyObservation.Of(
        LegacySubject.Published100,
        new LegacyScenario("a month") { Draw = LegacyDraw.Month, Month = month, Year = year, Culture = culture, UICulture = uiCulture },
        culture);

    private static string TitleOf(LegacyObservation observation) => observation.Calls[0].Text;

    private static CultureInfo Require(string name)
    {
        CultureInfo? culture = TestCultures.TryGet(name);
        Assume.That(culture, Is.Not.Null, "The culture " + name + " is not available on this machine.");
        return culture!;
    }

    private static string English(int month) => CultureInfo.InvariantCulture.DateTimeFormat.GetMonthName(month);

    // ---- the title is the name of that month number in the culture's own calendar ----

    [TestCase("")]
    [TestCase("en-US")]
    [TestCase("de-DE")]
    [TestCase("fr-FR")]
    [TestCase("ja-JP")]
    [TestCase("th-TH")]
    [TestCase("fa-IR")]
    [TestCase("ar-SA")]
    [TestCase("he-IL")]
    public void In100_TheTitle_IsTheCulturesOwnNameForThatMonthNumber_AndTheRestIsTheEnglishGregorianGrid(string cultureName)
    {
        CultureInfo culture = Require(cultureName);

        for (int month = 1; month <= 12; month++)
        {
            LegacyObservation observation = Month(month, 2023, culture);

            Assert.That(TitleOf(observation), Is.EqualTo(Published100Text.Title(CultureCalendars.OwnMonthName(culture, month), 2023)), $"{cultureName}, month {month}");
            Assert.That(observation.Text, Is.EqualTo(Published100Text.Month(2023, month, CultureCalendars.OwnMonthName(culture, month))), $"{cultureName}, month {month}");
            Assert.That(observation.Text.Substring(observation.Text.IndexOf('\n')), Is.EqualTo(Published100Text.Month(2023, month, English(month)).Substring(Published100Text.Month(2023, month, English(month)).IndexOf('\n'))),
                "everything below the title is the Gregorian month, with the English weekday row, weeks from Monday");
        }
    }

    /// <summary>
    /// The defect: under a culture whose calendar is the Persian one, the title is not that culture's name for
    /// the Gregorian month drawn. The names are the system's; the test assumes only that the two calendars name
    /// their months differently, which is what makes it a defect.
    /// </summary>
    [Test]
    public void In100_UnderTheNamesOfThePersianCalendar_TheTitleNamedAnotherMonthThanTheOneDrawn()
    {
        CultureInfo persian = Require("fa-IR");
        Assume.That(persian.DateTimeFormat.Calendar, Is.InstanceOf<PersianCalendar>(), "fa-IR does not use the Persian calendar on this machine.");

        int differing = 0;
        for (int month = 1; month <= 12; month++)
        {
            string own = CultureCalendars.OwnMonthName(persian, month);
            string? gregorian = CultureCalendars.GregorianMonthName(persian, month);
            Assume.That(gregorian, Is.Not.Null);

            Assert.That(TitleOf(Month(month, 2023, persian)), Is.EqualTo(Published100Text.Title(own, 2023)), $"month {month}");
            differing += own == gregorian ? 0 : 1;
        }
        Assert.That(differing, Is.EqualTo(12), "no Persian month has the name of the Gregorian month of the same number");
    }

    /// <summary>
    /// The same for a culture a program has given another of the calendars it offers: 1.0.0 named the month of
    /// that calendar. These cultures give the same evidence on a system where no culture defaults to such a
    /// calendar.
    /// </summary>
    [Test]
    public void In100_UnderACultureGivenAnotherOfItsCalendars_TheTitleNamedTheMonthOfThatCalendar()
    {
        CultureInfo?[] cultures =
        {
            CultureCalendars.WithCalendar<UmAlQuraCalendar>("ar-SA"),
            CultureCalendars.WithCalendar<HijriCalendar>("ar-SA"),
            CultureCalendars.WithCalendar<HebrewCalendar>("he-IL"),
            CultureCalendars.WithCalendar<JapaneseCalendar>("ja-JP"),
            CultureCalendars.WithCalendar<PersianCalendar>("fa-IR"),
            CultureCalendars.WithCalendar<HijriCalendar>("fa-IR"),
        };
        CultureInfo[] available = cultures.Where(culture => culture != null).Select(culture => culture!).ToArray();
        Assume.That(available, Is.Not.Empty, "This machine has none of these cultures with another calendar.");

        foreach (CultureInfo culture in available)
        {
            for (int month = 1; month <= 12; month++)
            {
                Assert.That(TitleOf(Month(month, 2023, culture)), Is.EqualTo(Published100Text.Title(CultureCalendars.OwnMonthName(culture, month), 2023)),
                    $"{culture.Name} with {culture.DateTimeFormat.Calendar.GetType().Name}, month {month}");
            }
        }
    }

    /// <summary>Every culture of the runtime, with the calendar it has: the title is its own name for the month number.</summary>
    [Test]
    public void In100_UnderEveryCultureOfTheRuntime_TheTitleIsTheCulturesOwnNameForTheMonthNumber()
    {
        CultureInfo[] cultures = CultureInfo.GetCultures(CultureTypes.AllCultures);
        Assert.That(cultures, Is.Not.Empty);

        foreach (CultureInfo culture in cultures)
        {
            foreach (int month in new[] { 1, 9 })
            {
                Assert.That(TitleOf(Month(month, 2023, culture)), Is.EqualTo(Published100Text.Title(CultureCalendars.OwnMonthName(culture, month), 2023)), culture.Name);
            }
        }
    }

    // ---- what the culture did not decide ----

    /// <summary>The year in the title was the argument, also where the culture counts years differently.</summary>
    [TestCase("th-TH")]
    [TestCase("fa-IR")]
    public void In100_TheYearInTheTitle_WasTheArgument_NotTheCulturesYear(string cultureName)
    {
        CultureInfo culture = Require(cultureName);
        Assume.That(CultureCalendars.NamesGregorianMonths(culture), Is.False, cultureName + " uses the Gregorian calendar on this machine.");

        Assert.That(TitleOf(Month(9, 2023, culture)), Does.EndWith(" 2023").Using(StringComparison.Ordinal));
        Assert.That(new DateTime(2023, 9, 1).ToString("yyyy", culture), Is.Not.EqualTo("2023"), "the culture itself writes another year");
    }

    [Test]
    public void In100_TheCurrentUICulture_HadNoEffect()
    {
        CultureInfo ui = TestCultures.WithMonthNames(month => "ui" + month);

        Assert.That(TitleOf(Month(5, 2023, CultureInfo.InvariantCulture, ui)), Is.EqualTo("     May 2023"));
    }

    /// <summary>The indent followed the length of the name that was written: five spaces up to 4 characters, four up to 7, else three.</summary>
    [TestCase(0, 5)]
    [TestCase(4, 5)]
    [TestCase(5, 4)]
    [TestCase(7, 4)]
    [TestCase(8, 3)]
    [TestCase(30, 3)]
    public void In100_TheIndentOfTheTitle_FollowedTheLengthOfTheName(int length, int spaces)
    {
        string name = new string('m', length);

        Assert.That(TitleOf(Month(5, 2023, TestCultures.WithMonthNames(_ => name))), Is.EqualTo(new string(' ', spaces) + name + " 2023"));
    }

    /// <summary>Drawing left the culture as it was.</summary>
    [Test]
    public void In100_DrawingAMonth_LeftTheCultureAsItWas()
    {
        CultureInfo culture = Require("fa-IR");
        string before = CultureCalendars.StateOf(culture);

        Month(9, 2023, culture);

        Assert.That(CultureCalendars.StateOf(culture), Is.EqualTo(before));
    }
}
