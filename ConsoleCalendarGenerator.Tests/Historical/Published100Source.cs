using System.Security.Cryptography;
using System.Text;
using ConsoleCalendarGenerator.Tests.Support;

namespace ConsoleCalendarGenerator.Tests.Historical;

/// <summary>
/// Where the frozen source in Historical/Published100/ConsoleCalendar.cs comes from, and how to check that it
/// is still that source.
///
/// <para>
/// <b>What was published.</b> ConsoleCalendarGenerator 1.0.0 is on nuget.org as
/// ConsoleCalendarGenerator.1.0.0.nupkg, SHA-256 <see cref="PackageSha256"/>. It holds one assembly,
/// lib/netstandard2.1/ConsoleCalendarGenerator.dll, SHA-256 <see cref="AssemblySha256"/>, dated 2023-06-24. The
/// package has no symbols and no repository commit, so nothing in it names its source.
/// </para>
/// <para>
/// <b>Which source that was.</b> The repository has had two versions of ConsoleCalendarGenerator/ConsoleCalendar.cs
/// with the original implementation:
/// </para>
/// <list type="bullet">
/// <item>
/// Git blob <see cref="GitBlob"/>, from commit <see cref="Commit"/> of 2023-06-24 until commit def8a3b of
/// 2023-08-16. It has a private method <c>ConvertToShortDay</c>.
/// </item>
/// <item>
/// Git blob 83c7945867de88a48e393bbfde261693a90c6bd2, from commit def8a3b ("removed unnecessary function",
/// seven weeks after the release) until commit 64995b6, the last one before the class was rebuilt as a facade.
/// It replaces that method with <c>DayOfWeek.ToString().Substring(0, 2)</c>, which gives the same two letters,
/// and changes nothing else.
/// </item>
/// </list>
/// <para>
/// The published assembly contains the method name <c>ConvertToShortDay</c>, so it was built from the first of
/// the two, and that is the one frozen here. The second is the one the digests in Facade/PreFacadeBaselineTests.cs
/// were recorded from. Both were compiled and put through all four recorded digests when this reference was
/// added, and both reproduce all four; Published100DigestTests keeps checking that for the frozen one.
/// </para>
/// <para>
/// <b>License.</b> The source is this repository's own, by the same author and under the same MIT license as
/// everything else here; see LICENSE.
/// </para>
/// </summary>
internal static class Published100Source
{
    /// <summary>The frozen file, relative to the repository root.</summary>
    public const string FrozenFile = "ConsoleCalendarGenerator.Tests/Historical/Published100/ConsoleCalendar.cs";

    /// <summary>The path the source had in the repository.</summary>
    public const string OriginalPath = "ConsoleCalendarGenerator/ConsoleCalendar.cs";

    /// <summary>The commit that added the source. It was the newest commit to the file when 1.0.0 was published.</summary>
    public const string Commit = "93f2d5d32537676b01806b2b6995a5628bd5c7dc";

    /// <summary>The Git object of the source: <c>git cat-file -p</c> of this prints it.</summary>
    public const string GitBlob = "02b3c1cf556e7b25c9b8cbb06ae4505cdb296796";

    /// <summary>The SHA-256 of the bytes of that Git object.</summary>
    public const string Sha256 = "d79695038673e2576b2e439bcb618cec663ced701c5bdbdb0c46b838d477de3e";

    /// <summary>The number of bytes of that Git object.</summary>
    public const int Length = 8906;

    /// <summary>The SHA-256 of ConsoleCalendarGenerator.1.0.0.nupkg as nuget.org serves it.</summary>
    public const string PackageSha256 = "542185beb4cf598cbcec3f45b02c6a234ee39aeefa3ec074a83fa6f54df1608e";

    /// <summary>The SHA-256 of the assembly inside that package.</summary>
    public const string AssemblySha256 = "d0725ab3e9a062f4cc0f8e46126632a779c3ed978beb9dbe5865d4b06b30f360";

    /// <summary>The line of the frozen file after which the source begins.</summary>
    public const string Marker = "// ---- the frozen source of 1.0.0 begins on the next line ----";

    public const string OriginalNamespace = "CalendarGenerator";

    public const string FrozenNamespace = "ConsoleCalendarGenerator.Tests.Historical.Published100";

    /// <summary>The comment in front of the source, up to and including the marker line.</summary>
    public static string Header()
    {
        string frozen = Repository.Read(FrozenFile);
        return frozen.Substring(0, MarkerEnd(frozen));
    }

    /// <summary>
    /// The bytes of the original file, made from the frozen file by undoing the four changes its header lists:
    /// the comment in front, the byte order mark, the namespace and the line feed at the end.
    /// </summary>
    public static byte[] ReconstructOriginal()
    {
        string frozen = Repository.Read(FrozenFile);
        string source = frozen.Substring(MarkerEnd(frozen));
        string frozenNamespaceLine = "namespace " + FrozenNamespace + "\n";
        int namespaceAt = source.IndexOf(frozenNamespaceLine, StringComparison.Ordinal);
        if (namespaceAt < 0 || source.IndexOf(frozenNamespaceLine, namespaceAt + 1, StringComparison.Ordinal) >= 0)
        {
            throw new InvalidOperationException(FrozenFile + " does not declare the namespace " + FrozenNamespace + " exactly once.");
        }
        if (!source.EndsWith('\n'))
        {
            throw new InvalidOperationException(FrozenFile + " does not end with a line feed.");
        }
        string original = source.Substring(0, namespaceAt)
            + "namespace " + OriginalNamespace + "\n"
            + source.Substring(namespaceAt + frozenNamespaceLine.Length, source.Length - namespaceAt - frozenNamespaceLine.Length - 1);
        return Encoding.UTF8.GetPreamble().Concat(new UTF8Encoding(encoderShouldEmitUTF8Identifier: false).GetBytes(original)).ToArray();
    }

    public static string Sha256Of(byte[] bytes) => Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();

    private static int MarkerEnd(string frozen)
    {
        string markerLine = Marker + "\n";
        int markerAt = frozen.IndexOf(markerLine, StringComparison.Ordinal);
        if (markerAt < 0)
        {
            throw new InvalidOperationException(FrozenFile + " has no marker line.");
        }
        return markerAt + markerLine.Length;
    }
}
