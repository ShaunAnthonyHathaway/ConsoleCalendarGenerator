using System.Reflection;

namespace ConsoleCalendarGenerator.Tests.States;

/// <summary>
/// A day state on its own: what a name and a marker may be, and that a state cannot be changed once it exists.
/// </summary>
[TestFixture]
public class DayStateTests
{
    // Text that cannot be typed safely into every editor is built from code points.
    private static readonly string Smiley = char.ConvertFromUtf32(0x1F600);
    private static readonly string EAcute = "e" + (char)0x0301;
    private static readonly string NoBreakSpace = ((char)0x00A0).ToString();

    [Test]
    public void AState_HasTheNameAndTheMarkerItWasCreatedWith()
    {
        var state = new DayState("selected", "*");

        Assert.That(state.Name, Is.EqualTo("selected"));
        Assert.That(state.Marker, Is.EqualTo("*"));
        Assert.That(state.ToString(), Is.EqualTo("selected"));
    }

    [TestCase("selected")]
    [TestCase("Selected")]
    [TestCase("fully booked")]
    [TestCase("state-2")]
    [TestCase("a")]
    [TestCase(" padded ")]
    [TestCase("42")]
    public void AnyNameWithAVisibleCharacterAndNoLineBreak_IsAccepted(string name)
    {
        Assert.That(new DayState(name, "*").Name, Is.EqualTo(name), "the name is kept exactly as given");
    }

    [Test]
    public void ANameInAnyScript_IsAccepted()
    {
        string name = Smiley + EAcute;

        Assert.That(new DayState(name, "*").Name, Is.EqualTo(name));
    }

    [TestCase("*")]
    [TestCase("#")]
    [TestCase(">>")]
    [TestCase("x!")]
    [TestCase("* ")]
    [TestCase(" *")]
    public void AnyMarkerWithAVisibleCharacterAndNoLineBreak_IsAccepted(string marker)
    {
        Assert.That(new DayState("selected", marker).Marker, Is.EqualTo(marker), "the marker is kept exactly as given");
    }

    [Test]
    public void AMarkerOutsideTheBasicPlane_OrWithACombiningMark_IsAccepted()
    {
        Assert.That(new DayState("happy", Smiley).Marker, Is.EqualTo(Smiley));
        Assert.That(new DayState("accented", EAcute).Marker, Is.EqualTo(EAcute));
    }

    // ---- names that are refused ----

    [Test]
    public void ANullName_IsRejected()
    {
        Assert.That(Assert.Throws<ArgumentNullException>(() => new DayState(null!, "*"))!.ParamName, Is.EqualTo("name"));
    }

    [TestCase("")]
    [TestCase(" ")]
    [TestCase("   ")]
    [TestCase("\t")]
    public void AnEmptyOrWhiteSpaceName_IsRejected(string name)
    {
        // Exactly ArgumentException: not ArgumentNullException, not ArgumentOutOfRangeException.
        Assert.That(Assert.Throws<ArgumentException>(() => new DayState(name, "*"))!.ParamName, Is.EqualTo("name"));
    }

    [Test]
    public void ANameOfOtherWhiteSpaceCharacters_IsRejected()
    {
        Assert.That(Assert.Throws<ArgumentException>(() => new DayState(NoBreakSpace + " ", "*"))!.ParamName, Is.EqualTo("name"));
    }

    /// <summary>
    /// Line feed, carriage return, form feed, next line (U+0085), line separator (U+2028) and paragraph
    /// separator (U+2029).
    /// </summary>
    [TestCase(0x000A)]
    [TestCase(0x000D)]
    [TestCase(0x000C)]
    [TestCase(0x0085)]
    [TestCase(0x2028)]
    [TestCase(0x2029)]
    public void ANameWithALineBreak_IsRejected(int codePoint)
    {
        Assert.That(Assert.Throws<ArgumentException>(() => new DayState("two" + (char)codePoint + "lines", "*"))!.ParamName, Is.EqualTo("name"));
    }

    // ---- markers that are refused ----

    [Test]
    public void ANullMarker_IsRejected()
    {
        Assert.That(Assert.Throws<ArgumentNullException>(() => new DayState("selected", null!))!.ParamName, Is.EqualTo("marker"));
    }

    /// <summary>A marker nobody can see would leave the state to color alone.</summary>
    [TestCase("")]
    [TestCase(" ")]
    [TestCase("  ")]
    [TestCase("\t")]
    public void AnEmptyOrInvisibleMarker_IsRejected(string marker)
    {
        Assert.That(Assert.Throws<ArgumentException>(() => new DayState("selected", marker))!.ParamName, Is.EqualTo("marker"));
    }

    [TestCase(0x000A)]
    [TestCase(0x000D)]
    [TestCase(0x000C)]
    [TestCase(0x0085)]
    [TestCase(0x2028)]
    [TestCase(0x2029)]
    public void AMarkerWithALineBreak_IsRejected(int codePoint)
    {
        Assert.That(Assert.Throws<ArgumentException>(() => new DayState("selected", "*" + (char)codePoint))!.ParamName, Is.EqualTo("marker"));
    }

    [Test]
    public void TheName_IsCheckedBeforeTheMarker()
    {
        Assert.That(Assert.Throws<ArgumentNullException>(() => new DayState(null!, null!))!.ParamName, Is.EqualTo("name"));
        Assert.That(Assert.Throws<ArgumentException>(() => new DayState(" ", string.Empty))!.ParamName, Is.EqualTo("name"));
    }

    /// <summary>A long marker is a valid state. Whether it fits a cell is decided when a month is rendered.</summary>
    [Test]
    public void AMarkerOfAnyLength_IsAccepted()
    {
        Assert.That(new DayState("long", new string('#', 50)).Marker, Has.Length.EqualTo(50));
    }

    // ---- identity ----

    /// <summary>
    /// A state is identified by its name. Two instances with the same name are the same kind of state to a
    /// theme, but they are still two objects: equality is not overridden.
    /// </summary>
    [Test]
    public void TwoInstancesWithTheSameNameAndMarker_AreDifferentObjects()
    {
        var first = new DayState("selected", "*");
        var second = new DayState("selected", "*");

        Assert.That(first.Equals(second), Is.False);
        Assert.That(first.Equals(first), Is.True);
        Assert.That(first.Name, Is.EqualTo(second.Name));
    }

    [Test]
    public void AState_IsSealed_WithReadOnlyPropertiesAndNoFields()
    {
        Type type = typeof(DayState);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(type.IsSealed, Is.True);
            Assert.That(type.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static), Is.Empty);
            Assert.That(type.GetProperties().Where(property => property.CanWrite), Is.Empty);
            Assert.That(type.GetProperties().Select(property => property.Name), Is.EquivalentTo(new[] { "Name", "Marker" }));
        }
    }
}
