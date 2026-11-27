using System.Reflection;
using System.Runtime.Versioning;
using CalendarGenerator;

namespace ConsoleCalendarGenerator.Tests;

/// <summary>
/// The library ships two builds, and the whole suite is run once against each (see the project file). This test
/// makes sure a run is really using the build it was asked to use, so that a mistake in the build setup cannot
/// quietly test the same build twice.
/// </summary>
[TestFixture]
public class LibraryBuildTests
{
    [Test]
    public void TheTestsRunAgainstTheLibraryBuildTheyWereBuiltFor()
    {
        string requested = typeof(LibraryBuildTests).Assembly
            .GetCustomAttributes<AssemblyMetadataAttribute>()
            .Single(attribute => attribute.Key == "LibraryTargetFramework")
            .Value!;
        string expected = requested switch
        {
            "net10.0" => ".NETCoreApp,Version=v10.0",
            "netstandard2.1" => ".NETStandard,Version=v2.1",
            _ => throw new InvalidOperationException("The library has no " + requested + " build."),
        };

        string? actual = typeof(ConsoleCalendar).Assembly.GetCustomAttribute<TargetFrameworkAttribute>()?.FrameworkName;

        Assert.That(actual, Is.EqualTo(expected));
    }
}
