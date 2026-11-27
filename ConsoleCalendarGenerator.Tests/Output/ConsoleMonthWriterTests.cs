using System.Text;
using ConsoleCalendarGenerator.Tests.Support;

namespace ConsoleCalendarGenerator.Tests.Output;

/// <summary>
/// Writing a rendered month to the console, observed inside the test process: <see cref="Console.Out"/> is
/// replaced by a writer that records every call together with the console colors in effect at that moment.
/// </summary>
/// <remarks>
/// What is written is checked on every system. Which color it is written in can only be read back without a
/// terminal on Linux and macOS, so those tests do not run on Windows; see
/// <see cref="ConsoleState.ColorsObservableOnUnixOnly"/>.
/// </remarks>
[TestFixture]
public class ConsoleMonthWriterTests
{
    /// <summary>February 2023, cells three wide, the 14th chosen; see <see cref="ModernChildScenario"/>.</summary>
    private static readonly RenderedMonth Month = ModernChildScenario.Rendered();

    private static readonly CalendarTheme Theme = ModernChildScenario.Theme;

    // ---- the text, on every system ----

    [Test]
    public void WithNoColors_TheTextIsWritten_EveryLineEndedByTheConsolesLineTerminator()
    {
        Captured captured = ConsoleCapture.Run(() => ConsoleMonthWriter.Write(Month, CalendarTheme.None));

        Assert.That(captured.Text, Is.EqualTo(ModernChildScenario.PlainText("\n")));
    }

    [Test]
    public void WithATheme_TheTextIsTheSame()
    {
        Captured captured = ConsoleCapture.Run(() => ConsoleMonthWriter.Write(Month, Theme));

        Assert.That(captured.Text, Is.EqualTo(ModernChildScenario.PlainText("\n")));
        Assert.That(captured.Text, Ordinal.DoesNotContain(TerminalOutput.EscapeCharacter), "the library writes no escape sequences itself");
    }

    [TestCase("\n")]
    [TestCase("\r\n")]
    public void TheLineTerminator_IsTheOneOfConsoleOut(string newLine)
    {
        Captured captured = ConsoleCapture.Run(() => ConsoleMonthWriter.Write(Month, Theme), newLine: newLine);

        Assert.That(captured.Text, Is.EqualTo(ModernChildScenario.PlainText(newLine)));
        Assert.That(captured.Events.Count(e => e.EndsLine), Is.EqualTo(Month.Lines.Count), "one line end per line, the last one included");
    }

    [Test]
    public void TheSameTextAsTheGenericWriter()
    {
        var writer = new StringWriter { NewLine = "\n" };
        MonthWriter.Write(Month, writer);

        Captured captured = ConsoleCapture.Run(() => ConsoleMonthWriter.Write(Month, Theme));

        Assert.That(captured.Text, Is.EqualTo(writer.ToString()));
    }

    [Test]
    public void EverySegment_IsWrittenAsItIs_OneAfterTheOther()
    {
        Captured captured = ConsoleCapture.Run(() => ConsoleMonthWriter.Write(Month, Theme));

        IEnumerable<string> expected = Month.Lines.SelectMany(line => line.Segments.Select(segment => "[" + segment.Text + "]").Append("<NL>"));

        Assert.That(captured.Events.Select(e => e.EndsLine ? "<NL>" : "[" + e.Payload + "]"), Is.EqualTo(expected));
    }

    [Test]
    public void WritingTwice_WritesTheMonthTwice()
    {
        Captured captured = ConsoleCapture.Run(() =>
        {
            ConsoleMonthWriter.Write(Month, Theme);
            ConsoleMonthWriter.Write(Month, CalendarTheme.None);
        });

        Assert.That(captured.Text, Is.EqualTo(ModernChildScenario.PlainText("\n") + ModernChildScenario.PlainText("\n")));
    }

    [Test]
    public void ConsoleOut_IsUsedAsItIs_AndNotReplaced()
    {
        TextWriter? during = null;
        TextWriter? after = null;

        Captured captured = ConsoleCapture.Run(() =>
        {
            during = Console.Out;
            ConsoleMonthWriter.Write(Month, Theme);
            after = Console.Out;
        });

        Assert.That(after, Is.SameAs(during));
        Assert.That(captured.Events, Is.Not.Empty, "the text went to the writer that was Console.Out");
    }

