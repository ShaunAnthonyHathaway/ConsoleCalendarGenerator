using System.Globalization;
using ConsoleCalendarGenerator.Tests.Support;

namespace ConsoleCalendarGenerator.Tests.Migration;

/// <summary>
/// The things about a use of the legacy calendar that a caller can observe, each of which two observations can
/// be compared in separately.
/// </summary>
[Flags]
internal enum LegacyAspects
{
    None = 0,

    /// <summary>What the four getters return after the constructor and the setters.</summary>
    StoredColors = 1,

    /// <summary>The complete text, with the writer's line terminator where the writer added one.</summary>
    Text = 2,

    /// <summary>
    /// How the text was divided into calls on the console's writer: which method each call used and with which
    /// text. A caller that replaces <see cref="Console.Out"/> with a writer of its own sees exactly this.
    /// </summary>
    Calls = 4,

    /// <summary>
    /// The foreground and background color in effect for every call, in order, and the colors left behind.
    /// On Windows a test host has no console to keep colors in, so every color reads the same there and this
    /// aspect can only show a difference on Linux and macOS.
    /// </summary>
    Colors = 8,

    /// <summary>The type of the exception that ended the use, or that there was none.</summary>
    ExceptionType = 16,

    /// <summary>The parameter the exception names, for an <see cref="ArgumentException"/>.</summary>
    ExceptionParameterName = 32,

    /// <summary>
    /// The message of the exception. It is the runtime's text, so it is only stable between two observations
    /// made in the same process under the same UI culture, which is how the migration tests make them.
    /// </summary>
    ExceptionMessage = 64,

    All = StoredColors | Text | Calls | Colors | ExceptionType | ExceptionParameterName | ExceptionMessage,
}

/// <summary>The colors a legacy calendar holds, as its four getters return them.</summary>
internal readonly record struct LegacyStoredColors(ConsoleColor Title, ConsoleColor DayOfWeek, ConsoleColor Date, ConsoleColor SelectedDate)
{
    public override string ToString() =>
        "title " + Title + ", weekday row " + DayOfWeek + ", dates " + Date + ", selected dates " + SelectedDate;
}

/// <summary>One aspect in which two observations differ, with the first place where they do.</summary>
internal readonly record struct LegacyDifference(LegacyAspects Aspect, string Description)
{
    public override string ToString() => Aspect + ": " + Description;
}

/// <summary>
/// Everything a caller can observe about one use of the legacy calendar: the colors it holds, every call it
/// made on the console's writer with the colors in effect, the colors it left behind, and the exception that
/// ended it. Two observations are compared aspect by aspect with <see cref="DifferencesFrom"/>.
/// </summary>
internal sealed class LegacyObservation
{
    public LegacyObservation(
        LegacyStoredColors? storedColors,
        IReadOnlyList<RecordedCall> calls,
        string newLine,
        ConsoleColor foregroundAfter,
        ConsoleColor backgroundAfter,
        string? exceptionType,
        string? exceptionParameterName,
        string? exceptionMessage)
    {
        StoredColors = storedColors;
        Calls = calls;
        NewLine = newLine;
        ForegroundAfter = foregroundAfter;
        BackgroundAfter = backgroundAfter;
        ExceptionType = exceptionType;
        ExceptionParameterName = exceptionParameterName;
        ExceptionMessage = exceptionMessage;
    }

    /// <summary>What the getters returned before anything was drawn. Null when no calendar was involved.</summary>
    public LegacyStoredColors? StoredColors { get; }

    /// <summary>Every call on the writer, in order, with the colors in effect for it.</summary>
    public IReadOnlyList<RecordedCall> Calls { get; }

    /// <summary>The line terminator of the writer the calls were made on.</summary>
    public string NewLine { get; }

    public ConsoleColor ForegroundAfter { get; }

    public ConsoleColor BackgroundAfter { get; }

    /// <summary>The full name of the type of the exception that ended the use, or null when it completed.</summary>
    public string? ExceptionType { get; }

    public string? ExceptionParameterName { get; }

    public string? ExceptionMessage { get; }

    /// <summary>The complete text, as a redirected console with this line terminator would have received it.</summary>
    public string Text => string.Concat(Calls.Select(call => call.EndsLine ? call.Text + NewLine : call.Text));

