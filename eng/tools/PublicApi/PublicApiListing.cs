using System.Reflection;

namespace ConsoleCalendarGenerator.ReleaseTools;

/// <summary>
/// The public API of an assembly, written out as lines of text: every exported type, and under it every
/// constructor, method, property, field and event that code outside the assembly can use, with the names of the
/// parameters, because a caller can use those too. The lines are in a fixed order and contain nothing that
/// depends on the machine, the runtime or the build, so the same API always gives the same text.
///
/// <para>
/// eng/public-api.txt holds this text for the library. The tool in this folder compares the assemblies of a
/// package with it, and the tests compare the library they were built against with it, so the file, the source
/// and a released package cannot disagree about what the public API is.
/// </para>
/// </summary>
internal static class PublicApiListing
{
    private const BindingFlags Declared =
        BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

    /// <summary>The lines of the listing: a type, then its members indented by two spaces, in ordinal order.</summary>
    public static IReadOnlyList<string> Of(Assembly assembly)
    {
        var lines = new List<string>();
        foreach (Type type in assembly.GetExportedTypes().OrderBy(type => type.FullName, StringComparer.Ordinal))
        {
            lines.Add(Describe(type));
            var members = new List<string>();
            foreach (ConstructorInfo constructor in type.GetConstructors(Declared).Where(IsVisible))
            {
                members.Add("  " + Access(constructor) + "constructor(" + Parameters(constructor) + ")");
            }
            foreach (MethodInfo method in type.GetMethods(Declared).Where(method => IsVisible(method) && !method.IsSpecialName))
            {
                members.Add("  " + Access(method) + Modifiers(method) + "method " + Name(method.ReturnType) + " " + method.Name
                    + (method.IsGenericMethodDefinition ? "<" + string.Join(", ", method.GetGenericArguments().Select(Name)) + ">" : string.Empty)
                    + "(" + Parameters(method) + ")");
            }
            foreach (PropertyInfo property in type.GetProperties(Declared))
            {
                MethodInfo[] accessors = property.GetAccessors(nonPublic: true).Where(IsVisible).ToArray();
                if (accessors.Length == 0)
                {
                    continue;
                }
                MethodInfo? getter = accessors.FirstOrDefault(accessor => accessor == property.GetMethod);
                MethodInfo? setter = accessors.FirstOrDefault(accessor => accessor == property.SetMethod);
                string index = property.GetIndexParameters().Length == 0
                    ? string.Empty
                    : "[" + string.Join(", ", property.GetIndexParameters().Select(Parameter)) + "]";
                members.Add("  " + Modifiers(accessors[0]) + "property " + Name(property.PropertyType) + " " + property.Name + index + " {"
                    + (getter == null ? string.Empty : " " + Access(getter) + "get;")
                    + (setter == null ? string.Empty : " " + Access(setter) + "set;") + " }");
            }
            foreach (FieldInfo field in type.GetFields(Declared).Where(field => IsVisible(field) && !field.IsSpecialName))
            {
                members.Add("  " + Access(field) + (field.IsLiteral ? "constant " : field.IsStatic ? "static " : string.Empty)
                    + (field.IsInitOnly ? "readonly " : string.Empty) + "field " + Name(field.FieldType) + " " + field.Name
                    + (field.IsLiteral ? " = " + Convert.ToString(field.GetRawConstantValue(), System.Globalization.CultureInfo.InvariantCulture) : string.Empty));
            }
            foreach (EventInfo declaredEvent in type.GetEvents(Declared))
            {
                MethodInfo? adder = declaredEvent.GetAddMethod(nonPublic: true);
                if (adder != null && IsVisible(adder))
                {
                    members.Add("  " + Access(adder) + Modifiers(adder) + "event " + Name(declaredEvent.EventHandlerType!) + " " + declaredEvent.Name);
                }
            }
            members.Sort(StringComparer.Ordinal);
            lines.AddRange(members);
        }
        return lines;
    }

    private static string Describe(Type type)
    {
        string kind =
            type.IsEnum ? "enum"
            : type.IsInterface ? "interface"
            : type.IsValueType ? "struct"
            : typeof(Delegate).IsAssignableFrom(type) ? "delegate"
            : type.IsAbstract && type.IsSealed ? "static class"
            : type.IsAbstract ? "abstract class"
            : type.IsSealed ? "sealed class"
            : "class";
        var parents = new List<string>();
        if (type.IsEnum)
        {
            parents.Add(Name(Enum.GetUnderlyingType(type)));
        }
        else if (type.BaseType != null && !type.IsValueType)
        {
            parents.Add(Name(type.BaseType));
        }
        // The interfaces the type adds to those of its base type. What a base type of the runtime implements,
        // System.Enum for one, differs between runtimes and is not this assembly's API.
        Type[] inherited = type.BaseType == null ? Type.EmptyTypes : type.BaseType.GetInterfaces();
        parents.AddRange(type.GetInterfaces().Except(inherited).Select(Name).OrderBy(name => name, StringComparer.Ordinal));
        return "public " + kind + " " + Name(type) + (parents.Count == 0 ? string.Empty : " : " + string.Join(", ", parents));
    }

    private static bool IsVisible(MethodBase method) => method.IsPublic || method.IsFamily || method.IsFamilyOrAssembly;

    private static bool IsVisible(FieldInfo field) => field.IsPublic || field.IsFamily || field.IsFamilyOrAssembly;

    private static string Access(MethodBase method) => method.IsPublic ? string.Empty : "protected ";

    private static string Access(FieldInfo field) => field.IsPublic ? string.Empty : "protected ";

    private static string Modifiers(MethodInfo method) =>
        method.IsStatic ? "static "
        : method.IsAbstract ? "abstract "
        : method.IsVirtual && method.GetBaseDefinition() != method ? (method.IsFinal ? "sealed override " : "override ")
        : method.IsVirtual && !method.IsFinal ? "virtual "
        : string.Empty;

    private static string Parameters(MethodBase method) => string.Join(", ", method.GetParameters().Select(Parameter));

    private static string Parameter(ParameterInfo parameter) =>
        (parameter.IsOut ? "out " : parameter.ParameterType.IsByRef ? "ref " : string.Empty)
        + (parameter.IsDefined(typeof(ParamArrayAttribute), inherit: false) ? "params " : string.Empty)
        + Name(parameter.ParameterType.IsByRef ? parameter.ParameterType.GetElementType()! : parameter.ParameterType)
        + " " + parameter.Name
        + (parameter.HasDefaultValue ? " = " + (Convert.ToString(parameter.RawDefaultValue, System.Globalization.CultureInfo.InvariantCulture) ?? "null") : string.Empty);

    /// <summary>A type's name as it is written in C#, with its namespace: no assembly, no arity marks.</summary>
    private static string Name(Type type)
    {
        if (type.IsGenericParameter)
        {
            return type.Name;
        }
        if (type.IsArray)
        {
            return Name(type.GetElementType()!) + "[" + new string(',', type.GetArrayRank() - 1) + "]";
        }
        if (type.IsByRef || type.IsPointer)
        {
            return Name(type.GetElementType()!) + (type.IsByRef ? "&" : "*");
        }
        string name = type.IsNested ? Name(type.DeclaringType!) + "." + type.Name : (type.Namespace == null ? string.Empty : type.Namespace + ".") + type.Name;
        if (!type.IsGenericType)
        {
            return name;
        }
        int mark = name.LastIndexOf('`');
        return (mark < 0 ? name : name.Substring(0, mark)) + "<" + string.Join(", ", type.GetGenericArguments().Select(Name)) + ">";
    }
}
