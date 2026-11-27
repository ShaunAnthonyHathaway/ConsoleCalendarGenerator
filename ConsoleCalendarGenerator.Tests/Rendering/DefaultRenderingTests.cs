using System.Globalization;
using ConsoleCalendarGenerator.Tests.Support;

namespace ConsoleCalendarGenerator.Tests.Rendering;

/// <summary>
/// What <see cref="MonthRenderOptions.Default"/> produces: the invariant culture, weeks from Monday, two-letter
/// labels, cells two wide with one space between, no indent, and a centered year-and-month title. The expected
/// text is written out line by line.
/// </summary>
[TestFixture]
public class DefaultRenderingTests
{
    private static string[] Lines(int year, int month) =>
        Rendered.Lines(MonthRenderer.Render(year, month, MonthRenderOptions.Default));

    [Test]
    public void TheDefaults_AreTheDocumentedOnes()
    {
        MonthRenderOptions options = MonthRenderOptions.Default;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(options.Culture, Is.SameAs(CultureInfo.InvariantCulture));
            Assert.That(options.FirstDayOfWeek, Is.EqualTo(DayOfWeek.Monday));
            Assert.That(options.WeekdayLabelStyle, Is.EqualTo(WeekdayLabelStyle.Shortest));
            Assert.That(options.WeekdayLabels, Is.Null);
            Assert.That(options.CellWidth, Is.EqualTo(2));
            Assert.That(options.ColumnGap, Is.EqualTo(1));
            Assert.That(options.GridWidth, Is.EqualTo(20));
            Assert.That(options.Indent, Is.EqualTo(string.Empty));
            Assert.That(options.TitleFormat, Is.EqualTo("Y"));
            Assert.That(options.TitleSuffix, Is.EqualTo(string.Empty));
            Assert.That(options.TitleAlignment, Is.EqualTo(TitleAlignment.Center));
            Assert.That(MonthRenderOptions.Default, Is.SameAs(options), "Default is one shared immutable instance");
        }
    }

    // ---- one month for every weekday the 1st can fall on ----

    [Test]
    public void FirstOnMonday_31Days_FiveWeeks()
    {
        Assert.That(Lines(2023, 5), Is.EqualTo(new[]
        {
            "      2023 May",
            "Mo Tu We Th Fr Sa Su",
            " 1  2  3  4  5  6  7",
            " 8  9 10 11 12 13 14",
            "15 16 17 18 19 20 21",
            "22 23 24 25 26 27 28",
            "29 30 31",
        }));
    }

    [Test]
    public void FirstOnTuesday_31Days()
    {
        Assert.That(Lines(2023, 8), Is.EqualTo(new[]
        {
            "    2023 August",
            "Mo Tu We Th Fr Sa Su",
            "    1  2  3  4  5  6",
            " 7  8  9 10 11 12 13",
            "14 15 16 17 18 19 20",
            "21 22 23 24 25 26 27",
            "28 29 30 31",
        }));
    }

    [Test]
    public void FirstOnWednesday_28Days()
    {
        Assert.That(Lines(2023, 2), Is.EqualTo(new[]
        {
            "   2023 February",
            "Mo Tu We Th Fr Sa Su",
            "       1  2  3  4  5",
            " 6  7  8  9 10 11 12",
            "13 14 15 16 17 18 19",
            "20 21 22 23 24 25 26",
            "27 28",
        }));
    }

    [Test]
    public void FirstOnThursday_30Days()
    {
        Assert.That(Lines(2023, 6), Is.EqualTo(new[]
        {
            "     2023 June",
            "Mo Tu We Th Fr Sa Su",
            "          1  2  3  4",
            " 5  6  7  8  9 10 11",
            "12 13 14 15 16 17 18",
            "19 20 21 22 23 24 25",
            "26 27 28 29 30",
        }));
    }

    [Test]
    public void FirstOnFriday_30Days()
    {
        Assert.That(Lines(2023, 9), Is.EqualTo(new[]
        {
            "   2023 September",
            "Mo Tu We Th Fr Sa Su",
            "             1  2  3",
            " 4  5  6  7  8  9 10",
            "11 12 13 14 15 16 17",
            "18 19 20 21 22 23 24",
            "25 26 27 28 29 30",
        }));
    }

    /// <summary>The month ends in the last column, so its last line is as wide as the grid.</summary>
    [Test]
    public void FirstOnSaturday_30Days_EndingInTheLastColumn()
    {
        Assert.That(Lines(2023, 4), Is.EqualTo(new[]
        {
            "     2023 April",
            "Mo Tu We Th Fr Sa Su",
            "                1  2",
            " 3  4  5  6  7  8  9",
            "10 11 12 13 14 15 16",
            "17 18 19 20 21 22 23",
            "24 25 26 27 28 29 30",
        }));
    }

    [Test]
    public void FirstOnSunday_31Days_SixWeeks()
    {
        Assert.That(Lines(2023, 1), Is.EqualTo(new[]
        {
            "    2023 January",
            "Mo Tu We Th Fr Sa Su",
            "                   1",
            " 2  3  4  5  6  7  8",
            " 9 10 11 12 13 14 15",
            "16 17 18 19 20 21 22",
            "23 24 25 26 27 28 29",
            "30 31",
        }));
    }

    // ---- February ----

    [Test]
    public void February2021_FourWeeks()
    {
        Assert.That(Lines(2021, 2), Is.EqualTo(new[]
        {
            "   2021 February",
            "Mo Tu We Th Fr Sa Su",
            " 1  2  3  4  5  6  7",
            " 8  9 10 11 12 13 14",
            "15 16 17 18 19 20 21",
            "22 23 24 25 26 27 28",
        }));
    }

    [Test]
    public void February2024_LeapYear_29Days()
    {
        Assert.That(Lines(2024, 2), Is.EqualTo(new[]
        {
            "   2024 February",
            "Mo Tu We Th Fr Sa Su",
            "          1  2  3  4",
            " 5  6  7  8  9 10 11",
            "12 13 14 15 16 17 18",
            "19 20 21 22 23 24 25",
            "26 27 28 29",
        }));
    }

    // ---- the first and the last month ----

    [Test]
    public void January0001_TheYearHasFourDigits()
    {
        Assert.That(Lines(1, 1), Is.EqualTo(new[]
        {
            "    0001 January",
            "Mo Tu We Th Fr Sa Su",
            " 1  2  3  4  5  6  7",
            " 8  9 10 11 12 13 14",
            "15 16 17 18 19 20 21",
            "22 23 24 25 26 27 28",
            "29 30 31",
        }));
    }

    [Test]
    public void December9999()
    {
        Assert.That(Lines(9999, 12), Is.EqualTo(new[]
        {
            "   9999 December",
            "Mo Tu We Th Fr Sa Su",
            "       1  2  3  4  5",
            " 6  7  8  9 10 11 12",
            "13 14 15 16 17 18 19",
            "20 21 22 23 24 25 26",
            "27 28 29 30 31",
        }));
    }

    // ---- shape ----

    [TestCase(2021, 2, 6)]
    [TestCase(2023, 5, 7)]
    [TestCase(2023, 1, 8)]
    public void ThereIsOneTitleLine_OneLabelLine_AndOneLinePerWeek(int year, int month, int expectedLines)
    {
        RenderedMonth rendered = MonthRenderer.Render(year, month, MonthRenderOptions.Default);

        Assert.That(rendered.Lines, Has.Count.EqualTo(expectedLines));
        Assert.That(rendered.Lines.Select(line => line.Kind), Is.EqualTo(
            new[] { RenderedLineKind.Title, RenderedLineKind.WeekdayLabels }
                .Concat(Enumerable.Repeat(RenderedLineKind.Week, expectedLines - 2))));
        Assert.That(rendered.Layout.Weeks, Has.Count.EqualTo(expectedLines - 2));
    }

    [Test]
    public void NoLineOfAnyMonthOf2023And2024_EndsWithASpace_OrIsWiderThanTheGrid()
    {
        for (int year = 2023; year <= 2024; year++)
        {
            for (int month = 1; month <= 12; month++)
            {
                foreach (string line in Lines(year, month))
                {
                    Assert.That(line, Does.Not.EndWith(" ").Using(StringComparison.Ordinal), $"{year}-{month:00}");
                    Assert.That(line.Length, Is.InRange(1, 20), $"{year}-{month:00}: <{line}>");
                }
            }
        }
    }

    [Test]
    public void TheText_IsTheLinesJoinedByLineFeeds()
    {
        Assert.That(MonthRenderer.Render(2021, 2, MonthRenderOptions.Default).ToText(), Is.EqualTo(
            "   2021 February\n" +
            "Mo Tu We Th Fr Sa Su\n" +
            " 1  2  3  4  5  6  7\n" +
            " 8  9 10 11 12 13 14\n" +
            "15 16 17 18 19 20 21\n" +
            "22 23 24 25 26 27 28"));
    }
}
