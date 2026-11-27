using System.Globalization;
using CalendarGenerator;
using ConsoleCalendarGenerator.Tests.Support;

namespace ConsoleCalendarGenerator.Tests.Facade;

/// <summary>
/// The legacy class draws its days where <see cref="MonthLayout"/> puts them. The class takes its grid from the
/// layout model, so there is one place in the library that decides which column and which row a day is in; these
/// tests read the grid back out of what the legacy class writes and compare it with the layout, cell by cell.
/// They would fail if the two ever came apart, whichever of them changed.
/// </summary>
[TestFixture]
public class FacadeLayoutTests
{
    /// <summary>
    /// The grid the legacy class draws: one array of seven per row, with the day in each column or 0 where the
    /// row has nothing. A column is three characters: two for the day and one space.
    /// </summary>
    private static int[][] DrawnGrid(int year, int month, List<DateTime>? selected = null)
    {
        string text = ConsoleCapture.Run(() => new ConsoleCalendar().GenerateMonth(month, year, selected ?? new List<DateTime>())).Text;
        string[] rows = text.Split('\n').Skip(2).TakeWhile(line => line.Length > 0).ToArray();
        var grid = new List<int[]>();
        foreach (string row in rows)
        {
            var days = new int[7];
            for (int column = 0; column * 3 < row.Length; column++)
            {
                string cell = row.Substring(column * 3, Math.Min(2, row.Length - (column * 3))).Trim();
                days[column] = cell.Length == 0 ? 0 : int.Parse(cell, CultureInfo.InvariantCulture);
            }
            grid.Add(days);
        }
        return grid.ToArray();
    }

    /// <summary>The same shape from the layout model: weeks from Monday, the day in each cell or 0.</summary>
    private static int[][] LayoutGrid(int year, int month) => MonthLayout.Create(year, month, DayOfWeek.Monday).Weeks
        .Select(week => week.Cells.Select(cell => cell.Date?.Day ?? 0).ToArray())
        .ToArray();

    [Test]
    public void TheWeekdayRow_IsTheColumnsOfAMondayFirstLayout()
    {
        string weekdayRow = ConsoleCapture.Run(() => new ConsoleCalendar().GenerateMonth(5, 2023)).Text.Split('\n')[1];

        IEnumerable<string> columns = MonthLayout.Create(2023, 5, DayOfWeek.Monday).DaysOfWeek.Select(day => day.ToString().Substring(0, 2));

        Assert.That(weekdayRow, Is.EqualTo(string.Join(" ", columns)));
    }

    [Test]
    public void February2023_IsDrawnWhereTheLayoutPutsIt()
    {
        Assert.That(DrawnGrid(2023, 2), Is.EqualTo(new[]
        {
            new[] { 0, 0, 1, 2, 3, 4, 5 },
            new[] { 6, 7, 8, 9, 10, 11, 12 },
            new[] { 13, 14, 15, 16, 17, 18, 19 },
            new[] { 20, 21, 22, 23, 24, 25, 26 },
            new[] { 27, 28, 0, 0, 0, 0, 0 },
        }));
        Assert.That(LayoutGrid(2023, 2), Is.EqualTo(DrawnGrid(2023, 2)));
    }

    /// <summary>2,412 months, among them every weekday for the 1st with every month length.</summary>
    [Test]
    public void EveryMonthFrom1900Through2100_IsDrawnWhereTheLayoutPutsIt()
    {
        var different = new List<string>();
        var weekCounts = new SortedSet<int>();

        for (int year = 1900; year <= 2100; year++)
        {
            for (int month = 1; month <= 12; month++)
            {
                int[][] drawn = DrawnGrid(year, month);
                int[][] layout = LayoutGrid(year, month);
                weekCounts.Add(drawn.Length);
                if (drawn.Length != layout.Length || !drawn.Zip(layout, (a, b) => a.SequenceEqual(b)).All(same => same))
                {
                    different.Add($"{year}-{month:00}");
                }
            }
        }

        Assert.That(different, Is.Empty);
        Assert.That(weekCounts, Is.EqualTo(new[] { 4, 5, 6 }));
    }

    [TestCase(1)]
    [TestCase(9999)]
    public void EveryMonthOfTheFirstAndTheLastYear_IsDrawnWhereTheLayoutPutsIt(int year)
    {
        for (int month = 1; month <= 12; month++)
        {
            Assert.That(DrawnGrid(year, month), Is.EqualTo(LayoutGrid(year, month)), $"{year}-{month:00}");
        }
    }

    /// <summary>Selected dates change colors only, so the grid is where the layout puts it with them too.</summary>
    [Test]
    public void SelectedDates_DoNotMoveAnything()
    {
        for (int month = 1; month <= 12; month++)
        {
            int m = month;
            var everyThirdDay = Enumerable.Range(1, DateTime.DaysInMonth(2024, month)).Where(day => day % 3 == 1)
                .Select(day => new DateTime(2024, m, day)).ToList();

            Assert.That(DrawnGrid(2024, month, everyThirdDay), Is.EqualTo(LayoutGrid(2024, month)), $"2024-{month:00}");
        }
    }
}
