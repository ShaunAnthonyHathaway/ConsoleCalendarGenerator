using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using CalendarGenerator;
using ConsoleCalendarGenerator.Tests.Migration;
using ConsoleCalendarGenerator.Tests.Support;

namespace ConsoleCalendarGenerator.Tests.Facade;

/// <summary>
/// Ties <see cref="ConsoleCalendar"/> to what it did before it was rebuilt on the modern layout.
///
/// The two values below were recorded on 2026-11-27 by running the code in this file against the class at commit
/// 64995b6, the last one with the original 1.0.0 implementation, and were not changed afterwards. They look
/// closer than the digests in Legacy/Published100BaselineTests.cs: for every write they record which method of
/// the console's writer was called (the original writes a two-digit Monday with <c>Console.Write(int)</c> and
/// everything else with strings), and they cover more: selected dates in many patterns, month names of every
/// length, both line terminators, all five constructors, setters, undefined colors, and invalid input.
///
/// Everything here uses the invariant culture or cultures built in the test, so the values are the same on every
/// system. If one stops matching, the legacy class changed. Do not record them again from the current code.
///
/// The scenarios take the implementation to run, so that Historical/Published100DigestTests.cs can put the
/// frozen source of 1.0.0 through the same code and compare it with the same two values.
/// </summary>
[TestFixture]
public class PreFacadeBaselineTests
{
    internal const string CallsBeforeTheFacade = "718a48c018593e81c16fd1c15739889effeeca101db9549edc63792980113c7d";
    internal const string ColorsBeforeTheFacade = "7693515e206b0278351d66cf6f8ca1692cc810497265913838ed39435a83f78b";

    /// <summary>The method, the text and the order of every call on the console's writer, and every exception.</summary>
    [Test]
    public void EveryCallOnTheConsolesWriter_IsWhatItWasBeforeTheFacade()
    {
        Assert.That(Digest(colors: false), Is.EqualTo(CallsBeforeTheFacade),
            "The legacy class writes differently than the original implementation. The tests in Legacy/ show where.");
    }

    /// <summary>The foreground and background color in effect for every one of those calls, and afterwards.</summary>
    [Test]
    [Platform(Exclude = "Win", Reason = ConsoleState.ColorsObservableOnUnixOnly)]
    public void TheColorOfEveryCall_IsWhatItWasBeforeTheFacade()
    {
        Assert.That(Digest(colors: true), Is.EqualTo(ColorsBeforeTheFacade),
            "The legacy class sets colors differently than the original implementation. The tests in Legacy/ show where.");
    }

    internal static string Digest(bool colors) => Digest(colors, LegacySubject.Library);