    /// <summary>Runs a scenario against one implementation of the legacy calendar and observes it.</summary>
    /// <param name="culture">The current culture, from <see cref="LegacyScenario.ResolveCulture"/>.</param>
    public static LegacyObservation Of(LegacySubject subject, LegacyScenario scenario, CultureInfo culture)
    {
        LegacyStoredColors? stored = null;
        return Capture(
            () =>
            {
                ILegacyCalendar calendar = scenario.CreateCalendar(subject);
                stored = new LegacyStoredColors(
                    calendar.GetTitleColor(), calendar.GetDayOfWeekColor(), calendar.GetDateColor(), calendar.GetSelectedDateColor());
                scenario.DrawWith(calendar);
            },
            scenario.NewLine,
            culture,
            scenario.UICulture,
            scenario.ForegroundBefore,
            scenario.BackgroundBefore,
            () => stored);
    }

    /// <summary>
    /// Observes any code that writes to the console, with <see cref="Console.Out"/> replaced and a known
    /// culture, and puts the process-wide state back: the writer, both cultures and the console colors.
    /// </summary>
    public static LegacyObservation Capture(
        Action action,
        string newLine = "\n",
        CultureInfo? culture = null,
        CultureInfo? uiCulture = null,
        ConsoleColor? foregroundBefore = null,
        ConsoleColor? backgroundBefore = null,
        Func<LegacyStoredColors?>? storedColors = null)
    {
        TextWriter originalOut = Console.Out;
        CultureInfo originalCulture = CultureInfo.CurrentCulture;
        CultureInfo originalUiCulture = CultureInfo.CurrentUICulture;
        var writer = new CallRecordingWriter(newLine);
        Exception? thrown = null;
        ConsoleColor foregroundAfter;
        ConsoleColor backgroundAfter;
        try
        {
            CultureInfo.CurrentCulture = culture ?? CultureInfo.InvariantCulture;
            CultureInfo.CurrentUICulture = uiCulture ?? CultureInfo.InvariantCulture;
            if (foregroundBefore.HasValue)
            {
                Console.ForegroundColor = foregroundBefore.Value;
            }
            if (backgroundBefore.HasValue)
            {
                Console.BackgroundColor = backgroundBefore.Value;
            }
            Console.SetOut(writer);
            try
            {
                action();
            }
            catch (Exception exception)
            {
                thrown = exception;
            }
            foregroundAfter = Console.ForegroundColor;
            backgroundAfter = Console.BackgroundColor;
        }
        finally
        {
            Console.SetOut(originalOut);
            CultureInfo.CurrentCulture = originalCulture;
            CultureInfo.CurrentUICulture = originalUiCulture;
            Console.ResetColor();
        }
        return new LegacyObservation(
            storedColors?.Invoke(),
            writer.Recorded.ToArray(),
            newLine,
            foregroundAfter,
            backgroundAfter,
            thrown?.GetType().FullName,
            (thrown as ArgumentException)?.ParamName,
            thrown?.Message);
    }

