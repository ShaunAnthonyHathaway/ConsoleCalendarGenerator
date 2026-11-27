using System.Globalization;
using CalendarGenerator;
using ConsoleCalendarGenerator.Tests.Support;

namespace ConsoleCalendarGenerator.Tests.Legacy;

/// <summary>
/// The title names the month that is drawn (D-61). The grid is always the Gregorian month, and since version
/// 2.0 the name above it is the current culture's name for that Gregorian month, also under a culture whose
/// dates use another calendar. Version 1.0.0 took the name of the same month number from the culture's own
/// calendar; what it did is recorded in Historical/Published100CultureTests.cs, and the two are held side by
/// side in Migration/GregorianTitleMigrationTests.cs.
///
/// <para>
/// Month names come from the operating system's culture data and differ between systems, so no name of a real
/// culture is spelled out here. An expectation is computed from the culture at run time, by
/// <see cref="CultureCalendars"/>, which takes another route than the library does. A culture that a machine
/// does not have makes its test inconclusive. The cultures written <c>name+CalendarType</c> are a culture given
/// one of the other calendars it offers, which is the same on every system that has the culture.
/// </para>
/// </summary>
[TestFixture]
public class GregorianTitleTests
{
    private static string Month(int month, int year, CultureInfo culture, CultureInfo? uiCulture = null) =>
        ConsoleCapture.Run(() => new ConsoleCalendar().GenerateMonth(month, year), culture: culture, uiCulture: uiCulture).Text;

    private static string TitleOf(string text) => text.Substring(0, text.IndexOf('\n'));

    private static string BelowTheTitle(string text) => text.Substring(text.IndexOf('\n'));

    private static string English(int month) => CultureInfo.InvariantCulture.DateTimeFormat.GetMonthName(month);

    private static IEnumerable<string> Focused() => CultureCalendars.Focused;

    private static CultureInfo Require(string label)
    {
        CultureInfo? culture = CultureCalendars.Resolve(label);
        Assume.That(culture, Is.Not.Null, "This machine does not have " + label + ".");
        return culture!;
    }

    private static CultureInfo RequireOneThatNamesOtherMonths()
    {
        CultureInfo? culture = CultureCalendars.AnyThatNamesOtherMonths();
        Assume.That(culture, Is.Not.Null, "This machine has no culture whose calendar names other months than the Gregorian one.");
        return culture!;
    }

    private static string GregorianName(CultureInfo culture, int month)
    {
        string? name = CultureCalendars.GregorianMonthName(culture, month);
        Assert.That(name, Is.Not.Null, culture.Name + " offers no Gregorian calendar.");
        return name!;
    }

    // ---- the title is the culture's name for the Gregorian month ----

    [TestCaseSource(nameof(Focused))]
    public void TheTitle_IsTheCulturesNameForTheGregorianMonth_AboveTheEnglishGregorianGrid(string label)
    {
        CultureInfo culture = Require(label);

        for (int month = 1; month <= 12; month++)
        {
            string name = GregorianName(culture, month);
            string text = Month(month, 2023, culture);

            Assert.That(TitleOf(text), Is.EqualTo(LegacyReference.Title(name, 2023)), $"{label}, month {month}");
            Assert.That(text, Is.EqualTo(LegacyReference.Month(2023, month, name)), $"{label}, month {month}");
            Assert.That(BelowTheTitle(text), Is.EqualTo(BelowTheTitle(LegacyReference.Month(2023, month, English(month)))),
                "everything below the title is the Gregorian month, with the English weekday row and weeks from Monday");
        }
    }

    /// <summary>Where the culture's dates use the Gregorian calendar, the name is the one its formats give, as in 1.0.0.</summary>
    [TestCase("invariant")]
    [TestCase("en-US")]
    [TestCase("de-DE")]
    [TestCase("fr-FR")]
    [TestCase("ja-JP")]
    [TestCase("ar-SA")]
    [TestCase("he-IL")]
    [TestCase("fa-IR+GregorianCalendar")]
    [TestCase("th-TH+GregorianCalendar")]
    public void UnderACultureWhoseDatesAreGregorian_TheTitleIsTheNameItsFormatsGiveTheMonth(string label)
    {
        CultureInfo culture = Require(label);
        Assume.That(CultureCalendars.NamesGregorianMonths(culture), Is.True, label + " does not use the Gregorian calendar on this machine.");

        for (int month = 1; month <= 12; month++)
        {
            Assert.That(TitleOf(Month(month, 2023, culture)), Is.EqualTo(LegacyReference.Title(culture.DateTimeFormat.GetMonthName(month), 2023)), $"{label}, month {month}");
        }
    }

