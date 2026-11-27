namespace ConsoleCalendarGenerator.Tests.Migration;

/// <summary>
/// The two corrections of how a month ends (D-59 and D-60), as exact changes to the list of calls that 1.0.0
/// made on the console's writer. They are the whole of what the migration tests allow the end of a month to
/// differ by: nothing here ignores white space, compares loosely or touches any other call.
///
/// <para>
/// 1.0.0 ended a month with <c>Write("\n")</c> and one empty <c>WriteLine()</c>; when the last day was a Sunday
/// the row had been ended by an empty <c>WriteLine()</c> just before. Version 2.0 ends the last row with one
/// empty <c>WriteLine()</c> and writes one more for the blank line:
/// </para>
/// <code>
///                          1.0.0                         after D-59                 after D-60 (2.0)
/// last day not a Sunday    Write("\n") WriteLine()       WriteLine() WriteLine()    WriteLine() WriteLine()
/// last day a Sunday        WriteLine() Write("\n")       WriteLine() WriteLine()    WriteLine() WriteLine()
///                          WriteLine()                   WriteLine()
/// </code>
/// <para>
/// The functions are generic over the kind of call, because the tests record calls in two forms
/// (<see cref="Support.RecordedCall"/> and <see cref="Support.WriteEvent"/>). Each returns the list it was given
/// when there is nothing to change.
/// </para>
/// </summary>
internal static class MonthEnd
{
    /// <summary>
    /// D-59: every call that writes the literal line feed becomes a call that ends the line with the writer's
    /// terminator. In 1.0.0 that literal is written only after the last day of a month and is always followed
    /// by the line end of the blank line; a literal anywhere else is not what this correction is about and is
    /// refused.
    /// </summary>
    public static IReadOnlyList<T> WithTheConsolesTerminator<T>(
        IReadOnlyList<T> calls, Func<T, bool> isLiteralLineFeed, Func<T, bool> isLineEnd, Func<T, T> asLineEnd)
    {
        if (!calls.Any(isLiteralLineFeed))
        {
            return calls;
        }
        var corrected = new List<T>(calls.Count);
        for (int index = 0; index < calls.Count; index++)
        {
            if (!isLiteralLineFeed(calls[index]))
            {
                corrected.Add(calls[index]);
                continue;
            }
            if (index + 1 >= calls.Count || !isLineEnd(calls[index + 1]))
            {
                throw new InvalidOperationException("Call " + (index + 1) + " writes a literal line feed that is not followed by the line end of a month's blank line.");
            }
            corrected.Add(asLineEnd(calls[index]));
        }
        return corrected;
    }

    /// <summary>
    /// D-60: where three line ends follow each other, one is removed. After D-59 that is exactly the end of a
    /// month whose last day is a Sunday: the end of its last row, the former literal, and the blank line. A row
    /// inside a month ends with one line end and every other month with two. More than three in a row is not
    /// something 1.0.0 wrote and is refused.
    /// </summary>
    public static IReadOnlyList<T> WithOneBlankLine<T>(IReadOnlyList<T> calls, Func<T, bool> isLineEnd)
    {
        var corrected = new List<T>(calls.Count);
        int run = 0;
        bool changed = false;
        foreach (T call in calls)
        {
            run = isLineEnd(call) ? run + 1 : 0;
            if (run > 3)
            {
                throw new InvalidOperationException("More than three line ends follow each other, which is not the end of a month.");
            }
            if (run == 3)
            {
                changed = true;
                continue;
            }
            corrected.Add(call);
        }
        return changed ? corrected : calls;
    }

    // ---- for the two forms in which the tests record calls ----

    public static IReadOnlyList<Support.RecordedCall> WithTheConsolesTerminator(IReadOnlyList<Support.RecordedCall> calls) =>
        WithTheConsolesTerminator(calls, IsLiteralLineFeed, IsLineEnd, call => new Support.RecordedCall("L", string.Empty, EndsLine: true, call.Foreground, call.Background));

    public static IReadOnlyList<Support.RecordedCall> WithOneBlankLine(IReadOnlyList<Support.RecordedCall> calls) => WithOneBlankLine(calls, IsLineEnd);

    public static IReadOnlyList<Support.WriteEvent> WithTheConsolesTerminator(IReadOnlyList<Support.WriteEvent> events) =>
        WithTheConsolesTerminator(events, IsLiteralLineFeed, IsLineEnd, written => written with { Payload = string.Empty, EndsLine = true });

    public static IReadOnlyList<Support.WriteEvent> WithOneBlankLine(IReadOnlyList<Support.WriteEvent> events) => WithOneBlankLine(events, IsLineEnd);

    /// <summary>Both corrections, in order: what the library writes where 1.0.0 wrote these calls.</summary>
    public static IReadOnlyList<Support.RecordedCall> Corrected(IReadOnlyList<Support.RecordedCall> calls) => WithOneBlankLine(WithTheConsolesTerminator(calls));

    /// <summary>Both corrections, in order.</summary>
    public static IReadOnlyList<Support.WriteEvent> Corrected(IReadOnlyList<Support.WriteEvent> events) => WithOneBlankLine(WithTheConsolesTerminator(events));

    private static bool IsLiteralLineFeed(Support.RecordedCall call) => call.Method == "S" && call.Text == "\n";

    private static bool IsLineEnd(Support.RecordedCall call) => call.Method == "L" && call.Text.Length == 0;

    private static bool IsLiteralLineFeed(Support.WriteEvent written) => !written.EndsLine && written.Payload == "\n";

    private static bool IsLineEnd(Support.WriteEvent written) => written.EndsLine && written.Payload.Length == 0;
}
