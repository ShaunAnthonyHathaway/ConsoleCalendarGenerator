using System.Globalization;

namespace ConsoleCalendarGenerator.Tests.Support;

/// <summary>Ways of reading a <see cref="RenderedMonth"/> in tests.</summary>
internal static class Rendered
{
    /// <summary>The text of every line, so that a test can spell out the month it expects line by line.</summary>
    public static string[] Lines(RenderedMonth month) => month.Lines.Select(line => line.Text).ToArray();

    /// <summary>
    /// One segment as text: its kind, its text in brackets, then its weekday, its date and its state where it
    /// has them, for example <c>Day[ 1] Saturday 2023-04-01</c> or <c>Day[*14] Tuesday 2023-02-14 state=open</c>.
    /// </summary>
    public static string Describe(RenderedSegment segment) =>
        segment.Kind + "[" + segment.Text + "]"
        + (segment.DayOfWeek is DayOfWeek dayOfWeek ? " " + dayOfWeek : string.Empty)
        + (segment.Date is DateTime date
            ? " " + date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
                + (date.TimeOfDay == TimeSpan.Zero && date.Kind == DateTimeKind.Unspecified ? string.Empty : " with a time or a kind")
            : string.Empty)
        + (segment.State is DayState state ? " state=" + state.Name : string.Empty);

    /// <summary>The segments of one line, described one by one.</summary>
    public static string[] Describe(RenderedLine line) => line.Segments.Select(Describe).ToArray();

    /// <summary>
    /// Everything a rendered month exposes, as one string: for each line its kind, its text and every segment.
    /// Two rendered months with the same description are indistinguishable through the public API.
    /// </summary>
    public static string Describe(RenderedMonth month) =>
        LayoutPicture.Describe(month.Layout) + "\n" + string.Join("\n", month.Lines.Select(line =>
            line.Kind + " <" + line.Text + "> " + string.Join(" | ", Describe(line))));
}
