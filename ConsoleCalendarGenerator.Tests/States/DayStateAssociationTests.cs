using ConsoleCalendarGenerator.Tests.Support;

namespace ConsoleCalendarGenerator.Tests.States;

/// <summary>
/// Giving days a state: which dates select which days, which segments carry the state, and what is refused.
/// Cells are three wide throughout, the narrowest that holds a one-character marker beside a two-digit day.
/// </summary>
[TestFixture]
public class DayStateAssociationTests
{
    private static readonly MonthRenderOptions Options = MonthRenderOptions.Default.WithCellWidth(3);
    private static readonly DayState Open = new("open", "*");
    private static readonly DayState Closed = new("closed", "x");

    private static KeyValuePair<DateTime, DayState> Entry(DateTime date, DayState state) => new(date, state);

    /// <summary>The days of the month that have a state, as "day:name".</summary>
    private static string[] StatesOf(RenderedMonth rendered) => rendered.Lines.SelectMany(line => line.Segments)
        .Where(segment => segment.State != null)
        .Select(segment => segment.Date!.Value.Day + ":" + segment.State!.Name)
        .ToArray();

    // ---- no states ----

    [Test]
    public void WithNoEntries_TheResultIsExactlyTheUnmarkedMonth()
    {
        string unmarked = Rendered.Describe(MonthRenderer.Render(2023, 2, Options));

        Assert.That(Rendered.Describe(MonthRenderer.Render(2023, 2, Options, Array.Empty<KeyValuePair<DateTime, DayState>>())), Is.EqualTo(unmarked));
        Assert.That(Rendered.Describe(MonthRenderer.Render(2023, 2, Options, new Dictionary<DateTime, DayState>())), Is.EqualTo(unmarked));
        Assert.That(Rendered.Describe(MonthRenderer.Render(2023, 2, Options, Array.Empty<DateTime>(), Open)), Is.EqualTo(unmarked));
    }

    [Test]
    public void TheOverloadWithoutStates_NeverPutsAStateOnASegment()
    {
        Assert.That(MonthRenderer.Render(2023, 2, Options).Lines.SelectMany(line => line.Segments).Select(segment => segment.State), Is.All.Null);
    }

    // ---- which days get a state ----

    [Test]
    public void OneStateOnOneDate()
    {
        RenderedMonth rendered = MonthRenderer.Render(2023, 2, Options, new[] { Entry(new DateTime(2023, 2, 14), Open) });

        Assert.That(StatesOf(rendered), Is.EqualTo(new[] { "14:open" }));
    }

    [Test]
    public void OneStateOnManyDates_ThroughEitherOverload()
    {
        DateTime[] dates = { new DateTime(2023, 2, 1), new DateTime(2023, 2, 14), new DateTime(2023, 2, 28) };

        RenderedMonth withDates = MonthRenderer.Render(2023, 2, Options, dates, Open);
        RenderedMonth withEntries = MonthRenderer.Render(2023, 2, Options, dates.Select(date => Entry(date, Open)));

        Assert.That(StatesOf(withDates), Is.EqualTo(new[] { "1:open", "14:open", "28:open" }));
        Assert.That(Rendered.Describe(withEntries), Is.EqualTo(Rendered.Describe(withDates)));
    }

    [Test]
    public void DifferentStatesOnDifferentDates()
    {
        RenderedMonth rendered = MonthRenderer.Render(2023, 2, Options, new[]
        {
            Entry(new DateTime(2023, 2, 20), Closed),
            Entry(new DateTime(2023, 2, 3), Open),
            Entry(new DateTime(2023, 2, 21), Closed),
            Entry(new DateTime(2023, 2, 4), Open),
        });

        Assert.That(StatesOf(rendered), Is.EqualTo(new[] { "3:open", "4:open", "20:closed", "21:closed" }), "in the order of the days, not of the entries");
    }

    [Test]
    public void ADictionary_CanBePassedAsItIs()
    {
        var states = new Dictionary<DateTime, DayState>
        {
            [new DateTime(2023, 2, 14)] = Open,
            [new DateTime(2023, 2, 15)] = Closed,
        };

        Assert.That(StatesOf(MonthRenderer.Render(2023, 2, Options, states)), Is.EqualTo(new[] { "14:open", "15:closed" }));
    }

    [Test]
    public void TheSegment_CarriesTheVeryInstanceThatWasPassed()
    {
        RenderedMonth rendered = MonthRenderer.Render(2023, 2, Options, new[] { new DateTime(2023, 2, 14) }, Open);

        RenderedSegment marked = rendered.Lines.SelectMany(line => line.Segments).Single(segment => segment.State != null);

        Assert.That(marked.State, Is.SameAs(Open));
        Assert.That(marked.Kind, Is.EqualTo(RenderedSegmentKind.Day));
        Assert.That(marked.Date, Is.EqualTo(new DateTime(2023, 2, 14)));
        Assert.That(marked.DayOfWeek, Is.EqualTo(DayOfWeek.Tuesday));
    }

