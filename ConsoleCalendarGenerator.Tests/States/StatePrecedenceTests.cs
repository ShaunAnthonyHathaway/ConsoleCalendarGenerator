using ConsoleCalendarGenerator.Tests.Support;

namespace ConsoleCalendarGenerator.Tests.States;

/// <summary>
/// A day has at most one state. When several entries select the same day, the first one in the order of the
/// input decides, and the rest are ignored.
/// </summary>
[TestFixture]
public class StatePrecedenceTests
{
    private static readonly MonthRenderOptions Options = MonthRenderOptions.Default.WithCellWidth(3);
    private static readonly DayState Open = new("open", "*");
    private static readonly DayState Closed = new("closed", "x");
    private static readonly DayState Held = new("held", "?");

    private static KeyValuePair<DateTime, DayState> Entry(DateTime date, DayState state) => new(date, state);

    private static RenderedSegment The14th(RenderedMonth rendered) => rendered.Lines.SelectMany(line => line.Segments)
        .Single(segment => segment.Date == new DateTime(2023, 2, 14));

    [Test]
    public void TheFirstEntryForADate_DecidesItsState()
    {
        var date = new DateTime(2023, 2, 14);

        RenderedMonth openFirst = MonthRenderer.Render(2023, 2, Options, new[] { Entry(date, Open), Entry(date, Closed), Entry(date, Held) });
        RenderedMonth closedFirst = MonthRenderer.Render(2023, 2, Options, new[] { Entry(date, Closed), Entry(date, Held), Entry(date, Open) });

        Assert.That(The14th(openFirst).State, Is.SameAs(Open));
        Assert.That(The14th(openFirst).Text, Is.EqualTo("*14"));
        Assert.That(The14th(closedFirst).State, Is.SameAs(Closed));
        Assert.That(The14th(closedFirst).Text, Is.EqualTo("x14"));
    }

    [Test]
    public void TheCellShowsOneMarker_NeverTwo()
    {
        var date = new DateTime(2023, 2, 14);

        RenderedMonth rendered = MonthRenderer.Render(2023, 2, Options, new[] { Entry(date, Open), Entry(date, Closed) });

        Assert.That(rendered.Lines[4].Text, Is.EqualTo(" 13 *14  15  16  17  18  19"));
    }

    [Test]
    public void TheSameStateGivenTwiceForADate_CountsOnce()
    {
        var date = new DateTime(2023, 2, 14);
        string once = Rendered.Describe(MonthRenderer.Render(2023, 2, Options, new[] { Entry(date, Open) }));

        Assert.That(Rendered.Describe(MonthRenderer.Render(2023, 2, Options, new[] { Entry(date, Open), Entry(date, Open) })), Is.EqualTo(once));
        Assert.That(Rendered.Describe(MonthRenderer.Render(2023, 2, Options, new[] { date, date, date }, Open)), Is.EqualTo(once));
    }

    /// <summary>Entries with different times of day, or kinds, on the same date are entries for the same day.</summary>
    [Test]
    public void DifferentTimesOnTheSameDate_AreTheSameDay_AndTheFirstStillWins()
    {
        var entries = new[]
        {
            Entry(new DateTime(2023, 2, 14, 18, 0, 0, DateTimeKind.Utc), Closed),
            Entry(new DateTime(2023, 2, 14, 0, 0, 0, DateTimeKind.Unspecified), Open),
            Entry(new DateTime(2023, 2, 14, 9, 0, 0, DateTimeKind.Local), Held),
        };

        Assert.That(The14th(MonthRenderer.Render(2023, 2, Options, entries)).State, Is.SameAs(Closed));
    }

    [Test]
    public void AnEarlierEntryForAnotherMonthOrYear_DoesNotTakeTheDay()
    {
        var entries = new[]
        {
            Entry(new DateTime(2023, 3, 14), Closed),
            Entry(new DateTime(2022, 2, 14), Held),
            Entry(new DateTime(2023, 2, 14), Open),
        };

        Assert.That(The14th(MonthRenderer.Render(2023, 2, Options, entries)).State, Is.SameAs(Open));
    }

    [Test]
    public void EachDate_IsDecidedOnItsOwn()
    {
        var entries = new[]
        {
            Entry(new DateTime(2023, 2, 14), Open),
            Entry(new DateTime(2023, 2, 15), Closed),
            Entry(new DateTime(2023, 2, 15), Open),
            Entry(new DateTime(2023, 2, 14), Closed),
            Entry(new DateTime(2023, 2, 16), Held),
        };

        Assert.That(MonthRenderer.Render(2023, 2, Options, entries).Lines[4].Text, Is.EqualTo(" 13 *14 x15 ?16  17  18  19"));
    }

    [Test]
    public void TheSameInput_AlwaysGivesTheSameResult()
    {
        var entries = new List<KeyValuePair<DateTime, DayState>>();
        for (int day = 1; day <= 28; day++)
        {
            entries.Add(Entry(new DateTime(2023, 2, day), day % 2 == 0 ? Open : Closed));
            entries.Add(Entry(new DateTime(2023, 2, day), Held));
        }

        string first = Rendered.Describe(MonthRenderer.Render(2023, 2, Options, entries));

        for (int repeat = 0; repeat < 5; repeat++)
        {
            Assert.That(Rendered.Describe(MonthRenderer.Render(2023, 2, Options, entries)), Is.EqualTo(first));
        }
        Assert.That(first, Does.Not.Contain("state=held"), "the second entry for each date never wins");
    }

    /// <summary>
    /// A state that never wins is still checked, so that whether the input is accepted does not depend on the
    /// order of the entries or on the month.
    /// </summary>
    [Test]
    public void AStateThatLosesToAnEarlierEntry_MustStillFitTheCell()
    {
        var tooWide = new DayState("wide", "##");
        var date = new DateTime(2023, 2, 14);

        Assert.That(() => MonthRenderer.Render(2023, 2, Options, new[] { Entry(date, Open), Entry(date, tooWide) }), Throws.TypeOf<ArgumentException>());
    }
}
