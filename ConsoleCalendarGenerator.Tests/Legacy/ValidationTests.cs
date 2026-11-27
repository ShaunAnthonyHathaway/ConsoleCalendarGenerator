using System.Globalization;
using CalendarGenerator;
using ConsoleCalendarGenerator.Tests.Support;

namespace ConsoleCalendarGenerator.Tests.Legacy;

/// <summary>
/// Invalid arguments. In every case the exception comes before anything is written.
///
/// An invalid month or year fails with the exception the runtime throws, as in 1.0.0. A null list fails with
/// ArgumentNullException for the parameter dates: that is the behavior of version 2.0, the second of its
/// corrections (D-58). Version 1.0.0 had no check and failed with NullReferenceException when it began to read
/// the list. What 1.0.0 did is recorded, unchanged, against its frozen source in
/// Historical/Published100NullListTests.cs, and the two are held side by side in
/// Migration/NullListMigrationTests.cs.
/// </summary>
[TestFixture]
public class ValidationTests
{
    private static readonly List<DateTime> NoDates = new();

    [TestCase(0)]
    [TestCase(13)]
    [TestCase(-1)]
    [TestCase(int.MinValue)]
    [TestCase(int.MaxValue)]
    public void GenerateMonth_WithAMonthOutsideOneToTwelve_ThrowsArgumentOutOfRange_AndWritesNothing(int month)
    {
        AssertFailsBeforeWriting<ArgumentOutOfRangeException>(calendar => calendar.GenerateMonth(month, 2023));
        AssertFailsBeforeWriting<ArgumentOutOfRangeException>(calendar => calendar.GenerateMonth(month, 2023, NoDates));
    }

    [TestCase(0)]
    [TestCase(10000)]
    [TestCase(-1)]
    [TestCase(int.MinValue)]
    [TestCase(int.MaxValue)]
    public void GenerateMonth_WithAYearOutsideOneTo9999_ThrowsArgumentOutOfRange_AndWritesNothing(int year)
    {
        AssertFailsBeforeWriting<ArgumentOutOfRangeException>(calendar => calendar.GenerateMonth(1, year));
        AssertFailsBeforeWriting<ArgumentOutOfRangeException>(calendar => calendar.GenerateMonth(1, year, NoDates));
    }

    [TestCase(0)]
    [TestCase(10000)]
    [TestCase(-1)]
    public void GenerateYear_WithAYearOutsideOneTo9999_ThrowsArgumentOutOfRange_AndWritesNothing(int year)
    {
        AssertFailsBeforeWriting<ArgumentOutOfRangeException>(calendar => calendar.GenerateYear(year));
        AssertFailsBeforeWriting<ArgumentOutOfRangeException>(calendar => calendar.GenerateYear(year, NoDates));
    }

    [Test]
    public void TheFirstAndTheLastSupportedMonth_DoNotThrow()
    {
        Assert.That(ConsoleCapture.Run(() => new ConsoleCalendar().GenerateMonth(1, 1)).Text, Does.StartWith("    January 1\n").Using(StringComparison.Ordinal));
        Assert.That(ConsoleCapture.Run(() => new ConsoleCalendar().GenerateMonth(12, 9999)).Text, Does.StartWith("   December 9999\n").Using(StringComparison.Ordinal));
    }

    // ---- a null list ----

    [Test]
    public void GenerateMonth_WithANullList_ThrowsArgumentNullExceptionForDates_AndWritesNothing()
    {
        AssertRefusesTheNullList(calendar => calendar.GenerateMonth(2, 2023, null!));
    }

    [Test]
    public void GenerateYear_WithANullList_ThrowsArgumentNullExceptionForDates_AndWritesNothing()
    {
        AssertRefusesTheNullList(calendar => calendar.GenerateYear(2023, null!));
    }

    /// <summary>The list is checked before the month and the year are looked at, whichever of them is invalid.</summary>
    [TestCase(0, 2023)]
    [TestCase(13, 2023)]
    [TestCase(-1, 2023)]
    [TestCase(int.MinValue, 2023)]
    [TestCase(int.MaxValue, 2023)]
    [TestCase(1, 0)]
    [TestCase(1, 10000)]
    [TestCase(1, -1)]
    [TestCase(13, 0, Description = "both invalid")]
    [TestCase(0, 10000, Description = "both invalid")]
    [TestCase(int.MinValue, int.MaxValue, Description = "both invalid")]
    public void ANullList_IsRefusedBeforeAnInvalidMonthOrYear(int month, int year)
    {
        AssertRefusesTheNullList(calendar => calendar.GenerateMonth(month, year, null!));
    }

    [TestCase(0)]
    [TestCase(10000)]
    [TestCase(-1)]
    public void ANullList_IsRefusedBeforeAnInvalidYear_InGenerateYear(int year)
    {
        AssertRefusesTheNullList(calendar => calendar.GenerateYear(year, null!));
    }

