using System.Globalization;
using ConsoleCalendarGenerator.Tests.Support;

namespace ConsoleCalendarGenerator.Tests.Migration;

/// <summary>
/// The tools the migration tests are built on, tested on their own with code written here: that an observation
/// records what was done, that a comparison finds each kind of difference and names only the aspects that
/// differ, and that a correction changes the expectation only once it is implemented. Nothing in this file
/// involves the legacy calendar's behavior, present or future.
/// </summary>
[TestFixture]
public class LegacyObservationTests
{
    private static LegacyAspects Compare(Action expected, Action actual, string newLine = "\n") =>
        LegacyObservation.Capture(actual, newLine).AspectsThatDifferFrom(LegacyObservation.Capture(expected, newLine));

    // ---- what an observation records ----

    [Test]
    public void AnObservation_RecordsEveryCallWithItsMethod_AndTheTextWithTheWritersTerminator()
    {
        LegacyObservation observation = LegacyObservation.Capture(
            () =>
            {
                Console.WriteLine("title");
                Console.Write(" 9");
                Console.Write(10);
                Console.Write("\n");
                Console.WriteLine();
            },
            newLine: "\r\n");

        Assert.That(observation.Calls.Select(call => call.Method + "[" + call.Text + "]"),
            Is.EqualTo(new[] { "LS[title]", "S[ 9]", "I[10]", "S[\n]", "L[]" }));
        Assert.That(observation.Text, Is.EqualTo("title\r\n 910\n\r\n"));
        Assert.That(observation.ExceptionType, Is.Null);
        Assert.That(observation.StoredColors, Is.Null);
    }

    [Test]
    public void AnObservation_RecordsTheException_AndWhatWasWrittenBeforeIt()
    {
        LegacyObservation observation = LegacyObservation.Capture(() =>
        {
            Console.Write("before");
            throw new ArgumentNullException("dates");
        });

        Assert.That(observation.Text, Is.EqualTo("before"));
        Assert.That(observation.ExceptionType, Is.EqualTo("System.ArgumentNullException"));
        Assert.That(observation.ExceptionParameterName, Is.EqualTo("dates"));
        Assert.That(observation.ExceptionMessage, Ordinal.Contains("dates"));
    }

    [Test]
    [Platform(Exclude = "Win", Reason = ConsoleState.ColorsObservableOnUnixOnly)]
    public void AnObservation_RecordsTheColorsOfEveryCall_AndTheColorsLeftBehind()
    {
        LegacyObservation observation = LegacyObservation.Capture(
            () =>
            {
                Console.Write("a");
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write("b");
                Console.ResetColor();
            },
            foregroundBefore: ConsoleColor.DarkCyan,
            backgroundBefore: ConsoleColor.DarkBlue);

        Assert.That(observation.Calls.Select(call => (call.Foreground, call.Background)), Is.EqualTo(new[]
        {
            (ConsoleColor.DarkCyan, ConsoleColor.DarkBlue),
            (ConsoleColor.Red, ConsoleColor.DarkBlue),
        }));
        Assert.That((observation.ForegroundAfter, observation.BackgroundAfter), Is.EqualTo((ConsoleState.Unset, ConsoleState.Unset)));
    }

    [Test]
    public void AnObservation_RunsUnderTheCulturesGiven_AndPutsEverythingBack()
    {
        object[] before = ProcessState.Snapshot();
        CultureInfo culture = TestCultures.WithMonthNames(_ => "name");
        CultureInfo uiCulture = TestCultures.WithForeignWeek();
        CultureInfo? seen = null;
        CultureInfo? seenUi = null;

        LegacyObservation.Capture(
            () =>
            {
                seen = CultureInfo.CurrentCulture;
                seenUi = CultureInfo.CurrentUICulture;
                throw new InvalidOperationException();
            },
            culture: culture,
            uiCulture: uiCulture,
            foregroundBefore: ConsoleColor.Red);

        Assert.That(seen, Is.SameAs(culture));
        Assert.That(seenUi, Is.SameAs(uiCulture));
        Assert.That(ProcessState.Snapshot(), Is.EqualTo(before));
    }

    // ---- what a comparison finds ----

    [Test]
    public void TheSameCalls_DifferInNothing()
    {
        Assert.That(Compare(() => Console.WriteLine("a"), () => Console.WriteLine("a")), Is.EqualTo(LegacyAspects.None));
    }

