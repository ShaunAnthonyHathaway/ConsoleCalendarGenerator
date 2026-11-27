using System.Reflection;

namespace ConsoleCalendarGenerator.Tests.Support;

/// <summary>Describes an assembly's public API as sorted text lines, one per type and member.</summary>
internal static class ApiSurface
{
    /// <param name="inNamespace">When given, only the types declared in exactly this namespace are described.</param>
    public static IReadOnlyList<string> Describe(Assembly assembly, string? inNamespace = null)
    {
        var lines = new List<string> { "assembly " + assembly.GetName().Name };
        foreach (Type type in assembly.GetExportedTypes()
            .Where(t => inNamespace == null || t.Namespace == inNamespace)
            .OrderBy(t => t.FullName, StringComparer.Ordinal))
        {
            lines.Add(DescribeType(type));
            const BindingFlags declared = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;
            var members = new List<string>();
            foreach (MemberInfo member in type.GetMembers(declared))
            {
                switch (member)
                {
                    case ConstructorInfo constructor:
                        members.Add("  .ctor(" + Parameters(constructor) + ")");
                        break;
                    case MethodInfo method:
                        members.Add("  " + Modifiers(method) + Name(method.ReturnType) + " " + method.Name + "(" + Parameters(method) + ")");
                        break;
                    case FieldInfo { IsSpecialName: true }:
                        // The hidden field that stores an enum's value. It is the same for every enum.
                        break;
                    case FieldInfo { IsLiteral: true } constant:
                        // The value of an enum member is part of the compiled contract, so it is listed too.
                        members.Add("  " + constant.Name + " = " + Convert.ToInt64(constant.GetRawConstantValue(), System.Globalization.CultureInfo.InvariantCulture));
                        break;
                    default:
                        members.Add("  " + member.MemberType.ToString().ToLowerInvariant() + " " + member.Name);
                        break;
                }
            }
            members.Sort(StringComparer.Ordinal);
            lines.AddRange(members);
        }
        return lines;
    }

    private static string DescribeType(Type type)
    {
        if (type.IsEnum)
        {
            // What an enum derives from and implements is decided by the runtime, not by the library.
            return "public enum " + type.FullName + " : " + Name(Enum.GetUnderlyingType(type));
        }
        string kind = type.IsInterface ? "interface" : type.IsEnum ? "enum" : type.IsValueType ? "struct" : "class";
        string modifiers = type.IsAbstract && type.IsSealed ? "static " : type.IsAbstract ? "abstract " : type.IsSealed ? "sealed " : string.Empty;
        string bases = string.Join(", ", new[] { type.BaseType }.Where(t => t != null).Select(t => Name(t!))
            .Concat(type.GetInterfaces().Select(Name).OrderBy(n => n, StringComparer.Ordinal)));
        return "public " + modifiers + kind + " " + type.FullName + (bases.Length > 0 ? " : " + bases : string.Empty);
    }

    private static string Modifiers(MethodInfo method) =>
        (method.IsStatic ? "static " : string.Empty) + (method.IsAbstract ? "abstract " : method.IsVirtual ? "virtual " : string.Empty);

    private static string Parameters(MethodBase method) =>
        string.Join(", ", method.GetParameters().Select(p => Name(p.ParameterType) + " " + p.Name));

    private static string Name(Type type)
    {
        if (!type.IsGenericType)
        {
            return type.FullName ?? type.Name;
        }
        string definition = type.GetGenericTypeDefinition().FullName!;
        return definition.Substring(0, definition.IndexOf('`')) + "<" + string.Join(", ", type.GetGenericArguments().Select(Name)) + ">";
    }
}