    /// <summary>
    /// The list is checked before any color is used, so it also comes before the failure of a color that is not
    /// defined, whichever constructor or setter put it there.
    /// </summary>
    [Test]
    public void ANullList_IsRefusedBeforeAColorThatIsNotDefinedIsUsed()
    {
        var undefined = (ConsoleColor)99;
        var bySetter = new ConsoleCalendar();
        bySetter.SetTitleColor(undefined);

        foreach (ConsoleCalendar calendar in new[] { bySetter, new ConsoleCalendar(undefined), new ConsoleCalendar(undefined, undefined, undefined, undefined) })
        {
            Captured month = ConsoleCapture.TryRun(() => calendar.GenerateMonth(2, 2023, null!));
            Captured year = ConsoleCapture.TryRun(() => calendar.GenerateYear(2023, null!));

            Assert.That(month.Exception, Is.TypeOf<ArgumentNullException>());
            Assert.That(year.Exception, Is.TypeOf<ArgumentNullException>());
            Assert.That(month.Events.Concat(year.Events), Is.Empty);
        }
    }

    /// <summary>
    /// The library gives the exception no message of its own: the message is the one the runtime writes for a
    /// null argument of that name. Only the type and the parameter name are the contract.
    /// </summary>
    [Test]
    public void TheExceptionForANullList_CarriesTheRuntimesMessageForThatParameter()
    {
        string runtimes = ProcessState.UnderCultures(CultureInfo.InvariantCulture, CultureInfo.InvariantCulture, () => new ArgumentNullException("dates").Message);

        Assert.That(ConsoleCapture.TryRun(() => new ConsoleCalendar().GenerateMonth(2, 2023, null!)).Exception!.Message, Is.EqualTo(runtimes));
        Assert.That(ConsoleCapture.TryRun(() => new ConsoleCalendar().GenerateYear(2023, null!)).Exception!.Message, Is.EqualTo(runtimes));
    }

    /// <summary>It is exactly that type, not one derived from it and not the NullReferenceException of 1.0.0.</summary>
    [Test]
    public void TheExceptionForANullList_IsNotANullReferenceException()
    {
        Exception? thrown = ConsoleCapture.TryRun(() => new ConsoleCalendar().GenerateMonth(2, 2023, null!)).Exception;

        Assert.That(thrown, Is.Not.InstanceOf<NullReferenceException>());
        Assert.That(thrown, Is.InstanceOf<ArgumentException>());
        Assert.That(thrown!.InnerException, Is.Null);
    }

    /// <summary>Nothing was done to the console before the exception: no color was set and none was reset.</summary>
    [Test]
    [Platform(Exclude = "Win", Reason = ConsoleState.ColorsObservableOnUnixOnly)]
    public void ANullList_LeavesTheCallersConsoleColorsUntouched()
    {
        foreach (Action<ConsoleCalendar> call in new Action<ConsoleCalendar>[]
        {
            calendar => calendar.GenerateMonth(2, 2023, null!),
            calendar => calendar.GenerateMonth(13, 0, null!),
            calendar => calendar.GenerateYear(2023, null!),
            calendar => calendar.GenerateYear(0, null!),
        })
        {
            Captured captured = ConsoleCapture.TryRun(
                () => call(new ConsoleCalendar()),
                foregroundBefore: ConsoleColor.DarkCyan,
                backgroundBefore: ConsoleColor.DarkBlue);

            Assert.That(captured.Exception, Is.TypeOf<ArgumentNullException>());
            Assert.That(captured.ForegroundAfter, Is.EqualTo(ConsoleColor.DarkCyan), "a reset would have removed it");
            Assert.That(captured.BackgroundAfter, Is.EqualTo(ConsoleColor.DarkBlue));
        }
    }

