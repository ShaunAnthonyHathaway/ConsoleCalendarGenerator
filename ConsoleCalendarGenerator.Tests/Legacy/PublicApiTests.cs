using CalendarGenerator;
using ConsoleCalendarGenerator.Tests.Support;

namespace ConsoleCalendarGenerator.Tests.Legacy;

/// <summary>
/// The public API of ConsoleCalendarGenerator 1.0.0. Anything that changes these tests is a change existing
/// consumers can see: a removed or renamed member breaks compiled programs, and a renamed parameter breaks
/// callers that use named arguments.
/// </summary>
[TestFixture]
public class PublicApiTests
{
    [Test]
    public void TheAssemblyExposesExactlyThePublished100Surface()
    {
        string[] expected =
        {
            "assembly ConsoleCalendarGenerator",
            "public class CalendarGenerator.ConsoleCalendar : System.Object",
            "  .ctor()",
            "  .ctor(System.ConsoleColor TitleColor)",
            "  .ctor(System.ConsoleColor TitleColor, System.ConsoleColor DateColor)",
            "  .ctor(System.ConsoleColor TitleColor, System.ConsoleColor DateColor, System.ConsoleColor SelectedDateColor)",
            "  .ctor(System.ConsoleColor TitleColor, System.ConsoleColor DateColor, System.ConsoleColor SelectedDateColor, System.ConsoleColor DayOfWeekColor)",
            "  System.ConsoleColor GetDateColor()",
            "  System.ConsoleColor GetDayOfWeekColor()",
            "  System.ConsoleColor GetSelectedDateColor()",
            "  System.ConsoleColor GetTitleColor()",
            "  System.Void GenerateMonth(System.Int32 Month, System.Int32 Year)",
            "  System.Void GenerateMonth(System.Int32 Month, System.Int32 Year, System.Collections.Generic.List<System.DateTime> dates)",
            "  System.Void GenerateYear(System.Int32 Year)",
            "  System.Void GenerateYear(System.Int32 Year, System.Collections.Generic.List<System.DateTime> dates)",
            // All four setters name their parameter TitleColor. That is the published signature.
            "  System.Void SetDateColor(System.ConsoleColor TitleColor)",
            "  System.Void SetDayOfWeekColor(System.ConsoleColor TitleColor)",
            "  System.Void SetSelectedDateColor(System.ConsoleColor TitleColor)",
            "  System.Void SetTitleColor(System.ConsoleColor TitleColor)",
        };

        // The assembly also holds the modern API, in the ConsoleCalendarGenerator namespace. That API is listed in
        // Layout/LayoutApiTests.cs, which also checks that the assembly exports nothing outside the two namespaces.
        Assert.That(ApiSurface.Describe(typeof(ConsoleCalendar).Assembly, inNamespace: "CalendarGenerator"), Is.EqualTo(expected));
    }

    [Test]
    public void ConsoleCalendar_KeepsItsNamespaceAndName()
    {
        Assert.That(typeof(ConsoleCalendar).FullName, Is.EqualTo("CalendarGenerator.ConsoleCalendar"));
        Assert.That(typeof(ConsoleCalendar).Assembly.GetName().Name, Is.EqualTo("ConsoleCalendarGenerator"));
    }

    [Test]
    public void ConsoleCalendar_IsAPlainUnsealedClass_WithNoInterfacesAndNoVirtualMembers()
    {
        Type type = typeof(ConsoleCalendar);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(type.IsClass && type.IsPublic, Is.True);
            Assert.That(type.IsSealed, Is.False, "consumers may have derived from it");
            Assert.That(type.IsAbstract, Is.False);
            Assert.That(type.BaseType, Is.EqualTo(typeof(object)));
            Assert.That(type.GetInterfaces(), Is.Empty);
            Assert.That(type.GetMethods().Where(m => m.DeclaringType == type && m.IsVirtual), Is.Empty);
            Assert.That(type.GetMethods().Where(m => m.DeclaringType == type && m.IsStatic), Is.Empty);
            Assert.That(type.GetProperties(), Is.Empty);
            Assert.That(type.GetFields(), Is.Empty);
            Assert.That(type.GetEvents(), Is.Empty);
        }
    }

    [Test]
    public void DateListParameters_AreTheConcreteListOfDateTime()
    {
        Assert.That(typeof(ConsoleCalendar).GetMethod("GenerateMonth", new[] { typeof(int), typeof(int), typeof(List<DateTime>) }), Is.Not.Null);
        Assert.That(typeof(ConsoleCalendar).GetMethod("GenerateYear", new[] { typeof(int), typeof(List<DateTime>) }), Is.Not.Null);
        Assert.That(typeof(ConsoleCalendar).GetMethod("GenerateMonth", new[] { typeof(int), typeof(int), typeof(IEnumerable<DateTime>) }), Is.Null);
    }

    /// <summary>
    /// Compiles only while every member keeps its name, its parameter order and its parameter names. Month comes
    /// before Year, and the selected dates are a parameter called "dates".
    /// </summary>
    [Test]
    public void SourceWrittenAgainst100_WithNamedArguments_StillCompilesAndRuns()
    {
        var dates = new List<DateTime> { new DateTime(2023, 1, 1) };

        Captured captured = ConsoleCapture.Run(() =>
        {
            var calendar = new ConsoleCalendar(
                TitleColor: ConsoleColor.Red,
                DateColor: ConsoleColor.Green,
                SelectedDateColor: ConsoleColor.Magenta,
                DayOfWeekColor: ConsoleColor.DarkYellow);
            _ = new ConsoleCalendar(TitleColor: ConsoleColor.Red, DateColor: ConsoleColor.Green, SelectedDateColor: ConsoleColor.Magenta);
            _ = new ConsoleCalendar(TitleColor: ConsoleColor.Red, DateColor: ConsoleColor.Green);
            _ = new ConsoleCalendar(TitleColor: ConsoleColor.Red);
            _ = new ConsoleCalendar();

            calendar.SetTitleColor(TitleColor: calendar.GetTitleColor());
            calendar.SetDayOfWeekColor(TitleColor: calendar.GetDayOfWeekColor());
            calendar.SetDateColor(TitleColor: calendar.GetDateColor());
            calendar.SetSelectedDateColor(TitleColor: calendar.GetSelectedDateColor());

            calendar.GenerateMonth(Month: 1, Year: 2023);
            calendar.GenerateMonth(Month: 1, Year: 2023, dates: dates);
            calendar.GenerateYear(Year: 2023);
            calendar.GenerateYear(Year: 2023, dates: dates);
        });

        Assert.That(captured.Text, Does.StartWith("    January 2023\n").Using(StringComparison.Ordinal));
    }
}
