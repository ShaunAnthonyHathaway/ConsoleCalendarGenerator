using System.Globalization;
using System.Reflection;
using System.Runtime.Versioning;
using CalendarGenerator;

// Uses the legacy class the way a program written for version 1.0.0 does, and says what it observes. Every
// line it prints is one observable fact. The facts under "unchanged" are the same with 1.0.0 and with 2.0;
// the five under "corrected in 2.0" are the five behaviors that version 2.0 corrects, and each line reads
// differently with the two versions. The scripts compare the output with expected-1.0.0.txt and
// expected-2.0.txt. Nothing here depends on a terminal, on colors being readable or on the operating system.

Assembly library = typeof(ConsoleCalendar).Assembly;
Console.WriteLine("library: " + library.GetCustomAttribute<TargetFrameworkAttribute>()?.FrameworkName
    + ", assembly version " + library.GetName().Version);

CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;

Console.WriteLine();
Console.WriteLine("[unchanged]");
string february = Draw(calendar => calendar.GenerateMonth(2, 2023));
string[] lines = february.Split('\n');
Console.WriteLine("title: \"" + lines[0] + "\"");
Console.WriteLine("weekday row: \"" + lines[1] + "\"");
Console.WriteLine("first week, from Monday: \"" + lines[2] + "\"");
Console.WriteLine("last row: \"" + lines[6] + "\"");
string selected = Draw(calendar => calendar.GenerateMonth(2, 2023, new List<DateTime> { new DateTime(2023, 2, 14, 9, 30, 0), new DateTime(2024, 2, 15) }));
Console.WriteLine("a selected date changes the text: " + (selected != february));
string year = Draw(calendar => calendar.GenerateYear(2023));
Console.WriteLine("GenerateYear draws this many weekday rows: " + year.Split('\n').Count(line => line == "Mo Tu We Th Fr Sa Su"));
var defaults = new ConsoleCalendar();
Console.WriteLine("default colors: title " + defaults.GetTitleColor() + ", weekday row " + defaults.GetDayOfWeekColor()
    + ", dates " + defaults.GetDateColor() + ", selected dates " + defaults.GetSelectedDateColor());
defaults.SetTitleColor(ConsoleColor.Green);
defaults.SetDayOfWeekColor(ConsoleColor.DarkRed);
defaults.SetDateColor(ConsoleColor.Gray);
defaults.SetSelectedDateColor(ConsoleColor.DarkMagenta);
Console.WriteLine("after the four setters: title " + defaults.GetTitleColor() + ", weekday row " + defaults.GetDayOfWeekColor()
    + ", dates " + defaults.GetDateColor() + ", selected dates " + defaults.GetSelectedDateColor());
Console.WriteLine("month 13: " + Thrown(calendar => calendar.GenerateMonth(13, 2023)));
Console.WriteLine("year 0: " + Thrown(calendar => calendar.GenerateYear(0)));
Console.WriteLine("named arguments compile: " + (new ConsoleCalendar(TitleColor: ConsoleColor.Red, DateColor: ConsoleColor.White,
    SelectedDateColor: ConsoleColor.Blue, DayOfWeekColor: ConsoleColor.Cyan).GetDateColor() == ConsoleColor.White));

Console.WriteLine();
Console.WriteLine("[corrected in 2.0]");

// 1. Which element a constructor argument colors.
var one = new ConsoleCalendar(ConsoleColor.Red);
var four = new ConsoleCalendar(ConsoleColor.Red, ConsoleColor.Green, ConsoleColor.Blue, ConsoleColor.Magenta);
Console.WriteLine("1. new ConsoleCalendar(Red): title " + one.GetTitleColor() + ", weekday row " + one.GetDayOfWeekColor());
Console.WriteLine("   new ConsoleCalendar(Red, Green, Blue, Magenta): title " + four.GetTitleColor() + ", weekday row " + four.GetDayOfWeekColor()
    + ", dates " + four.GetDateColor() + ", selected dates " + four.GetSelectedDateColor());

