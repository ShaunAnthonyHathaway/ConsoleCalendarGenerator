using ConsoleCalendarGenerator.Tests.Support;

namespace ConsoleCalendarGenerator.Tests.Migration;

/// <summary>
/// The scenarios in which the library is compared with the frozen source of 1.0.0. There is one group for each
/// of the five behaviors that 2.0 corrects, holding the scenarios in which 1.0.0 shows that behavior, and one
/// group for behavior that no correction is about. Every scenario is compared in every aspect, whichever group
/// it is in.
///
/// <para>
/// Real cultures are named, not looked up here, so that the list is the same on every machine. Exact text never
/// depends on them: both implementations run under the same culture and are compared with each other.
/// </para>
/// </summary>
internal static class MigrationMatrix
{
    // One color for each constructor parameter, all different from each other and from the four defaults.
    private const ConsoleColor TitleArgument = ConsoleColor.Red;
    private const ConsoleColor DateArgument = ConsoleColor.Green;
    private const ConsoleColor SelectedDateArgument = ConsoleColor.Magenta;
    private const ConsoleColor DayOfWeekArgument = ConsoleColor.DarkYellow;

    private static readonly ConsoleColor[] FourArguments = { TitleArgument, DateArgument, SelectedDateArgument, DayOfWeekArgument };

    private static readonly ConsoleColor Undefined = (ConsoleColor)99;

    /// <summary>Selected dates: in and out of the months drawn, a duplicate, a time of day, kinds, the 1st and last days.</summary>
    private static readonly DateTime[] Selection =
    {
        new DateTime(2023, 2, 1),
        new DateTime(2023, 2, 14),
        new DateTime(2023, 2, 14, 23, 59, 59, DateTimeKind.Utc),
        new DateTime(2023, 2, 28, 12, 0, 0, DateTimeKind.Local),
        new DateTime(2023, 4, 30),
        new DateTime(2023, 5, 31),
        new DateTime(2023, 10, 9),
        new DateTime(2023, 12, 31),
        new DateTime(2024, 1, 1),
        new DateTime(2024, 2, 29),
        new DateTime(2024, 6, 30),
        new DateTime(2022, 2, 14),
        new DateTime(1, 1, 1),
        new DateTime(9999, 12, 31),
    };

    /// <summary>The constructors that take colors (D-57).</summary>
    public static IReadOnlyList<LegacyScenario> ConstructorColors { get; } = Group("constructor colors", ConstructorColorScenarios());

    /// <summary>A null list (D-58).</summary>
    public static IReadOnlyList<LegacyScenario> NullDateList { get; } = Group("null list", NullDateListScenarios());

    /// <summary>The end of the last row of days (D-59).</summary>
    public static IReadOnlyList<LegacyScenario> LineTerminator { get; } = Group("line terminator", LineTerminatorScenarios());

    /// <summary>Months whose last day is a Sunday (D-60).</summary>
    public static IReadOnlyList<LegacyScenario> BlankLineAfterSunday { get; } = Group("blank line after Sunday", BlankLineScenarios());

    /// <summary>Titles under cultures whose own calendar is not the Gregorian one (D-61).</summary>
    public static IReadOnlyList<LegacyScenario> CultureTitle { get; } = Group("culture title", CultureTitleScenarios());

    /// <summary>Behavior that none of the five corrections is about.</summary>
    public static IReadOnlyList<LegacyScenario> OtherBehavior { get; } = Group("other behavior", OtherBehaviorScenarios());

    public static IReadOnlyList<LegacyScenario> All { get; } =
        ConstructorColors.Concat(NullDateList).Concat(LineTerminator).Concat(BlankLineAfterSunday).Concat(CultureTitle).Concat(OtherBehavior).ToArray();

    private static LegacyScenario[] Group(string area, IEnumerable<LegacyScenario> scenarios) =>
        scenarios.Select(scenario => scenario.In(area)).ToArray();