    /// <summary>
    /// The correction itself: under a culture whose calendar is not the Gregorian one, the title is the name
    /// the culture gives the days that are drawn, the first and the last of them alike, once its dates are
    /// counted in the Gregorian calendar. It is no longer the culture's name for that month number of its own
    /// calendar, wherever the two differ.
    /// </summary>
    [TestCase("fa-IR")]
    [TestCase("ps-AF")]
    [TestCase("th-TH")]
    [TestCase("ar-SA")]
    [TestCase("ar-SA+UmAlQuraCalendar")]
    [TestCase("ar-SA+HijriCalendar")]
    [TestCase("he-IL+HebrewCalendar")]
    [TestCase("ja-JP+JapaneseCalendar")]
    [TestCase("fa-IR+HijriCalendar")]
    [TestCase("ko-KR+KoreanCalendar")]
    [TestCase("zh-TW+TaiwanCalendar")]
    public void UnderACultureWhoseDatesAreNotGregorian_TheTitleNamesTheMonthOfTheDaysThatAreDrawn(string label)
    {
        CultureInfo culture = Require(label);
        Assume.That(CultureCalendars.NamesGregorianMonths(culture), Is.False, label + " uses the Gregorian calendar on this machine.");
        var gregorian = (CultureInfo)culture.Clone();
        gregorian.DateTimeFormat.Calendar = culture.OptionalCalendars.OfType<GregorianCalendar>().First();

        for (int month = 1; month <= 12; month++)
        {
            string title = TitleOf(Month(month, 2023, culture));
            string nameOfTheFirstDay = new DateTime(2023, month, 1).ToString("MMMM", gregorian);
            string nameOfTheLastDay = new DateTime(2023, month, DateTime.DaysInMonth(2023, month)).ToString("MMMM", gregorian);
            string own = CultureCalendars.OwnMonthName(culture, month);

            Assert.That(nameOfTheLastDay, Is.EqualTo(nameOfTheFirstDay));
            Assert.That(title, Is.EqualTo(LegacyReference.Title(nameOfTheFirstDay, 2023)), $"{label}, month {month}");
            if (own != nameOfTheFirstDay)
            {
                Assert.That(title, Is.Not.EqualTo(LegacyReference.Title(own, 2023)), $"{label}, month {month}: not the name of month {month} of the culture's own calendar");
            }
        }
    }

    /// <summary>
    /// Every culture the runtime has, every month. The numbers this test writes to its output are the evidence
    /// of which cultures the correction touches on the machine it runs on.
    /// </summary>
    [Test]
    public void UnderEveryCultureOfTheRuntime_TheTitleIsItsNameForTheGregorianMonth()
    {
        CultureInfo[] cultures = CultureInfo.GetCultures(CultureTypes.AllCultures);
        Assert.That(cultures, Is.Not.Empty);
        var notGregorian = new List<string>();
        var differing = new List<string>();
        var withoutGregorian = new List<string>();

        foreach (CultureInfo culture in cultures)
        {
            if (!CultureCalendars.NamesGregorianMonths(culture))
            {
                notGregorian.Add(culture.Name + " (" + culture.DateTimeFormat.Calendar.GetType().Name + ")");
            }
            bool differs = false;
            for (int month = 1; month <= 12; month++)
            {
                string? name = CultureCalendars.GregorianMonthName(culture, month);
                if (name == null)
                {
                    name = English(month);
                    if (month == 1)
                    {
                        withoutGregorian.Add(culture.Name);
                    }
                }
                differs |= name != CultureCalendars.OwnMonthName(culture, month);

                Assert.That(TitleOf(Month(month, 2023, culture)), Is.EqualTo(LegacyReference.Title(name, 2023)), $"{culture.Name}, month {month}");
            }
            if (differs)
            {
                differing.Add(culture.Name);
            }
        }

        TestContext.Out.WriteLine("Cultures examined: " + cultures.Length);
        TestContext.Out.WriteLine("Whose dates use a calendar that is not Gregorian: " + notGregorian.Count + ": " + string.Join(", ", notGregorian));
        TestContext.Out.WriteLine("Whose title differs from that of 1.0.0 in at least one month: " + differing.Count + ": " + string.Join(", ", differing));
        TestContext.Out.WriteLine("That offer no Gregorian calendar: " + withoutGregorian.Count + ": " + string.Join(", ", withoutGregorian));
        Assert.That(differing, Is.SubsetOf(notGregorian.Select(entry => entry.Substring(0, entry.IndexOf(' ', StringComparison.Ordinal))).ToArray()),
            "no title changes under a culture whose dates use the Gregorian calendar");
    }