    [Test]
    public void ANullMonthOrTheme_IsRejected_BeforeAnythingIsWritten()
    {
        Captured captured = ConsoleCapture.Run(() =>
        {
            Assert.That(Assert.Throws<ArgumentNullException>(() => ConsoleMonthWriter.Write(null!, Theme))!.ParamName, Is.EqualTo("month"));
            Assert.That(Assert.Throws<ArgumentNullException>(() => ConsoleMonthWriter.Write(Month, null!))!.ParamName, Is.EqualTo("theme"));
            Assert.That(Assert.Throws<ArgumentNullException>(() => ConsoleMonthWriter.Write(null!, null!))!.ParamName, Is.EqualTo("month"));
        });

        Assert.That(captured.Events, Is.Empty);
    }

    [Test]
    public void AnExceptionFromConsoleOut_IsPassedOn_AndWhatWasWrittenStays()
    {
        var failing = new FailingWriter(failOnCall: 20);

        Captured captured = ConsoleCapture.TryRun(() => WriteTo(failing, Theme));

        Assert.That(captured.Exception, Is.TypeOf<InvalidOperationException>());
        Assert.That(failing.Text, Does.StartWith("February 2023 (* = chosen)\n Mo  Tu").Using(StringComparison.Ordinal));
        Assert.That(failing.Calls, Is.EqualTo(20));
    }

    [Test]
    public void WritingToTheConsole_ChangesNoProcessWideState_AndNotTheMonth()
    {
        string month = Rendered.Describe(Month);
        object[]? before = null;
        object[]? after = null;

        ConsoleCapture.Run(() =>
        {
            before = ProcessState.Snapshot();
            ConsoleMonthWriter.Write(Month, Theme);
            ConsoleMonthWriter.Write(Month, CalendarTheme.None);
            after = ProcessState.Snapshot();
        });

        Assert.That(after, Is.EqualTo(before), "cultures, console writers, environment and current directory");
        Assert.That(Rendered.Describe(Month), Is.EqualTo(month));
    }

    /// <summary>
    /// Sixteen threads write different months at once. Each month arrives whole: the output is a sequence of
    /// complete months, never lines of one inside another.
    /// </summary>
    [Test]
    public void CallsFromSeveralThreads_DoNotMixTheirOutput()
    {
        RenderedMonth[] months = Enumerable.Range(1, 12)
            .Select(month => MonthRenderer.Render(2024, month, MonthRenderOptions.Default.WithTitleFormat("MMMM yyyy"))).ToArray();
        string[] texts = months.Select(month => string.Concat(month.Lines.Select(line => line.Text + "\n"))).ToArray();
        const int Writes = 480;

        Captured captured = ConsoleCapture.Run(() =>
            Parallel.For(0, Writes, new ParallelOptions { MaxDegreeOfParallelism = 16 }, index =>
                ConsoleMonthWriter.Write(months[index % months.Length], index % 2 == 0 ? Theme : CalendarTheme.None)));

        string output = captured.Text;
        int position = 0;
        int whole = 0;
        while (position < output.Length)
        {
            string? next = texts.FirstOrDefault(text => string.CompareOrdinal(output, position, text, 0, text.Length) == 0);
            Assert.That(next, Is.Not.Null, "at character " + position + " the output is not the start of a complete month");
            position += next!.Length;
            whole++;
        }
        Assert.That(whole, Is.EqualTo(Writes));
    }

    // ---- colors, where they can be read back ----

    [Test]
    [Platform(Exclude = "Win", Reason = ConsoleState.ColorsObservableOnUnixOnly)]
    public void WithNoColors_NoColorIsEverChanged()
    {
        Captured captured = ConsoleCapture.Run(
            () => ConsoleMonthWriter.Write(Month, CalendarTheme.None),
            foregroundBefore: ConsoleColor.DarkCyan,
            backgroundBefore: ConsoleColor.DarkBlue);

        Assert.That(captured.Events.Select(e => e.Foreground), Is.All.EqualTo(ConsoleColor.DarkCyan));
        Assert.That(captured.Events.Select(e => e.Background), Is.All.EqualTo(ConsoleColor.DarkBlue));
        Assert.That(captured.ForegroundAfter, Is.EqualTo(ConsoleColor.DarkCyan));
        Assert.That(captured.BackgroundAfter, Is.EqualTo(ConsoleColor.DarkBlue));
    }

