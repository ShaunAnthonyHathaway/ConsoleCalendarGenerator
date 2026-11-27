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
            Assert.That(exceptions, Has.No.Member("T:System.NullReferenceException"), IdOf(method) + ": that was 1.0.0");
            if (method.GetParameters().Any(parameter => parameter.Name == "dates"))
            {
                Assert.That(exceptions, Has.Member("T:System.ArgumentNullException"), IdOf(method));
            }
        }
    }

    /// <summary>
    /// The two methods that take a list: a null list is documented as refused with ArgumentNullException, first
    /// and before anything is written, which is what version 2.0 does (D-58). The comment also says what 1.0.0
    /// threw, and the parameter itself is documented as not to be null.
    /// </summary>
    [Test]
    public void TheLegacyMethodsThatTakeAList_DocumentThatANullListIsRefused_AndSayWhat100Threw()
    {
        MethodInfo[] withAList = typeof(ConsoleCalendar).GetMethods(Declared)
            .Where(method => method.GetParameters().Any(parameter => parameter.Name == "dates"))
            .ToArray();

        Assert.That(withAList.Select(method => method.Name), Is.EquivalentTo(new[] { "GenerateMonth", "GenerateYear" }));
        foreach (MethodInfo method in withAList)
        {
            XElement member = Documentation.Value[IdOf(method)];
            XElement refused = member.Elements("exception").Single(element => (string?)element.Attribute("cref") == "T:System.ArgumentNullException");
            string parameter = TextOf(member.Elements("param").Single(element => (string?)element.Attribute("name") == "dates"));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(refused.Elements("paramref").Select(reference => (string?)reference.Attribute("name")), Has.Member("dates"), IdOf(method));
                Assert.That(TextOf(refused), Ordinal.Contains("checked first"), IdOf(method));
                Assert.That(TextOf(refused), Ordinal.Contains("nothing is written"), IdOf(method));
                Assert.That(TextOf(refused), Ordinal.Contains("Version 1.0.0 threw"), IdOf(method));
                Assert.That(refused.Elements("see").Select(see => (string?)see.Attribute("cref")), Has.Member("T:System.NullReferenceException"), IdOf(method));
                Assert.That(parameter, Ordinal.Contains("must not be"), IdOf(method));
            }
        }
    }

    /// <summary>
    /// How a month ends is documented as version 2.0 does it (D-59 and D-60): every line ends with the console
    /// writer's line terminator, and exactly one empty line follows a month. The comments say what 1.0.0 did,
    /// and no longer describe it as what happens.
    /// </summary>
    [Test]
    public void TheLegacyMethodsThatDraw_DocumentHowAMonthEnds_AndSayWhat100Did()
    {
        MethodInfo month = typeof(ConsoleCalendar).GetMethod("GenerateMonth", new[] { typeof(int), typeof(int), typeof(List<DateTime>) })!;
        string remarks = TextOf(Documentation.Value[IdOf(month)].Element("remarks"));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(remarks, Ordinal.Contains("followed by exactly one empty line"));
            Assert.That(remarks, Ordinal.Contains("Every line, the last line of days and the empty line after it included, ends with the line terminator"));
            Assert.That(remarks, Ordinal.Contains("Version 1.0.0 wrote two after a month whose last day is a Sunday"));
            Assert.That(remarks, Ordinal.Contains("Version 1.0.0 ended the last line of days with a line feed"));
            Assert.That(remarks, Does.Not.Match("or by two when|except the last line of days|contains both forms"), "the old behavior is not described as current");
        }

        foreach (MethodInfo year in typeof(ConsoleCalendar).GetMethods(Declared).Where(method => method.Name == "GenerateYear"))
        {
            Assert.That(TextOf(Documentation.Value[IdOf(year)].Element("remarks")), Ordinal.Contains("Every month, December included, is followed by one empty line"), IdOf(year));
        }
    }

    /// <summary>
    /// The title is documented as version 2.0 writes it (D-61): the current culture's name for the Gregorian
    /// month. The comments say what 1.0.0 did, and no longer describe the other month as what happens. What the
    /// culture does not decide is still documented: the English weekday row, weeks from Monday, the year.
    /// </summary>
    [Test]
    public void TheLegacyMethodsThatDraw_DocumentTheGregorianTitle_AndSayWhat100Did()
    {
        MethodInfo month = typeof(ConsoleCalendar).GetMethod("GenerateMonth", new[] { typeof(int), typeof(int), typeof(List<DateTime>) })!;
        string remarks = TextOf(Documentation.Value[IdOf(month)].Element("remarks"));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(remarks, Ordinal.Contains("the title names that month"));
            Assert.That(remarks, Ordinal.Contains("has for the month of the Gregorian calendar"));
            Assert.That(remarks, Ordinal.Contains("Version 1.0.0 wrote the culture's name for month number"));
            Assert.That(remarks, Ordinal.Contains("version 2.0 corrects that"));
            Assert.That(remarks, Ordinal.Contains("weeks start on Monday"));
            Assert.That(remarks, Ordinal.Contains("The weekday row is the English one"));
            Assert.That(remarks, Ordinal.Contains("never converted"));
            Assert.That(remarks, Does.Not.Match("can therefore name a different month|what the calendar of"), "the old behavior is not described as current");
        }

        foreach (MethodInfo year in typeof(ConsoleCalendar).GetMethods(Declared).Where(method => method.Name == "GenerateYear"))
        {
            Assert.That(TextOf(Documentation.Value[IdOf(year)].Element("remarks")), Ordinal.Contains("Every title names the Gregorian month below it"), IdOf(year));
        }
    }

    /// <summary>
    /// The remarks of the legacy class describe it as it is in version 2.0: kept, supported, with the API of
    /// 1.0.0 and five corrected behaviors. Nothing in the documentation file speaks of a 1.x line, which was
    /// never published.
    /// </summary>
    [Test]
    public void TheLegacyClass_IsDocumentedAsItIsInVersion20()
    {
        XElement type = Documentation.Value["T:CalendarGenerator.ConsoleCalendar"];
        string summary = TextOf(type.Element("summary"));
        string remarks = TextOf(type.Element("remarks"));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(summary, Ordinal.Contains("supported in version 2.0"));
            Assert.That(remarks, Ordinal.Contains("Version 2.0 corrects five things"));
            Assert.That(remarks, Ordinal.Contains("without being changed"), "a program written for 1.0.0 compiles and runs");
            Assert.That(remarks, Ordinal.Contains("Gregorian"), "which month the title names");
            Assert.That(remarks, Ordinal.Contains("The weekday row is always in English"));
            Assert.That(remarks, Ordinal.Contains("Weeks start on Monday"));
            Assert.That(remarks, Ordinal.Contains("Drawing is not synchronized"));
            Assert.That(string.Concat(Documentation.Value.Values.Select(member => member.ToString())), Does.Not.Match(@"1\.x\b"));
        }
    }

    /// <summary>
    /// Text the caller supplies is written as it is, and the documentation says so where the text is supplied
    /// and where it is written. Rendering/SuppliedTextTests.cs shows that it is true.
    /// </summary>
    [Test]
    public void TheModernApi_DocumentsThatSuppliedTextIsWrittenAsItIs()
    {
        string options = TextOf(Documentation.Value["T:ConsoleCalendarGenerator.MonthRenderOptions"].Element("remarks"));
        string consoleWriter = TextOf(Documentation.Value["T:ConsoleCalendarGenerator.ConsoleMonthWriter"].Element("remarks"));

        Assert.That(options, Ordinal.Contains("is put into the rendered month as it is"));
        Assert.That(options, Ordinal.Contains("control characters included"));
        Assert.That(consoleWriter, Ordinal.Contains("written exactly as it was rendered"));
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
