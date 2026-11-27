using CalendarGenerator;
using ConsoleCalendarGenerator.Tests.Support;

namespace ConsoleCalendarGenerator.Tests.Legacy;

/// <summary>GenerateYear: the twelve months of GenerateMonth, one below the other, and nothing else.</summary>
[TestFixture]
public class GenerateYearTests
{
    private static readonly List<DateTime> Selection = new()
    {
        new DateTime(2023, 3, 5),
        new DateTime(2023, 7, 4),
        // Other years, on days that are not selected in 2023.
        new DateTime(2022, 7, 20),
        new DateTime(2024, 3, 25),
        new DateTime(2024, 2, 29),
    };

    [TestCase(2023)]
    [TestCase(2024)]
    [TestCase(1)]
    [TestCase(9999)]
    public void IsExactlyGenerateMonthForMonthsOneThroughTwelve(int year)
    {
        var calendar = new ConsoleCalendar();

        Captured wholeYear = ConsoleCapture.Run(() => calendar.GenerateYear(year));
        Captured monthByMonth = ConsoleCapture.Run(() =>
        {
            for (int month = 1; month <= 12; month++)
            {
                calendar.GenerateMonth(month, year);
            }
        });

        Assert.That(wholeYear.Text, Is.EqualTo(monthByMonth.Text));
        Assert.That(wholeYear.Transcript, Is.EqualTo(monthByMonth.Transcript), "the same individual writes");
    }

    [Test]
    public void WithSelectedDates_IsExactlyGenerateMonthWithTheSameList()
    {
        var calendar = new ConsoleCalendar();

        Captured wholeYear = ConsoleCapture.Run(() => calendar.GenerateYear(2023, Selection));
        Captured monthByMonth = ConsoleCapture.Run(() =>
        {
            for (int month = 1; month <= 12; month++)
            {
                calendar.GenerateMonth(month, 2023, Selection);
            }
        });

        // Events include the colors in effect at every write, wherever the platform lets a test read them.
        Assert.That(wholeYear.Events, Is.EqualTo(monthByMonth.Events));
    }

    [Test]
    public void WritesTwelveMonthsInCalendarOrder_WithNoYearHeading()
    {
        string text = ConsoleCapture.Run(
            () => new ConsoleCalendar().GenerateYear(2023),
            culture: TestCultures.WithMonthNames(month => $"M{month:00}")).Text;

        string[] titles = text.Split('\n').Where(line => line.Contains('M') && !line.StartsWith("Mo ")).ToArray();

        Assert.That(titles, Is.EqualTo(Enumerable.Range(1, 12).Select(month => $"     M{month:00} 2023")));
        Assert.That(text, Does.StartWith("     M01 2023\nMo Tu We Th Fr Sa Su\n").Using(StringComparison.Ordinal), "the first line is January's title");
        Assert.That(text, Is.EqualTo(LegacyReference.Year(2023, month => $"M{month:00}")));
    }

    [Test]
    public void MonthsAreSeparatedByTheBlankLinesThatEndEachMonth()
    {
        string text = ConsoleCapture.Run(() => new ConsoleCalendar().GenerateYear(2023)).Text;

        Assert.That(text, Ordinal.Contains("30 31\n\n   February 2023\n"), "one blank line after January");
        Assert.That(text, Ordinal.Contains("24 25 26 27 28 29 30\n\n\n     May 2023\n"), "two after April, which ends on a Sunday");
        Assert.That(text, Does.EndWith("25 26 27 28 29 30 31\n\n\n").Using(StringComparison.Ordinal), "December 2023 ends on a Sunday as well");
    }

    [Test]
    public void WithoutAList_IsTheSameAsWithAnEmptyList()
    {
        var calendar = new ConsoleCalendar();

        Captured withoutList = ConsoleCapture.Run(() => calendar.GenerateYear(2023));
        Captured withEmptyList = ConsoleCapture.Run(() => calendar.GenerateYear(2023, new List<DateTime>()));

        Assert.That(withoutList.Events, Is.EqualTo(withEmptyList.Events));
    }

    [Test]
    [Platform(Exclude = "Win", Reason = ConsoleState.ColorsObservableOnUnixOnly)]
    public void SelectedDates_AreHighlightedOnlyInTheirOwnMonthOfTheRenderedYear()
    {
        Captured captured = ConsoleCapture.Run(() => new ConsoleCalendar().GenerateYear(2023, Selection));

        Assert.That(SelectedDays.ByMonth(captured, ConsoleColor.Blue), Is.EqualTo(new[] { (3, 5), (7, 4) }),
            "5 March and 4 July 2023; the 2022 and 2024 dates are ignored");
    }
}
