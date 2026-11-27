using System.Reflection;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using CalendarGenerator;
using ConsoleCalendarGenerator.Tests.Support;

namespace ConsoleCalendarGenerator.Tests.Documentation;

/// <summary>
/// The XML documentation file that is built with the library and ships in the package documents every public
/// type and member, the legacy class included, and nothing turns the compiler's check for it off. The tests
/// read the file built beside the library build under test, so both builds are covered.
/// </summary>
[TestFixture]
public class XmlDocumentationTests
{
    private const BindingFlags Declared = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

    private static readonly Assembly Library = typeof(MonthLayout).Assembly;

    private static readonly Lazy<Dictionary<string, XElement>> Documentation = new(() =>
        XDocument.Load(Path.ChangeExtension(Library.Location, ".xml"))
            .Descendants("member")
            .ToDictionary(member => (string)member.Attribute("name")!, StringComparer.Ordinal));

    /// <summary>The documentation ID of every public type and member, as the compiler writes it.</summary>
    private static IEnumerable<string> PublicApi()
    {
        foreach (Type type in Library.GetExportedTypes().OrderBy(type => type.FullName, StringComparer.Ordinal))
        {
            yield return "T:" + type.FullName;
            foreach (MemberInfo member in type.GetMembers(Declared))
            {
                switch (member)
                {
                    case ConstructorInfo constructor:
                        yield return "M:" + type.FullName + ".#ctor" + Parameters(constructor);
                        break;
                    case MethodInfo { IsSpecialName: false } method:
                        yield return "M:" + type.FullName + "." + method.Name + Parameters(method);
                        break;
                    case PropertyInfo property:
                        yield return "P:" + type.FullName + "." + property.Name;
                        break;
                    case FieldInfo { IsSpecialName: false } field:
                        yield return "F:" + type.FullName + "." + field.Name;
                        break;
                }
            }
        }
    }

    private static string Parameters(MethodBase method)
    {
        ParameterInfo[] parameters = method.GetParameters();
        return parameters.Length == 0 ? string.Empty : "(" + string.Join(",", parameters.Select(parameter => Id(parameter.ParameterType))) + ")";
    }

    private static string Id(Type type)
    {
        if (type.IsArray)
        {
            return Id(type.GetElementType()!) + "[]";
        }
        if (!type.IsGenericType)
        {
            return type.FullName!;
        }
        string name = type.GetGenericTypeDefinition().FullName!;
        return name.Substring(0, name.IndexOf('`')) + "{" + string.Join(",", type.GetGenericArguments().Select(Id)) + "}";
    }

    private static string TextOf(XElement? element) => element == null ? string.Empty : Regex.Replace(element.Value, @"\s+", " ").Trim();

    private static MethodBase[] PublicMethods() => Library.GetExportedTypes()
        .SelectMany(type => type.GetMembers(Declared).OfType<MethodBase>())
        .Where(method => method is ConstructorInfo || !method.IsSpecialName)
        .ToArray();

    private static string IdOf(MethodBase method) =>
        "M:" + method.DeclaringType!.FullName + "." + (method is ConstructorInfo ? "#ctor" : method.Name) + Parameters(method);

    [Test]
    public void TheDocumentationFile_IsBuiltBesideTheLibrary()
    {
        Assert.That(File.Exists(Path.ChangeExtension(Library.Location, ".xml")), Path.ChangeExtension(Library.Location, ".xml"));
        Assert.That(Documentation.Value, Has.Count.GreaterThan(100));
    }

    [Test]
    public void EveryPublicTypeAndMember_HasASummary()
    {
        string[] api = PublicApi().ToArray();

        Assert.That(api, Has.Length.GreaterThan(100));
        Assert.Multiple(() =>
        {
            foreach (string id in api)
            {
                Assert.That(Documentation.Value.ContainsKey(id), id + " is not in the documentation file");
                if (Documentation.Value.TryGetValue(id, out XElement? member))
                {
                    Assert.That(TextOf(member.Element("summary")), Is.Not.Empty, id + " has no summary");
                }
            }
        });
    }

    [Test]
    public void EveryParameterOfEveryPublicMethod_IsDescribed()
    {
        Assert.Multiple(() =>
        {
            foreach (MethodBase method in PublicMethods())
            {
                Documentation.Value.TryGetValue(IdOf(method), out XElement? member);
                foreach (ParameterInfo parameter in method.GetParameters())
                {
                    XElement? described = member?.Elements("param").SingleOrDefault(element => (string?)element.Attribute("name") == parameter.Name);
                    Assert.That(TextOf(described), Is.Not.Empty, IdOf(method) + ": parameter " + parameter.Name);
                }
            }
        });
    }

    [Test]
    public void NoSummaryIsAPlaceholder()
    {
        Assert.Multiple(() =>
        {
            foreach (KeyValuePair<string, XElement> member in Documentation.Value)
            {
                Assert.That(TextOf(member.Value), Does.Not.Match(@"(?i)\b(TODO|TBD|FIXME)\b|^Gets or sets the\.?$"), member.Key);
            }
        });
    }

    // ---- the legacy class ----

