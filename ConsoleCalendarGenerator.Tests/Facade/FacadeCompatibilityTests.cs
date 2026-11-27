using System.Globalization;
using CalendarGenerator;
using ConsoleCalendarGenerator.Tests.Support;

namespace ConsoleCalendarGenerator.Tests.Facade;

/// <summary>
/// The legacy class shares its grid with the modern API and nothing else. In each of the places below the modern
/// API deliberately behaves differently from 1.0.0, and the legacy class must keep behaving as 1.0.0 does. Each
/// test sets the two side by side, so that it fails if the legacy class ever picks up the modern behavior.
/// </summary>
[TestFixture]
public class FacadeCompatibilityTests
{
    // ---- invalid months and years ----

    /// <summary>
    /// The exception for an invalid month or year is the runtime's own, the one
    /// <see cref="DateTime.DaysInMonth"/> throws, with its parameter name, its value and its message. The modern
    /// layout checks for itself, year first and with messages of its own.
    /// </summary>
    [TestCase(0, 2023)]
    [TestCase(13, 2023)]
    [TestCase(-1, 2023)]
    [TestCase(1, 0)]
    [TestCase(1, 10000)]
    [TestCase(1, -1)]
    [TestCase(13, 0, Description = "both invalid")]
    [TestCase(0, 10000, Description = "both invalid")]
    public void AnInvalidMonthOrYear_FailsWithExactlyTheRuntimesOwnException(int month, int year)
    {
        ArgumentOutOfRangeException runtime = ProcessState.UnderCultures(CultureInfo.InvariantCulture, CultureInfo.InvariantCulture,
            () => Assert.Throws<ArgumentOutOfRangeException>(() => DateTime.DaysInMonth(year, month))!);

        Captured fromMonth = ConsoleCapture.TryRun(() => new ConsoleCalendar().GenerateMonth(month, year));
        Captured fromMonthWithDates = ConsoleCapture.TryRun(() => new ConsoleCalendar().GenerateMonth(month, year, new List<DateTime> { DateTime.MinValue }));

        foreach (Captured captured in new[] { fromMonth, fromMonthWithDates })
        {
            Assert.That(captured.Exception, Is.TypeOf<ArgumentOutOfRangeException>());
            var thrown = (ArgumentOutOfRangeException)captured.Exception!;
            using (Assert.EnterMultipleScope())
            {
                Assert.That(thrown.ParamName, Is.EqualTo(runtime.ParamName));
                Assert.That(thrown.ActualValue, Is.EqualTo(runtime.ActualValue));
                Assert.That(thrown.Message, Is.EqualTo(runtime.Message));
                Assert.That(captured.Events, Is.Empty, "nothing is written first");
            }
        }
    }

    /// <summary>With both invalid, the two APIs name different parameters: the runtime checks the month first.</summary>
    [Test]
    public void WithAnInvalidMonthAndYear_TheLegacyClassNamesTheMonth_AndTheModernLayoutTheYear()
    {
        Captured legacy = ConsoleCapture.TryRun(() => new ConsoleCalendar().GenerateMonth(13, 0));
        ArgumentOutOfRangeException modern = Assert.Throws<ArgumentOutOfRangeException>(() => MonthLayout.Create(0, 13, DayOfWeek.Monday))!;

        Assert.That(((ArgumentOutOfRangeException)legacy.Exception!).ParamName, Is.EqualTo("month"));
        Assert.That(modern.ParamName, Is.EqualTo("year"));
    }

    [TestCase(0)]
    [TestCase(10000)]
    public void AnInvalidYear_FailsGenerateYearWithTheRuntimesOwnException(int year)
    {
        ArgumentOutOfRangeException runtime = ProcessState.UnderCultures(CultureInfo.InvariantCulture, CultureInfo.InvariantCulture,
            () => Assert.Throws<ArgumentOutOfRangeException>(() => DateTime.DaysInMonth(year, 1))!);

        Captured captured = ConsoleCapture.TryRun(() => new ConsoleCalendar().GenerateYear(year));

        var thrown = (ArgumentOutOfRangeException)captured.Exception!;
        Assert.That((thrown.ParamName, thrown.Message), Is.EqualTo((runtime.ParamName, runtime.Message)));
        Assert.That(captured.Events, Is.Empty);
    }

