using ConsoleCalendarGenerator.Tests.Support;

namespace ConsoleCalendarGenerator.Tests.Legacy;

/// <summary>
/// Ties the assembly under test to the package that is actually on nuget.org.
///
/// The values below were recorded on 2026-11-27 by running the code in Support/BehaviorDigest.cs against
/// lib/netstandard2.1/ConsoleCalendarGenerator.dll taken from ConsoleCalendarGenerator 1.0.0 on nuget.org
/// (ConsoleCalendarGenerator.1.0.0.nupkg, SHA-256
/// 542185beb4cf598cbcec3f45b02c6a234ee39aeefa3ec074a83fa6f54df1608e). They are not derived from this
/// repository's source. The same run confirmed that the published assembly has exactly the public API listed in
/// PublicApiTests.
///
/// The package is not downloaded by the tests and is not checked in. To record the values again, compile
/// Support/RecordingWriter.cs, ConsoleCapture.cs, ConsoleState.cs and BehaviorDigest.cs into a console program
/// that references the published assembly, and print the two digests. The color digest must be recorded on
/// Linux or macOS.
/// </summary>
[TestFixture]
public class Published100BaselineTests
{
    private const string PublishedTextAndWrites = "b3de1456dc0d5094cfa787731904dc29e31b668bb8bdeaa678b20ca9a10f61ae";
    private const string PublishedColors = "bf3d53d84ed3a6ddd7c24840a53c8dcc9c555dc734351889f6b2a0e362554a86";

    /// <summary>
    /// Every month from 1900 through 2100, the years 1 and 9999, a year through a "\r\n" writer and the
    /// individual writes of a year, compared with the published assembly.
    /// </summary>
    [Test]
    public void TextAndWriteSequence_AreIdenticalToPublished100()
    {
        Assert.That(BehaviorDigest.TextAndWrites(), Is.EqualTo(PublishedTextAndWrites),
            "The text or the write sequence differs from ConsoleCalendarGenerator 1.0.0. The other legacy tests show where.");
    }

    /// <summary>
    /// The color of every piece of text for a year drawn through each of the five constructors, and the colors
    /// left behind, compared with the published assembly.
    /// </summary>
    [Test]
    [Platform(Exclude = "Win", Reason = ConsoleState.ColorsObservableOnUnixOnly)]
    public void ColorSequence_IsIdenticalToPublished100()
    {
        Assert.That(BehaviorDigest.Colors(), Is.EqualTo(PublishedColors),
            "The color sequence differs from ConsoleCalendarGenerator 1.0.0. The other legacy tests show where.");
    }
}