    [Test]
    public void DifferentText_IsADifferenceInTextAndInCalls()
    {
        Assert.That(Compare(() => Console.Write("a"), () => Console.Write("b")), Is.EqualTo(LegacyAspects.Text | LegacyAspects.Calls));
    }

    /// <summary>A literal line feed and the writer's own terminator are the same text only where that terminator is a line feed.</summary>
    [Test]
    public void ALineEndWrittenAnotherWay_IsADifferenceInCalls_AndInTextOnlyWhereTheTerminatorIsNotALineFeed()
    {
        Action literal = () => Console.Write("\n");
        Action terminator = () => Console.WriteLine();

        Assert.That(Compare(literal, terminator, newLine: "\n"), Is.EqualTo(LegacyAspects.Calls));
        Assert.That(Compare(literal, terminator, newLine: "\r\n"), Is.EqualTo(LegacyAspects.Calls | LegacyAspects.Text));
    }

    [Test]
    public void TheSameTextThroughAnotherMethodOrInOtherPieces_IsADifferenceInCallsOnly()
    {
        Assert.That(Compare(() => Console.Write(10), () => Console.Write("10")), Is.EqualTo(LegacyAspects.Calls));
        Assert.That(Compare(() => Console.WriteLine("ab"), () => { Console.Write("ab"); Console.WriteLine(); }),
            Is.EqualTo(LegacyAspects.Calls | LegacyAspects.Colors), "one call more is also one color more");
    }

    [Test]
    public void ACallMoreOrLess_IsFound()
    {
        Action one = () => Console.WriteLine();
        Action two = () => { Console.WriteLine(); Console.WriteLine(); };

        Assert.That(Compare(one, two), Is.EqualTo(LegacyAspects.Text | LegacyAspects.Calls | LegacyAspects.Colors));
        Assert.That(LegacyObservation.Capture(two).DifferencesFrom(LegacyObservation.Capture(one)).Select(difference => difference.Description),
            Has.Some.Contains("1 calls expected, but there were 2"));
    }

    [Test]
    [Platform(Exclude = "Win", Reason = ConsoleState.ColorsObservableOnUnixOnly)]
    public void AnotherColorForACall_OrAnotherColorLeftBehind_IsADifferenceInColorsOnly()
    {
        Action yellow = () => { Console.ForegroundColor = ConsoleColor.Yellow; Console.Write("a"); Console.ResetColor(); };
        Action red = () => { Console.ForegroundColor = ConsoleColor.Red; Console.Write("a"); Console.ResetColor(); };
        Action notReset = () => { Console.ForegroundColor = ConsoleColor.Yellow; Console.Write("a"); };

        Assert.That(Compare(yellow, red), Is.EqualTo(LegacyAspects.Colors));
        Assert.That(Compare(yellow, notReset), Is.EqualTo(LegacyAspects.Colors));
    }

    [Test]
    public void AnotherExceptionType_IsFound_WithTheParameterNameAndTheMessageThatComeWithIt()
    {
        Assert.That(Compare(() => throw new NullReferenceException(), () => throw new ArgumentNullException("dates")),
            Is.EqualTo(LegacyAspects.ExceptionType | LegacyAspects.ExceptionParameterName | LegacyAspects.ExceptionMessage));
        Assert.That(Compare(() => { }, () => throw new InvalidOperationException("message")),
            Is.EqualTo(LegacyAspects.ExceptionType | LegacyAspects.ExceptionMessage));
    }

    [Test]
    public void AnotherParameterName_OrAnotherMessage_IsFoundOnItsOwn()
    {
        Assert.That(Compare(() => throw new ArgumentOutOfRangeException("month", "text"), () => throw new ArgumentOutOfRangeException("year", "text")),
            Is.EqualTo(LegacyAspects.ExceptionParameterName | LegacyAspects.ExceptionMessage), "the runtime puts the parameter name in the message");
        Assert.That(Compare(() => throw new InvalidOperationException("one"), () => throw new InvalidOperationException("two")),
            Is.EqualTo(LegacyAspects.ExceptionMessage));
    }

