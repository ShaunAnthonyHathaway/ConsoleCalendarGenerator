namespace ConsoleCalendarGenerator.Tests.Support;

/// <summary>
/// The files of the repository these tests were built in, for the tests that check the documentation and the
/// example against each other. Paths are relative to the repository root and use "/".
/// </summary>
internal static class Repository
{
    /// <summary>The documents written for users of the package and for contributors.</summary>
    public static readonly string[] PublicDocuments = { "README.md", "CHANGELOG.md", "CONTRIBUTING.md", "SECURITY.md" };

    /// <summary>Folders that hold build output or Git's own files, never anything written for this repository.</summary>
    private static readonly string[] SkippedFolders = { ".git", ".vs", "artifacts", "bin", "obj", "TestResults" };

    private static readonly Lazy<string> RootFolder = new(FindRoot);

    public static string Root => RootFolder.Value;

    public static string PathOf(string relativePath) => Path.Combine(Root, relativePath.Replace('/', Path.DirectorySeparatorChar));

    public static bool HasFile(string relativePath) => File.Exists(PathOf(relativePath));

    public static bool HasFolder(string relativePath) => Directory.Exists(PathOf(relativePath));

    /// <summary>The text of a file with every line ending as "\n", whatever the checkout made of them.</summary>
    public static string Read(string relativePath) =>
        File.ReadAllText(PathOf(relativePath)).Replace("\r\n", "\n", StringComparison.Ordinal);

    /// <summary>Every file with one of the extensions (such as ".md"), outside build output, in ordinal order.</summary>
    public static IReadOnlyList<string> FilesWithExtension(params string[] extensions)
    {
        var found = new List<string>();
        Collect(Root, string.Empty, extensions, found);
        found.Sort(StringComparer.Ordinal);
        return found;
    }

    private static void Collect(string folder, string relativeFolder, string[] extensions, List<string> found)
    {
        foreach (string file in Directory.GetFiles(folder))
        {
            if (extensions.Contains(Path.GetExtension(file), StringComparer.OrdinalIgnoreCase))
            {
                found.Add(relativeFolder + Path.GetFileName(file));
            }
        }
        foreach (string subfolder in Directory.GetDirectories(folder))
        {
            string name = Path.GetFileName(subfolder);
            if (!SkippedFolders.Contains(name, StringComparer.OrdinalIgnoreCase))
            {
                Collect(subfolder, relativeFolder + name + "/", extensions, found);
            }
        }
    }

    private static string FindRoot()
    {
        for (DirectoryInfo? folder = new(AppContext.BaseDirectory); folder != null; folder = folder.Parent)
        {
            if (File.Exists(Path.Combine(folder.FullName, "ConsoleCalendarGenerator.sln")))
            {
                return folder.FullName;
            }
        }
        throw new InvalidOperationException(
            "ConsoleCalendarGenerator.sln was not found above " + AppContext.BaseDirectory
            + ". The documentation tests read the repository's files and must run from a build inside it.");
    }
}
