namespace ConsoleCalendarGenerator.Tests.Migration;

/// <summary>
/// One of the behaviors of the legacy calendar that version 2.0 corrects on purpose. There are exactly five,
/// each with a decision in docs/ai-context/decisions.md, and nothing else about the legacy calendar may change.
///
/// <para>
/// A correction starts here as approved and not implemented: <see cref="Corrected"/> is null, and the migration
/// tests require the library to do exactly what 1.0.0 did. The pull request that implements a correction in the
/// library sets <see cref="Corrected"/> in the same change, to a function that says what the corrected library
/// does in a scenario, given what 1.0.0 did in it. From then on the tests require exactly that, in every
/// aspect and in every scenario, so one correction can be made without loosening anything about the others or
/// about the behavior that stays.
/// </para>
/// </summary>
internal sealed class ApprovedCorrection
{
    public ApprovedCorrection(
        string decision,
        string summary,
        Func<IReadOnlyList<LegacyScenario>> scenarios,
        Func<LegacyScenario, LegacyObservation, LegacyObservation>? corrected = null)
    {
        Decision = decision;
        Summary = summary;
        _scenarios = scenarios;
        Corrected = corrected;
    }

    private readonly Func<IReadOnlyList<LegacyScenario>> _scenarios;

    /// <summary>The identifier of the decision in docs/ai-context/decisions.md, such as <c>D-57</c>.</summary>
    public string Decision { get; }

    /// <summary>What 1.0.0 does and what 2.0 is to do instead, in a sentence.</summary>
    public string Summary { get; }

    /// <summary>The scenarios of <see cref="MigrationMatrix"/> in which 1.0.0 shows the behavior being corrected.</summary>
    public IReadOnlyList<LegacyScenario> Scenarios => _scenarios();

    /// <summary>
    /// Null until the library implements the correction. Then: what the corrected library does in a scenario,
    /// given what was expected of it before this correction. It returns its argument unchanged for a scenario
    /// the correction does not touch.
    /// </summary>
    public Func<LegacyScenario, LegacyObservation, LegacyObservation>? Corrected { get; }

    public bool IsImplemented => Corrected != null;

    public override string ToString() => Decision + ": " + Summary;

    /// <summary>The constructors that take colors assign each argument to the element it is named for.</summary>
    public static ApprovedCorrection ConstructorColors { get; } = new(
        "D-57",
        "In the constructors that take colors, each argument sets the color it is named for. In 1.0.0 TitleColor"
        + " sets the weekday row and, with four arguments, DayOfWeekColor sets the title.",
        () => MigrationMatrix.ConstructorColors);

    /// <summary>A null list is refused with ArgumentNullException for the parameter dates.</summary>
    public static ApprovedCorrection NullDateList { get; } = new(
        "D-58",
        "A null list throws ArgumentNullException naming the parameter dates, still before the month and the"
        + " year are checked. In 1.0.0 it throws NullReferenceException.",
        () => MigrationMatrix.NullDateList);

    /// <summary>The last row of days ends with the console's line terminator.</summary>
    public static ApprovedCorrection LineTerminator { get; } = new(
        "D-59",
        "The last row of days of a month ends with the console's line terminator. In 1.0.0 it ends with a"
        + " literal line feed.",
        () => MigrationMatrix.LineTerminator);

    /// <summary>Every month is followed by exactly one blank line.</summary>
    public static ApprovedCorrection OneBlankLine { get; } = new(
        "D-60",
        "Every month is followed by exactly one blank line. In 1.0.0 a month whose last day is a Sunday is"
        + " followed by two.",
        () => MigrationMatrix.BlankLineAfterSunday);

    /// <summary>The title names the Gregorian month that the grid shows.</summary>
    public static ApprovedCorrection GregorianTitle { get; } = new(
        "D-61",
        "The title names the Gregorian month the grid shows. In 1.0.0 it is the month of that number in the"
        + " current culture's own calendar, which is another month under a culture whose calendar is not Gregorian.",
        () => MigrationMatrix.CultureTitle);

    /// <summary>The five approved corrections, in the order they are applied.</summary>
    public static IReadOnlyList<ApprovedCorrection> All { get; } =
        new[] { ConstructorColors, NullDateList, LineTerminator, OneBlankLine, GregorianTitle };

    /// <summary>
    /// What the library is expected to do in a scenario: what 1.0.0 did, with every implemented correction
    /// applied in order. While no correction is implemented, that is exactly what 1.0.0 did.
    /// </summary>
    public static LegacyObservation ExpectedOfTheLibrary(LegacyScenario scenario, LegacyObservation published100) =>
        Expected(scenario, published100, All);

    /// <summary>The same for any list of corrections. Corrections that are not implemented change nothing.</summary>
    public static LegacyObservation Expected(LegacyScenario scenario, LegacyObservation published100, IEnumerable<ApprovedCorrection> corrections)
    {
        LegacyObservation expected = published100;
        foreach (ApprovedCorrection correction in corrections)
        {
            if (correction.Corrected != null)
            {
                expected = correction.Corrected(scenario, expected);
            }
        }
        return expected;
    }
}