    /// <summary>The same for either implementation of the legacy calendar.</summary>
    internal static string Digest(bool colors, LegacySubject subject)
    {
        var all = new StringBuilder();
        foreach ((string name, string calls, string colorsOfCalls) in Scenarios(subject))
        {
            all.Append(name).Append('=').Append(colors ? colorsOfCalls : calls).Append((char)1);
        }
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(all.ToString()))).ToLowerInvariant();
    }

    /// <summary>Selected dates for 2023 and 2024: in and out of those years, duplicates, times of day and kinds.</summary>
    private static List<DateTime> Selection() => new()
    {
        new DateTime(2024, 1, 1),
        new DateTime(2024, 1, 1, 23, 59, 59, DateTimeKind.Utc),
        new DateTime(2024, 2, 29, 12, 0, 0, DateTimeKind.Local),
        new DateTime(2024, 3, 31),
        new DateTime(2024, 6, 30),
        new DateTime(2024, 9, 1),
        new DateTime(2024, 12, 31),
        new DateTime(2023, 1, 1),
        new DateTime(2023, 4, 30),
        new DateTime(2023, 10, 9),
        new DateTime(2023, 10, 10),
        new DateTime(2022, 10, 10),
        new DateTime(2025, 1, 1),
        new DateTime(1, 1, 1),
        new DateTime(9999, 12, 31),
    };

    private static IEnumerable<(string Name, string Calls, string Colors)> Scenarios(LegacySubject subject)
    {
        ILegacyCalendar plain = subject.Create();

        // Every month of 201 years, and of the first and the last year, without selected dates.
        yield return Run("1900-2100", () =>
        {
            for (int year = 1900; year <= 2100; year++)
            {
                for (int month = 1; month <= 12; month++)
                {
                    plain.GenerateMonth(month, year);
                }
            }
        });
        yield return Run("years 1 and 9999", () =>
        {
            plain.GenerateYear(1);
            plain.GenerateYear(9999);
        });

        // Selected dates: none, an empty list, a mixed list, every day, every n-th day, the 1st, the last day.
        yield return Run("selection, year", () => plain.GenerateYear(2024, Selection()));
        yield return Run("selection, months", () =>
        {
            for (int month = 1; month <= 12; month++)
            {
                plain.GenerateMonth(month, 2023, Selection());
                plain.GenerateMonth(month, 2023, new List<DateTime>());
            }
        });
        foreach (int step in new[] { 1, 2, 3, 7, 10 })
        {
            int every = step;
            yield return Run("every " + step, () =>
            {
                for (int year = 2023; year <= 2024; year++)
                {
                    for (int month = 1; month <= 12; month++)
                    {
                        int y = year;
                        int m = month;
                        plain.GenerateMonth(month, year, Enumerable.Range(1, DateTime.DaysInMonth(year, month))
                            .Where(day => day % every == 0).Select(day => new DateTime(y, m, day)).ToList());
                    }
                }
            });
        }
        yield return Run("first and last days", () =>
        {
            for (int month = 1; month <= 12; month++)
            {
                plain.GenerateMonth(month, 2023, new List<DateTime> { new DateTime(2023, month, 1) });
                plain.GenerateMonth(month, 2023, new List<DateTime> { new DateTime(2023, month, DateTime.DaysInMonth(2023, month)) });
            }
        });

        // Both line terminators of the console's writer.
        yield return Run("carriage return line feed", () => plain.GenerateYear(2024, Selection()), newLine: "\r\n");

        // The five constructors, and setters, with the caller's colors set beforehand.
        var constructed = new[]
        {
            subject.Create(),
            subject.Create(ConsoleColor.Red),
            subject.Create(ConsoleColor.Red, ConsoleColor.Green),
            subject.Create(ConsoleColor.Red, ConsoleColor.Green, ConsoleColor.Magenta),
            subject.Create(ConsoleColor.Red, ConsoleColor.Green, ConsoleColor.Magenta, ConsoleColor.DarkYellow),
        };
        for (int index = 0; index < constructed.Length; index++)
        {
            ILegacyCalendar calendar = constructed[index];
            yield return Run("constructor " + index, () => calendar.GenerateYear(2024, Selection()),
                foreground: ConsoleColor.DarkCyan, background: ConsoleColor.DarkBlue);
            yield return Run("constructor " + index + " getters", () => Console.Out.Write(string.Join(",",
                calendar.GetTitleColor(), calendar.GetDayOfWeekColor(), calendar.GetDateColor(), calendar.GetSelectedDateColor())));
        }
        yield return Run("setters", () =>
        {
            ILegacyCalendar calendar = subject.Create();
            calendar.SetTitleColor(ConsoleColor.DarkGreen);
            calendar.SetDayOfWeekColor(ConsoleColor.DarkRed);
            calendar.SetDateColor(ConsoleColor.Gray);
            calendar.SetSelectedDateColor(ConsoleColor.DarkMagenta);
            calendar.GenerateMonth(1, 2023, Selection());
            calendar.SetDateColor(ConsoleColor.DarkGray);
            calendar.GenerateMonth(10, 2023, Selection());
        }, background: ConsoleColor.DarkBlue);

        // Month names of every length from none to long, which decide the indent of the title, and names with
        // spaces and non-ASCII letters.
        for (int length = 0; length <= 12; length++)
        {
            string name = new string('m', length);
            yield return Run("name length " + length, () => plain.GenerateMonth(5, 2023), culture: TestCultures.WithMonthNames(_ => name));
        }
        yield return Run("long name", () => plain.GenerateMonth(5, 2023), culture: TestCultures.WithMonthNames(_ => new string('x', 30)));
        yield return Run("name with spaces", () => plain.GenerateMonth(5, 2023), culture: TestCultures.WithMonthNames(month => " month " + month + " "));
        yield return Run("accented name", () => plain.GenerateMonth(5, 2023), culture: TestCultures.WithMonthNames(_ => "M" + (char)0x00E4 + "rz"));
        yield return Run("another week", () => plain.GenerateMonth(5, 2023), culture: TestCultures.WithForeignWeek(), uiCulture: TestCultures.WithMonthNames(_ => "ui"));

        // An undefined color in each of the four places: where the render fails and what was written before.
        foreach (int value in new[] { 16, 99 })
        {
            var undefined = (ConsoleColor)value;
            yield return Run("undefined title " + value, () => With(subject, c => c.SetTitleColor(undefined)).GenerateMonth(1, 2023, Selection()));
            yield return Run("undefined weekday " + value, () => With(subject, c => c.SetDayOfWeekColor(undefined)).GenerateMonth(1, 2023, Selection()));
            yield return Run("undefined date " + value, () => With(subject, c => c.SetDateColor(undefined)).GenerateMonth(1, 2023, Selection()));
            yield return Run("undefined selected, the 1st " + value, () => With(subject, c => c.SetSelectedDateColor(undefined)).GenerateMonth(1, 2023, Selection()));
            yield return Run("undefined selected, later " + value, () => With(subject, c => c.SetSelectedDateColor(undefined)).GenerateMonth(10, 2023, Selection()));
            yield return Run("undefined selected, unused " + value, () => With(subject, c => c.SetSelectedDateColor(undefined)).GenerateMonth(5, 2023, Selection()));
            yield return Run("undefined in a year " + value, () => With(subject, c => c.SetSelectedDateColor(undefined)).GenerateYear(2023, Selection()));
        }

        // Invalid months, years and lists, alone and together.
        foreach ((int month, int year) in new[]
        {
            (0, 2023), (13, 2023), (-1, 2023), (int.MinValue, 2023), (int.MaxValue, 2023),
            (1, 0), (1, 10000), (1, -1), (1, int.MinValue), (1, int.MaxValue), (0, 0), (13, 10000), (13, 0),
        })
        {
            yield return Run($"invalid {month}/{year}", () => plain.GenerateMonth(month, year));
            yield return Run($"invalid {month}/{year} with dates", () => plain.GenerateMonth(month, year, Selection()));
            yield return Run($"invalid {month}/{year} with null", () => plain.GenerateMonth(month, year, null!));
        }
        foreach (int year in new[] { 0, 10000, -1 })
        {
            yield return Run("invalid year " + year, () => plain.GenerateYear(year));
            yield return Run("invalid year " + year + " with null", () => plain.GenerateYear(year, null!));
        }
        yield return Run("null list", () => plain.GenerateMonth(2, 2023, null!));
        yield return Run("null list, year", () => plain.GenerateYear(2023, null!));
    }

    private static ILegacyCalendar With(LegacySubject subject, Action<ILegacyCalendar> change)
    {
        ILegacyCalendar calendar = subject.Create();
        change(calendar);
        return calendar;
    }

    /// <summary>
    /// Runs one scenario with the console's writer replaced, and gives back its calls, the colors of its calls
    /// with the colors left behind, and the type of the exception that ended it, if one did.
    /// </summary>
    private static (string Name, string Calls, string Colors) Run(
        string name,
        Action scenario,
        string newLine = "\n",
        CultureInfo? culture = null,
        CultureInfo? uiCulture = null,
        ConsoleColor? foreground = null,
        ConsoleColor? background = null)
    {
        TextWriter originalOut = Console.Out;
        CultureInfo originalCulture = CultureInfo.CurrentCulture;
        CultureInfo originalUiCulture = CultureInfo.CurrentUICulture;
        var writer = new CallRecordingWriter(newLine);
        string outcome = "completed";
        string after;
        try
        {
            CultureInfo.CurrentCulture = culture ?? CultureInfo.InvariantCulture;
            CultureInfo.CurrentUICulture = uiCulture ?? CultureInfo.InvariantCulture;
            if (foreground.HasValue)
            {
                Console.ForegroundColor = foreground.Value;
            }
            if (background.HasValue)
            {
                Console.BackgroundColor = background.Value;
            }
            Console.SetOut(writer);
            try
            {
                scenario();
            }
            catch (Exception exception)
            {
                outcome = exception.GetType().FullName!;
            }
            after = (int)Console.ForegroundColor + "/" + (int)Console.BackgroundColor;
        }
        finally
        {
            Console.SetOut(originalOut);
            CultureInfo.CurrentCulture = originalCulture;
            CultureInfo.CurrentUICulture = originalUiCulture;
            Console.ResetColor();
        }
        return (name, writer.Calls + " -> " + outcome, writer.Colors + "-> " + after);
    }
}