// 2. The exception for a null list, which comes before an invalid month is looked at.
Console.WriteLine("2. a null list: " + Thrown(calendar => calendar.GenerateMonth(2, 2023, null!)));
Console.WriteLine("   a null list and month 13: " + Thrown(calendar => calendar.GenerateMonth(13, 2023, null!)));

// 3. The line end of the last row of days, under a writer whose line terminator is CRLF.
string windows = Draw(calendar => calendar.GenerateMonth(2, 2023), "\r\n");
Console.WriteLine("3. with a CRLF writer: " + (windows.Replace("\r\n", string.Empty).Contains('\n') ? "a bare LF is mixed in" : "every line ends with CRLF"));

// 4. The blank lines after a month whose last day is a Sunday.
string april = Draw(calendar => calendar.GenerateMonth(4, 2023));
Console.WriteLine("4. blank lines after April 2023, which ends on a Sunday: " + (april.Length - april.TrimEnd('\n').Length - 1));
Console.WriteLine("   blank lines after February 2023, which does not: " + (february.Length - february.TrimEnd('\n').Length - 1));

// 5. The month the title names under a culture whose calendar is not the Gregorian one. The names are the
//    system's, so they are compared and not printed.
Console.WriteLine("5. the title under a culture with another calendar: " + TitleUnderAnotherCalendar());

#if MODERN_API
Console.WriteLine();
Modern.Run();
#endif
return 0;

static string Draw(Action<ConsoleCalendar> draw, string newLine = "\n")
{
    TextWriter console = Console.Out;
    var text = new StringWriter { NewLine = newLine };
    try
    {
        Console.SetOut(text);
        draw(new ConsoleCalendar());
    }
    finally
    {
        Console.SetOut(console);
    }
    return text.ToString();
}

static string Thrown(Action<ConsoleCalendar> draw)
{
    try
    {
        Draw(draw);
        return "nothing thrown";
    }
    catch (ArgumentException exception)
    {
        return exception.GetType().Name + ", parameter " + (exception.ParamName ?? "(none)");
    }
    catch (Exception exception)
    {
        return exception.GetType().Name;
    }
}

static string TitleUnderAnotherCalendar()
{
    // A culture given one of the other calendars it offers, which is the same on every system that has it.
    foreach ((string name, Type calendarType) in new[] { ("he-IL", typeof(HebrewCalendar)), ("ar-SA", typeof(UmAlQuraCalendar)), ("fa-IR", typeof(PersianCalendar)) })
    {
        CultureInfo culture;
        try
        {
            culture = (CultureInfo)CultureInfo.GetCultureInfo(name).Clone();
        }
        catch (CultureNotFoundException)
        {
            continue;
        }
        Calendar? other = culture.OptionalCalendars.FirstOrDefault(calendar => calendar.GetType() == calendarType);
        Calendar? gregorian = culture.OptionalCalendars.FirstOrDefault(calendar => calendar is GregorianCalendar);
        if (other == null || gregorian == null)
        {
            continue;
        }
        culture.DateTimeFormat.Calendar = gregorian;
        string gregorianName = culture.DateTimeFormat.GetMonthName(9);
        culture.DateTimeFormat.Calendar = other;
        string otherName = culture.DateTimeFormat.GetMonthName(9);
        if (gregorianName == otherName)
        {
            continue;
        }

        CultureInfo.CurrentCulture = culture;
        string title;
        try
        {
            title = Draw(calendar => calendar.GenerateMonth(9, 2023)).Split('\n')[0].Trim();
        }
        finally
        {
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        }
        return title == gregorianName + " 2023" ? "names the Gregorian month that is drawn"
            : title == otherName + " 2023" ? "names the month of that number in the other calendar"
            : "is something else";
    }
    return "no such culture on this system";
}
