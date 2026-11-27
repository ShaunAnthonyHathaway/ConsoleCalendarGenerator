using CalendarGenerator;
using ConsoleCalendarGenerator.Tests.Support;

namespace ConsoleCalendarGenerator.Tests;

/// <summary>
/// The public API of the modern namespace, ConsoleCalendarGenerator: the layout model and the rendering types.
/// These names are a commitment once a package containing them is published, so a change here is a change
/// consumers can see, and anything that appears here unplanned is a mistake. The legacy API is listed
/// separately, in Legacy/PublicApiTests.cs.
/// </summary>
[TestFixture]
public class ModernApiTests
{
    [Test]
    public void TheModernNamespace_ExposesExactlyTheLayoutAndRenderingTypes()
    {
        string[] expected =
        {
            "assembly ConsoleCalendarGenerator",
            "public sealed class ConsoleCalendarGenerator.MonthLayout : System.Object",
            "  System.Collections.Generic.IReadOnlyList<ConsoleCalendarGenerator.MonthLayoutWeek> get_Weeks()",
            "  System.Collections.Generic.IReadOnlyList<System.DayOfWeek> get_DaysOfWeek()",
            "  System.DayOfWeek get_FirstDayOfWeek()",
            "  System.Int32 get_Month()",
            "  System.Int32 get_Year()",
            "  property DaysOfWeek",
            "  property FirstDayOfWeek",
            "  property Month",
            "  property Weeks",
            "  property Year",
            "  static ConsoleCalendarGenerator.MonthLayout Create(System.Int32 year, System.Int32 month, System.DayOfWeek firstDayOfWeek)",
            "public sealed class ConsoleCalendarGenerator.MonthLayoutCell : System.Object",
            "  System.Boolean get_IsEmpty()",
            "  System.DayOfWeek get_DayOfWeek()",
            "  System.Nullable<System.DateTime> get_Date()",
            "  property Date",
            "  property DayOfWeek",
            "  property IsEmpty",
            "public sealed class ConsoleCalendarGenerator.MonthLayoutWeek : System.Object",
            "  System.Collections.Generic.IReadOnlyList<ConsoleCalendarGenerator.MonthLayoutCell> get_Cells()",
            "  property Cells",
            "public sealed class ConsoleCalendarGenerator.MonthRenderOptions : System.Object",
            "  ConsoleCalendarGenerator.MonthRenderOptions WithCellWidth(System.Int32 cellWidth)",
            "  ConsoleCalendarGenerator.MonthRenderOptions WithColumnGap(System.Int32 columnGap)",
            "  ConsoleCalendarGenerator.MonthRenderOptions WithCulture(System.Globalization.CultureInfo culture)",
            "  ConsoleCalendarGenerator.MonthRenderOptions WithFirstDayOfWeek(System.DayOfWeek firstDayOfWeek)",
            "  ConsoleCalendarGenerator.MonthRenderOptions WithIndent(System.String indent)",
            "  ConsoleCalendarGenerator.MonthRenderOptions WithTitleAlignment(ConsoleCalendarGenerator.TitleAlignment titleAlignment)",
            "  ConsoleCalendarGenerator.MonthRenderOptions WithTitleFormat(System.String titleFormat)",
            "  ConsoleCalendarGenerator.MonthRenderOptions WithTitleSuffix(System.String titleSuffix)",
            "  ConsoleCalendarGenerator.MonthRenderOptions WithWeekdayLabelStyle(ConsoleCalendarGenerator.WeekdayLabelStyle weekdayLabelStyle)",
            "  ConsoleCalendarGenerator.MonthRenderOptions WithWeekdayLabels(System.Collections.Generic.IEnumerable<System.String> weekdayLabels)",
            "  ConsoleCalendarGenerator.TitleAlignment get_TitleAlignment()",
            "  ConsoleCalendarGenerator.WeekdayLabelStyle get_WeekdayLabelStyle()",
            "  System.Collections.Generic.IReadOnlyList<System.String> get_WeekdayLabels()",
            "  System.DayOfWeek get_FirstDayOfWeek()",
            "  System.Globalization.CultureInfo get_Culture()",
            "  System.Int32 get_CellWidth()",
            "  System.Int32 get_ColumnGap()",
            "  System.Int32 get_GridWidth()",
            "  System.String get_Indent()",
            "  System.String get_TitleFormat()",
            "  System.String get_TitleSuffix()",
            "  property CellWidth",
            "  property ColumnGap",
            "  property Culture",
            "  property Default",
            "  property FirstDayOfWeek",
            "  property GridWidth",
            "  property Indent",
            "  property TitleAlignment",
            "  property TitleFormat",
            "  property TitleSuffix",
            "  property WeekdayLabelStyle",
            "  property WeekdayLabels",
            "  static ConsoleCalendarGenerator.MonthRenderOptions get_Default()",
            "public static class ConsoleCalendarGenerator.MonthRenderer : System.Object",
            "  static ConsoleCalendarGenerator.RenderedMonth Render(System.Int32 year, System.Int32 month, ConsoleCalendarGenerator.MonthRenderOptions options)",
            "public sealed class ConsoleCalendarGenerator.RenderedLine : System.Object",
            "  ConsoleCalendarGenerator.RenderedLineKind get_Kind()",
            "  System.Collections.Generic.IReadOnlyList<ConsoleCalendarGenerator.RenderedSegment> get_Segments()",
            "  System.String get_Text()",
            "  property Kind",
            "  property Segments",
            "  property Text",
            "  virtual System.String ToString()",
            "public enum ConsoleCalendarGenerator.RenderedLineKind : System.Int32",
            "  Title = 0",
            "  Week = 2",
            "  WeekdayLabels = 1",
            "public sealed class ConsoleCalendarGenerator.RenderedMonth : System.Object",
            "  ConsoleCalendarGenerator.MonthLayout get_Layout()",
            "  System.Collections.Generic.IReadOnlyList<ConsoleCalendarGenerator.RenderedLine> get_Lines()",
            "  System.String ToText()",
            "  System.String ToText(System.String newLine)",
            "  property Layout",
            "  property Lines",
            "  virtual System.String ToString()",
            "public sealed class ConsoleCalendarGenerator.RenderedSegment : System.Object",
            "  ConsoleCalendarGenerator.RenderedSegmentKind get_Kind()",
            "  System.Nullable<System.DateTime> get_Date()",
            "  System.Nullable<System.DayOfWeek> get_DayOfWeek()",
            "  System.String get_Text()",
            "  property Date",
            "  property DayOfWeek",
            "  property Kind",
            "  property Text",
            "  virtual System.String ToString()",
            "public enum ConsoleCalendarGenerator.RenderedSegmentKind : System.Int32",
            "  Day = 7",
            "  EmptyDay = 6",
            "  Gap = 5",
            "  Indent = 0",
            "  Padding = 1",
            "  Title = 2",
            "  TitleSuffix = 3",
            "  WeekdayLabel = 4",
            "public enum ConsoleCalendarGenerator.TitleAlignment : System.Int32",
            "  Center = 1",
            "  Left = 0",
            "public enum ConsoleCalendarGenerator.WeekdayLabelStyle : System.Int32",
            "  Abbreviated = 1",
            "  Shortest = 0",
        };

        Assert.That(ApiSurface.Describe(typeof(MonthLayout).Assembly, inNamespace: "ConsoleCalendarGenerator"), Is.EqualTo(expected));
    }

    /// <summary>
    /// Together with the two lists of members, this accounts for everything the assembly exports: the legacy
    /// class in its published namespace, the modern types in the namespace named after the package, and nothing
    /// anywhere else.
    /// </summary>
    [Test]
    public void TheAssembly_ExportsOnlyTheLegacyClassAndTheModernNamespace()
    {
        Type[] exported = typeof(MonthLayout).Assembly.GetExportedTypes();

        Assert.That(exported.Select(type => type.Namespace).Distinct().OrderBy(name => name, StringComparer.Ordinal),
            Is.EqualTo(new[] { "CalendarGenerator", "ConsoleCalendarGenerator" }));
        Assert.That(exported.Where(type => type.Namespace == "CalendarGenerator"), Is.EqualTo(new[] { typeof(ConsoleCalendar) }));
        Assert.That(typeof(MonthLayout).Assembly, Is.SameAs(typeof(ConsoleCalendar).Assembly));
    }
}