    // ---- which names are used ----

    /// <summary>
    /// A program can set month names on a culture. Under a culture whose dates are Gregorian they are names of
    /// Gregorian months and are used, as before (Legacy/CultureTests.cs). Set on a culture whose dates use
    /// another calendar they are names of that calendar's months, and are not given to the Gregorian month.
    /// </summary>
    [Test]
    public void NamesAProgramSetForTheMonthsOfAnotherCalendar_AreNotGivenToTheGregorianMonth()
    {
        CultureInfo other = RequireOneThatNamesOtherMonths();
        var culture = (CultureInfo)other.Clone();
        string[] names = Enumerable.Range(1, 13).Select(month => "own" + month).ToArray();
        culture.DateTimeFormat.MonthNames = names;

        for (int month = 1; month <= 12; month++)
        {
            Assert.That(TitleOf(Month(month, 2023, culture)), Is.EqualTo(LegacyReference.Title(GregorianName(other, month), 2023)), $"month {month}");
        }
        Assert.That(culture.DateTimeFormat.MonthNames, Is.EqualTo(names), "the names the program set are still there");
    }

    /// <summary>
    /// A program can give one culture the date formats of another. The name follows the formats, which is where
    /// 1.0.0 took it from as well: their language, and their name for the Gregorian month.
    /// </summary>
    [Test]
    public void UnderACultureGivenTheDateFormatsOfAnother_TheTitleIsTheGregorianNameOfThoseFormats()
    {
        CultureInfo other = RequireOneThatNamesOtherMonths();
        CultureInfo? english = TestCultures.TryGet("en-US");
        Assume.That(english, Is.Not.Null, "This machine does not have en-US.");
        var mixed = (CultureInfo)english!.Clone();
        mixed.DateTimeFormat = (DateTimeFormatInfo)other.DateTimeFormat.Clone();

        for (int month = 1; month <= 12; month++)
        {
            Assert.That(TitleOf(Month(month, 2023, mixed)), Is.EqualTo(LegacyReference.Title(GregorianName(other, month), 2023)), $"month {month}");
        }
    }

    // ---- cultures that no operating system is known to have ----

    /// <summary>
    /// A culture that offers no Gregorian calendar. None is known, on any system; this one is made here. The
    /// title is then the invariant name of the month drawn: the right month in English, and never a month of
    /// another calendar.
    /// </summary>
    [Test]
    public void UnderACultureThatOffersNoGregorianCalendar_TheTitleIsTheInvariantNameOfTheMonthDrawn()
    {
        CultureInfo other = RequireOneThatNamesOtherMonths();
        var culture = new CultureOffering(other, other.OptionalCalendars.Where(calendar => calendar is not GregorianCalendar).ToArray());
        Assert.That(culture.OptionalCalendars.OfType<GregorianCalendar>(), Is.Empty);
        Assert.That(culture.DateTimeFormat.Calendar, Is.Not.InstanceOf<GregorianCalendar>());

        Assert.That(TitleOf(Month(9, 2023, culture)), Is.EqualTo("   September 2023"));
        for (int month = 1; month <= 12; month++)
        {
            Assert.That(Month(month, 2023, culture), Is.EqualTo(LegacyReference.Month(2023, month, English(month))), $"month {month}");
        }
    }

