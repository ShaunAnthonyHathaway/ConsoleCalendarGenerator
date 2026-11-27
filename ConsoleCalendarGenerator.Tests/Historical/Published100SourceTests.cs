using System.Reflection;
using ConsoleCalendarGenerator.Tests.Support;

namespace ConsoleCalendarGenerator.Tests.Historical;

/// <summary>
/// The frozen source of version 1.0.0 is what it says it is, has not been touched, and exists only in the
/// tests. See <see cref="Published100Source"/> for where it comes from.
/// </summary>
[TestFixture]
public class Published100SourceTests
{
    private static readonly Type Frozen = typeof(Published100.ConsoleCalendar);
    private static readonly Type InTheLibrary = typeof(CalendarGenerator.ConsoleCalendar);

    // ---- frozen ----

    /// <summary>
    /// The frozen file, with the four changes its header lists undone, is the original byte for byte. Any other
    /// edit, however small or well meant, fails here: formatting, a renamed variable, a corrected defect.
    /// </summary>
    [Test]
    public void TheFrozenSource_IsTheOriginalFileByteForByte()
    {
        byte[] original = Published100Source.ReconstructOriginal();

        Assert.That(original, Has.Length.EqualTo(Published100Source.Length));
        Assert.That(Published100Source.Sha256Of(original), Is.EqualTo(Published100Source.Sha256),
            Published100Source.FrozenFile + " was changed. It is historical evidence and is never edited; restore it from Git.");
    }

    /// <summary>The comment in front of the source names the same commit, object and hash as the tests use.</summary>
    [Test]
    public void TheHeaderOfTheFrozenFile_RecordsWhereTheSourceComesFrom()
    {
        string header = Published100Source.Header();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(header, Ordinal.Contains(Published100Source.Commit));
            Assert.That(header, Ordinal.Contains(Published100Source.GitBlob));
            Assert.That(header, Ordinal.Contains(Published100Source.Sha256));
            Assert.That(header, Ordinal.Contains(Published100Source.OriginalPath));
            Assert.That(header, Ordinal.Contains(Published100Source.FrozenNamespace));
            Assert.That(header, Ordinal.Contains("Copyright (c) 2023 Shaun Hathaway"));
            Assert.That(header, Ordinal.Contains("MIT License"));
            Assert.That(header.Split('\n', StringSplitOptions.RemoveEmptyEntries), Has.All.StartsWith("//"), "the header is a comment and nothing else");
        }
    }

    /// <summary>The copyright line of the header is the one of the license the repository is under.</summary>
    [Test]
    public void TheCopyrightOfTheFrozenSource_IsTheOneOfTheRepositorysLicense()
    {
        string license = Repository.Read("LICENSE");

        Assert.That(license, Does.StartWith("MIT License").Using(StringComparison.Ordinal));
        Assert.That(license, Ordinal.Contains("Copyright (c) 2023 Shaun Hathaway"));
    }

    // ---- named apart from the library's class ----

    [Test]
    public void TheFrozenClass_IsInANamespaceOfTheTests_AndNotTheOneOfTheLibrary()
    {
        Assert.That(Frozen.FullName, Is.EqualTo(Published100Source.FrozenNamespace + ".ConsoleCalendar"));
        Assert.That(Frozen.Namespace, Does.StartWith("ConsoleCalendarGenerator.Tests.Historical").Using(StringComparison.Ordinal));
        Assert.That(Frozen, Is.Not.SameAs(InTheLibrary));
        Assert.That(typeof(Published100SourceTests).Assembly.GetTypes().Where(type => type.Namespace == "CalendarGenerator"), Is.Empty,
            "the test assembly declares nothing in the library's legacy namespace");
    }

    /// <summary>
    /// The frozen class has the public members of 1.0.0, which are also the public members of the library's
    /// class: no approved change touches a signature or a parameter name.
    /// </summary>
    [Test]
    public void TheFrozenClass_HasThePublicMembersOfTheLibrarysClass()
    {
        string[] frozen = ApiSurface.Describe(Frozen.Assembly, Frozen.Namespace).Skip(1)
            .Select(line => line.Replace(Published100Source.FrozenNamespace, Published100Source.OriginalNamespace, StringComparison.Ordinal))
            .ToArray();
        string[] inTheLibrary = ApiSurface.Describe(InTheLibrary.Assembly, Published100Source.OriginalNamespace).Skip(1).ToArray();

        Assert.That(frozen, Has.Length.EqualTo(18), "the class and its 17 public members");
        Assert.That(frozen, Is.EqualTo(inTheLibrary));
    }

    // ---- only in the tests ----

    [Test]
    public void TheFrozenClass_IsCompiledIntoTheTestAssembly_WhichTheLibraryDoesNotReference()
    {
        Assembly tests = typeof(Published100SourceTests).Assembly;
        Assembly library = InTheLibrary.Assembly;

        Assert.That(Frozen.Assembly, Is.SameAs(tests));
        Assert.That(Frozen.Assembly, Is.Not.SameAs(library));
        Assert.That(library.GetReferencedAssemblies().Select(name => name.Name), Has.None.Contains("Tests"));
    }

    /// <summary>
    /// Nothing of the frozen source is in the library: not its namespace, and not one of the private members that
    /// only the original implementation has.
    /// </summary>
    [Test]
    public void TheLibrary_ContainsNothingOfTheFrozenSource()
    {
        const BindingFlags everything = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;
        string[] onlyInTheOriginal = Frozen.GetMethods(everything)
            .Where(method => method.IsPrivate && !method.IsSpecialName)
            .Select(method => method.Name)
            .ToArray();
        Type[] libraryTypes = InTheLibrary.Assembly.GetTypes();

        Assert.That(onlyInTheOriginal, Is.EquivalentTo(new[] { "SetColor", "GetSpaces", "GetNextShortDay", "ConvertToShortDay" }));
        Assert.That(libraryTypes, Has.Length.GreaterThan(15));
        Assert.That(libraryTypes.Select(type => type.FullName!).Where(name =>
            name.Contains("Historical", StringComparison.Ordinal)
            || name.Contains("Published100", StringComparison.Ordinal)
            || name.Contains("Tests", StringComparison.Ordinal)), Is.Empty);
        Assert.That(libraryTypes.SelectMany(type => type.GetMethods(everything)).Select(method => method.Name).Intersect(onlyInTheOriginal), Is.Empty);
    }

    /// <summary>The tests are never packed, and the library compiles nothing from outside its own folder.</summary>
    [Test]
    public void TheTestProjectIsNotPackable_AndTheLibraryCompilesOnlyItsOwnFolder()
    {
        string library = Repository.Read("ConsoleCalendarGenerator/ConsoleCalendarGenerator.csproj");
        string tests = Repository.Read("ConsoleCalendarGenerator.Tests/ConsoleCalendarGenerator.Tests.csproj");
        string shared = Repository.Read("Directory.Build.props");

        Assert.That(shared, Ordinal.Contains("<IsPackable>false</IsPackable>"));
        Assert.That(tests, Does.Not.Match("IsPackable|PackageId|GeneratePackageOnBuild"));
        Assert.That(library, Does.Not.Match(@"<Compile\s|<EmbeddedResource\s|<Content\s|ProjectReference|InternalsVisibleTo"));
        Assert.That(Repository.FilesWithExtension(".cs").Where(file => file.StartsWith("ConsoleCalendarGenerator/", StringComparison.Ordinal)
            && (file.Contains("Historical", StringComparison.Ordinal) || file.Contains("Published100", StringComparison.Ordinal))), Is.Empty);
    }
}