    /// <summary>
    /// Every segment is written in the color the theme gives it, and every segment the theme has no color for,
    /// and every line end, in the color the caller had.
    /// </summary>
    [Test]
    [Platform(Exclude = "Win", Reason = ConsoleState.ColorsObservableOnUnixOnly)]
    public void EverySegment_IsWrittenInItsThemeColor_AndTheRestInTheCallersColor()
    {
        Captured captured = ConsoleCapture.Run(
            () => ConsoleMonthWriter.Write(Month, Theme),
            foregroundBefore: ConsoleColor.DarkCyan,
            backgroundBefore: ConsoleColor.DarkBlue);

        var expected = new List<(string Text, ConsoleColor Color)>();
        foreach (RenderedLine line in Month.Lines)
        {
            expected.AddRange(line.Segments.Select(segment => (segment.Text, Theme.GetColor(segment) ?? ConsoleColor.DarkCyan)));
            expected.Add((string.Empty, ConsoleColor.DarkCyan));
        }

        Assert.That(captured.Events.Select(e => (e.Payload, e.Foreground)), Is.EqualTo(expected));
    }

    /// <summary>The same, spelled out for the title and for the week that holds the chosen day.</summary>
    [Test]
    [Platform(Exclude = "Win", Reason = ConsoleState.ColorsObservableOnUnixOnly)]
    public void TheTitle_TheLabels_TheDays_AndTheChosenDay_EachHaveTheirColor()
    {
        Captured captured = ConsoleCapture.Run(() => ConsoleMonthWriter.Write(Month, Theme), foregroundBefore: ConsoleColor.DarkCyan);

        string[] events = captured.Events.Select(e => (e.EndsLine ? "<NL>" : "[" + e.Payload + "]") + e.Foreground).ToArray();

        Assert.That(events.Take(3), Is.EqualTo(new[]
        {
            "[February 2023]Yellow",
            "[ (* = chosen)]DarkCyan",
            "<NL>DarkCyan",
        }));
        Assert.That(events.Skip(3).Take(4), Is.EqualTo(new[] { "[ Mo]Cyan", "[ ]DarkCyan", "[ Tu]Cyan", "[ ]DarkCyan" }));
        int week = Array.IndexOf(events, "[ 13]Green");
        Assert.That(events.Skip(week).Take(6), Is.EqualTo(new[]
        {
            "[ 13]Green",
            "[ ]DarkCyan",
            "[*14]Magenta",
            "[ ]DarkCyan",
            "[ 15]Green",
            "[ ]DarkCyan",
        }));
        Assert.That(events.Where(e => e.StartsWith("[   ]", StringComparison.Ordinal)), Is.All.EqualTo("[   ]DarkCyan"), "empty cells have no color");
    }

    /// <summary>The chosen day's state has no color in this theme, so it is written in the color of the days.</summary>
    [Test]
    [Platform(Exclude = "Win", Reason = ConsoleState.ColorsObservableOnUnixOnly)]
    public void AStateWithoutAColor_IsWrittenInTheDayColor()
    {
        CalendarTheme withoutStateColor = Theme.WithStateColor("chosen", null);

        Captured captured = ConsoleCapture.Run(() => ConsoleMonthWriter.Write(Month, withoutStateColor));

        Assert.That(captured.Events.Single(e => e.Payload == "*14").Foreground, Is.EqualTo(ConsoleColor.Green));
    }

    /// <summary>
    /// The colors the console had when the call began are the colors it has when the call returns, whether the
    /// caller had set them or not. A color that was never set is shown here as null.
    /// </summary>
    [TestCase(ConsoleColor.DarkCyan, ConsoleColor.DarkBlue)]
    [TestCase(ConsoleColor.DarkCyan, null)]
    [TestCase(null, ConsoleColor.DarkBlue)]
    [TestCase(null, null)]
    [TestCase(ConsoleColor.Yellow, ConsoleColor.Black, Description = "the caller's color is one the theme also uses")]
    [Platform(Exclude = "Win", Reason = ConsoleState.ColorsObservableOnUnixOnly)]
    public void TheCallersColors_AreBackAfterwards(ConsoleColor? foreground, ConsoleColor? background)
    {
        Captured captured = ConsoleCapture.Run(
            () => ConsoleMonthWriter.Write(Month, Theme),
            foregroundBefore: foreground,
            backgroundBefore: background);

        Assert.That(captured.ForegroundAfter, Is.EqualTo(foreground ?? ConsoleState.Unset));
        Assert.That(captured.BackgroundAfter, Is.EqualTo(background ?? ConsoleState.Unset));
    }

