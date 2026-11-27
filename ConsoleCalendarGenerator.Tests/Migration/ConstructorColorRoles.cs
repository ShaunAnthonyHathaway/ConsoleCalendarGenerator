namespace ConsoleCalendarGenerator.Tests.Migration;

/// <summary>
/// Which color each argument of a constructor sets, in version 2.0 and in version 1.0.0, written as the setter
/// calls that have the same effect on a calendar made by the parameterless constructor. The arguments are in
/// the order of the published parameters: <c>TitleColor</c>, <c>DateColor</c>, <c>SelectedDateColor</c>,
/// <c>DayOfWeekColor</c>. A color with no argument keeps its default in both versions.
///
/// <para>
/// Both lists are checked against the implementation they describe: <see cref="In100"/> against the frozen
/// source of 1.0.0, <see cref="In20"/> against the library (Migration/ConstructorColorMigrationTests.cs).
/// </para>
/// </summary>
internal static class ConstructorColorRoles
{
    /// <summary>Version 2.0 (D-57): each argument sets the color it is named for.</summary>
    private static readonly LegacyColorRole[] RolesIn20 =
    {
        LegacyColorRole.Title, LegacyColorRole.Date, LegacyColorRole.SelectedDate, LegacyColorRole.DayOfWeek,
    };

    /// <summary>
    /// Version 1.0.0: the argument named <c>TitleColor</c> sets the weekday row, and the argument named
    /// <c>DayOfWeekColor</c>, which only the four-argument constructor has, sets the title.
    /// </summary>
    private static readonly LegacyColorRole[] RolesIn100 =
    {
        LegacyColorRole.DayOfWeek, LegacyColorRole.Date, LegacyColorRole.SelectedDate, LegacyColorRole.Title,
    };

    public static IReadOnlyList<(LegacyColorRole Role, ConsoleColor Color)> In20(IReadOnlyList<ConsoleColor> constructorArguments) =>
        Assign(RolesIn20, constructorArguments);

    public static IReadOnlyList<(LegacyColorRole Role, ConsoleColor Color)> In100(IReadOnlyList<ConsoleColor> constructorArguments) =>
        Assign(RolesIn100, constructorArguments);

    private static (LegacyColorRole Role, ConsoleColor Color)[] Assign(LegacyColorRole[] roles, IReadOnlyList<ConsoleColor> constructorArguments)
    {
        if (constructorArguments.Count > roles.Length)
        {
            throw new ArgumentException("The legacy calendar has no constructor that takes " + constructorArguments.Count + " colors.", nameof(constructorArguments));
        }
        return constructorArguments.Select((color, index) => (roles[index], color)).ToArray();
    }
}
