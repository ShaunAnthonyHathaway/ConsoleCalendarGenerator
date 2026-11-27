namespace ConsoleCalendarGenerator.Tests.Layout;

/// <summary>
/// Invalid arguments to <see cref="MonthLayout.Create"/>. Each is rejected with an
/// <see cref="ArgumentOutOfRangeException"/> that names the parameter and carries the value, and nothing is
/// returned.
/// </summary>
[TestFixture]
public class LayoutValidationTests
{
    [TestCase(0)]
    [TestCase(-1)]
    [TestCase(10000)]
    [TestCase(int.MinValue)]
    [TestCase(int.MaxValue)]
    public void AYearOutsideOneThrough9999_IsRejected(int year)
    {
        AssertRejected(() => MonthLayout.Create(year, 1, DayOfWeek.Monday), "year", year);
    }

    [TestCase(0)]
    [TestCase(-1)]
    [TestCase(13)]
    [TestCase(int.MinValue)]
    [TestCase(int.MaxValue)]
    public void AMonthOutsideOneThroughTwelve_IsRejected(int month)
    {
        AssertRejected(() => MonthLayout.Create(2023, month, DayOfWeek.Monday), "month", month);
    }

    [TestCase(-1)]
    [TestCase(7)]
    [TestCase(8)]
    [TestCase(int.MinValue)]
    [TestCase(int.MaxValue)]
    public void AFirstDayOfWeekThatIsNotADayOfTheWeek_IsRejected(int value)
    {
        var firstDayOfWeek = (DayOfWeek)value;

        AssertRejected(() => MonthLayout.Create(2023, 1, firstDayOfWeek), "firstDayOfWeek", firstDayOfWeek);
    }

    /// <summary>The arguments are checked in the order they are declared: year, month, first day of the week.</summary>
    [Test]
    public void WhenSeveralArgumentsAreInvalid_TheFirstOneInOrderIsReported()
    {
        AssertRejected(() => MonthLayout.Create(0, 13, (DayOfWeek)7), "year", 0);
        AssertRejected(() => MonthLayout.Create(0, 13, DayOfWeek.Monday), "year", 0);
        AssertRejected(() => MonthLayout.Create(0, 1, (DayOfWeek)7), "year", 0);
        AssertRejected(() => MonthLayout.Create(2023, 13, (DayOfWeek)7), "month", 13);
    }

    [TestCase(1, 1)]
    [TestCase(1, 12)]
    [TestCase(9999, 1)]
    [TestCase(9999, 12)]
    public void TheSmallestAndLargestYearAndMonth_AreAccepted_WithEveryDayOfTheWeek(int year, int month)
    {
        foreach (DayOfWeek firstDayOfWeek in Enum.GetValues<DayOfWeek>())
        {
            MonthLayout layout = MonthLayout.Create(year, month, firstDayOfWeek);

            Assert.That((layout.Year, layout.Month, layout.FirstDayOfWeek), Is.EqualTo((year, month, firstDayOfWeek)));
        }
    }

    private static void AssertRejected(Func<MonthLayout> create, string parameterName, object value)
    {
        MonthLayout? result = null;

        // Exactly ArgumentOutOfRangeException, not a type derived from it.
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => result = create())!;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(exception.ParamName, Is.EqualTo(parameterName));
            Assert.That(exception.ActualValue, Is.EqualTo(value));
            Assert.That(result, Is.Null, "nothing is returned");
        }
    }
}