    /// <summary>The background is never the theme's to change, and is the caller's for every single write.</summary>
    [TestCase(ConsoleColor.DarkCyan, ConsoleColor.DarkBlue)]
    [TestCase(null, ConsoleColor.DarkBlue)]
    [TestCase(null, null)]
    [Platform(Exclude = "Win", Reason = ConsoleState.ColorsObservableOnUnixOnly)]
    public void TheBackground_IsTheCallersForEveryWrite(ConsoleColor? foreground, ConsoleColor? background)
    {
        Captured captured = ConsoleCapture.Run(
            () => ConsoleMonthWriter.Write(Month, Theme),
            foregroundBefore: foreground,
            backgroundBefore: background);

        Assert.That(captured.Events.Select(e => e.Background), Is.All.EqualTo(background ?? ConsoleState.Unset));
        Assert.That(captured.Events.Where(e => e.EndsLine).Select(e => e.Foreground), Is.All.EqualTo(foreground ?? ConsoleState.Unset),
            "every line end is written in the caller's foreground");
    }

    /// <summary>
    /// The writer fails while a theme color is in effect: on the title, in the middle of the labels, on a day
    /// and on the chosen day. Each time the exception is passed on and the caller's colors are back.
    /// </summary>
    [TestCase(1, ConsoleColor.DarkCyan, ConsoleColor.DarkBlue)]
    [TestCase(6, ConsoleColor.DarkCyan, ConsoleColor.DarkBlue)]
    [TestCase(1, null, null)]
    [TestCase(6, null, ConsoleColor.DarkBlue)]
    [TestCase(30, null, null)]
    [TestCase(30, ConsoleColor.Red, null)]
    [Platform(Exclude = "Win", Reason = ConsoleState.ColorsObservableOnUnixOnly)]
    public void WhenWritingFails_TheCallersColorsAreStillPutBack(int failOnCall, ConsoleColor? foreground, ConsoleColor? background)
    {
        var failing = new FailingWriter(failOnCall);

        Captured captured = ConsoleCapture.TryRun(
            () => WriteTo(failing, Theme),
            foregroundBefore: foreground,
            backgroundBefore: background);

        Assert.That(captured.Exception, Is.TypeOf<InvalidOperationException>());
        Assert.That(failing.ColorAtFailure, Is.Not.EqualTo(foreground ?? ConsoleState.Unset), "a theme color was in effect when the writer failed");
        Assert.That(captured.ForegroundAfter, Is.EqualTo(foreground ?? ConsoleState.Unset));
        Assert.That(captured.BackgroundAfter, Is.EqualTo(background ?? ConsoleState.Unset));
    }

    [Test]
    [Platform(Exclude = "Win", Reason = ConsoleState.ColorsObservableOnUnixOnly)]
    public void AfterManyWrites_FromManyThreads_TheCallersColorsAreBack()
    {
        Captured captured = ConsoleCapture.Run(
            () => Parallel.For(0, 200, new ParallelOptions { MaxDegreeOfParallelism = 8 }, _ => ConsoleMonthWriter.Write(Month, Theme)),
            foregroundBefore: ConsoleColor.DarkCyan,
            backgroundBefore: ConsoleColor.DarkBlue);

        Assert.That(captured.ForegroundAfter, Is.EqualTo(ConsoleColor.DarkCyan));
        Assert.That(captured.BackgroundAfter, Is.EqualTo(ConsoleColor.DarkBlue));
        Assert.That(captured.Events.Where(e => e.EndsLine).Select(e => e.Foreground), Is.All.EqualTo(ConsoleColor.DarkCyan));
    }

    /// <summary>Makes <paramref name="writer"/> the console's writer for one call, then puts the previous one back.</summary>
    private static void WriteTo(TextWriter writer, CalendarTheme theme)
    {
        TextWriter previous = Console.Out;
        Console.SetOut(writer);
        try
        {
            ConsoleMonthWriter.Write(Month, theme);
        }
        finally
        {
            Console.SetOut(previous);
        }
    }

    /// <summary>Fails on a given call, and remembers the console's foreground color at that moment.</summary>
    private sealed class FailingWriter : TextWriter
    {
        private readonly int _failOnCall;
        private readonly StringBuilder _text = new();

        public FailingWriter(int failOnCall)
        {
            _failOnCall = failOnCall;
            NewLine = "\n";
        }

        public int Calls { get; private set; }

        public ConsoleColor ColorAtFailure { get; private set; }

        public string Text => _text.ToString();

        public override Encoding Encoding => Encoding.UTF8;

        public override void Write(string? value) => Record(value ?? string.Empty);

        public override void WriteLine() => Record(NewLine);

        public override void Write(char value) => Record(value.ToString());

        private void Record(string text)
        {
            Calls++;
            if (Calls == _failOnCall)
            {
                ColorAtFailure = Console.ForegroundColor;
                throw new InvalidOperationException("The console writer failed.");
            }
            _text.Append(text);
        }
    }
}