    /// <summary>Every day of a month marked: the state is on the day cells and nowhere else.</summary>
    [Test]
    public void OnlyDaySegments_EverCarryAState()
    {
        IEnumerable<DateTime> everyDay = Enumerable.Range(1, 30).Select(day => new DateTime(2023, 4, day));

        RenderedMonth rendered = MonthRenderer.Render(2023, 4, Options.WithIndent("  ").WithTitleSuffix(" (* = open)"), everyDay, Open);

        foreach (RenderedSegment segment in rendered.Lines.SelectMany(line => line.Segments))
        {
            Assert.That(segment.State != null, Is.EqualTo(segment.Kind == RenderedSegmentKind.Day), Rendered.Describe(segment));
        }
        Assert.That(StatesOf(rendered), Has.Length.EqualTo(30));
    }

    // ---- how a date selects a day ----

    [Test]
    public void TheTimeOfDay_IsIgnored()
    {
        DateTime[] dates = { new DateTime(2023, 2, 14, 23, 59, 59, 999), new DateTime(2023, 2, 15, 0, 0, 0).AddTicks(1) };

        Assert.That(StatesOf(MonthRenderer.Render(2023, 2, Options, dates, Open)), Is.EqualTo(new[] { "14:open", "15:open" }));
    }

    /// <summary>The year, month and day of the value are used as they are. Nothing is converted between time zones.</summary>
    [TestCase(DateTimeKind.Unspecified)]
    [TestCase(DateTimeKind.Utc)]
    [TestCase(DateTimeKind.Local)]
    public void TheDateTimeKind_IsIgnored(DateTimeKind kind)
    {
        DateTime[] dates = { new DateTime(2023, 2, 1, 0, 30, 0, kind), new DateTime(2023, 2, 28, 23, 30, 0, kind) };

        Assert.That(StatesOf(MonthRenderer.Render(2023, 2, Options, dates, Open)), Is.EqualTo(new[] { "1:open", "28:open" }));
    }

    [Test]
    public void DatesInAnotherMonth_AreIgnored()
    {
        DateTime[] dates = { new DateTime(2023, 1, 31), new DateTime(2023, 3, 1), new DateTime(2023, 3, 14), new DateTime(2023, 2, 14) };

        Assert.That(StatesOf(MonthRenderer.Render(2023, 2, Options, dates, Open)), Is.EqualTo(new[] { "14:open" }));
    }

    [Test]
    public void TheSameMonthOfAnotherYear_IsIgnored()
    {
        DateTime[] dates = { new DateTime(2022, 2, 14), new DateTime(2024, 2, 14), new DateTime(1, 2, 14), new DateTime(9999, 2, 14) };

        Assert.That(StatesOf(MonthRenderer.Render(2023, 2, Options, dates, Open)), Is.Empty);
    }

    [Test]
    public void TheFirstAndTheLastDay_OfTheFirstAndTheLastMonth_CanHaveAState()
    {
        Assert.That(StatesOf(MonthRenderer.Render(1, 1, Options, new[] { DateTime.MinValue }, Open)), Is.EqualTo(new[] { "1:open" }));
        Assert.That(StatesOf(MonthRenderer.Render(9999, 12, Options, new[] { DateTime.MaxValue }, Open)), Is.EqualTo(new[] { "31:open" }));
    }

    // ---- the input ----

    [Test]
    public void TheCollections_AreNotChanged()
    {
        var dates = new List<DateTime> { new DateTime(2023, 2, 14, 9, 0, 0), new DateTime(2023, 2, 14), new DateTime(2022, 1, 1) };
        var entries = new List<KeyValuePair<DateTime, DayState>> { Entry(new DateTime(2023, 2, 14), Open), Entry(new DateTime(2023, 2, 14), Closed) };
        DateTime[] datesBefore = dates.ToArray();
        KeyValuePair<DateTime, DayState>[] entriesBefore = entries.ToArray();

        _ = MonthRenderer.Render(2023, 2, Options, dates, Open);
        _ = MonthRenderer.Render(2023, 2, Options, entries);

        Assert.That(dates, Is.EqualTo(datesBefore));
        Assert.That(entries, Is.EqualTo(entriesBefore));
    }

    [Test]
    public void ASequence_IsReadOnce_FromBeginningToEnd()
    {
        int enumerations = 0;
        int items = 0;
        IEnumerable<DateTime> Dates()
        {
            enumerations++;
            foreach (int day in new[] { 14, 1, 28 })
            {
                items++;
                yield return new DateTime(2023, 2, day);
            }
        }

        RenderedMonth rendered = MonthRenderer.Render(2023, 2, Options, Dates(), Open);

        Assert.That(enumerations, Is.EqualTo(1));
        Assert.That(items, Is.EqualTo(3));
        Assert.That(StatesOf(rendered), Is.EqualTo(new[] { "1:open", "14:open", "28:open" }));
    }

    [Test]
    public void ChangingTheCollectionAfterwards_DoesNotChangeTheResult()
    {
        var dates = new List<DateTime> { new DateTime(2023, 2, 14) };
        RenderedMonth rendered = MonthRenderer.Render(2023, 2, Options, dates, Open);
        string before = Rendered.Describe(rendered);

        dates.Add(new DateTime(2023, 2, 15));
        dates.Clear();

        Assert.That(Rendered.Describe(rendered), Is.EqualTo(before));
    }

