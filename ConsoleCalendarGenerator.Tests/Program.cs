using System.Globalization;
using ConsoleCalendarGenerator.Tests.Support;

namespace ConsoleCalendarGenerator.Tests;

/// <summary>
/// Entry point used only when the tests start this assembly as a child process (Support/ChildProcess.cs). It
/// draws one fixed calendar to the real console streams, which a test in the same process cannot observe: with
/// the legacy class, with the modern console writer, or with the frozen source of the legacy class as it was
/// in version 1.0.0.
/// </summary>
internal static class Program
{
    internal const string RenderCommand = "render-child-scenario";
    internal const string WriteModernCommand = "write-modern-child-scenario";
    internal const string RenderPublished100Command = "render-child-scenario-with-published-100";

    private static int Main(string[] args)
    {
        if (args.Length == 1 && args[0] == RenderCommand)
        {
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            ChildScenario.Render();
            return 0;
        }
        if (args.Length == 1 && args[0] == WriteModernCommand)
        {
            ModernChildScenario.Write();
            return 0;
        }
        if (args.Length == 1 && args[0] == RenderPublished100Command)
        {
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            ChildScenario.RenderWithPublished100();
            return 0;
        }

        Console.Error.WriteLine("This is a test assembly. Run it with 'dotnet test'.");
        return 64;
    }
}
