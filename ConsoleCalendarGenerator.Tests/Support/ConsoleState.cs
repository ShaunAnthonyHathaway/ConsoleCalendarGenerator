namespace ConsoleCalendarGenerator.Tests.Support;

/// <summary>Facts about the process-wide console state that the tests rely on.</summary>
internal static class ConsoleState
{
    /// <summary>
    /// What <see cref="Console.ForegroundColor"/> and <see cref="Console.BackgroundColor"/> report on Linux and
    /// macOS for a color that was never set or has been reset.
    /// </summary>
    public const ConsoleColor Unset = (ConsoleColor)(-1);

    /// <summary>
    /// Why the color-sequence tests do not run on Windows. On Linux and macOS the runtime keeps the current
    /// colors in the process, so a test can read them back whether or not a terminal is attached. On Windows the
    /// colors live in the console itself, and a test host without a console ignores every change.
    /// </summary>
    public const string ColorsObservableOnUnixOnly =
        "Console colors can only be read back without a terminal on Linux and macOS.";
}

/// <summary>NUnit categories used by this project.</summary>
internal static class Categories
{
    /// <summary>
    /// Tests that need a terminal database or a pseudo-terminal. They are kept out of the normal run with
    /// <c>--filter "TestCategory!=Terminal"</c> and run on their own with <c>--filter "TestCategory=Terminal"</c>.
    /// </summary>
    public const string Terminal = "Terminal";
}