    /// <summary>
    /// Some systems offer several kinds of Gregorian calendar for a culture, and the kinds other than the
    /// culture's own name the months in English or French. The culture's own kind is used, wherever it stands
    /// in the list.
    /// </summary>
    [Test]
    public void UnderACultureThatOffersSeveralKindsOfGregorianCalendar_TheTitleIsItsOwnNameForTheMonth()
    {
        CultureInfo other = RequireOneThatNamesOtherMonths();
        var culture = new CultureOffering(
            other,
            other.DateTimeFormat.Calendar,
            new GregorianCalendar(GregorianCalendarTypes.USEnglish),
            new GregorianCalendar(GregorianCalendarTypes.MiddleEastFrench),
            new GregorianCalendar(GregorianCalendarTypes.Localized));

        for (int month = 1; month <= 12; month++)
        {
            Assert.That(TitleOf(Month(month, 2023, culture)), Is.EqualTo(LegacyReference.Title(GregorianName(other, month), 2023)), $"month {month}");
        }
    }

    /// <summary>
    /// A culture can offer a Gregorian calendar that its date formats do not take, when the formats are not its
    /// own. Version 1.0.0 threw nothing when it looked up the name, and neither does 2.0: the title is the name
    /// the formats give once they take the calendar, and the invariant name of the month where they refuse it.
    /// </summary>
    [Test]
    public void UnderACultureWhoseFormatsRefuseTheGregorianCalendarItOffers_NothingIsThrown_AndTheMonthIsStillTheOneDrawn()
    {
        CultureInfo other = RequireOneThatNamesOtherMonths();
        var offered = new GregorianCalendar(GregorianCalendarTypes.USEnglish);
        var culture = new CultureOffering(other, other.DateTimeFormat.Calendar, offered);
        var formats = (DateTimeFormatInfo)other.DateTimeFormat.Clone();
        bool taken = true;
        try
        {
            formats.Calendar = offered;
        }
        catch (ArgumentException)
        {
            taken = false;
        }

        for (int month = 1; month <= 12; month++)
        {
            string name = taken ? formats.GetMonthName(month) : English(month);

            Assert.That(Month(month, 2023, culture), Is.EqualTo(LegacyReference.Month(2023, month, name)), $"month {month}");
        }
    }

    // ---- the current culture, and nothing else ----

    [Test]
    public void TheCurrentCulture_DecidesTheLanguageOfTheTitle_AndTheCurrentUICultureAloneDoesNot()
    {
        CultureInfo other = RequireOneThatNamesOtherMonths();
        CultureInfo? english = TestCultures.TryGet("en-US");

        // Only the UI culture is another one: the title is the invariant one.
        Assert.That(Month(9, 2023, CultureInfo.InvariantCulture, uiCulture: other), Is.EqualTo(LegacyReference.Month(2023, 9, "September")));
        Assert.That(Month(9, 2023, CultureInfo.InvariantCulture, uiCulture: TestCultures.WithMonthNames(month => "ui" + month)), Is.EqualTo(LegacyReference.Month(2023, 9, "September")));

        // Only the UI culture changes: the title stays that of the current culture.
        string expected = LegacyReference.Month(2023, 9, GregorianName(other, 9));
        Assert.That(Month(9, 2023, other, uiCulture: CultureInfo.InvariantCulture), Is.EqualTo(expected));
        Assert.That(Month(9, 2023, other, uiCulture: other), Is.EqualTo(expected));
        if (english != null)
        {
            Assert.That(Month(9, 2023, other, uiCulture: english), Is.EqualTo(expected));
        }
    }