    private static IEnumerable<LegacyScenario> ConstructorColorScenarios()
    {
        for (int count = 1; count <= 4; count++)
        {
            ConsoleColor[] arguments = FourArguments.Take(count).ToArray();
            yield return new LegacyScenario(count + " arguments, the colors held") { ConstructorArguments = arguments };
            yield return new LegacyScenario(count + " arguments, a month with selected dates")
            {
                ConstructorArguments = arguments,
                Draw = LegacyDraw.MonthWithDates,
                Month = 2,
                Year = 2023,
                Dates = Selection,
                ForegroundBefore = ConsoleColor.DarkCyan,
                BackgroundBefore = ConsoleColor.DarkBlue,
            };
        }
        yield return new LegacyScenario("4 arguments, a year")
        {
            ConstructorArguments = FourArguments,
            Draw = LegacyDraw.YearWithDates,
            Year = 2024,
            Dates = Selection,
        };
        yield return new LegacyScenario("4 arguments, then every setter")
        {
            ConstructorArguments = FourArguments,
            Setters = new[]
            {
                (LegacyColorRole.Title, ConsoleColor.DarkGreen),
                (LegacyColorRole.DayOfWeek, ConsoleColor.DarkRed),
                (LegacyColorRole.Date, ConsoleColor.Gray),
                (LegacyColorRole.SelectedDate, ConsoleColor.DarkMagenta),
            },
            Draw = LegacyDraw.MonthWithDates,
            Month = 2,
            Year = 2023,
            Dates = Selection,
        };
        // A color that is not defined fails when it is first used, so which element an argument colors also
        // decides how much is written before the failure.
        yield return new LegacyScenario("1 argument that is not a defined color")
        {
            ConstructorArguments = new[] { Undefined },
            Draw = LegacyDraw.Month,
            Month = 2,
            Year = 2023,
        };
        yield return new LegacyScenario("4 arguments, the last not a defined color")
        {
            ConstructorArguments = new[] { TitleArgument, DateArgument, SelectedDateArgument, Undefined },
            Draw = LegacyDraw.Month,
            Month = 2,
            Year = 2023,
        };
    }

    private static IEnumerable<LegacyScenario> NullDateListScenarios()
    {
        yield return new LegacyScenario("a month") { Draw = LegacyDraw.MonthWithDates, Month = 2, Year = 2023, Dates = null };
        yield return new LegacyScenario("a year") { Draw = LegacyDraw.YearWithDates, Year = 2023, Dates = null };
        yield return new LegacyScenario("a month, with the caller's colors set")
        {
            Draw = LegacyDraw.MonthWithDates,
            Month = 2,
            Year = 2023,
            Dates = null,
            ForegroundBefore = ConsoleColor.DarkCyan,
            BackgroundBefore = ConsoleColor.DarkBlue,
        };
        // The list is looked at before the month and the year.
        foreach ((int month, int year) in new[] { (13, 2023), (0, 2023), (1, 0), (1, 10000), (13, 0), (int.MinValue, int.MaxValue) })
        {
            yield return new LegacyScenario($"month {month} of year {year}") { Draw = LegacyDraw.MonthWithDates, Month = month, Year = year, Dates = null };
        }
        foreach (int year in new[] { 0, 10000, -1 })
        {
            yield return new LegacyScenario($"year {year}") { Draw = LegacyDraw.YearWithDates, Year = year, Dates = null };
        }
        yield return new LegacyScenario("a month, from a constructor that takes colors")
        {
            ConstructorArguments = FourArguments,
            Draw = LegacyDraw.MonthWithDates,
            Month = 2,
            Year = 2023,
            Dates = null,
        };
    }

    private static IEnumerable<LegacyScenario> LineTerminatorScenarios()
    {
        foreach (string newLine in new[] { "\n", "\r\n", "<EOL>" })
        {
            string with = ", terminator " + Show(newLine);
            yield return new LegacyScenario("May 2023, which ends on a Wednesday" + with) { Draw = LegacyDraw.Month, Month = 5, Year = 2023, NewLine = newLine };
            yield return new LegacyScenario("September 2023, which ends on a Saturday" + with) { Draw = LegacyDraw.Month, Month = 9, Year = 2023, NewLine = newLine };
            yield return new LegacyScenario("July 2023, which ends on a Monday" + with) { Draw = LegacyDraw.Month, Month = 7, Year = 2023, NewLine = newLine };
        }
        yield return new LegacyScenario("February 2024, terminator \\r\\n") { Draw = LegacyDraw.Month, Month = 2, Year = 2024, NewLine = "\r\n" };
        yield return new LegacyScenario("a month whose last day is selected, terminator \\r\\n")
        {
            Draw = LegacyDraw.MonthWithDates,
            Month = 5,
            Year = 2023,
            Dates = Selection,
            NewLine = "\r\n",
            ForegroundBefore = ConsoleColor.DarkCyan,
            BackgroundBefore = ConsoleColor.DarkBlue,
        };
        yield return new LegacyScenario("the year 2022, terminator \\r\\n") { Draw = LegacyDraw.Year, Year = 2022, NewLine = "\r\n" };
        yield return new LegacyScenario("the year 2022, terminator \\n") { Draw = LegacyDraw.Year, Year = 2022 };
    }

