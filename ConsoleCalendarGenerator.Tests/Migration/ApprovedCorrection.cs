namespace ConsoleCalendarGenerator.Tests.Migration;

/// <summary>
/// One of the behaviors of the legacy calendar that version 2.0 corrects on purpose. There are exactly five,
/// each with a decision in docs/ai-context/decisions.md, and nothing else about the legacy calendar may change.
///
/// <para>
/// A correction starts here as approved and not implemented: it has neither of the two functions below, and
/// the migration tests require the library to do exactly what 1.0.0 did. The pull request that implements a
/// correction in the library gives it one of them in the same change. From then on the tests require exactly
/// what that function says, in every aspect and in every scenario, so one correction can be made without
/// loosening anything about the others or about the behavior that stays.
/// </para>
/// <para>
/// There are two ways to say what a correction changes, and a correction uses the one that states it exactly:
/// </para>
/// <list type="bullet">
/// <item>
/// <see cref="EquivalentIn100"/>: the use of 1.0.0 that does what the corrected library does in a scenario. It
/// fits a correction that makes one thing behave like another thing 1.0.0 already had.
/// </item>
/// <item>
/// <see cref="Corrected"/>: what the corrected library does, given what 1.0.0 did. It fits a correction whose
/// result 1.0.0 could not produce at all.
/// </item>
/// </list>
/// </summary>
internal sealed class ApprovedCorrection
{
    public ApprovedCorrection(
        string decision,
        string summary,
        Func<IReadOnlyList<LegacyScenario>> scenarios,
        Func<LegacyScenario, LegacyScenario>? equivalentIn100 = null,
        Func<LegacyScenario, LegacyObservation, LegacyObservation>? corrected = null)
    {
        Decision = decision;
        Summary = summary;
        _scenarios = scenarios;
        EquivalentIn100 = equivalentIn100;
        Corrected = corrected;
    }

    private readonly Func<IReadOnlyList<LegacyScenario>> _scenarios;

    /// <summary>The identifier of the decision in docs/ai-context/decisions.md, such as <c>D-57</c>.</summary>
    public string Decision { get; }

    /// <summary>What 1.0.0 does and what 2.0 does instead, in a sentence.</summary>
    public string Summary { get; }

    /// <summary>The scenarios of <see cref="MigrationMatrix"/> in which 1.0.0 shows the behavior being corrected.</summary>
    public IReadOnlyList<LegacyScenario> Scenarios => _scenarios();

    /// <summary>
    /// Null unless the correction is stated this way. Given a scenario, the scenario in which 1.0.0 does what
    /// the corrected library does in the given one. It returns its argument for a scenario the correction does
    /// not touch.
    /// </summary>
    public Func<LegacyScenario, LegacyScenario>? EquivalentIn100 { get; }

    /// <summary>
    /// Null unless the correction is stated this way. What the corrected library does in a scenario, given what
    /// was expected of it before this correction. It returns its argument unchanged for a scenario the
    /// correction does not touch.
    /// </summary>
    public Func<LegacyScenario, LegacyObservation, LegacyObservation>? Corrected { get; }

    public bool IsImplemented => EquivalentIn100 != null || Corrected != null;

    public override string ToString() => Decision + ": " + Summary;

    /// <summary>
    /// The constructors that take colors assign each argument to the element it is named for. Implemented: a
    /// constructor of the corrected library does what 1.0.0 did with its parameterless constructor followed by
    /// the setter for each argument (<see cref="ConstructorColorRoles.In20"/>). The setters of 1.0.0 were
    /// right, so that is a statement in terms of 1.0.0, and it covers everything that follows from the colors
    /// held: what is drawn in which color, and where a color that is not defined makes the drawing fail.
    /// </summary>
    public static ApprovedCorrection ConstructorColors { get; } = new(
        "D-57",
        "In the constructors that take colors, each argument sets the color it is named for. In 1.0.0 TitleColor"
        + " sets the weekday row and, with four arguments, DayOfWeekColor sets the title.",
        () => MigrationMatrix.ConstructorColors,
        equivalentIn100: scenario => !scenario.UsesAConstructorThatTakesColors
            ? scenario
            : scenario.WithConstructorAndSetters(
                Array.Empty<ConsoleColor>(),
                ConstructorColorRoles.In20(scenario.ConstructorArguments).Concat(scenario.Setters).ToArray()));