    [Test]
    public void TheLegacyClass_HasAll18OfItsPublicMembersDocumented()
    {
        string[] legacy = PublicApi().Where(id => id.Substring(2).StartsWith("CalendarGenerator.ConsoleCalendar", StringComparison.Ordinal)).ToArray();

        Assert.That(legacy, Has.Length.EqualTo(18), "the type, five constructors and twelve methods");
        Assert.That(legacy.Where(id => !Documentation.Value.ContainsKey(id)), Is.Empty);
    }

    [Test]
    public void TheLegacyClass_SaysItIsKeptForCompatibility_AndPointsToTheModernApi()
    {
        XElement type = Documentation.Value["T:" + typeof(ConsoleCalendar).FullName];

        Assert.That(TextOf(type), Ordinal.Contains("1.0.0"));
        Assert.That(type.Descendants("see").Select(see => (string?)see.Attribute("cref")),
            Is.SupersetOf(new[] { "T:ConsoleCalendarGenerator.MonthRenderer", "T:ConsoleCalendarGenerator.ConsoleMonthWriter" }));
    }

    /// <summary>
    /// The constructors that take colors: every argument is documented as coloring the element it is named for,
    /// which is what version 2.0 does (D-57). Each of them also says that 1.0.0 did otherwise and points to the
    /// parameterless constructor, and none describes the old assignment as what happens now.
    /// </summary>
    [Test]
    public void TheLegacyConstructorsThatTakeColors_DocumentEachArgumentAsColoringWhatItIsNamedFor_AndSayThat100DidOtherwise()
    {
        var colored = new Dictionary<string, string>
        {
            ["TitleColor"] = "The color of the title.",
            ["DateColor"] = "The color of the days that are not selected.",
            ["SelectedDateColor"] = "The color of the selected days.",
            ["DayOfWeekColor"] = "The color of the weekday row.",
        };
        ConstructorInfo[] withColors = typeof(ConsoleCalendar).GetConstructors().Where(constructor => constructor.GetParameters().Length > 0).ToArray();

        Assert.That(withColors, Has.Length.EqualTo(4));
        foreach (ConstructorInfo constructor in withColors)
        {
            XElement member = Documentation.Value[IdOf(constructor)];
            foreach (ParameterInfo parameter in constructor.GetParameters())
            {
                string text = TextOf(member.Elements("param").Single(element => (string?)element.Attribute("name") == parameter.Name));

                Assert.That(text, Is.EqualTo(colored[parameter.Name!]), IdOf(constructor) + " " + parameter.Name);
            }
            string remarks = TextOf(member.Element("remarks"));
            Assert.That(remarks, Ordinal.Contains("Version 1.0.0 gave"), IdOf(constructor));
            Assert.That(remarks, Ordinal.Contains("Version 2.0 corrects that"), IdOf(constructor));
            Assert.That(remarks, Ordinal.Contains("parameterless constructor"), IdOf(constructor));
            Assert.That(TextOf(member), Does.Not.Match("Despite its name|does not set"), IdOf(constructor));
        }
    }

    [Test]
    public void TheLegacyMethodsThatDraw_DocumentTheirExceptions()
    {
        MethodInfo[] drawing = typeof(ConsoleCalendar).GetMethods(Declared).Where(method => method.Name.StartsWith("Generate", StringComparison.Ordinal)).ToArray();

        Assert.That(drawing, Has.Length.EqualTo(4));
        foreach (MethodInfo method in drawing)
        {
            string?[] exceptions = Documentation.Value[IdOf(method)].Elements("exception").Select(element => (string?)element.Attribute("cref")).ToArray();

            Assert.That(exceptions, Has.Member("T:System.ArgumentOutOfRangeException"), IdOf(method));
            if (method.GetParameters().Any(parameter => parameter.Name == "dates"))
            {
                Assert.That(exceptions, Has.Member("T:System.NullReferenceException"), IdOf(method));
            }
        }
    }

    // ---- nothing turns the check off ----

    [Test]
    public void TheMissingCommentWarning_IsNotTurnedOffAnywhere()
    {
        foreach (string file in new[] { ".editorconfig", "Directory.Build.props" })
        {
            Assert.That(Repository.Read(file), Does.Not.Match(@"1591|CS1573|CS1572|CS1570|CS1574"), file);
        }
        string project = Repository.Read("ConsoleCalendarGenerator/ConsoleCalendarGenerator.csproj");
        Assert.That(project, Ordinal.Contains("<GenerateDocumentationFile>true</GenerateDocumentationFile>"));
        Assert.That(project, Does.Not.Match(@"<NoWarn|WarningsNotAsErrors|<DocumentationFile>"));
    }

    [Test]
    public void NoLibrarySourceFile_SuppressesAWarning()
    {
        string[] sources = Repository.FilesWithExtension(".cs").Where(file => file.StartsWith("ConsoleCalendarGenerator/", StringComparison.Ordinal)).ToArray();

        Assert.That(sources, Has.Length.GreaterThan(15));
        foreach (string source in sources)
        {
            Assert.That(Repository.Read(source), Does.Not.Match(@"#pragma\s+warning|SuppressMessage"), source);
        }
    }
}
