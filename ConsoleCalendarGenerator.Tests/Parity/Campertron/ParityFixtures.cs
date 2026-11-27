using System.Globalization;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ConsoleCalendarGenerator.Tests.Parity.Campertron;

/// <summary>Where the fixtures were recorded from. See README.md beside this file.</summary>
internal sealed record ManifestSource(string Repository, string Commit, string File, string Symbol, string Returns);

/// <summary>What the recorded renderer always does, whatever it is called with.</summary>
internal sealed record ManifestFixed(
    DayOfWeek FirstDayOfWeek, string WeekdayRow, int CellWidth, int ColumnGap, string Marker, string TitleFormat, string TitleSuffix, string TitleAlignment);

/// <summary>What the places that call the recorded renderer pass, and the width they write to.</summary>
internal sealed record ManifestCallSites(string File, string Indent, ConsoleColor MarkColor, int MaximumWidth);

internal sealed record Manifest(int SchemaVersion, ManifestSource Source, ManifestFixed FixedByTheSource, ManifestCallSites LiveCallSites, IReadOnlyList<string> Files);

internal sealed record FixtureFile(int SchemaVersion, string About, IReadOnlyList<FixtureCase> Cases);

/// <summary>One recorded call: its input and the lines it returned.</summary>
public sealed record FixtureCase(string Id, string Covers, string Title, FixtureInput Input, IReadOnlyList<FixtureLine> Lines)
{
    public const string ExactTitle = "exact";
    public const string IntentionallyDifferentTitle = "intentionally-different";

    /// <summary>The file the case is in, filled in when it is loaded.</summary>
    [JsonIgnore]
    public string File { get; init; } = string.Empty;

    public override string ToString() => File + "/" + Id;
}

public sealed record FixtureInput(int Year, int Month, string Culture, string Indent, ConsoleColor MarkColor, IReadOnlyList<FixtureDate> Dates)
{
    /// <summary>The dates exactly as they were passed: the recorded wall-clock value with the recorded kind.</summary>
    public IReadOnlyList<DateTime> DateTimes() => Dates.Select(date => DateTime.SpecifyKind(
        DateTime.ParseExact(date.Date, FixtureDate.Format, CultureInfo.InvariantCulture, DateTimeStyles.None), date.Kind)).ToList();
}

public sealed record FixtureDate(string Date, DateTimeKind Kind)
{
    public const string Format = "yyyy-MM-dd'T'HH:mm:ss.fffffff";
}

/// <summary>One recorded line: its text, and its spans as [text, color] pairs, color null where there is none.</summary>
public sealed record FixtureLine(string Text, IReadOnlyList<IReadOnlyList<string?>> Spans)
{
    /// <summary>The spans as (text, color).</summary>
    public IReadOnlyList<(string Text, ConsoleColor? Color)> ColoredSpans() => Spans
        .Select(span => (span[0]!, span[1] == null ? (ConsoleColor?)null : Enum.Parse<ConsoleColor>(span[1]!)))
        .ToList();
}

/// <summary>
/// Reads the fixtures, which are embedded in the test assembly so that the tests need no files beside it, no
/// Campertron checkout and no network. The reading is strict: a field the schema does not know is an error.
/// </summary>
internal static class ParityFixtures
{
    private const string ResourcePrefix = "ParityFixtures.Campertron.";

    private static readonly JsonSerializerOptions Strict = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        Converters = { new JsonStringEnumConverter(namingPolicy: null, allowIntegerValues: false) },
    };

    private static readonly Lazy<Manifest> LoadedManifest = new(() => Read<Manifest>("manifest.json"));

    private static readonly Lazy<IReadOnlyList<FixtureCase>> LoadedCases = new(() => Manifest.Files
        .SelectMany(file => Read<FixtureFile>(file).Cases.Select(fixture => fixture with { File = file }))
        .ToList());

    public static Manifest Manifest => LoadedManifest.Value;

    /// <summary>Every case of every file, in the order of the manifest and of the files.</summary>
    public static IReadOnlyList<FixtureCase> Cases => LoadedCases.Value;

    /// <summary>The names of the embedded fixture files.</summary>
    public static IReadOnlyList<string> EmbeddedFiles() => typeof(ParityFixtures).Assembly.GetManifestResourceNames()
        .Where(name => name.StartsWith(ResourcePrefix, StringComparison.Ordinal))
        .Select(name => name.Substring(ResourcePrefix.Length))
        .OrderBy(name => name, StringComparer.Ordinal)
        .ToList();

    /// <summary>The text of an embedded fixture file exactly as it is in the repository.</summary>
    public static string RawText(string file)
    {
        using Stream stream = typeof(ParityFixtures).Assembly.GetManifestResourceStream(ResourcePrefix + file)
            ?? throw new FileNotFoundException("The fixture file " + file + " is not embedded in the test assembly.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    public static FixtureFile ReadFile(string file) => Read<FixtureFile>(file);

    /// <summary>Test cases for NUnit, one per recorded case, named by file and id.</summary>
    public static IEnumerable<TestCaseData> AllCases() => Cases.Select(fixture => new TestCaseData(fixture).SetArgDisplayNames(fixture.ToString()));

    public static IEnumerable<TestCaseData> CasesWithAnExactTitle() => Cases.Where(fixture => fixture.Title == FixtureCase.ExactTitle)
        .Select(fixture => new TestCaseData(fixture).SetArgDisplayNames(fixture.ToString()));

    public static IEnumerable<TestCaseData> CasesWithAnIntentionallyDifferentTitle() => Cases.Where(fixture => fixture.Title == FixtureCase.IntentionallyDifferentTitle)
        .Select(fixture => new TestCaseData(fixture).SetArgDisplayNames(fixture.ToString()));

    private static T Read<T>(string file) =>
        JsonSerializer.Deserialize<T>(RawText(file), Strict) ?? throw new InvalidDataException(file + " is empty.");
}
