namespace ConsoleCalendarGenerator.Tests.Rendering;

/// <summary>
/// Invalid arguments to <see cref="MonthRenderer.Render"/>. Invalid settings are refused earlier, when the
/// options are created; those are tested with the setting they belong to.
/// </summary>
[TestFixture]
public class RenderValidationTests
{
    [TestCase(0)]
    [TestCase(-1)]
    [TestCase(10000)]
    [TestCase(int.MinValue)]
    [TestCase(int.MaxValue)]
    public void AYearOutsideOneThrough9999_IsRejected(int year)
    {
        AssertOutOfRange(() => MonthRenderer.Render(year, 1, MonthRenderOptions.Default), "year", year);
    }

    [TestCase(0)]
    [TestCase(-1)]
    [TestCase(13)]
    [TestCase(int.MinValue)]
    [TestCase(int.MaxValue)]
    public void AMonthOutsideOneThroughTwelve_IsRejected(int month)
    {
        AssertOutOfRange(() => MonthRenderer.Render(2023, month, MonthRenderOptions.Default), "month", month);
    }

    [Test]
    public void NullOptions_AreRejected()
    {
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => MonthRenderer.Render(2023, 5, null!))!;

        Assert.That(exception.ParamName, Is.EqualTo("options"));
    }

    /// <summary>The arguments are checked in the order they are declared: year, month, options.</summary>
    [Test]
    public void WhenSeveralArgumentsAreInvalid_TheFirstOneInOrderIsReported()
    {
        AssertOutOfRange(() => MonthRenderer.Render(0, 13, null!), "year", 0);
        AssertOutOfRange(() => MonthRenderer.Render(0, 13, MonthRenderOptions.Default), "year", 0);
        AssertOutOfRange(() => MonthRenderer.Render(0, 5, null!), "year", 0);
        AssertOutOfRange(() => MonthRenderer.Render(2023, 13, null!), "month", 13);
    }

    [TestCase(1, 1)]
    [TestCase(1, 12)]
    [TestCase(9999, 1)]
    [TestCase(9999, 12)]
    public void TheSmallestAndLargestYearAndMonth_AreRendered_WithEveryFirstDayOfWeek(int year, int month)
    {
        foreach (DayOfWeek firstDayOfWeek in Enum.GetValues<DayOfWeek>())
        {
            RenderedMonth rendered = MonthRenderer.Render(year, month, MonthRenderOptions.Default.WithFirstDayOfWeek(firstDayOfWeek));

            Assert.That((rendered.Layout.Year, rendered.Layout.Month, rendered.Layout.FirstDayOfWeek), Is.EqualTo((year, month, firstDayOfWeek)));
            Assert.That(rendered.Lines, Has.Count.EqualTo(2 + rendered.Layout.Weeks.Count));
        }
    }

    private static void AssertOutOfRange(Func<RenderedMonth> render, string parameterName, object value)
    {
        RenderedMonth? result = null;

        // Exactly ArgumentOutOfRangeException, not a type derived from it.
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => result = render())!;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(exception.ParamName, Is.EqualTo(parameterName));
            Assert.That(exception.ActualValue, Is.EqualTo(value));
            Assert.That(result, Is.Null, "nothing is returned");
        }
    }
}