    /// <summary>
    /// Drawing reads the culture and changes nothing: the current culture and the current UI culture are the
    /// same objects afterwards, the culture has the same formats and the formats the same calendar, and every
    /// name and setting is as it was. That holds for the shared, read-only cultures of the runtime and for the
    /// writable copies alike.
    /// </summary>
    [TestCaseSource(nameof(Focused))]
    public void DrawingAYear_LeavesTheCultureItsFormatsAndItsCalendarAsTheyWere(string label)
    {
        CultureInfo culture = Require(label);
        CultureInfo ui = TestCultures.WithMonthNames(month => "ui" + month);
        DateTimeFormatInfo formats = culture.DateTimeFormat;
        Calendar calendarOfTheFormats = formats.Calendar;
        Calendar calendarOfTheCulture = culture.Calendar;
        string state = CultureCalendars.StateOf(culture);
        string stateOfTheUICulture = CultureCalendars.StateOf(ui);
        CultureInfo? cultureAfterDrawing = null;
        CultureInfo? uiCultureAfterDrawing = null;

        ConsoleCapture.Run(
            () =>
            {
                new ConsoleCalendar().GenerateYear(2023);
                cultureAfterDrawing = CultureInfo.CurrentCulture;
                uiCultureAfterDrawing = CultureInfo.CurrentUICulture;
            },
            culture: culture,
            uiCulture: ui);

        Assert.That(cultureAfterDrawing, Is.SameAs(culture), "CultureInfo.CurrentCulture is not replaced");
        Assert.That(uiCultureAfterDrawing, Is.SameAs(ui), "CultureInfo.CurrentUICulture is not replaced");
        Assert.That(culture.DateTimeFormat, Is.SameAs(formats), "the culture keeps its formats");
        Assert.That(formats.Calendar, Is.SameAs(calendarOfTheFormats), "the formats keep their calendar");
        Assert.That(culture.Calendar, Is.SameAs(calendarOfTheCulture), "the culture keeps its calendar");
        Assert.That(CultureCalendars.StateOf(culture), Is.EqualTo(state));
        Assert.That(CultureCalendars.StateOf(ui), Is.EqualTo(stateOfTheUICulture));
    }

    /// <summary>
    /// What is drawn under one culture leaves nothing behind for the next: each month is what it is when drawn
    /// alone, in whatever order the cultures follow each other and with one calendar object for all of them.
    /// </summary>
    [Test]
    public void MonthsDrawnUnderOneCultureAfterAnother_AreEachWhatTheyAreAlone()
    {
        CultureInfo[] cultures = CultureCalendars.Focused.Select(CultureCalendars.Resolve).Where(culture => culture != null).Select(culture => culture!).ToArray();
        Assert.That(cultures, Is.Not.Empty);
        string[] alone = cultures.Select(culture => Month(9, 2023, culture)).ToArray();
        var calendar = new ConsoleCalendar();
        object[] before = ProcessState.Snapshot();

        for (int round = 0; round < 3; round++)
        {
            foreach (int index in round == 1 ? Enumerable.Range(0, cultures.Length).Reverse() : Enumerable.Range(0, cultures.Length))
            {
                string text = ConsoleCapture.Run(() => calendar.GenerateMonth(9, 2023), culture: cultures[index]).Text;

                Assert.That(text, Is.EqualTo(alone[index]), $"round {round}, {CultureCalendars.Focused[index]}");
            }
        }
        Assert.That(ProcessState.Snapshot(), Is.EqualTo(before), "the cultures, the console streams and the rest of the process are as they were");
    }

    // ---- the year ----

    /// <summary>The year in the title is the argument, also where the culture counts years differently.</summary>
    [TestCase(1)]
    [TestCase(99)]
    [TestCase(1800)]
    [TestCase(2023)]
    [TestCase(9999)]
    public void TheYearInTheTitle_IsTheArgument_NotTheCulturesYear_AndNoDateIsConverted(int year)
    {
        string[] labels = { "th-TH", "fa-IR", "ar-SA+UmAlQuraCalendar", "he-IL+HebrewCalendar", "ja-JP+JapaneseCalendar", "zh-TW+TaiwanCalendar" };
        CultureInfo[] cultures = labels.Select(CultureCalendars.Resolve).Where(culture => culture != null && !CultureCalendars.NamesGregorianMonths(culture)).Select(culture => culture!).ToArray();
        Assume.That(cultures, Is.Not.Empty, "This machine has none of these cultures with a calendar that is not Gregorian.");

        foreach (CultureInfo culture in cultures)
        {
            // Years far outside what the culture's own calendar can express are drawn like any other, because
            // no date is ever converted to that calendar.
            string text = Month(9, year, culture);

            Assert.That(text, Is.EqualTo(LegacyReference.Month(year, 9, GregorianName(culture, 9))), culture.Name);
            Assert.That(TitleOf(text), Does.EndWith(" " + year.ToString(CultureInfo.InvariantCulture)).Using(StringComparison.Ordinal));
        }
    }

