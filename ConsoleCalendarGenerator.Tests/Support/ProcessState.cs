using System.Collections;
using System.Globalization;

namespace ConsoleCalendarGenerator.Tests.Support;

/// <summary>The process-wide state that pure code must leave alone.</summary>
internal static class ProcessState
{
    /// <summary>
    /// The current cultures, the default cultures for new threads, the console writers, the current directory
    /// and the environment variables. Two snapshots are equal when none of these changed in between.
    /// </summary>
    public static object[] Snapshot() => new object[]
    {
        CultureInfo.CurrentCulture,
        CultureInfo.CurrentUICulture,
        CultureInfo.DefaultThreadCurrentCulture ?? (object)"none",
        CultureInfo.DefaultThreadCurrentUICulture ?? (object)"none",
        Console.Out,
        Console.Error,
        Environment.CurrentDirectory,
        string.Join("\n", Environment.GetEnvironmentVariables().Cast<DictionaryEntry>()
            .Select(variable => variable.Key + "=" + variable.Value)
            .OrderBy(line => line, StringComparer.Ordinal)),
    };

    /// <summary>Runs <paramref name="action"/> with the given current culture and current UI culture, then puts both back.</summary>
    public static T UnderCultures<T>(CultureInfo culture, CultureInfo uiCulture, Func<T> action)
    {
        CultureInfo originalCulture = CultureInfo.CurrentCulture;
        CultureInfo originalUiCulture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentCulture = culture;
            CultureInfo.CurrentUICulture = uiCulture;
            return action();
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
            CultureInfo.CurrentUICulture = originalUiCulture;
        }
    }
}
