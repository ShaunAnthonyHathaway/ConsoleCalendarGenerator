using System.Reflection;
using System.Runtime.CompilerServices;

namespace ConsoleCalendarGenerator.Tests.Parity.Campertron;

/// <summary>
/// The parity tests prove something about the package only if they reach it the way a consumer does. These tests
/// pin that: the library gives no assembly access to its internals, so the adapter and the profile cannot have
/// used any; and nothing here is built against Campertron.
/// </summary>
[TestFixture]
public class ApiSufficiencyTests
{
    private static readonly Assembly Library = typeof(MonthRenderer).Assembly;

    /// <summary>
    /// With no InternalsVisibleTo on the library, code in this assembly can only compile against what is public,
    /// exactly like code in a consumer's assembly.
    /// </summary>
    [Test]
    public void TheLibrary_GivesNoOtherAssemblyAccessToItsInternals()
    {
        Assert.That(Library.GetCustomAttributes<InternalsVisibleToAttribute>(), Is.Empty);
    }

    /// <summary>Every package type the adapter and the profile touch is a public type of the modern namespace.</summary>
    [Test]
    public void TheAdapterAndTheProfile_UseOnlyPublicTypesOfThePackage()
    {
        Type[] used =
        {
            typeof(MonthRenderer), typeof(MonthRenderOptions), typeof(RenderedMonth), typeof(RenderedLine), typeof(RenderedSegment),
            typeof(RenderedLineKind), typeof(RenderedSegmentKind), typeof(TitleAlignment), typeof(DayState), typeof(CalendarTheme),
        };

        Assert.That(used.Select(type => type.IsPublic), Is.All.True);
        Assert.That(used.Select(type => type.Namespace), Is.All.EqualTo("ConsoleCalendarGenerator"));
        Assert.That(used.Select(type => type.Assembly), Is.All.SameAs(Library));
    }

    /// <summary>The adapter and the profile do their work without reflection: no field of theirs holds any.</summary>
    [Test]
    public void TheAdapterAndTheProfile_HoldNoReflectionObjects()
    {
        foreach (Type type in new[] { typeof(SegmentAdapter), typeof(ParityProfile) })
        {
            FieldInfo[] fields = type.GetFields(BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            Assert.That(fields.Select(field => field.FieldType).Where(fieldType => typeof(MemberInfo).IsAssignableFrom(fieldType) || fieldType == typeof(Assembly)),
                Is.Empty, type.Name);
        }
    }

    [Test]
    public void TheTests_AreNotBuiltAgainstCampertron()
    {
        string[] referenced = typeof(ApiSufficiencyTests).Assembly.GetReferencedAssemblies().Select(name => name.Name!).ToArray();

        Assert.That(referenced, Has.None.Contains("Campertron").IgnoreCase);
        Assert.That(referenced, Does.Contain("ConsoleCalendarGenerator"));
    }

    /// <summary>The fixtures are test data inside the test assembly. The library carries none of them.</summary>
    [Test]
    public void TheFixtures_AreInTheTestAssembly_AndNotInTheLibrary()
    {
        Assert.That(ParityFixtures.EmbeddedFiles(), Has.Count.EqualTo(ParityFixtures.Manifest.Files.Count + 1));
        Assert.That(Library.GetManifestResourceNames(), Is.Empty);
    }

    /// <summary>The package has no type, member or option named after a consumer.</summary>
    [Test]
    public void ThePackage_HasNothingNamedAfterTheConsumer()
    {
        IEnumerable<string> names = Library.GetExportedTypes()
            .SelectMany(type => type.GetMembers().Select(member => type.FullName + "." + member.Name).Append(type.FullName!));

        Assert.That(names.Where(name => name.Contains("Campertron", StringComparison.OrdinalIgnoreCase) || name.Contains("Open", StringComparison.Ordinal)), Is.Empty);
    }
}
