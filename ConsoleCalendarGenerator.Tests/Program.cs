using System.Globalization;
using ConsoleCalendarGenerator.Tests.Support;

namespace ConsoleCalendarGenerator.Tests;

/// <summary>
/// Entry point used only when the tests start this assembly as a child process (Support/ChildProcess.cs). It
/// renders one fixed calendar to the real console streams, which a test in the same process cannot observe.
/// </summary>
internal static class Program
{
    internal const string RenderCommand = "render-child-scenario";

    private static int Main(string[] args)
    {
        if (args.Length == 1 && args[0] == RenderCommand)
        {
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            ChildScenario.Render();
            return 0;
        }

        Console.Error.WriteLine("This is a test assembly. Run it with 'dotnet test'.");
        return 64;
    }
}
