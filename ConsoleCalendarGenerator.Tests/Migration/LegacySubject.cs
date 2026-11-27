using CurrentCalendar = CalendarGenerator.ConsoleCalendar;
using Published100Calendar = ConsoleCalendarGenerator.Tests.Historical.Published100.ConsoleCalendar;

namespace ConsoleCalendarGenerator.Tests.Migration;

/// <summary>
/// The members of the legacy calendar, so that the same test code can drive either implementation of it. Every
/// member passes its arguments straight on and adds nothing: no check, no console call, no catch.
/// </summary>
internal interface ILegacyCalendar
{
    void GenerateMonth(int month, int year);

    void GenerateMonth(int month, int year, List<DateTime> dates);

    void GenerateYear(int year);

    void GenerateYear(int year, List<DateTime> dates);

    ConsoleColor GetTitleColor();

    ConsoleColor GetDayOfWeekColor();

    ConsoleColor GetDateColor();

    ConsoleColor GetSelectedDateColor();

    void SetTitleColor(ConsoleColor color);

    void SetDayOfWeekColor(ConsoleColor color);

    void SetDateColor(ConsoleColor color);

    void SetSelectedDateColor(ConsoleColor color);
}

/// <summary>
/// One implementation of the legacy calendar: the class in the library under test, or the frozen source of
/// version 1.0.0. The migration tests run both through the same scenarios and compare what they do.
/// </summary>
internal abstract class LegacySubject
{
    /// <summary><c>CalendarGenerator.ConsoleCalendar</c> of the library the tests run against.</summary>
    public static LegacySubject Library { get; } = new LibrarySubject();

    /// <summary>The source of 1.0.0, frozen in Historical/Published100/ConsoleCalendar.cs.</summary>
    public static LegacySubject Published100 { get; } = new Published100Subject();

    public abstract string Name { get; }

    /// <summary>
    /// Calls the constructor that takes this many colors, none to four, with the colors in the order given. The
    /// order is the published one: <c>TitleColor</c>, <c>DateColor</c>, <c>SelectedDateColor</c>,
    /// <c>DayOfWeekColor</c>.
    /// </summary>
    public abstract ILegacyCalendar Create(params ConsoleColor[] constructorArguments);

    public override string ToString() => Name;

    private static ArgumentException NoSuchConstructor(ConsoleColor[] arguments) =>
        new("The legacy calendar has no constructor that takes " + arguments.Length + " colors.", nameof(arguments));

    private sealed class LibrarySubject : LegacySubject
    {
        public override string Name => "the library";

        public override ILegacyCalendar Create(params ConsoleColor[] constructorArguments) => new Adapter(constructorArguments.Length switch
        {
            0 => new CurrentCalendar(),
            1 => new CurrentCalendar(constructorArguments[0]),
            2 => new CurrentCalendar(constructorArguments[0], constructorArguments[1]),
            3 => new CurrentCalendar(constructorArguments[0], constructorArguments[1], constructorArguments[2]),
            4 => new CurrentCalendar(constructorArguments[0], constructorArguments[1], constructorArguments[2], constructorArguments[3]),
            _ => throw NoSuchConstructor(constructorArguments),
        });

        private sealed class Adapter : ILegacyCalendar
        {
            private readonly CurrentCalendar _calendar;

            public Adapter(CurrentCalendar calendar) => _calendar = calendar;

            public void GenerateMonth(int month, int year) => _calendar.GenerateMonth(month, year);

            public void GenerateMonth(int month, int year, List<DateTime> dates) => _calendar.GenerateMonth(month, year, dates);

            public void GenerateYear(int year) => _calendar.GenerateYear(year);

            public void GenerateYear(int year, List<DateTime> dates) => _calendar.GenerateYear(year, dates);

            public ConsoleColor GetTitleColor() => _calendar.GetTitleColor();

            public ConsoleColor GetDayOfWeekColor() => _calendar.GetDayOfWeekColor();

            public ConsoleColor GetDateColor() => _calendar.GetDateColor();

            public ConsoleColor GetSelectedDateColor() => _calendar.GetSelectedDateColor();

            public void SetTitleColor(ConsoleColor color) => _calendar.SetTitleColor(color);

            public void SetDayOfWeekColor(ConsoleColor color) => _calendar.SetDayOfWeekColor(color);

            public void SetDateColor(ConsoleColor color) => _calendar.SetDateColor(color);

            public void SetSelectedDateColor(ConsoleColor color) => _calendar.SetSelectedDateColor(color);
        }
    }

    private sealed class Published100Subject : LegacySubject
    {
        public override string Name => "the frozen source of 1.0.0";

        public override ILegacyCalendar Create(params ConsoleColor[] constructorArguments) => new Adapter(constructorArguments.Length switch
        {
            0 => new Published100Calendar(),
            1 => new Published100Calendar(constructorArguments[0]),
            2 => new Published100Calendar(constructorArguments[0], constructorArguments[1]),
            3 => new Published100Calendar(constructorArguments[0], constructorArguments[1], constructorArguments[2]),
            4 => new Published100Calendar(constructorArguments[0], constructorArguments[1], constructorArguments[2], constructorArguments[3]),
            _ => throw NoSuchConstructor(constructorArguments),
        });

        private sealed class Adapter : ILegacyCalendar
        {
            private readonly Published100Calendar _calendar;

            public Adapter(Published100Calendar calendar) => _calendar = calendar;

            public void GenerateMonth(int month, int year) => _calendar.GenerateMonth(month, year);

            public void GenerateMonth(int month, int year, List<DateTime> dates) => _calendar.GenerateMonth(month, year, dates);

            public void GenerateYear(int year) => _calendar.GenerateYear(year);

            public void GenerateYear(int year, List<DateTime> dates) => _calendar.GenerateYear(year, dates);

            public ConsoleColor GetTitleColor() => _calendar.GetTitleColor();

            public ConsoleColor GetDayOfWeekColor() => _calendar.GetDayOfWeekColor();

            public ConsoleColor GetDateColor() => _calendar.GetDateColor();

            public ConsoleColor GetSelectedDateColor() => _calendar.GetSelectedDateColor();

            public void SetTitleColor(ConsoleColor color) => _calendar.SetTitleColor(color);

            public void SetDayOfWeekColor(ConsoleColor color) => _calendar.SetDayOfWeekColor(color);

            public void SetDateColor(ConsoleColor color) => _calendar.SetDateColor(color);

            public void SetSelectedDateColor(ConsoleColor color) => _calendar.SetSelectedDateColor(color);
        }
    }
}