    // ---- colors ----

    /// <summary>After the legacy class has drawn a month the caller's colors are reset, not restored.</summary>
    [Test]
    [Platform(Exclude = "Win", Reason = ConsoleState.ColorsObservableOnUnixOnly)]
    public void TheLegacyClass_StillResetsTheCallersColors_UnlikeTheModernConsoleWriter()
    {
        Captured legacy = ConsoleCapture.Run(
            () => new ConsoleCalendar().GenerateMonth(2, 2023),
            foregroundBefore: ConsoleColor.DarkCyan,
            backgroundBefore: ConsoleColor.DarkBlue);
        Captured modern = ConsoleCapture.Run(
            () => ConsoleMonthWriter.Write(MonthRenderer.Render(2023, 2, MonthRenderOptions.Default), CalendarTheme.None.WithDayColor(ConsoleColor.White)),
            foregroundBefore: ConsoleColor.DarkCyan,
            backgroundBefore: ConsoleColor.DarkBlue);

        Assert.That((legacy.ForegroundAfter, legacy.BackgroundAfter), Is.EqualTo((ConsoleState.Unset, ConsoleState.Unset)));
        Assert.That((modern.ForegroundAfter, modern.BackgroundAfter), Is.EqualTo((ConsoleColor.DarkCyan, ConsoleColor.DarkBlue)));
    }

    /// <summary>
    /// An undefined color is accepted by the legacy class and fails only when the console is asked to use it,
    /// after the title has been written. A theme refuses the same value when it is built.
    /// </summary>
    [Test]
    public void AnUndefinedColor_StillFailsPartWayThroughTheLegacyRender_UnlikeInATheme()
    {
        var undefined = (ConsoleColor)99;
        var calendar = new ConsoleCalendar();
        calendar.SetDayOfWeekColor(undefined);

        Captured legacy = ConsoleCapture.TryRun(() => calendar.GenerateMonth(2, 2023));

        Assert.That(legacy.Exception, Is.TypeOf<ArgumentException>());
        Assert.That(legacy.Text, Is.EqualTo("   February 2023\n"), "the title was already written");
        Assert.That(() => CalendarTheme.None.WithWeekdayLabelColor(undefined), Throws.TypeOf<ArgumentOutOfRangeException>());
    }

    // ---- line endings and markers ----

    [Test]
    public void TheLegacyClass_EndsEveryLineWithTheWritersTerminator_LikeTheModernWriters_AndAddsABlankLine()
    {
        Captured legacy = ConsoleCapture.Run(() => new ConsoleCalendar().GenerateMonth(2, 2023), newLine: "\r\n");
        Captured modern = ConsoleCapture.Run(
            () => ConsoleMonthWriter.Write(MonthRenderer.Render(2023, 2, MonthRenderOptions.Default), CalendarTheme.None), newLine: "\r\n");

        // Since version 2.0 the legacy class no longer ends its last row with a bare line feed (D-59). What is
        // left of the difference is the blank line it writes after a month, which the modern writers do not.
        Assert.That(legacy.Text, Does.EndWith("27 28\r\n\r\n").Using(StringComparison.Ordinal));
        Assert.That(modern.Text, Does.EndWith("27 28\r\n").Using(StringComparison.Ordinal));
        Assert.That(modern.Text, Does.Not.EndWith("\r\n\r\n"));
        Assert.That(legacy.Text.Replace("\r\n", string.Empty), Ordinal.DoesNotContain("\n"));
        Assert.That(modern.Text.Replace("\r\n", string.Empty), Ordinal.DoesNotContain("\n"));
    }

    /// <summary>A selected date changes color only. The modern API cannot give a day a state without a marker.</summary>
    [Test]
    public void ASelectedDate_StillAddsNothingToTheText()
    {
        var everyDay = Enumerable.Range(1, 28).Select(day => new DateTime(2023, 2, day)).ToList();

        string plain = ConsoleCapture.Run(() => new ConsoleCalendar().GenerateMonth(2, 2023)).Text;
        string selected = ConsoleCapture.Run(() => new ConsoleCalendar().GenerateMonth(2, 2023, everyDay)).Text;

        Assert.That(selected, Is.EqualTo(plain));
    }

