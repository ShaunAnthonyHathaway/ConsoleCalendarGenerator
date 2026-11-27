using System.Text;
using ConsoleCalendarGenerator.Tests.Support;

namespace ConsoleCalendarGenerator.Tests.Output;

/// <summary>
/// Writing a rendered month to a <see cref="TextWriter"/>: the exact text, the writer's own line terminator
/// after every line, and nothing done to the writer or to the console.
/// </summary>
[TestFixture]
public class MonthWriterTests
{
    private static readonly RenderedMonth February2021 = MonthRenderer.Render(2021, 2, MonthRenderOptions.Default);

    private static string Written(RenderedMonth month, string newLine)
    {
        var writer = new StringWriter { NewLine = newLine };
        MonthWriter.Write(month, writer);
        return writer.ToString();
    }

    // ---- the text ----

    [Test]
    public void EveryLine_IsWritten_FollowedByTheWritersLineTerminator()
    {
        Assert.That(Written(February2021, "\n"), Is.EqualTo(
            "   2021 February\n" +
            "Mo Tu We Th Fr Sa Su\n" +
            " 1  2  3  4  5  6  7\n" +
            " 8  9 10 11 12 13 14\n" +
            "15 16 17 18 19 20 21\n" +
            "22 23 24 25 26 27 28\n"));
    }

    [Test]
    public void AWriterWithCarriageReturnLineFeed_GetsThat()
    {
        Assert.That(Written(February2021, "\r\n"), Is.EqualTo(
            "   2021 February\r\n" +
            "Mo Tu We Th Fr Sa Su\r\n" +
            " 1  2  3  4  5  6  7\r\n" +
            " 8  9 10 11 12 13 14\r\n" +
            "15 16 17 18 19 20 21\r\n" +
            "22 23 24 25 26 27 28\r\n"));
    }

    /// <summary>Whatever the writer's terminator is, it is the writer's business: it is used as it is.</summary>
    [TestCase("\n")]
    [TestCase("\r\n")]
    [TestCase("\r")]
    [TestCase("<br>\n")]
    [TestCase(" | ")]
    [TestCase("")]
    public void AnyLineTerminatorTheWriterHas_EndsEveryLineExactlyOnce(string newLine)
    {
        string expected = string.Concat(February2021.Lines.Select(line => line.Text + newLine));

        Assert.That(Written(February2021, newLine), Is.EqualTo(expected));
    }

    /// <summary>The last line is ended too. <c>ToText</c> puts a terminator only between lines.</summary>
    [Test]
    public void TheLastLine_IsTerminated_UnlikeInToText()
    {
        Assert.That(Written(February2021, "\n"), Is.EqualTo(February2021.ToText() + "\n"));
        Assert.That(Written(February2021, "\r\n"), Is.EqualTo(February2021.ToText("\r\n") + "\r\n"));
    }

    [Test]
    public void AWriterLeftAtItsDefault_UsesThePlatformsLineTerminator()
    {
        var writer = new StringWriter();

        MonthWriter.Write(February2021, writer);

        Assert.That(writer.NewLine, Is.EqualTo(Environment.NewLine));
        Assert.That(writer.ToString(), Is.EqualTo(string.Concat(February2021.Lines.Select(line => line.Text + Environment.NewLine))));
    }

    [Test]
    public void AMonthWithStates_IsWrittenWithItsMarkers()
    {
        RenderedMonth marked = MonthRenderer.Render(2023, 2, MonthRenderOptions.Default.WithCellWidth(3).WithTitleSuffix(" (* = open)"),
            new[] { new DateTime(2023, 2, 14) }, new DayState("open", "*"));

        Assert.That(Written(marked, "\n"), Is.EqualTo(
            " 2023 February (* = open)\n" +
            " Mo  Tu  We  Th  Fr  Sa  Su\n" +
            "          1   2   3   4   5\n" +
            "  6   7   8   9  10  11  12\n" +
            " 13 *14  15  16  17  18  19\n" +
            " 20  21  22  23  24  25  26\n" +
            " 27  28\n"));
    }

    [Test]
    public void WritingTwice_WritesTheMonthTwice()
    {
        var writer = new StringWriter { NewLine = "\n" };

        MonthWriter.Write(February2021, writer);
        MonthWriter.Write(February2021, writer);

        Assert.That(writer.ToString(), Is.EqualTo(Written(February2021, "\n") + Written(February2021, "\n")));
    }

    [Test]
    public void WhatIsAlreadyInTheWriter_IsLeftBeforeTheMonth()
    {
        var writer = new StringWriter { NewLine = "\n" };
        writer.Write("before:");

        MonthWriter.Write(February2021, writer);

        Assert.That(writer.ToString(), Does.StartWith("before:   2021 February\n").Using(StringComparison.Ordinal));
    }

    // ---- the writer is the caller's ----

