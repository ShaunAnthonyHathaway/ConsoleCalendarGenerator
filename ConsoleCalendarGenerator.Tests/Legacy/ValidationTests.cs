using CalendarGenerator;
using ConsoleCalendarGenerator.Tests.Support;

namespace ConsoleCalendarGenerator.Tests.Legacy;

/// <summary>
/// Invalid arguments. The exception types are the ones 1.0.0 lets through from the runtime, including a
/// NullReferenceException for a null list. They are recorded as they are, not as they should be. In every case
/// the exception comes before anything is written.
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

    [Test]
    public void GenerateMonth_WithANullList_Characterized1xBehavior_ThrowsNullReferenceException_AndWritesNothing()
    {
        AssertFailsBeforeWriting<NullReferenceException>(calendar => calendar.GenerateMonth(2, 2023, null!));
    }

    [Test]
    public void GenerateYear_WithANullList_Characterized1xBehavior_ThrowsNullReferenceException_AndWritesNothing()
    {
        AssertFailsBeforeWriting<NullReferenceException>(calendar => calendar.GenerateYear(2023, null!));
    }

    /// <summary>The list is read before the month and year are looked at.</summary>
    [Test]
    public void ANullList_IsReportedBeforeAnInvalidMonthOrYear()
    {
        AssertFailsBeforeWriting<NullReferenceException>(calendar => calendar.GenerateMonth(13, 2023, null!));
        AssertFailsBeforeWriting<NullReferenceException>(calendar => calendar.GenerateMonth(1, 0, null!));
    }

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

    private static void AssertFailsBeforeWriting<TException>(Action<ConsoleCalendar> call)
        where TException : Exception
    {
        Captured captured = ConsoleCapture.TryRun(() => call(new ConsoleCalendar()));

        Assert.That(captured.Exception, Is.TypeOf<TException>());
        Assert.That(captured.Events, Is.Empty, "nothing may be written before the exception");
    }
}
