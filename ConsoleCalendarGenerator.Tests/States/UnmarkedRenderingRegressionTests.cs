using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace ConsoleCalendarGenerator.Tests.States;

/// <summary>
/// Ties unmarked rendering to what it was before day states existed.
///
/// The value below was recorded on 2026-11-27 by running <see cref="Digest"/> against the renderer at commit
/// 1e71486, the last one without day states, and was not changed afterwards. It covers the text and every
/// segment (kind, text, weekday, date) of every month from 1900 through 2100 and of the years 1 and 9999, in
/// five different shapes, all with the invariant culture so that it is the same on every system.
///
/// If it stops matching, rendering without states changed. Do not record it again from the current code.
/// </summary>
[TestFixture]
public class UnmarkedRenderingRegressionTests
{
    private const string RecordedBeforeDayStates = "61af8f9d6394237318bc2b7d6ff89512431ae388fe89730df859547b08d25142";

    private static MonthRenderOptions[] Shapes() => new[]
    {
        MonthRenderOptions.Default,
        MonthRenderOptions.Default.WithCellWidth(3).WithIndent("    ").WithTitleAlignment(TitleAlignment.Left).WithTitleSuffix(" (* = open)"),
        MonthRenderOptions.Default.WithColumnGap(0).WithCellWidth(4).WithFirstDayOfWeek(DayOfWeek.Sunday).WithTitleFormat("MMMM yyyy"),
        MonthRenderOptions.Default.WithColumnGap(3).WithWeekdayLabelStyle(WeekdayLabelStyle.Abbreviated).WithFirstDayOfWeek(DayOfWeek.Saturday),
        MonthRenderOptions.Default.WithWeekdayLabels(new[] { "S", "M", "T", "W", "T", "F", "S" }).WithIndent("\t").WithCellWidth(5),
    };

    [Test]
    public void RenderingWithoutStates_IsWhatItWasBeforeDayStatesExisted()
    {
        Assert.That(Digest(), Is.EqualTo(RecordedBeforeDayStates),
            "Unmarked rendering changed. The rendering tests show where.");
    }

    internal static string Digest()
    {
        var all = new StringBuilder();
        foreach (MonthRenderOptions options in Shapes())
        {
            foreach (int year in Enumerable.Range(1900, 201).Concat(new[] { 1, 9999 }))
            {
                for (int month = 1; month <= 12; month++)
                {
                    Append(all, MonthRenderer.Render(year, month, options));
                }
            }
            all.Append((char)1);
        }
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(all.ToString()))).ToLowerInvariant();
    }

    /// <summary>Only what a rendered month exposed before day states: nothing here may mention a state.</summary>
    private static void Append(StringBuilder all, RenderedMonth rendered)
    {
        all.Append(rendered.ToText()).Append((char)2);
        foreach (RenderedLine line in rendered.Lines)
        {
            all.Append((int)line.Kind).Append(':');
            foreach (RenderedSegment segment in line.Segments)
            {
                all.Append((int)segment.Kind).Append('[').Append(segment.Text).Append(']')
                    .Append(segment.DayOfWeek.HasValue ? ((int)segment.DayOfWeek.Value).ToString(CultureInfo.InvariantCulture) : "-")
                    .Append(segment.Date.HasValue ? segment.Date.Value.ToString("yyyyMMddHHmmssfffffff", CultureInfo.InvariantCulture) + (int)segment.Date.Value.Kind : "-")
                    .Append('|');
            }
            all.Append((char)3);
        }
    }
}