    [Test]
    public void TheWriter_IsNotFlushedClosedDisposedOrChanged()
    {
        var writer = new WatchedWriter { NewLine = "\r\n" };

        MonthWriter.Write(February2021, writer);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(writer.Flushes, Is.Zero);
            Assert.That(writer.Closes, Is.Zero);
            Assert.That(writer.Disposals, Is.Zero);
            Assert.That(writer.NewLine, Is.EqualTo("\r\n"));
            Assert.That(writer.Text, Is.EqualTo(February2021.ToText("\r\n") + "\r\n"));
        }

        // Still usable afterwards.
        writer.Write("more");
        Assert.That(writer.Text, Does.EndWith("more").Using(StringComparison.Ordinal));
    }

    [Test]
    public void AnExceptionFromTheWriter_IsPassedOn_AndWhatWasWrittenStays()
    {
        var writer = new WatchedWriter { NewLine = "\n", FailAfterLines = 3 };

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => MonthWriter.Write(February2021, writer))!;

        Assert.That(exception.Message, Is.EqualTo("The writer failed."));
        Assert.That(writer.Text, Is.EqualTo(
            "   2021 February\n" +
            "Mo Tu We Th Fr Sa Su\n" +
            " 1  2  3  4  5  6  7\n"), "the three lines before the failure are not taken back");
    }

    // ---- invalid arguments ----

    [Test]
    public void ANullMonth_IsRejected()
    {
        Assert.That(Assert.Throws<ArgumentNullException>(() => MonthWriter.Write(null!, new StringWriter()))!.ParamName, Is.EqualTo("month"));
    }

    [Test]
    public void ANullWriter_IsRejected()
    {
        Assert.That(Assert.Throws<ArgumentNullException>(() => MonthWriter.Write(February2021, null!))!.ParamName, Is.EqualTo("writer"));
        Assert.That(Assert.Throws<ArgumentNullException>(() => MonthWriter.Write(null!, null!))!.ParamName, Is.EqualTo("month"), "the month is checked first");
    }

    // ---- nothing else is touched ----

    [Test]
    public void WritingToAWriter_WritesNothingToTheConsole()
    {
        var writer = new StringWriter();

        Captured captured = ConsoleCapture.Run(() => MonthWriter.Write(February2021, writer));

        Assert.That(captured.Events, Is.Empty);
        Assert.That(writer.ToString(), Is.Not.Empty);
    }

    [Test]
    [Platform(Exclude = "Win", Reason = ConsoleState.ColorsObservableOnUnixOnly)]
    public void WritingToAWriter_LeavesTheConsoleColorsAlone()
    {
        Captured captured = ConsoleCapture.Run(
            () => MonthWriter.Write(February2021, new StringWriter()),
            foregroundBefore: ConsoleColor.DarkCyan,
            backgroundBefore: ConsoleColor.DarkBlue);

        Assert.That(captured.ForegroundAfter, Is.EqualTo(ConsoleColor.DarkCyan));
        Assert.That(captured.BackgroundAfter, Is.EqualTo(ConsoleColor.DarkBlue));
    }

    [Test]
    public void WritingToAWriter_ChangesNoProcessWideState_AndNotTheMonth()
    {
        object[] before = ProcessState.Snapshot();
        string month = Rendered.Describe(February2021);

        MonthWriter.Write(February2021, new StringWriter());
        Assert.That(() => MonthWriter.Write(February2021, new WatchedWriter { FailAfterLines = 0 }), Throws.TypeOf<InvalidOperationException>());

        Assert.That(ProcessState.Snapshot(), Is.EqualTo(before));
        Assert.That(Rendered.Describe(February2021), Is.EqualTo(month));
    }

    [Test]
    public void ManyThreads_CanEachWriteToTheirOwnWriter()
    {
        string expected = Written(February2021, "\n");
        int wrong = 0;

        Parallel.For(0, 500, new ParallelOptions { MaxDegreeOfParallelism = 8 }, _ =>
        {
            if (Written(February2021, "\n") != expected)
            {
                Interlocked.Increment(ref wrong);
            }
        });

        Assert.That(wrong, Is.Zero);
    }

    /// <summary>Records what is done to it, and can be told to fail when a given number of lines has been written.</summary>
    private sealed class WatchedWriter : TextWriter
    {
        private readonly StringBuilder _text = new();
        private int _lines;

        public int Flushes { get; private set; }

        public int Closes { get; private set; }

        public int Disposals { get; private set; }

        public int? FailAfterLines { get; init; }

        public string Text => _text.ToString();

        public override Encoding Encoding => Encoding.UTF8;

        public override void Write(char value)
        {
            if (_lines == FailAfterLines)
            {
                throw new InvalidOperationException("The writer failed.");
            }
            _text.Append(value);
            if (CoreNewLine.Length > 0 && _text.Length >= CoreNewLine.Length
                && _text.ToString(_text.Length - CoreNewLine.Length, CoreNewLine.Length) == new string(CoreNewLine))
            {
                _lines++;
            }
        }

        public override void Flush() => Flushes++;

        public override void Close() => Closes++;

        protected override void Dispose(bool disposing) => Disposals++;
    }
}
