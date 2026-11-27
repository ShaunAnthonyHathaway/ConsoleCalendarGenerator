using ConsoleCalendarGenerator.Tests.Facade;
using ConsoleCalendarGenerator.Tests.Legacy;
using ConsoleCalendarGenerator.Tests.Migration;
using ConsoleCalendarGenerator.Tests.Support;

namespace ConsoleCalendarGenerator.Tests.Historical;

/// <summary>
/// The frozen source of 1.0.0 against the four values that were recorded from version 1.0.0 before this
/// reference existed: two from the assembly in the package on nuget.org
/// (<see cref="Published100BaselineTests"/>) and two from the class at commit 64995b6, before it was rebuilt as
/// a facade (<see cref="PreFacadeBaselineTests"/>). The values and the code that computes them are the ones
/// those tests use; only the implementation that is run differs.
///
/// <para>
/// This is what makes the frozen source a reference for 1.0.0 rather than a claim about it. These tests keep
/// holding when the library is corrected in 2.0, because the frozen source does not change.
/// </para>
/// </summary>
[TestFixture]
public class Published100DigestTests
{
    [Test]
    public void TextAndWriteSequence_OfTheFrozenSource_AreThoseOfThePublishedAssembly()
    {
        Assert.That(BehaviorDigest.TextAndWrites(LegacySubject.Published100), Is.EqualTo(Published100BaselineTests.PublishedTextAndWrites));
    }

    [Test]
    [Platform(Exclude = "Win", Reason = ConsoleState.ColorsObservableOnUnixOnly)]
    public void ColorSequence_OfTheFrozenSource_IsThatOfThePublishedAssembly()
    {
        Assert.That(BehaviorDigest.Colors(LegacySubject.Published100), Is.EqualTo(Published100BaselineTests.PublishedColors));
    }

    [Test]
    public void EveryCallOnTheConsolesWriter_ByTheFrozenSource_IsWhatWasRecordedBeforeTheFacade()
    {
        Assert.That(PreFacadeBaselineTests.Digest(colors: false, LegacySubject.Published100), Is.EqualTo(PreFacadeBaselineTests.CallsBeforeTheFacade));
    }

    [Test]
    [Platform(Exclude = "Win", Reason = ConsoleState.ColorsObservableOnUnixOnly)]
    public void TheColorOfEveryCall_ByTheFrozenSource_IsWhatWasRecordedBeforeTheFacade()
    {
        Assert.That(PreFacadeBaselineTests.Digest(colors: true, LegacySubject.Published100), Is.EqualTo(PreFacadeBaselineTests.ColorsBeforeTheFacade));
    }
}
