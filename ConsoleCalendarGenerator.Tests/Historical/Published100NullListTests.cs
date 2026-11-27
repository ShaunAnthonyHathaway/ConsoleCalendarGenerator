using System.Globalization;
using ConsoleCalendarGenerator.Tests.Migration;
using ConsoleCalendarGenerator.Tests.Support;

namespace ConsoleCalendarGenerator.Tests.Historical;

/// <summary>
/// A null list in version 1.0.0, complete. The two methods that take a list did not check it: they began to read
/// it, and the runtime threw <see cref="NullReferenceException"/>, which names no parameter. Reading the list
/// was the first thing they did, so that happened before the month and the year were looked at and before
/// anything was written or any color set. Everything here runs the frozen source of 1.0.0. Version 2.0 throws
/// <see cref="ArgumentNullException"/> instead (D-58); these tests stay as they are, as the record of what was
/// corrected. The corrected behavior is in Legacy/ValidationTests.cs, and the two side by side in
/// Migration/NullListMigrationTests.cs.
/// </summary>
[TestFixture]
public class Published100NullListTests
{
    /// <summary>A null list passed to <c>GenerateMonth</c>, with the caller's colors set beforehand.</summary>
    private static LegacyObservation Month(int month, int year) => LegacyObservation.Of(
        LegacySubject.Published100,
        new LegacyScenario("a null list")
        {
            Draw = LegacyDraw.MonthWithDates,
            Month = month,
            Year = year,
            Dates = null,
            ForegroundBefore = ConsoleColor.DarkCyan,
            BackgroundBefore = ConsoleColor.DarkBlue,
        },
        CultureInfo.InvariantCulture);

    /// <summary>A null list passed to <c>GenerateYear</c>, with the caller's colors set beforehand.</summary>
    private static LegacyObservation Year(int year) => LegacyObservation.Of(
        LegacySubject.Published100,
        new LegacyScenario("a null list")
        {
            Draw = LegacyDraw.YearWithDates,
            Year = year,
            Dates = null,
            ForegroundBefore = ConsoleColor.DarkCyan,
            BackgroundBefore = ConsoleColor.DarkBlue,
        },
        CultureInfo.InvariantCulture);

    private static void AssertNullReference(LegacyObservation observation, string what)
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(observation.ExceptionType, Is.EqualTo("System.NullReferenceException"), what);
            Assert.That(observation.ExceptionParameterName, Is.Null, what + ": the exception names no parameter");
            Assert.That(observation.Calls, Is.Empty, what + ": nothing is written first");
        }
    }

    [Test]
    public void In100_GenerateMonthWithANullList_ThrowsNullReferenceException_WhichNamesNoParameter()
    {
        AssertNullReference(Month(2, 2023), "a valid month and year");
    }

    [Test]
    public void In100_GenerateYearWithANullList_ThrowsNullReferenceException_WhichNamesNoParameter()
    {
        AssertNullReference(Year(2023), "a valid year");
    }

    /// <summary>The list was read before the month and the year were looked at.</summary>
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
    public void In100_ANullList_ComesBeforeAnInvalidMonthOrYear(int month, int year)
    {
        AssertNullReference(Month(month, year), $"month {month} of year {year}");
    }

    [TestCase(0)]
    [TestCase(10000)]
    [TestCase(-1)]
    public void In100_ANullList_ComesBeforeAnInvalidYear_InGenerateYear(int year)
    {
        AssertNullReference(Year(year), $"year {year}");
    }

    /// <summary>The message is the runtime's own for the exception, because the runtime threw it.</summary>
    [Test]
    public void In100_TheMessage_IsTheRuntimesOwnForANullReference()
    {
        string runtimes = ProcessState.UnderCultures(CultureInfo.InvariantCulture, CultureInfo.InvariantCulture, () => new NullReferenceException().Message);

        Assert.That(Month(2, 2023).ExceptionMessage, Is.EqualTo(runtimes));
        Assert.That(Year(2023).ExceptionMessage, Is.EqualTo(runtimes));
    }

    /// <summary>Nothing had been done to the console when the exception was thrown: the caller's colors are as they were.</summary>
    [Test]
    [Platform(Exclude = "Win", Reason = ConsoleState.ColorsObservableOnUnixOnly)]
    public void In100_ANullList_LeavesTheCallersColorsAsTheyWere()
    {
        foreach (LegacyObservation observation in new[] { Month(2, 2023), Month(13, 0), Year(2023), Year(0) })
        {
            Assert.That((observation.ForegroundAfter, observation.BackgroundAfter), Is.EqualTo((ConsoleColor.DarkCyan, ConsoleColor.DarkBlue)));
        }
    }

    /// <summary>With a list that is not null, the month and the year fail with the runtime's range exception, as ever.</summary>
    [Test]
    public void In100_WithAList_AnInvalidMonthOrYearFailsWithTheRangeException()
    {
        var withAList = new LegacyScenario("an empty list") { Draw = LegacyDraw.MonthWithDates, Month = 13, Year = 2023 };

        LegacyObservation observation = LegacyObservation.Of(LegacySubject.Published100, withAList, CultureInfo.InvariantCulture);

        Assert.That(observation.ExceptionType, Is.EqualTo("System.ArgumentOutOfRangeException"));
        Assert.That(observation.ExceptionParameterName, Is.EqualTo("month"));
        Assert.That(observation.Calls, Is.Empty);
    }
}
