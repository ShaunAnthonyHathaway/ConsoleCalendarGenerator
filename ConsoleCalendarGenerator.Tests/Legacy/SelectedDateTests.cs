using CalendarGenerator;
using ConsoleCalendarGenerator.Tests.Support;

namespace ConsoleCalendarGenerator.Tests.Legacy;

/// <summary>
/// Selected dates. A selected day is written in the selected-date color and is otherwise identical: selection
/// never changes the text. Which days are highlighted can therefore only be checked where console colors can be
/// read back; the tests that need that are marked for Linux and macOS.
/// </summary>
[TestFixture]
public class SelectedDateTests
{
    private const ConsoleColor Selected = ConsoleColor.Blue;

    private static Captured February2023(List<DateTime> dates) =>
        ConsoleCapture.Run(() => new ConsoleCalendar().GenerateMonth(2, 2023, dates));

    // ---- on every platform ----

    [Test]
    public void Selection_DoesNotChangeTheTextOrTheIndividualWrites()
    {
        Captured plain = ConsoleCapture.Run(() => new ConsoleCalendar().GenerateMonth(2, 2023));
        Captured selected = February2023(new List<DateTime> { new DateTime(2023, 2, 1), new DateTime(2023, 2, 14), new DateTime(2023, 2, 28) });

        Assert.That(selected.Text, Is.EqualTo(plain.Text));
        Assert.That(selected.Transcript, Is.EqualTo(plain.Transcript));
    }

    [Test]
    public void AnEmptyList_IsTheSameAsTheOverloadWithoutAList()
    {
        Captured withoutList = ConsoleCapture.Run(() => new ConsoleCalendar().GenerateMonth(2, 2023));
        Captured withEmptyList = February2023(new List<DateTime>());

        Assert.That(withEmptyList.Events, Is.EqualTo(withoutList.Events));
    }

    [Test]
    public void TheCallersList_IsNotChanged()
    {
        var dates = new List<DateTime>
        {
            new DateTime(2023, 2, 28),
            new DateTime(2023, 2, 14, 9, 30, 0),
            new DateTime(2023, 2, 14),
            new DateTime(2024, 2, 14),
            new DateTime(2023, 2, 1),
        };
        DateTime[] before = dates.ToArray();
        var calendar = new ConsoleCalendar();

        ConsoleCapture.Run(() =>
        {
            calendar.GenerateMonth(2, 2023, dates);
            calendar.GenerateYear(2023, dates);
        });

        Assert.That(dates, Is.EqualTo(before), "same dates, same order, duplicates kept");
    }

    // ---- where colors can be read back ----

    [Test]
    [Platform(Exclude = "Win", Reason = ConsoleState.ColorsObservableOnUnixOnly)]
    public void ADateInTheRenderedMonth_IsHighlighted()
    {
        Assert.That(SelectedDays.Of(February2023(new List<DateTime> { new DateTime(2023, 2, 14) }), Selected), Is.EqualTo(new[] { 14 }));
    }

    [Test]
    [Platform(Exclude = "Win", Reason = ConsoleState.ColorsObservableOnUnixOnly)]
    public void WithoutSelection_NothingIsHighlighted()
    {
        Assert.That(SelectedDays.Of(February2023(new List<DateTime>()), Selected), Is.Empty);
    }

    [Test]
    [Platform(Exclude = "Win", Reason = ConsoleState.ColorsObservableOnUnixOnly)]
    public void TheTimeOfDay_IsIgnored()
    {
        var dates = new List<DateTime> { new DateTime(2023, 2, 14, 0, 0, 0), new DateTime(2023, 2, 15, 23, 59, 59, 999) };

        Assert.That(SelectedDays.Of(February2023(dates), Selected), Is.EqualTo(new[] { 14, 15 }));
    }

