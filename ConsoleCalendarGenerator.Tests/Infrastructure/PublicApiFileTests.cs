using System.Text.RegularExpressions;
using CalendarGenerator;
using ConsoleCalendarGenerator.ReleaseTools;
using ConsoleCalendarGenerator.Tests.Support;

namespace ConsoleCalendarGenerator.Tests.Infrastructure;

/// <summary>
/// eng/public-api.txt states the public API of the library, one line for each type and member. The scripts of
/// a publication compare the assemblies of the package with it before anything is pushed, and the assemblies
/// nuget.org serves afterwards. These tests hold the file to the library they were built against, with the
/// same code the scripts use, so the file cannot fall behind the source: a change of the public API changes
/// this file in the same change, where it is seen.
///
/// <para>
/// <see cref="ModernApiTests"/> and Legacy/PublicApiTests state the API in their own words and are the
/// tests that say what it must be. This file is the form a script can compare a built assembly with.
/// </para>
/// </summary>
[TestFixture]
public class PublicApiFileTests
{
    private const string File = "eng/public-api.txt";

    private static string[] Stated() => Repository.Read(File).Split('\n', StringSplitOptions.RemoveEmptyEntries);

    [Test]
    public void TheFile_IsThePublicApiOfTheLibraryUnderTest()
    {
        IReadOnlyList<string> actual = PublicApiListing.Of(typeof(MonthLayout).Assembly);

        Assert.That(actual, Is.EqualTo(Stated()),
            "the public API changed, or eng/public-api.txt did. If the API was meant to change, write the file again with the tool in eng/tools/PublicApi.");
    }

    [Test]
    public void TheFile_ListsBothNamespaces_AndEveryTypeOnce()
    {
        string[] types = Stated().Where(line => !line.StartsWith(' ')).ToArray();

        Assert.That(types, Is.Unique);
        Assert.That(types.Select(line => Regex.Match(line, @"^public (static |sealed |abstract )?(class|enum|struct|interface|delegate) (?<name>\S+)").Groups["name"].Value),
            Is.Ordered.Using((IComparer<string>)StringComparer.Ordinal), "types in the ordinal order of their names, as the tool writes them");
        Assert.That(types, Has.All.StartsWith("public ").Using(StringComparison.Ordinal));
        Assert.That(types.Count(line => line.Contains(" ConsoleCalendarGenerator.", StringComparison.Ordinal)), Is.EqualTo(16), "the types of the modern namespace");
        Assert.That(types.Single(line => line.Contains(" CalendarGenerator.", StringComparison.Ordinal)),
            Is.EqualTo("public class " + typeof(ConsoleCalendar).FullName + " : System.Object"), "and the original class, which is not sealed");
        Assert.That(Repository.Read(File), Does.EndWith("\n").Using(StringComparison.Ordinal));
        Assert.That(Repository.Read(File), Does.Not.Match(@"[^\x20-\x7E\n]"), "plain text, the same on every system");
    }
}