    [Test]
    public void OtherStoredColors_AreADifferenceInStoredColorsOnly()
    {
        LegacyObservation yellowTitle = LegacyObservation.Capture(
            () => { }, storedColors: () => new LegacyStoredColors(ConsoleColor.Yellow, ConsoleColor.Cyan, ConsoleColor.White, ConsoleColor.Blue));
        LegacyObservation redTitle = LegacyObservation.Capture(
            () => { }, storedColors: () => new LegacyStoredColors(ConsoleColor.Red, ConsoleColor.Cyan, ConsoleColor.White, ConsoleColor.Blue));

        Assert.That(redTitle.AspectsThatDifferFrom(yellowTitle), Is.EqualTo(LegacyAspects.StoredColors));
        Assert.That(redTitle.DifferencesFrom(yellowTitle).Single().Description, Is.EqualTo(
            "expected title Yellow, weekday row Cyan, dates White, selected dates Blue, but was title Red, weekday row Cyan, dates White, selected dates Blue"));
    }

    [Test]
    public void AComparison_LooksOnlyAtTheAspectsAskedFor()
    {
        LegacyObservation expected = LegacyObservation.Capture(() => { Console.Write("a"); throw new NullReferenceException(); });
        LegacyObservation actual = LegacyObservation.Capture(() => { Console.Write("b"); throw new ArgumentNullException("dates"); });

        Assert.That(actual.AspectsThatDifferFrom(expected, LegacyAspects.Text), Is.EqualTo(LegacyAspects.Text));
        Assert.That(actual.AspectsThatDifferFrom(expected, LegacyAspects.ExceptionType | LegacyAspects.StoredColors), Is.EqualTo(LegacyAspects.ExceptionType));
        Assert.That(actual.AspectsThatDifferFrom(expected, LegacyAspects.StoredColors), Is.EqualTo(LegacyAspects.None));
    }

    [Test]
    public void ADifference_SaysWhereTheFirstOneIs()
    {
        LegacyObservation expected = LegacyObservation.Capture(() => { Console.WriteLine("same"); Console.Write(" 1"); });
        LegacyObservation actual = LegacyObservation.Capture(() => { Console.WriteLine("same"); Console.Write(" 2"); });

        Assert.That(actual.DifferencesFrom(expected).Select(difference => difference.ToString()), Is.EqualTo(new[]
        {
            "Text: line 2: expected \" 1\", but was \" 2\"",
            "Calls: call 2: expected S[ 1], but was S[ 2]",
        }));
    }

    // ---- the two implementations, driven the same way ----

    [Test]
    public void BothSubjects_CreateTheirOwnClass_ThroughTheConstructorWithThatManyColors()
    {
        var scenario = new LegacyScenario("setters")
        {
            Setters = new[]
            {
                (LegacyColorRole.Title, ConsoleColor.DarkGreen),
                (LegacyColorRole.DayOfWeek, ConsoleColor.DarkRed),
                (LegacyColorRole.Date, ConsoleColor.Gray),
                (LegacyColorRole.SelectedDate, ConsoleColor.DarkMagenta),
            },
        };

        foreach (LegacySubject subject in new[] { LegacySubject.Library, LegacySubject.Published100 })
        {
            LegacyObservation observation = LegacyObservation.Of(subject, scenario, CultureInfo.InvariantCulture);

            Assert.That(observation.StoredColors, Is.EqualTo(
                new LegacyStoredColors(ConsoleColor.DarkGreen, ConsoleColor.DarkRed, ConsoleColor.Gray, ConsoleColor.DarkMagenta)), subject.Name);
            Assert.That(observation.Calls, Is.Empty, subject.Name);
            Assert.That(() => subject.Create(new ConsoleColor[5]), Throws.ArgumentException, subject.Name);
        }
    }

    // ---- corrections ----

    [Test]
    public void ACorrection_ChangesWhatIsExpected_OnlyOnceItIsImplemented()
    {
        var scenario = new LegacyScenario("any");
        LegacyObservation published100 = LegacyObservation.Capture(() => Console.Write("as in 1.0.0"));
        LegacyObservation corrected = LegacyObservation.Capture(() => Console.Write("corrected"));
        var pending = new ApprovedCorrection("D-00", "not implemented", () => Array.Empty<LegacyScenario>());
        var implemented = new ApprovedCorrection("D-00", "implemented", () => Array.Empty<LegacyScenario>(), corrected: (_, _) => corrected);

        Assert.That(pending.IsImplemented, Is.False);
        Assert.That(implemented.IsImplemented, Is.True);
        Assert.That(ApprovedCorrection.Expected(scenario, published100, new[] { pending }), Is.SameAs(published100));
        Assert.That(ApprovedCorrection.Expected(scenario, published100, new[] { pending, implemented }), Is.SameAs(corrected));
    }