    /// <summary>
    /// The title names the Gregorian month that is drawn, in the culture's language, also where the culture's
    /// own calendar is not the Gregorian one (D-61). That is the name the modern renderer writes for the same
    /// month. The legacy class finds it by itself; it does not use the renderer.
    /// </summary>
    [Test]
    public void UnderANonGregorianCulture_TheLegacyTitleNamesTheGregorianMonth_AsTheModernTitleDoes()
    {
        CultureInfo? persian = TestCultures.TryGet("fa-IR");
        Assume.That(persian, Is.Not.Null, "The culture fa-IR is not available on this system.");
        Assume.That(persian!.DateTimeFormat.Calendar, Is.InstanceOf<PersianCalendar>());

        for (int month = 1; month <= 12; month++)
        {
            string legacy = ConsoleCapture.Run(() => new ConsoleCalendar().GenerateMonth(month, 2023), culture: persian).Text.Split('\n')[0];
            string modern = MonthRenderer.Render(2023, month, MonthRenderOptions.Default.WithCulture(persian).WithTitleFormat("MMMM")).Lines[0].Text.Trim();

            Assert.That(legacy.Trim(), Is.EqualTo(modern + " 2023"), $"month {month}: the Persian name of the Gregorian month, with the Gregorian year");
            Assert.That(modern, Is.Not.EqualTo(persian.DateTimeFormat.GetMonthName(month)), $"month {month}: not the month of that number in the Persian calendar");
        }
    }

    // ---- no lock ----

    /// <summary>
    /// The modern console writer takes a lock, so that one month is written whole before another begins. The
    /// legacy class takes none, and must not start to: here one thread is stopped in the middle of drawing a
    /// month, after it has set the title color and before it writes the title, and a second thread then draws a
    /// complete month. With a lock around a month the second thread could not finish while the first is stopped.
    /// </summary>
    [Test]
    public void TheLegacyClass_StillTakesNoLock_ASecondThreadDrawsWhileAFirstIsInTheMiddle()
    {
        using var firstIsInside = new ManualResetEventSlim();
        using var secondIsDone = new ManualResetEventSlim();
        bool secondFinishedWhileFirstWasInside = false;
        Exception? failure = null;

        Captured captured = ConsoleCapture.Run(() =>
        {
            var first = new Thread(() =>
            {
                try
                {
                    // The current culture is per thread. This one stops the thread when the legacy class asks it
                    // for the month name, which is in the middle of drawing.
                    CultureInfo.CurrentCulture = new StoppingCulture(() =>
                    {
                        firstIsInside.Set();
                        secondFinishedWhileFirstWasInside = secondIsDone.Wait(TimeSpan.FromSeconds(30));
                    });
                    new ConsoleCalendar().GenerateMonth(1, 2023);
                }
                catch (Exception exception)
                {
                    failure = exception;
                }
            });
            var second = new Thread(() =>
            {
                try
                {
                    CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
                    firstIsInside.Wait(TimeSpan.FromSeconds(30));
                    new ConsoleCalendar().GenerateMonth(2, 2023);
                    secondIsDone.Set();
                }
                catch (Exception exception)
                {
                    failure = exception;
                }
            });
            first.Start();
            second.Start();
            first.Join();
            second.Join();
        });

        Assert.That(failure, Is.Null);
        Assert.That(secondFinishedWhileFirstWasInside, Is.True, "the second month was drawn while the first was being drawn");
        Assert.That(captured.Text, Does.StartWith("   February 2023\n").Using(StringComparison.Ordinal), "February's text arrived before January's title");
        Assert.That(captured.Text, Ordinal.Contains("    January 2023\n"));
    }

    /// <summary>The invariant culture, except that asking for its date format runs an action first, once.</summary>
    private sealed class StoppingCulture : CultureInfo
    {
        private Action? _whenAsked;

        public StoppingCulture(Action whenAsked)
            : base(string.Empty)
        {
            _whenAsked = whenAsked;
        }

        public override DateTimeFormatInfo DateTimeFormat
        {
            get
            {
                Action? action = Interlocked.Exchange(ref _whenAsked, null);
                action?.Invoke();
                return base.DateTimeFormat;
            }

            set => base.DateTimeFormat = value;
        }
    }
}
