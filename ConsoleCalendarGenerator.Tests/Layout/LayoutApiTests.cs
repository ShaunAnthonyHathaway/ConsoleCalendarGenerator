using CalendarGenerator;
using ConsoleCalendarGenerator.Tests.Support;

namespace ConsoleCalendarGenerator.Tests.Layout;

/// <summary>
/// The public API of the layout layer. These names are a commitment once a package containing them is
/// published, so a change here is a change consumers can see. The legacy API is listed separately, in
/// Legacy/PublicApiTests.cs.
/// </summary>
[TestFixture]
public class LayoutApiTests
{
    [Test]
    public void TheModernNamespace_ExposesExactlyTheLayoutTypes()
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
