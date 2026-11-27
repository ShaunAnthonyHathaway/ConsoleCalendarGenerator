using ConsoleCalendarGenerator.Tests.Migration;
using ConsoleCalendarGenerator.Tests.Support;

namespace ConsoleCalendarGenerator.Tests.Legacy;

/// <summary>
/// Ties the assembly under test to the package that is actually on nuget.org.
///
/// The two values below were recorded on 2026-11-27 by running the code in Support/BehaviorDigest.cs against
/// lib/netstandard2.1/ConsoleCalendarGenerator.dll taken from ConsoleCalendarGenerator 1.0.0 on nuget.org
/// (ConsoleCalendarGenerator.1.0.0.nupkg, SHA-256
/// 542185beb4cf598cbcec3f45b02c6a234ee39aeefa3ec074a83fa6f54df1608e). They are not derived from this
/// repository's source. The same run confirmed that the published assembly has exactly the public API listed in
/// PublicApiTests.
///
/// The package is not downloaded by the tests and is not checked in. To record the values again, compile
/// Support/RecordingWriter.cs, ConsoleCapture.cs, ConsoleState.cs and BehaviorDigest.cs, together with
/// Migration/LegacySubject.cs and Historical/Published100/ConsoleCalendar.cs, which BehaviorDigest.cs needs since
/// it takes the implementation to run, into a console program that references the published assembly, and print
/// the two digests of LegacySubject.Library, which is then the published class. The color digest must be
/// recorded on Linux or macOS.
///
/// <para>
/// <b>The values belong to 1.0.0 and are never recorded again.</b> Historical/Published100DigestTests.cs
/// compares the frozen source of 1.0.0 with both, and keeps doing so. The library no longer reproduces them, on
/// purpose: version 2.0 ends the last row of every month with the console's line terminator (D-59) and follows
/// every month with exactly one blank line (D-60), which changes the text and the writes of every month the
/// values cover. So the library is not compared with the values any more. It is compared with the frozen source
/// directly, over everything the values cover, write by write: every write must be the one 1.0.0 made, in the
/// same color, except for those two differences, which are applied to what 1.0.0 wrote and to nothing else
/// (Migration/MonthEnd.cs). That is stricter than a hash, and a failure says where.
/// </para>
/// <para>
/// For the colors the library's calendars are given the colors 1.0.0 gave them, because 2.0 also corrected which
/// element a constructor argument colors (D-57); Legacy/ConstructorColorTests.cs and
/// Migration/ConstructorColorMigrationTests.cs cover the constructors themselves.
/// </para>
/// </summary>
[TestFixture]
public class Published100BaselineTests
{
    internal const string PublishedTextAndWrites = "b3de1456dc0d5094cfa787731904dc29e31b668bb8bdeaa678b20ca9a10f61ae";
    internal const string PublishedColors = "bf3d53d84ed3a6ddd7c24840a53c8dcc9c555dc734351889f6b2a0e362554a86";

    /// <summary>
    /// Every month from 1900 through 2100, the years 1 and 9999, a year through a "\r\n" writer and the
    /// individual writes of a year: each write the library makes is the write 1.0.0 made, apart from how a month
    /// ends.
    /// </summary>
    [Test]
    public void EveryWrite_IsTheOne100Made_ApartFromHowAMonthEnds()
    {
        IReadOnlyList<Captured> published100 = BehaviorDigest.TextAndWritesParts(LegacySubject.Published100);
        IReadOnlyList<Captured> library = BehaviorDigest.TextAndWritesParts(LegacySubject.Library);

        Assert.That(library, Has.Count.EqualTo(published100.Count));
        int changed = 0;
        for (int part = 0; part < published100.Count; part++)
        {
            IReadOnlyList<WriteEvent> expected = MonthEnd.Corrected(published100[part].Events);

            AssertSameWrites(library[part].Events, expected, colors: false, "part " + (part + 1));
            changed += ReferenceEquals(expected, published100[part].Events) ? 0 : 1;
        }
        Assert.That(changed, Is.EqualTo(published100.Count), "every part draws months, so every part differs from 1.0.0 in how they end");
    }

    /// <summary>
    /// The color of every write of a year drawn with the colors each of the five constructors of 1.0.0 gave a
    /// calendar, and the colors left behind: the same as 1.0.0, apart from how a month ends.
    /// </summary>
    [Test]
    [Platform(Exclude = "Win", Reason = ConsoleState.ColorsObservableOnUnixOnly)]
    public void TheColorOfEveryWrite_IsTheOne100Used_ApartFromHowAMonthEnds_GivenTheConstructorColorsOf100()
    {
        IReadOnlyList<Captured> published100 = BehaviorDigest.ColorsParts(LegacySubject.Published100);
        IReadOnlyList<Captured> library = BehaviorDigest.ColorsParts(LegacySubject.LibraryWithTheConstructorColorsOf100);

        Assert.That(library, Has.Count.EqualTo(5));
        for (int part = 0; part < published100.Count; part++)
        {
            AssertSameWrites(library[part].Events, MonthEnd.Corrected(published100[part].Events), colors: true, "constructor with " + part + " arguments");
            Assert.That((library[part].ForegroundAfter, library[part].BackgroundAfter),
                Is.EqualTo((published100[part].ForegroundAfter, published100[part].BackgroundAfter)), "the colors left behind");
        }
    }

    /// <summary>The same writes in the same order: the text, whether the call ended the line, and where asked the colors.</summary>
    private static void AssertSameWrites(IReadOnlyList<WriteEvent> actual, IReadOnlyList<WriteEvent> expected, bool colors, string what)
    {
        Assert.That(actual, Has.Count.EqualTo(expected.Count), what + ": the number of writes");
        for (int index = 0; index < expected.Count; index++)
        {
            WriteEvent e = expected[index];
            WriteEvent a = actual[index];
            if (a.Payload != e.Payload || a.EndsLine != e.EndsLine || (colors && (a.Foreground != e.Foreground || a.Background != e.Background)))
            {
                Assert.Fail($"{what}: write {index + 1} differs. Expected {Show(e, colors)}, but was {Show(a, colors)}.");
            }
        }
    }

    private static string Show(WriteEvent written, bool colors) =>
        "\"" + written.Payload.Replace("\r", "\\r").Replace("\n", "\\n") + "\"" + (written.EndsLine ? " and a line end" : string.Empty)
        + (colors ? " in " + written.Foreground + " on " + written.Background : string.Empty);
}