    /// <summary>Nothing in the process is changed either: not the cultures, not the console's writers.</summary>
    [Test]
    public void ANullList_ChangesNothingInTheProcess()
    {
        var calendar = new ConsoleCalendar();
        object[] before = ProcessState.Snapshot();

        Assert.That(() => calendar.GenerateMonth(2, 2023, null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => calendar.GenerateYear(2023, null!), Throws.TypeOf<ArgumentNullException>());

        Assert.That(ProcessState.Snapshot(), Is.EqualTo(before));
        Assert.That((calendar.GetTitleColor(), calendar.GetDayOfWeekColor(), calendar.GetDateColor(), calendar.GetSelectedDateColor()),
            Is.EqualTo((ConsoleColor.Yellow, ConsoleColor.Cyan, ConsoleColor.White, ConsoleColor.Blue)), "the calendar is as it was");
    }

    /// <summary>
    /// The list is refused before the title is put together: the current culture is never asked for a month
    /// name. A valid call asks, which shows that the test would notice.
    /// </summary>
    [Test]
    public void ANullList_IsRefusedBeforeTheCurrentCultureIsAskedForAMonthName()
    {
        var culture = new WatchedCulture();

        ConsoleCapture.TryRun(() => new ConsoleCalendar().GenerateMonth(2, 2023, null!), culture: culture);
        ConsoleCapture.TryRun(() => new ConsoleCalendar().GenerateYear(2023, null!), culture: culture);
        int afterTheNullLists = culture.TimesAskedForDateFormats;
        ConsoleCapture.Run(() => new ConsoleCalendar().GenerateMonth(2, 2023, NoDates), culture: culture);

        Assert.That(afterTheNullLists, Is.Zero);
        Assert.That(culture.TimesAskedForDateFormats, Is.GreaterThan(0), "a month that is drawn takes its name from the culture");
    }

    /// <summary>A list that is not null is never refused, however little is in it.</summary>
    [Test]
    public void AnEmptyList_IsNotANullList()
    {
        Assert.That(ConsoleCapture.Run(() => new ConsoleCalendar().GenerateMonth(2, 2023, new List<DateTime>())).Text,
            Is.EqualTo(LegacyReference.Month(2023, 2, "February")));
        Assert.That(ConsoleCapture.Run(() => new ConsoleCalendar().GenerateYear(2023, new List<DateTime>())).Text,
            Is.EqualTo(LegacyReference.Year(2023, LegacyReference.EnglishMonthName)));
    }

    /// <summary>With a list, an invalid month or year still fails with the runtime's own exception and its parameter name.</summary>
    [Test]
    public void WithAList_AnInvalidMonthOrYear_StillFailsWithTheRuntimesRangeException()
    {
        Captured month = ConsoleCapture.TryRun(() => new ConsoleCalendar().GenerateMonth(13, 2023, NoDates));
        Captured year = ConsoleCapture.TryRun(() => new ConsoleCalendar().GenerateMonth(1, 0, NoDates));
        Captured both = ConsoleCapture.TryRun(() => new ConsoleCalendar().GenerateMonth(13, 0, NoDates));

        Assert.That(((ArgumentOutOfRangeException)month.Exception!).ParamName, Is.EqualTo("month"));
        Assert.That(((ArgumentOutOfRangeException)year.Exception!).ParamName, Is.EqualTo("year"));
        Assert.That(((ArgumentOutOfRangeException)both.Exception!).ParamName, Is.EqualTo("month"), "the runtime looks at the month first");
    }

    // ---- colors after any invalid argument ----

    [Test]
    [Platform(Exclude = "Win", Reason = ConsoleState.ColorsObservableOnUnixOnly)]
    public void AnInvalidArgument_LeavesTheCallersConsoleColorsUntouched()
    {
        foreach (Action<ConsoleCalendar> call in new Action<ConsoleCalendar>[]
        {
            calendar => calendar.GenerateMonth(13, 2023),
            calendar => calendar.GenerateMonth(1, 0),
            calendar => calendar.GenerateYear(10000),
            calendar => calendar.GenerateMonth(2, 2023, null!),
        })
        {
            Captured captured = ConsoleCapture.TryRun(
                () => call(new ConsoleCalendar()),
                foregroundBefore: ConsoleColor.DarkCyan,
                backgroundBefore: ConsoleColor.DarkBlue);

            Assert.That(captured.Exception, Is.Not.Null);
            Assert.That(captured.ForegroundAfter, Is.EqualTo(ConsoleColor.DarkCyan));
            Assert.That(captured.BackgroundAfter, Is.EqualTo(ConsoleColor.DarkBlue));
        }
    }

    private static void AssertRefusesTheNullList(Action<ConsoleCalendar> call)
    {
        Captured captured = ConsoleCapture.TryRun(() => call(new ConsoleCalendar()));

        Assert.That(captured.Exception, Is.TypeOf<ArgumentNullException>());
        Assert.That(((ArgumentNullException)captured.Exception!).ParamName, Is.EqualTo("dates"));
        Assert.That(captured.Events, Is.Empty, "nothing may be written before the exception");
    }

    /// <summary>A copy of the invariant culture that counts how often its date formats are asked for.</summary>
    private sealed class WatchedCulture : CultureInfo
    {
        public WatchedCulture()
            : base(string.Empty)
        {
        }

        public int TimesAskedForDateFormats { get; private set; }

        public override DateTimeFormatInfo DateTimeFormat
        {
            get
            {
                TimesAskedForDateFormats++;
                return base.DateTimeFormat;
            }

            set => base.DateTimeFormat = value;
        }
    }

    private static void AssertFailsBeforeWriting<TException>(Action<ConsoleCalendar> call)
        where TException : Exception
    {
        Captured captured = ConsoleCapture.TryRun(() => call(new ConsoleCalendar()));

        Assert.That(captured.Exception, Is.TypeOf<TException>());
        Assert.That(captured.Events, Is.Empty, "nothing may be written before the exception");
    }
}
