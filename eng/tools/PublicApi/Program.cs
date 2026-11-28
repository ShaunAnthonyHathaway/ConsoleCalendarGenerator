using System.Reflection;
using ConsoleCalendarGenerator.ReleaseTools;

// Prints the public API of an assembly, or checks it against a file that states it.
//
//   PublicApi <assembly>                  prints the API, one line for each type and member
//   PublicApi <assembly> <expected file>  fails unless the API is exactly the lines of the file
//
// The assembly is loaded to be read, and none of its code is run. The scripts use this on the two assemblies of
// a package, before it is published and on what nuget.org serves afterwards: both must have the API that
// eng/public-api.txt states, which is the API the tests require of the source.

if (args.Length is < 1 or > 2)
{
    Console.Error.WriteLine("usage: PublicApi <assembly> [<expected file>]");
    return 64;
}

IReadOnlyList<string> actual = PublicApiListing.Of(Assembly.LoadFrom(Path.GetFullPath(args[0])));
if (args.Length == 1)
{
    foreach (string line in actual)
    {
        Console.Out.Write(line + "\n");
    }
    return 0;
}

string[] expected = File.ReadAllText(args[1]).Replace("\r\n", "\n").Split('\n', StringSplitOptions.RemoveEmptyEntries);
string[] missing = expected.Except(actual, StringComparer.Ordinal).ToArray();
string[] added = actual.Except(expected, StringComparer.Ordinal).ToArray();
if (missing.Length == 0 && added.Length == 0 && expected.SequenceEqual(actual, StringComparer.Ordinal))
{
    Console.Out.Write("entries=" + actual.Count + "\n");
    return 0;
}

foreach (string line in missing)
{
    Console.Error.WriteLine("missing from the assembly: " + line.Trim());
}
foreach (string line in added)
{
    Console.Error.WriteLine("in the assembly and not expected: " + line.Trim());
}
if (missing.Length == 0 && added.Length == 0)
{
    Console.Error.WriteLine("the entries are the expected ones, in another order");
}
Console.Error.WriteLine("error: the public API of " + args[0] + " is not the one " + args[1] + " states");
return 1;
