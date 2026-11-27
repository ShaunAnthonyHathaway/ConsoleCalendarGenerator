using NUnit.Framework.Constraints;

namespace ConsoleCalendarGenerator.Tests.Support;

/// <summary>
/// Substring constraints that compare character by character. NUnit's Does.Contain compares with the current
/// culture, which ignores control characters such as the escape character and so "finds" them in any text.
/// </summary>
internal static class Ordinal
{
    public static Constraint Contains(string expected) => new SubstringConstraint(expected).Using(StringComparison.Ordinal);

    public static Constraint DoesNotContain(string expected) => new NotConstraint(Contains(expected));
}