    /// <summary>
    /// The aspects, among those asked for, in which this observation differs from the expected one, each with
    /// the first place where it does. Empty when the two are the same in every aspect asked for.
    /// </summary>
    public IReadOnlyList<LegacyDifference> DifferencesFrom(LegacyObservation expected, LegacyAspects aspects = LegacyAspects.All)
    {
        var differences = new List<LegacyDifference>();

        if (aspects.HasFlag(LegacyAspects.StoredColors) && StoredColors != expected.StoredColors)
        {
            differences.Add(new LegacyDifference(LegacyAspects.StoredColors, Expected(Show(expected.StoredColors), Show(StoredColors))));
        }

        if (aspects.HasFlag(LegacyAspects.Text) && !string.Equals(Text, expected.Text, StringComparison.Ordinal))
        {
            differences.Add(new LegacyDifference(LegacyAspects.Text, FirstDifferentLine(expected.Text, Text)));
        }

        if (aspects.HasFlag(LegacyAspects.Calls))
        {
            string? difference = FirstDifferentEntry(
                expected.Calls.Select(call => call.Method + "[" + Visible(call.Text) + "]").ToArray(),
                Calls.Select(call => call.Method + "[" + Visible(call.Text) + "]").ToArray(),
                "call");
            if (difference != null)
            {
                differences.Add(new LegacyDifference(LegacyAspects.Calls, difference));
            }
        }

        if (aspects.HasFlag(LegacyAspects.Colors))
        {
            string? difference = FirstDifferentEntry(
                expected.Calls.Select(call => call.Foreground + " on " + call.Background).ToArray(),
                Calls.Select(call => call.Foreground + " on " + call.Background).ToArray(),
                "the colors of call");
            if (difference == null && (ForegroundAfter != expected.ForegroundAfter || BackgroundAfter != expected.BackgroundAfter))
            {
                difference = "the colors left behind: " + Expected(
                    expected.ForegroundAfter + " on " + expected.BackgroundAfter, ForegroundAfter + " on " + BackgroundAfter);
            }
            if (difference != null)
            {
                differences.Add(new LegacyDifference(LegacyAspects.Colors, difference));
            }
        }

        if (aspects.HasFlag(LegacyAspects.ExceptionType) && !string.Equals(ExceptionType, expected.ExceptionType, StringComparison.Ordinal))
        {
            differences.Add(new LegacyDifference(LegacyAspects.ExceptionType, Expected(Show(expected.ExceptionType), Show(ExceptionType))));
        }

        if (aspects.HasFlag(LegacyAspects.ExceptionParameterName)
            && !string.Equals(ExceptionParameterName, expected.ExceptionParameterName, StringComparison.Ordinal))
        {
            differences.Add(new LegacyDifference(
                LegacyAspects.ExceptionParameterName, Expected(Show(expected.ExceptionParameterName), Show(ExceptionParameterName))));
        }

        if (aspects.HasFlag(LegacyAspects.ExceptionMessage) && !string.Equals(ExceptionMessage, expected.ExceptionMessage, StringComparison.Ordinal))
        {
            differences.Add(new LegacyDifference(LegacyAspects.ExceptionMessage, Expected(Show(expected.ExceptionMessage), Show(ExceptionMessage))));
        }

        return differences;
    }

    /// <summary>The aspects in which this observation differs from the expected one, as one value.</summary>
    public LegacyAspects AspectsThatDifferFrom(LegacyObservation expected, LegacyAspects aspects = LegacyAspects.All) =>
        DifferencesFrom(expected, aspects).Aggregate(LegacyAspects.None, (all, difference) => all | difference.Aspect);

    private static string Expected(string expected, string actual) => "expected " + expected + ", but was " + actual;

    private static string Show(LegacyStoredColors? colors) => colors.HasValue ? colors.Value.ToString() : "no calendar";

    private static string Show(string? value) => value == null ? "none" : "\"" + Visible(value) + "\"";

    private static string Visible(string text) => text.Replace("\r", "\\r", StringComparison.Ordinal).Replace("\n", "\\n", StringComparison.Ordinal);

    private static string FirstDifferentLine(string expected, string actual)
    {
        string[] expectedLines = expected.Split('\n');
        string[] actualLines = actual.Split('\n');
        for (int index = 0; index < Math.Max(expectedLines.Length, actualLines.Length); index++)
        {
            string? expectedLine = index < expectedLines.Length ? expectedLines[index] : null;
            string? actualLine = index < actualLines.Length ? actualLines[index] : null;
            if (!string.Equals(expectedLine, actualLine, StringComparison.Ordinal))
            {
                return "line " + (index + 1).ToString(CultureInfo.InvariantCulture) + ": " + Expected(Show(expectedLine), Show(actualLine));
            }
        }
        return "the texts differ";
    }

    private static string? FirstDifferentEntry(string[] expected, string[] actual, string what)
    {
        for (int index = 0; index < Math.Min(expected.Length, actual.Length); index++)
        {
            if (!string.Equals(expected[index], actual[index], StringComparison.Ordinal))
            {
                return what + " " + (index + 1).ToString(CultureInfo.InvariantCulture) + ": " + Expected(expected[index], actual[index]);
            }
        }
        if (expected.Length != actual.Length)
        {
            return string.Create(CultureInfo.InvariantCulture,
                $"{expected.Length} calls expected, but there were {actual.Length}; the first {Math.Min(expected.Length, actual.Length)} are the same");
        }
        return null;
    }
}
