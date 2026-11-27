using System.Security.Cryptography;
using System.Text;
using ConsoleCalendarGenerator.Tests.Migration;

namespace ConsoleCalendarGenerator.Tests.Support;

/// <summary>
/// Condenses a large amount of ConsoleCalendar output into one SHA-256 value, so that the output of the
/// assembly under test can be compared with values recorded from the published 1.0.0 assembly
/// (see Legacy/Published100BaselineTests.cs). The same code condenses the output of the frozen source of 1.0.0
/// (see Historical/Published100DigestTests.cs), which is why it takes the implementation to run.
/// </summary>
internal static class BehaviorDigest
{
    /// <summary>Selected dates used by the digests: in and out of the rendered years, with a duplicate and a time.</summary>
    public static List<DateTime> Selection() => new()
    {
        new DateTime(2024, 1, 1),
        new DateTime(2024, 2, 29),
        new DateTime(2024, 2, 29, 13, 45, 0),
        new DateTime(2024, 6, 30),
        new DateTime(2024, 12, 31),
        new DateTime(2023, 4, 1),
        new DateTime(2023, 12, 31),
        new DateTime(2025, 1, 1),
    };

    /// <summary>
    /// Text and write sequence: every month from 1900 to 2100, the first and last supported years, a year
    /// through a "\r\n" writer, and the individual write calls of one year. The same on every platform.
    /// </summary>
    public static string TextAndWrites(LegacySubject subject)
    {
        ILegacyCalendar calendar = subject.Create();
        var all = new StringBuilder();

        all.Append(ConsoleCapture.Run(() =>
        {
            for (int year = 1900; year <= 2100; year++)
            {
                for (int month = 1; month <= 12; month++)
                {
                    calendar.GenerateMonth(month, year);
                }
            }
        }).Text);
        all.Append("\u0001");
        all.Append(ConsoleCapture.Run(() =>
        {
            calendar.GenerateYear(1);
            calendar.GenerateYear(9999);
        }).Text);
        all.Append("\u0001");
        all.Append(ConsoleCapture.Run(() => calendar.GenerateYear(2024, Selection()), newLine: "\r\n").Text);
        all.Append("\u0001");
        all.Append(ConsoleCapture.Run(() => calendar.GenerateYear(2023, Selection())).Transcript);

        return Sha256(all.ToString());
    }

    /// <summary>
    /// Color sequence: the foreground color of every piece of text for a year rendered through each of the
    /// five constructors, and the colors left behind. Only meaningful where colors can be read back
    /// (<see cref="ConsoleState.ColorsObservableOnUnixOnly"/>).
    /// </summary>
    public static string Colors(LegacySubject subject)
    {
        var calendars = new[]
        {
            subject.Create(),
            subject.Create(ConsoleColor.Red),
            subject.Create(ConsoleColor.Red, ConsoleColor.Green),
            subject.Create(ConsoleColor.Red, ConsoleColor.Green, ConsoleColor.Magenta),
            subject.Create(ConsoleColor.Red, ConsoleColor.Green, ConsoleColor.Magenta, ConsoleColor.DarkYellow),
        };
        var all = new StringBuilder();
        foreach (ILegacyCalendar calendar in calendars)
        {
            Captured captured = ConsoleCapture.Run(
                () => calendar.GenerateYear(2024, Selection()),
                foregroundBefore: ConsoleColor.DarkCyan,
                backgroundBefore: ConsoleColor.DarkBlue);
            foreach ((ConsoleColor color, string text) in captured.ForegroundRuns)
            {
                all.Append((int)color).Append(':').Append(text).Append('|');
            }
            all.Append("\u0001");
            foreach ((ConsoleColor color, string text) in captured.BackgroundRuns)
            {
                all.Append((int)color).Append(':').Append(text.Length).Append('|');
            }
            all.Append("\u0001").Append((int)captured.ForegroundAfter).Append(',').Append((int)captured.BackgroundAfter).Append("\u0002");
        }
        return Sha256(all.ToString());
    }

    private static string Sha256(string text) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text))).ToLowerInvariant();
}