    private static IEnumerable<LegacyScenario> BlankLineScenarios()
    {
        foreach ((int month, int year, string what) in new[]
        {
            (4, 2023, "April 2023"),
            (12, 2023, "December 2023"),
            (2, 2021, "February 2021, four full weeks"),
            (6, 2024, "June 2024"),
        })
        {
            yield return new LegacyScenario(what) { Draw = LegacyDraw.Month, Month = month, Year = year };
            yield return new LegacyScenario(what + ", terminator \\r\\n") { Draw = LegacyDraw.Month, Month = month, Year = year, NewLine = "\r\n" };
        }
        yield return new LegacyScenario("April 2023 with its last day selected")
        {
            Draw = LegacyDraw.MonthWithDates,
            Month = 4,
            Year = 2023,
            Dates = Selection,
            ForegroundBefore = ConsoleColor.DarkCyan,
            BackgroundBefore = ConsoleColor.DarkBlue,
        };
        yield return new LegacyScenario("the year 2023, with two such months") { Draw = LegacyDraw.YearWithDates, Year = 2023, Dates = Selection };
        yield return new LegacyScenario("the year 2024, terminator \\r\\n") { Draw = LegacyDraw.YearWithDates, Year = 2024, Dates = Selection, NewLine = "\r\n" };
    }

    private static IEnumerable<LegacyScenario> CultureTitleScenarios()
    {
        // The Persian calendar, whose months do not line up with the Gregorian ones.
        foreach (int month in new[] { 1, 5, 9, 12 })
        {
            yield return new LegacyScenario("fa-IR, month " + month) { Draw = LegacyDraw.Month, Month = month, Year = 2023, CultureName = "fa-IR" };
        }
        yield return new LegacyScenario("fa-IR, a year with selected dates") { Draw = LegacyDraw.YearWithDates, Year = 2023, Dates = Selection, CultureName = "fa-IR" };
        // The Thai Buddhist calendar, whose months are the Gregorian ones and whose years are not.
        yield return new LegacyScenario("th-TH, month 9") { Draw = LegacyDraw.Month, Month = 9, Year = 2023, CultureName = "th-TH" };
        yield return new LegacyScenario("th-TH, a year") { Draw = LegacyDraw.Year, Year = 2023, CultureName = "th-TH" };
        // A second culture with the Persian calendar, and other names for its months.
        yield return new LegacyScenario("ps-AF, a year") { Draw = LegacyDraw.Year, Year = 2023, CultureName = "ps-AF" };
        // A culture whose own calendar is the Um Al Qura calendar on some systems and the Gregorian one on others.
        yield return new LegacyScenario("ar-SA, a year") { Draw = LegacyDraw.Year, Year = 2023, CultureName = "ar-SA" };
    }

