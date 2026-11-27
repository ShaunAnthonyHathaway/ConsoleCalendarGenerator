using System.Globalization;
using ConsoleCalendarGenerator.Tests.Support;

namespace ConsoleCalendarGenerator.Tests.Migration;

/// <summary>The four colors of the legacy calendar.</summary>
internal enum LegacyColorRole
{
    Title,
    DayOfWeek,
    Date,
    SelectedDate,
}

/// <summary>Which of the four drawing methods a scenario calls, if any.</summary>
internal enum LegacyDraw
{
    /// <summary>Nothing is drawn. The scenario is about the colors the calendar holds.</summary>
    Nothing,

    /// <summary><c>GenerateMonth(Month, Year)</c>.</summary>
    Month,

    /// <summary><c>GenerateMonth(Month, Year, dates)</c>.</summary>
    MonthWithDates,

    /// <summary><c>GenerateYear(Year)</c>.</summary>
    Year,

    /// <summary><c>GenerateYear(Year, dates)</c>.</summary>
    YearWithDates,
}

/// <summary>
/// One use of the legacy calendar, written as data: a constructor, setters, one drawing call, and the state of
/// the process it runs in. It is data and not a delegate so that it can be run against any
/// <see cref="LegacySubject"/>, and so that an approved correction can say from the scenario alone what it
/// changes (see <see cref="ApprovedCorrection"/>).
/// </summary>
internal sealed class LegacyScenario
{
    public LegacyScenario(string name)
    {
        Name = name;
    }

    public string Name { get; }

    /// <summary>The part of the matrix the scenario belongs to. Set by <see cref="MigrationMatrix"/>.</summary>
    public string Area { get; private set; } = string.Empty;

    /// <summary>None to four colors, in the order of the published constructor parameters.</summary>
    public IReadOnlyList<ConsoleColor> ConstructorArguments { get; init; } = Array.Empty<ConsoleColor>();

    /// <summary>Setters called after the constructor, in this order.</summary>
    public IReadOnlyList<(LegacyColorRole Role, ConsoleColor Color)> Setters { get; init; } = Array.Empty<(LegacyColorRole, ConsoleColor)>();

    public LegacyDraw Draw { get; init; } = LegacyDraw.Nothing;

    public int Month { get; init; }

    public int Year { get; init; }

    /// <summary>
    /// The list for the two forms that take one. Null means that a null list is passed. Every run gets a list
    /// of its own.
    /// </summary>
    public IReadOnlyList<DateTime>? Dates { get; init; } = Array.Empty<DateTime>();

    /// <summary>The line terminator of the writer that stands in for the console's.</summary>
    public string NewLine { get; init; } = "\n";

    /// <summary>A culture built by the test, to be the current culture. The invariant culture when neither this nor <see cref="CultureName"/> is set.</summary>
    public CultureInfo? Culture { get; init; }

    /// <summary>The name of a culture of the operating system, to be the current culture. It may be missing on a machine.</summary>
    public string? CultureName { get; init; }

    /// <summary>The current UI culture. The invariant culture when not set.</summary>
    public CultureInfo? UICulture { get; init; }

    /// <summary>A foreground color the caller has set before the calendar is used.</summary>
    public ConsoleColor? ForegroundBefore { get; init; }

    /// <summary>A background color the caller has set before the calendar is used.</summary>
    public ConsoleColor? BackgroundBefore { get; init; }

    /// <summary>True when the scenario calls one of the four constructors that take colors.</summary>
    public bool UsesAConstructorThatTakesColors => ConstructorArguments.Count > 0;

    /// <summary>True when the scenario passes a null list to a drawing method.</summary>
    public bool PassesANullList => Dates == null && Draw is LegacyDraw.MonthWithDates or LegacyDraw.YearWithDates;

    /// <summary>
    /// The current culture of the scenario, or null when it names a culture this machine does not have. A
    /// test then uses <c>Assume.That</c>, as the other tests with real cultures do.
    /// </summary>
    public CultureInfo? ResolveCulture()
    {
        if (CultureName != null)
        {
            return TestCultures.TryGet(CultureName);
        }
        return Culture ?? CultureInfo.InvariantCulture;
    }

    /// <summary>Creates the calendar, calls the setters and returns it, without drawing.</summary>
    public ILegacyCalendar CreateCalendar(LegacySubject subject)
    {
        ILegacyCalendar calendar = subject.Create(ConstructorArguments.ToArray());
        foreach ((LegacyColorRole role, ConsoleColor color) in Setters)
        {
            switch (role)
            {
                case LegacyColorRole.Title:
                    calendar.SetTitleColor(color);
                    break;
                case LegacyColorRole.DayOfWeek:
                    calendar.SetDayOfWeekColor(color);
                    break;
                case LegacyColorRole.Date:
                    calendar.SetDateColor(color);
                    break;
                case LegacyColorRole.SelectedDate:
                    calendar.SetSelectedDateColor(color);
                    break;
                default:
                    throw new InvalidOperationException("Unknown color role " + role + ".");
            }
        }
        return calendar;
    }

    /// <summary>Makes the drawing call of the scenario on a calendar.</summary>
    public void DrawWith(ILegacyCalendar calendar)
    {
        switch (Draw)
        {
            case LegacyDraw.Nothing:
                break;
            case LegacyDraw.Month:
                calendar.GenerateMonth(Month, Year);
                break;
            case LegacyDraw.MonthWithDates:
                calendar.GenerateMonth(Month, Year, Dates?.ToList()!);
                break;
            case LegacyDraw.Year:
                calendar.GenerateYear(Year);
                break;
            case LegacyDraw.YearWithDates:
                calendar.GenerateYear(Year, Dates?.ToList()!);
                break;
            default:
                throw new InvalidOperationException("Unknown drawing call " + Draw + ".");
        }
    }

    /// <summary>Puts the scenario in a part of the matrix and returns it.</summary>
    public LegacyScenario In(string area)
    {
        Area = area;
        return this;
    }

    public override string ToString() => Area.Length > 0 ? Area + ": " + Name : Name;
}