    // ---- the indent ----

    /// <summary>
    /// The indent still has its three steps, by the number of characters of the name that is written: five
    /// spaces up to 4, four up to 7, three for anything longer. The correction chooses the name and nothing
    /// about the indent.
    /// </summary>
    [TestCase(0, 5)]
    [TestCase(1, 5)]
    [TestCase(4, 5)]
    [TestCase(5, 4)]
    [TestCase(7, 4)]
    [TestCase(8, 3)]
    [TestCase(30, 3)]
    public void TheIndentOfTheTitle_HasItsThreeSteps(int length, int spaces)
    {
        string name = new string('m', length);

        Assert.That(TitleOf(Month(5, 2023, TestCultures.WithMonthNames(_ => name))), Is.EqualTo(new string(' ', spaces) + name + " 2023"));
    }

    /// <summary>
    /// Where the Gregorian name falls on another step than the name 1.0.0 wrote, the indent is the one for the
    /// name that is written now. Looked for in every culture of the runtime and in the focused cultures.
    /// </summary>
    [Test]
    public void WhereTheGregorianNameIsOnAnotherStepThanTheNameOf100_TheIndentIsThatOfTheNameWritten()
    {
        IEnumerable<CultureInfo> cultures = CultureInfo.GetCultures(CultureTypes.AllCultures)
            .Concat(CultureCalendars.Focused.Select(CultureCalendars.Resolve).Where(culture => culture != null).Select(culture => culture!))
            .Where(culture => !CultureCalendars.NamesGregorianMonths(culture));
        int onAnotherStep = 0;

        foreach (CultureInfo culture in cultures)
        {
            for (int month = 1; month <= 12; month++)
            {
                string own = CultureCalendars.OwnMonthName(culture, month);
                string? gregorian = CultureCalendars.GregorianMonthName(culture, month);
                if (gregorian == null || LegacyReference.TitleIndent(own) == LegacyReference.TitleIndent(gregorian))
                {
                    continue;
                }
                onAnotherStep++;

                Assert.That(TitleOf(Month(month, 2023, culture)), Is.EqualTo(LegacyReference.TitleIndent(gregorian) + gregorian + " 2023"), $"{culture.Name}, month {month}");
            }
        }
        Assume.That(onAnotherStep, Is.GreaterThan(0), "This machine has no culture whose two names for a month fall on different steps.");
    }

    /// <summary>
    /// The length is the length of the string, in UTF-16 code units, as it always was: not the number of
    /// characters a reader sees and not the width on the screen. Nothing here centers the title.
    /// </summary>
    [Test]
    public void TheIndent_CountsTheCodeUnitsOfTheName_NotWhatAReaderSees()
    {
        string face = char.ConvertFromUtf32(0x1F600);
        string accentedE = "e" + (char)0x0301;
        // Two characters to a reader, four code units: still five spaces.
        string two = face + face;
        // Three characters to a reader, five code units: four spaces, where three characters would get five.
        string three = accentedE + accentedE + "e";
        // Four characters to a reader, eight code units: three spaces.
        string four = face + face + face + face;

        Assert.That(two.Length, Is.EqualTo(4));
        Assert.That(three.Length, Is.EqualTo(5));
        Assert.That(four.Length, Is.EqualTo(8));
        Assert.That(TitleOf(Month(5, 2023, TestCultures.WithMonthNames(_ => two))), Is.EqualTo("     " + two + " 2023"));
        Assert.That(TitleOf(Month(5, 2023, TestCultures.WithMonthNames(_ => three))), Is.EqualTo("    " + three + " 2023"));
        Assert.That(TitleOf(Month(5, 2023, TestCultures.WithMonthNames(_ => four))), Is.EqualTo("   " + four + " 2023"));
    }
}