    /// <summary>No time zone conversion: the date is taken as written, whatever its kind.</summary>
    [TestCase(DateTimeKind.Unspecified)]
    [TestCase(DateTimeKind.Utc)]
    [TestCase(DateTimeKind.Local)]
    [Platform(Exclude = "Win", Reason = ConsoleState.ColorsObservableOnUnixOnly)]
    public void TheDateTimeKind_IsIgnored(DateTimeKind kind)
    {
        var dates = new List<DateTime>
        {
            new DateTime(2023, 2, 14, 0, 30, 0, kind),
            new DateTime(2023, 2, 15, 23, 30, 0, kind),
        };

        Assert.That(SelectedDays.Of(February2023(dates), Selected), Is.EqualTo(new[] { 14, 15 }));
    }

    [Test]
    [Platform(Exclude = "Win", Reason = ConsoleState.ColorsObservableOnUnixOnly)]
    public void DuplicateDates_HighlightTheDayOnce_LikeASingleDate()
    {
        Captured once = February2023(new List<DateTime> { new DateTime(2023, 2, 14) });
        Captured threeTimes = February2023(new List<DateTime>
        {
            new DateTime(2023, 2, 14),
            new DateTime(2023, 2, 14),
            new DateTime(2023, 2, 14, 18, 0, 0),
        });

        Assert.That(SelectedDays.Of(threeTimes, Selected), Is.EqualTo(new[] { 14 }));
        Assert.That(threeTimes.Events, Is.EqualTo(once.Events));
    }

    [Test]
    [Platform(Exclude = "Win", Reason = ConsoleState.ColorsObservableOnUnixOnly)]
    public void TheOrderOfTheList_DoesNotMatter()
    {
        Captured ascending = February2023(new List<DateTime> { new DateTime(2023, 2, 1), new DateTime(2023, 2, 14), new DateTime(2023, 2, 28) });
        Captured shuffled = February2023(new List<DateTime> { new DateTime(2023, 2, 28), new DateTime(2023, 2, 1), new DateTime(2023, 2, 14) });

        Assert.That(SelectedDays.Of(shuffled, Selected), Is.EqualTo(new[] { 1, 14, 28 }));
        Assert.That(shuffled.Events, Is.EqualTo(ascending.Events));
    }

    [Test]
    [Platform(Exclude = "Win", Reason = ConsoleState.ColorsObservableOnUnixOnly)]
    public void DatesInAnotherMonthOrAnotherYear_AreIgnored()
    {
        var dates = new List<DateTime>
        {
            new DateTime(2023, 1, 14),
            new DateTime(2023, 3, 14),
            new DateTime(2022, 2, 14),
            new DateTime(2024, 2, 14),
            new DateTime(2024, 2, 29),
            DateTime.MinValue,
            DateTime.MaxValue,
        };

        Assert.That(SelectedDays.Of(February2023(dates), Selected), Is.Empty);
    }

    [Test]
    [Platform(Exclude = "Win", Reason = ConsoleState.ColorsObservableOnUnixOnly)]
    public void TheFirstAndTheLastDayOfAMonth_CanBeSelected()
    {
        var dates = new List<DateTime> { new DateTime(2024, 2, 1), new DateTime(2024, 2, 29) };

        Captured captured = ConsoleCapture.Run(() => new ConsoleCalendar().GenerateMonth(2, 2024, dates));

        Assert.That(SelectedDays.Of(captured, Selected), Is.EqualTo(new[] { 1, 29 }));
    }

    [Test]
    [Platform(Exclude = "Win", Reason = ConsoleState.ColorsObservableOnUnixOnly)]
    public void TheSelectedColor_ComesFromTheCalendar()
    {
        var calendar = new ConsoleCalendar();
        calendar.SetSelectedDateColor(ConsoleColor.DarkMagenta);

        Captured captured = ConsoleCapture.Run(() => calendar.GenerateMonth(2, 2023, new List<DateTime> { new DateTime(2023, 2, 14) }));

        Assert.That(SelectedDays.Of(captured, ConsoleColor.DarkMagenta), Is.EqualTo(new[] { 14 }));
        Assert.That(SelectedDays.Of(captured, ConsoleColor.Blue), Is.Empty);
    }
}