    /// <summary>
    /// A null list is refused with ArgumentNullException for the parameter dates. Implemented: where 1.0.0
    /// failed with <see cref="NullReferenceException"/> because it was given a null list, the corrected library
    /// fails at the same point, with the same nothing written and the same colors left, and only the exception
    /// is another: its type, the parameter it names, and the message the runtime gives an exception of that type
    /// for that parameter. No use of 1.0.0 throws that exception, so this is stated as a change of the
    /// observation. Every scenario that passes a list is returned unchanged.
    /// </summary>
    public static ApprovedCorrection NullDateList { get; } = new(
        "D-58",
        "A null list throws ArgumentNullException naming the parameter dates, still before the month and the"
        + " year are checked. In 1.0.0 it throws NullReferenceException.",
        () => MigrationMatrix.NullDateList,
        corrected: (scenario, expected) =>
            !scenario.PassesANullList || expected.ExceptionType != typeof(NullReferenceException).FullName
                ? expected
                : new LegacyObservation(
                    expected.StoredColors,
                    expected.Calls,
                    expected.NewLine,
                    expected.ForegroundAfter,
                    expected.BackgroundAfter,
                    exceptionType: typeof(ArgumentNullException).FullName,
                    exceptionParameterName: NullDateListParameter,
                    exceptionMessage: MessageOfArgumentNull(scenario)));

    /// <summary>The name of the parameter that the corrected library names when the list is null.</summary>
    public const string NullDateListParameter = "dates";

    /// <summary>
    /// The message the runtime gives <see cref="ArgumentNullException"/> for that parameter under the UI culture
    /// of a scenario. The library passes no message of its own, so this is the whole of it.
    /// </summary>
    private static string MessageOfArgumentNull(LegacyScenario scenario) => Support.ProcessState.UnderCultures(
        System.Globalization.CultureInfo.InvariantCulture,
        scenario.UICulture ?? System.Globalization.CultureInfo.InvariantCulture,
        () => new ArgumentNullException(NullDateListParameter).Message);

    /// <summary>The last row of days ends with the console's line terminator. Not implemented.</summary>
    public static ApprovedCorrection LineTerminator { get; } = new(
        "D-59",
        "The last row of days of a month ends with the console's line terminator. In 1.0.0 it ends with a"
        + " literal line feed.",
        () => MigrationMatrix.LineTerminator);

    /// <summary>Every month is followed by exactly one blank line. Not implemented.</summary>
    public static ApprovedCorrection OneBlankLine { get; } = new(
        "D-60",
        "Every month is followed by exactly one blank line. In 1.0.0 a month whose last day is a Sunday is"
        + " followed by two.",
        () => MigrationMatrix.BlankLineAfterSunday);

    /// <summary>The title names the Gregorian month that the grid shows. Not implemented.</summary>
    public static ApprovedCorrection GregorianTitle { get; } = new(
        "D-61",
        "The title names the Gregorian month the grid shows. In 1.0.0 it is the month of that number in the"
        + " current culture's own calendar, which is another month under a culture whose calendar is not Gregorian.",
        () => MigrationMatrix.CultureTitle);

    /// <summary>The five approved corrections, in the order they are applied.</summary>
    public static IReadOnlyList<ApprovedCorrection> All { get; } =
        new[] { ConstructorColors, NullDateList, LineTerminator, OneBlankLine, GregorianTitle };

    /// <summary>
    /// The scenario to run 1.0.0 in, to see what the library is expected to do in the given one: the scenario
    /// itself, restated by every correction that is implemented and stated as an equivalent use of 1.0.0.
    /// </summary>
    public static LegacyScenario ScenarioFor100(LegacyScenario scenario) => ScenarioFor100(scenario, All);

    /// <summary>The same for any list of corrections.</summary>
    public static LegacyScenario ScenarioFor100(LegacyScenario scenario, IEnumerable<ApprovedCorrection> corrections)
    {
        LegacyScenario equivalent = scenario;
        foreach (ApprovedCorrection correction in corrections)
        {
            if (correction.EquivalentIn100 != null)
            {
                equivalent = correction.EquivalentIn100(equivalent);
            }
        }
        return equivalent;
    }

    /// <summary>
    /// What the library is expected to do in a scenario, given what 1.0.0 did in
    /// <see cref="ScenarioFor100(LegacyScenario)"/>: that, with every implemented correction that is stated as
    /// a change of the observation applied in order. While no correction is implemented, both steps change
    /// nothing and the expectation is exactly what 1.0.0 did.
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