    [Test]
    public void ImplementedCorrections_AreAppliedInOrder_EachToTheResultOfTheOneBefore()
    {
        var scenario = new LegacyScenario("any");
        LegacyObservation Append(LegacyObservation observation, string text) =>
            LegacyObservation.Capture(() => Console.Write(observation.Text + text));
        var first = new ApprovedCorrection("D-00", "first", () => Array.Empty<LegacyScenario>(), corrected: (_, observation) => Append(observation, "1"));
        var second = new ApprovedCorrection("D-00", "second", () => Array.Empty<LegacyScenario>(), corrected: (_, observation) => Append(observation, "2"));

        LegacyObservation expected = ApprovedCorrection.Expected(scenario, LegacyObservation.Capture(() => Console.Write("0")), new[] { first, second });

        Assert.That(expected.Text, Is.EqualTo("012"));
    }

    /// <summary>The other way to state a correction: as the use of 1.0.0 that does the same.</summary>
    [Test]
    public void ACorrectionStatedAsAnEquivalentUseOf100_ChangesTheScenarioThat100IsRunIn_AndNotTheObservation()
    {
        var scenario = new LegacyScenario("any") { ConstructorArguments = new[] { ConsoleColor.Red } };
        LegacyScenario restated = scenario.WithConstructorAndSetters(Array.Empty<ConsoleColor>(), new[] { (LegacyColorRole.Title, ConsoleColor.Red) });
        LegacyObservation published100 = LegacyObservation.Capture(() => Console.Write("as in 1.0.0"));
        var pending = new ApprovedCorrection("D-00", "not implemented", () => Array.Empty<LegacyScenario>());
        var implemented = new ApprovedCorrection("D-00", "implemented", () => Array.Empty<LegacyScenario>(), equivalentIn100: _ => restated);

        Assert.That(implemented.IsImplemented, Is.True);
        Assert.That(ApprovedCorrection.ScenarioFor100(scenario, new[] { pending }), Is.SameAs(scenario));
        Assert.That(ApprovedCorrection.ScenarioFor100(scenario, new[] { pending, implemented }), Is.SameAs(restated));
        Assert.That(ApprovedCorrection.Expected(scenario, published100, new[] { pending, implemented }), Is.SameAs(published100));
    }

    [Test]
    public void AScenarioWithAnotherConstructorAndOtherSetters_IsOtherwiseTheSameScenario()
    {
        var scenario = new LegacyScenario("name")
        {
            ConstructorArguments = new[] { ConsoleColor.Red, ConsoleColor.Green },
            Setters = new[] { (LegacyColorRole.Date, ConsoleColor.Gray) },
            Draw = LegacyDraw.MonthWithDates,
            Month = 2,
            Year = 2023,
            Dates = null,
            NewLine = "\r\n",
            Culture = TestCultures.WithForeignWeek(),
            UICulture = TestCultures.WithMonthNames(_ => "ui"),
            ForegroundBefore = ConsoleColor.DarkCyan,
            BackgroundBefore = ConsoleColor.DarkBlue,
        }.In("area");

        LegacyScenario other = scenario.WithConstructorAndSetters(Array.Empty<ConsoleColor>(), new[] { (LegacyColorRole.Title, ConsoleColor.Red) });

        using (Assert.EnterMultipleScope())
        {
            Assert.That(other.ConstructorArguments, Is.Empty);
            Assert.That(other.Setters, Is.EqualTo(new[] { (LegacyColorRole.Title, ConsoleColor.Red) }));
            Assert.That(other.ToString(), Is.EqualTo(scenario.ToString()));
            Assert.That((other.Draw, other.Month, other.Year, other.Dates, other.NewLine), Is.EqualTo((scenario.Draw, scenario.Month, scenario.Year, scenario.Dates, scenario.NewLine)));
            Assert.That(other.Culture, Is.SameAs(scenario.Culture));
            Assert.That(other.UICulture, Is.SameAs(scenario.UICulture));
            Assert.That(other.CultureName, Is.EqualTo(scenario.CultureName));
            Assert.That((other.ForegroundBefore, other.BackgroundBefore), Is.EqualTo((scenario.ForegroundBefore, scenario.BackgroundBefore)));
            Assert.That(other.PassesANullList, Is.True);
        }
    }
}