    private static IEnumerable<LegacyScenario> OtherBehaviorScenarios()
    {
        // The colors held, with no constructor argument involved.
        yield return new LegacyScenario("the parameterless constructor, the colors held");
        yield return new LegacyScenario("every setter, the colors held")
        {
            Setters = new[]
            {
                (LegacyColorRole.Title, ConsoleColor.DarkGreen),
                (LegacyColorRole.DayOfWeek, ConsoleColor.DarkRed),
                (LegacyColorRole.Date, ConsoleColor.Gray),
                (LegacyColorRole.SelectedDate, ConsoleColor.DarkMagenta),
                (LegacyColorRole.Date, ConsoleColor.DarkGray),
            },
        };
        yield return new LegacyScenario("setters with values that are not defined colors, the colors held")
        {
            Setters = new[] { (LegacyColorRole.Title, (ConsoleColor)16), (LegacyColorRole.SelectedDate, (ConsoleColor)(-1)) },
        };

        // Invalid months and years: the runtime's own exception, with its parameter name and its message,
        // before anything is written and with the caller's colors left alone.
        foreach ((int month, int year) in new[]
        {
            (0, 2023), (13, 2023), (-1, 2023), (int.MinValue, 2023), (int.MaxValue, 2023),
            (1, 0), (1, 10000), (1, -1), (1, int.MinValue), (1, int.MaxValue), (0, 0), (13, 10000), (13, 0),
        })
        {
            yield return new LegacyScenario($"invalid month {month} of year {year}")
            {
                Draw = LegacyDraw.Month,
                Month = month,
                Year = year,
                ForegroundBefore = ConsoleColor.DarkCyan,
                BackgroundBefore = ConsoleColor.DarkBlue,
            };
            yield return new LegacyScenario($"invalid month {month} of year {year}, with dates") { Draw = LegacyDraw.MonthWithDates, Month = month, Year = year, Dates = Selection };
        }
        foreach (int year in new[] { 0, 10000, -1 })
        {
            yield return new LegacyScenario($"invalid year {year}") { Draw = LegacyDraw.Year, Year = year };
            yield return new LegacyScenario($"invalid year {year}, with dates") { Draw = LegacyDraw.YearWithDates, Year = year, Dates = Selection };
        }

        // A color that is not defined, set through a setter: where the drawing fails and what was written first.
        // 16 is the first value after the sixteen colors.
        foreach (ConsoleColor undefined in new[] { (ConsoleColor)16, Undefined })
        {
            string value = " (" + (int)undefined + ")";
            foreach (LegacyColorRole role in Enum.GetValues<LegacyColorRole>())
            {
                yield return new LegacyScenario("a " + role + " color that is not defined" + value)
                {
                    Setters = new[] { (role, undefined) },
                    Draw = LegacyDraw.MonthWithDates,
                    Month = 10,
                    Year = 2023,
                    Dates = Selection,
                    ForegroundBefore = ConsoleColor.DarkCyan,
                    BackgroundBefore = ConsoleColor.DarkBlue,
                };
            }
            yield return new LegacyScenario("a SelectedDate color that is not defined, with the 1st selected" + value)
            {
                Setters = new[] { (LegacyColorRole.SelectedDate, undefined) },
                Draw = LegacyDraw.MonthWithDates,
                Month = 2,
                Year = 2023,
                Dates = Selection,
            };
            yield return new LegacyScenario("a SelectedDate color that is not defined and never used" + value)
            {
                Setters = new[] { (LegacyColorRole.SelectedDate, undefined) },
                Draw = LegacyDraw.MonthWithDates,
                Month = 3,
                Year = 2023,
                Dates = Selection,
            };
            yield return new LegacyScenario("a SelectedDate color that is not defined, in a year" + value)
            {
                Setters = new[] { (LegacyColorRole.SelectedDate, undefined) },
                Draw = LegacyDraw.YearWithDates,
                Year = 2023,
                Dates = Selection,
            };
        }

        // Selected dates. The 1st of February 2023 is a Wednesday, so its color is also that of the empty cells.
        yield return new LegacyScenario("selected dates, with setters and the caller's colors set")
        {
            Setters = new[] { (LegacyColorRole.Title, ConsoleColor.DarkGreen), (LegacyColorRole.SelectedDate, ConsoleColor.DarkMagenta) },
            Draw = LegacyDraw.MonthWithDates,
            Month = 2,
            Year = 2023,
            Dates = Selection,
            ForegroundBefore = ConsoleColor.DarkCyan,
            BackgroundBefore = ConsoleColor.DarkBlue,
        };
        yield return new LegacyScenario("an empty list") { Draw = LegacyDraw.MonthWithDates, Month = 2, Year = 2023 };
        yield return new LegacyScenario("every day selected")
        {
            Draw = LegacyDraw.MonthWithDates,
            Month = 10,
            Year = 2023,
            Dates = Enumerable.Range(1, 31).Select(day => new DateTime(2023, 10, day)).ToArray(),
        };
        yield return new LegacyScenario("only dates of other months and years")
        {
            Draw = LegacyDraw.MonthWithDates,
            Month = 10,
            Year = 2023,
            Dates = new[] { new DateTime(2023, 9, 9), new DateTime(2022, 10, 9), new DateTime(2024, 10, 9) },
        };

        // The first and the last month there is.
        yield return new LegacyScenario("January of the year 1") { Draw = LegacyDraw.MonthWithDates, Month = 1, Year = 1, Dates = Selection };
        yield return new LegacyScenario("December of the year 9999") { Draw = LegacyDraw.MonthWithDates, Month = 12, Year = 9999, Dates = Selection };

        // The month name, which decides the indent of the title, and what does not follow the culture.
        foreach (int length in Enumerable.Range(0, 13).Append(30))
        {
            string name = new string('m', length);
            yield return new LegacyScenario("a month name of " + length + " characters")
            {
                Draw = LegacyDraw.Month,
                Month = 5,
                Year = 2023,
                Culture = TestCultures.WithMonthNames(_ => name),
            };
        }
        yield return new LegacyScenario("a month name with a letter outside ASCII")
        {
            Draw = LegacyDraw.Month,
            Month = 5,
            Year = 2023,
            Culture = TestCultures.WithMonthNames(_ => "M" + (char)0x00E4 + "rz"),
        };
        yield return new LegacyScenario("a month name with spaces around it")
        {
            Draw = LegacyDraw.Month,
            Month = 5,
            Year = 2023,
            Culture = TestCultures.WithMonthNames(month => " month " + month + " "),
        };
        yield return new LegacyScenario("a culture with another week, and a UI culture with other month names")
        {
            Draw = LegacyDraw.Month,
            Month = 5,
            Year = 2023,
            Culture = TestCultures.WithForeignWeek(),
            UICulture = TestCultures.WithMonthNames(_ => "ui"),
        };
        foreach (string culture in new[] { "en-US", "de-DE", "fr-FR", "ja-JP" })
        {
            yield return new LegacyScenario(culture + ", a year") { Draw = LegacyDraw.Year, Year = 2023, CultureName = culture };
        }
    }

    private static string Show(string newLine) => newLine.Replace("\r", "\\r", StringComparison.Ordinal).Replace("\n", "\\n", StringComparison.Ordinal);
}
