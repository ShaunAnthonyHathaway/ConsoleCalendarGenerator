using System.Security.Cryptography;
using System.Text;
using ConsoleCalendarGenerator.Tests.Migration;

namespace ConsoleCalendarGenerator.Tests.Support;

/// <summary>
/// Condenses a large amount of ConsoleCalendar output into one SHA-256 value, so that it can be compared with
/// values recorded from the published 1.0.0 assembly (see Legacy/Published100BaselineTests.cs). The same code
/// condenses the output of the frozen source of 1.0.0 (see Historical/Published100DigestTests.cs), which is why
/// it takes the implementation to run.
///
/// <para>
/// The output is also available before it is condensed (<see cref="TextAndWritesParts"/> and
/// <see cref="ColorsParts"/>), so that the library, which since version 2.0 deliberately differs from 1.0.0 in
/// how a month ends, can be compared with the frozen source write by write instead of hash against hash.
/// </para>
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
    /// What the text digest covers, in its four parts: every month from 1900 to 2100, the first and last
    /// supported years, a year through a "\r\n" writer, and one more year whose individual write calls count.
    /// </summary>
    public static IReadOnlyList<Captured> TextAndWritesParts(LegacySubject subject)
    {
        ILegacyCalendar calendar = subject.Create();
        return new[]
        {
            ConsoleCapture.Run(() =>
            {
                for (int year = 1900; year <= 2100; year++)
                {
                    for (int month = 1; month <= 12; month++)
                    {
                        calendar.GenerateMonth(month, year);
                    }
                }
            }),
            ConsoleCapture.Run(() =>
            {
                calendar.GenerateYear(1);
                calendar.GenerateYear(9999);
            }),
            ConsoleCapture.Run(() => calendar.GenerateYear(2024, Selection()), newLine: "\r\n"),
            ConsoleCapture.Run(() => calendar.GenerateYear(2023, Selection())),
        };
    }

    /// <summary>
    /// Text and write sequence: the text of the first three parts and the individual write calls of the fourth.
    /// The same on every platform.
    /// </summary>
    public static string TextAndWrites(LegacySubject subject)
    {
        IReadOnlyList<Captured> parts = TextAndWritesParts(subject);
        var all = new StringBuilder();
        all.Append(parts[0].Text);
        all.Append("\u0001");
        all.Append(parts[1].Text);
        all.Append("\u0001");
        all.Append(parts[2].Text);
        all.Append("\u0001");
        all.Append(parts[3].Transcript);

        return Sha256(all.ToString());
    }

    /// <summary>
    /// What the color digest covers: a year rendered through each of the five constructors, with the caller's
    /// colors set beforehand.
    /// </summary>
    public static IReadOnlyList<Captured> ColorsParts(LegacySubject subject)
    {
        var calendars = new[]
        {
            subject.Create(),
            subject.Create(ConsoleColor.Red),
            subject.Create(ConsoleColor.Red, ConsoleColor.Green),
            subject.Create(ConsoleColor.Red, ConsoleColor.Green, ConsoleColor.Magenta),
            subject.Create(ConsoleColor.Red, ConsoleColor.Green, ConsoleColor.Magenta, ConsoleColor.DarkYellow),
        };
        return calendars
            .Select(calendar => ConsoleCapture.Run(
                () => calendar.GenerateYear(2024, Selection()),
                foregroundBefore: ConsoleColor.DarkCyan,
                backgroundBefore: ConsoleColor.DarkBlue))
            .ToArray();
    }

    /// <summary>
    /// Color sequence: the foreground color of every piece of text of those five years, and the colors left
    /// behind. Only meaningful where colors can be read back
    /// (<see cref="ConsoleState.ColorsObservableOnUnixOnly"/>).
    /// </summary>
    public static string Colors(LegacySubject subject)
    {
        var all = new StringBuilder();
        foreach (Captured captured in ColorsParts(subject))
        {
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
