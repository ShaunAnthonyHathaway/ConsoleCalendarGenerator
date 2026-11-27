using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace ConsoleCalendarGenerator.Tests.Support;

internal sealed record ChildResult(int ExitCode, string StandardOutput, string StandardError);

/// <summary>
/// Starts this test assembly as a separate process that renders <see cref="ChildScenario"/>, or on request
/// <see cref="ModernChildScenario"/>, to its real console streams, so that the tests can look at exactly what
/// reaches standard output.
/// </summary>
internal static class ChildProcess
{
    private const string AnsiRedirectionVariable = "DOTNET_SYSTEM_CONSOLE_ALLOW_ANSI_COLOR_REDIRECTION";
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(60);

    /// <summary>Path of the util-linux <c>script</c> program, which runs a command on a pseudo-terminal, or null.</summary>
    public static string? ScriptProgram =>
        new[] { "/usr/bin/script", "/bin/script" }.FirstOrDefault(File.Exists);

    /// <summary>Standard output redirected to a pipe, which is how most programs capture console output.</summary>
    /// <param name="allowAnsiWhenRedirected">Sets the runtime's opt-in for color sequences in redirected output.</param>
    /// <param name="command">Which scenario the child process draws; see Program.cs.</param>
    public static ChildResult RunRedirected(bool allowAnsiWhenRedirected = false, string command = Program.RenderCommand)
    {
        ProcessStartInfo start = NewStartInfo(DotNetHost());
        start.ArgumentList.Add(TestAssembly());
        start.ArgumentList.Add(command);
        start.Environment.Remove("NO_COLOR");
        if (allowAnsiWhenRedirected)
        {
            start.Environment[AnsiRedirectionVariable] = "1";
            start.Environment["TERM"] = "xterm-256color";
        }
        else
        {
            start.Environment.Remove(AnsiRedirectionVariable);
        }
        return Run(start);
    }

    /// <summary>The same process started without the render command, which must draw nothing.</summary>
    public static ChildResult RunWithoutCommand()
    {
        ProcessStartInfo start = NewStartInfo(DotNetHost());
        start.ArgumentList.Add(TestAssembly());
        return Run(start);
    }

    /// <summary>Standard output connected to a pseudo-terminal created by <c>script</c> (Linux).</summary>
    public static ChildResult RunOnPseudoTerminal(bool noColor, string command = Program.RenderCommand)
    {
        ProcessStartInfo start = NewStartInfo(ScriptProgram ?? throw new InvalidOperationException("script is not installed."));
        start.ArgumentList.Add("-qec");
        start.ArgumentList.Add($"'{DotNetHost()}' '{TestAssembly()}' {command}");
        start.ArgumentList.Add("/dev/null");
        start.Environment["TERM"] = "xterm-256color";
        start.Environment.Remove(AnsiRedirectionVariable);
        if (noColor)
        {
            start.Environment["NO_COLOR"] = "1";
        }
        else
        {
            start.Environment.Remove("NO_COLOR");
        }
        return Run(start);
    }

    /// <summary>
    /// Another program of this repository, started with no arguments. Its standard input is closed at once, so
    /// a program that waits for input fails instead of hanging.
    /// </summary>
    /// <param name="assemblyPath">The program's main assembly.</param>
    /// <param name="onPseudoTerminal">Connects standard output to a pseudo-terminal created by <c>script</c> (Linux).</param>
    /// <param name="environment">Variables to set for the program.</param>
    public static ChildResult RunProgram(string assemblyPath, bool onPseudoTerminal = false, IReadOnlyDictionary<string, string>? environment = null)
    {
        ProcessStartInfo start;
        if (onPseudoTerminal)
        {
            start = NewStartInfo(ScriptProgram ?? throw new InvalidOperationException("script is not installed."));
            start.ArgumentList.Add("-qec");
            start.ArgumentList.Add($"'{DotNetHost()}' '{assemblyPath}'");
            start.ArgumentList.Add("/dev/null");
            start.Environment["TERM"] = "xterm-256color";
        }
        else
        {
            start = NewStartInfo(DotNetHost());
            start.ArgumentList.Add(assemblyPath);
        }
        start.Environment.Remove("NO_COLOR");
        start.Environment.Remove(AnsiRedirectionVariable);
        foreach (KeyValuePair<string, string> variable in environment ?? new Dictionary<string, string>())
        {
            start.Environment[variable.Key] = variable.Value;
        }
        return Run(start);
    }

    private static ProcessStartInfo NewStartInfo(string fileName) => new(fileName)
    {
        UseShellExecute = false,
        CreateNoWindow = true,
        RedirectStandardInput = true,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        StandardOutputEncoding = Encoding.UTF8,
        StandardErrorEncoding = Encoding.UTF8,
    };

    private static ChildResult Run(ProcessStartInfo start)
    {
        using Process process = Process.Start(start) ?? throw new InvalidOperationException("The child process did not start.");
        process.StandardInput.Close();
        Task<string> output = process.StandardOutput.ReadToEndAsync();
        Task<string> error = process.StandardError.ReadToEndAsync();
        if (!process.WaitForExit((int)Timeout.TotalMilliseconds))
        {
            process.Kill(entireProcessTree: true);
            throw new TimeoutException("The child process did not finish within " + Timeout.TotalSeconds + " seconds.");
        }
        process.WaitForExit();
        return new ChildResult(process.ExitCode, output.GetAwaiter().GetResult(), error.GetAwaiter().GetResult());
    }

    private static string TestAssembly() => typeof(ChildProcess).Assembly.Location;

    /// <summary>The <c>dotnet</c> host of the runtime these tests are running on.</summary>
    private static string DotNetHost()
    {
        string name = OperatingSystem.IsWindows() ? "dotnet.exe" : "dotnet";
        // <root>/shared/Microsoft.NETCore.App/<version>/ is the runtime directory; the host is in <root>.
        string besideRuntime = Path.GetFullPath(Path.Combine(RuntimeEnvironment.GetRuntimeDirectory(), "..", "..", "..", name));
        if (File.Exists(besideRuntime))
        {
            return besideRuntime;
        }
        string? fromSdk = Environment.GetEnvironmentVariable("DOTNET_HOST_PATH");
        return !string.IsNullOrEmpty(fromSdk) && File.Exists(fromSdk) ? fromSdk : name;
    }
}