    // ---- what is refused ----

    [Test]
    public void NullEntries_AreRejected()
    {
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(
            () => MonthRenderer.Render(2023, 2, Options, (IEnumerable<KeyValuePair<DateTime, DayState>>)null!))!;

        Assert.That(exception.ParamName, Is.EqualTo("dayStates"));
    }

    [Test]
    public void NullDates_AndANullState_AreRejected()
    {
        Assert.That(Assert.Throws<ArgumentNullException>(() => MonthRenderer.Render(2023, 2, Options, null!, Open))!.ParamName, Is.EqualTo("dates"));
        Assert.That(Assert.Throws<ArgumentNullException>(() => MonthRenderer.Render(2023, 2, Options, Array.Empty<DateTime>(), null!))!.ParamName, Is.EqualTo("state"));
    }

    /// <summary>Also when the entry is for a date outside the month: the input is valid or not, whatever is rendered.</summary>
    [TestCase(2023, 2, 14)]
    [TestCase(2023, 3, 14)]
    [TestCase(1999, 1, 1)]
    public void AnEntryWithoutAState_IsRejected(int year, int month, int day)
    {
        var entries = new[] { Entry(new DateTime(2023, 2, 1), Open), Entry(new DateTime(year, month, day), null!) };

        // Exactly ArgumentException.
        ArgumentException exception = Assert.Throws<ArgumentException>(() => MonthRenderer.Render(2023, 2, Options, entries))!;

        Assert.That(exception.ParamName, Is.EqualTo("dayStates"));
    }

    /// <summary>The arguments are checked in the order they are declared.</summary>
    [Test]
    public void TheArguments_AreCheckedInOrder()
    {
        IEnumerable<KeyValuePair<DateTime, DayState>> noEntries = null!;

        Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => MonthRenderer.Render(0, 13, null!, noEntries))!.ParamName, Is.EqualTo("year"));
        Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => MonthRenderer.Render(2023, 13, null!, noEntries))!.ParamName, Is.EqualTo("month"));
        Assert.That(Assert.Throws<ArgumentNullException>(() => MonthRenderer.Render(2023, 2, null!, noEntries))!.ParamName, Is.EqualTo("options"));
        Assert.That(Assert.Throws<ArgumentNullException>(() => MonthRenderer.Render(2023, 2, Options, noEntries))!.ParamName, Is.EqualTo("dayStates"));

        Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => MonthRenderer.Render(0, 13, null!, null!, null!))!.ParamName, Is.EqualTo("year"));
        Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => MonthRenderer.Render(2023, 13, null!, null!, null!))!.ParamName, Is.EqualTo("month"));
        Assert.That(Assert.Throws<ArgumentNullException>(() => MonthRenderer.Render(2023, 2, null!, null!, null!))!.ParamName, Is.EqualTo("options"));
        Assert.That(Assert.Throws<ArgumentNullException>(() => MonthRenderer.Render(2023, 2, Options, null!, null!))!.ParamName, Is.EqualTo("dates"));
    }

    // ---- names ----

    /// <summary>A name identifies one state. Two states that share a name must also share their marker.</summary>
    [Test]
    public void TwoStatesWithTheSameNameAndDifferentMarkers_AreRejected()
    {
        var entries = new[] { Entry(new DateTime(2023, 2, 1), new DayState("open", "*")), Entry(new DateTime(2023, 2, 2), new DayState("open", "+")) };

        Assert.That(Assert.Throws<ArgumentException>(() => MonthRenderer.Render(2023, 2, Options, entries))!.ParamName, Is.EqualTo("dayStates"));
        // Also when the second one is for a date that is not rendered.
        entries[1] = Entry(new DateTime(2024, 6, 2), new DayState("open", "+"));
        Assert.That(Assert.Throws<ArgumentException>(() => MonthRenderer.Render(2023, 2, Options, entries))!.ParamName, Is.EqualTo("dayStates"));
    }

    [Test]
    public void TwoInstancesWithTheSameNameAndMarker_AreTheSameState()
    {
        var entries = new[] { Entry(new DateTime(2023, 2, 1), new DayState("open", "*")), Entry(new DateTime(2023, 2, 2), new DayState("open", "*")) };

        Assert.That(StatesOf(MonthRenderer.Render(2023, 2, Options, entries)), Is.EqualTo(new[] { "1:open", "2:open" }));
    }

    /// <summary>Names are compared character by character: these are two different states.</summary>
    [Test]
    public void NamesThatDifferOnlyInCase_AreDifferentStates()
    {
        var entries = new[] { Entry(new DateTime(2023, 2, 1), new DayState("open", "*")), Entry(new DateTime(2023, 2, 2), new DayState("Open", "+")) };

        RenderedMonth rendered = MonthRenderer.Render(2023, 2, Options, entries);

        Assert.That(StatesOf(rendered), Is.EqualTo(new[] { "1:open", "2:Open" }));
        Assert.That(rendered.Lines[2].Text, Is.EqualTo("         *1  +2   3   4   5"));
    }
}
