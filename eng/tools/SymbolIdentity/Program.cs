using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography;

// Says which symbol file (portable PDB) an assembly was built with, and checks that a given file is that one.
//
//   SymbolIdentity <assembly>                 prints the identity
//   SymbolIdentity <assembly> <symbol file>   prints it, and fails unless the symbol file is the assembly's
//
// An assembly records two things about its symbols: an identifier, and a checksum of the whole symbol file.
// A symbol server is asked for a file by the identifier; nuget.org's also wants the checksum, and it accepts a
// symbol package only if every symbol file in it matches an assembly of the package this way. So this is the
// check nuget.org makes, made before anything is published, and the key it prints is the address under which
// the published symbols can be fetched back and compared.
//
// Output, one "name=value" a line:
//   pdb-name     the file name the assembly gives its symbols
//   key          the identifier as a symbol server spells it: the GUID in hexadecimal and FFFFFFFF
//   checksum     the checksum the assembly records, as "<algorithm>:<hexadecimal>"
//   server-path  the path of the file on a symbol server, to be appended to the server's address

if (args.Length is < 1 or > 2)
{
    Console.Error.WriteLine("usage: SymbolIdentity <assembly> [<symbol file>]");
    return 64;
}

using var assembly = new PEReader(File.OpenRead(args[0]));
DebugDirectoryEntry[] entries = assembly.ReadDebugDirectory().ToArray();
DebugDirectoryEntry[] codeView = entries.Where(entry => entry.Type == DebugDirectoryEntryType.CodeView).ToArray();
DebugDirectoryEntry[] checksums = entries.Where(entry => entry.Type == DebugDirectoryEntryType.PdbChecksum).ToArray();
if (codeView.Length != 1 || !codeView[0].IsPortableCodeView)
{
    Console.Error.WriteLine("error: " + args[0] + " does not name exactly one portable symbol file");
    return 1;
}
if (checksums.Length != 1)
{
    Console.Error.WriteLine("error: " + args[0] + " does not record exactly one checksum of its symbol file");
    return 1;
}

CodeViewDebugDirectoryData identity = assembly.ReadCodeViewDebugDirectoryData(codeView[0]);
PdbChecksumDebugDirectoryData recorded = assembly.ReadPdbChecksumDebugDirectoryData(checksums[0]);
string name = Path.GetFileName(identity.Path.Replace('\\', '/'));
string key = identity.Guid.ToString("N") + "FFFFFFFF";
string checksum = recorded.AlgorithmName + ":" + Convert.ToHexString(recorded.Checksum.ToArray()).ToLowerInvariant();
Console.WriteLine("pdb-name=" + name);
Console.WriteLine("key=" + key);
Console.WriteLine("checksum=" + checksum);
Console.WriteLine("server-path=" + name.ToLowerInvariant() + "/" + key + "/" + name.ToLowerInvariant());

if (args.Length == 1)
{
    return 0;
}

// The symbol file carries the same identifier: a GUID and the stamp of the assembly's entry. Its checksum is
// taken over the whole file with the identifier set to zero, which is how the compiler computes it.
byte[] symbols = File.ReadAllBytes(args[1]);
using MetadataReaderProvider provider = MetadataReaderProvider.FromPortablePdbImage(System.Collections.Immutable.ImmutableArray.Create(symbols));
DebugMetadataHeader? header = provider.GetMetadataReader().DebugMetadataHeader;
if (header == null)
{
    Console.Error.WriteLine("error: " + args[1] + " is not a portable symbol file");
    return 1;
}
byte[] id = header.Id.ToArray();
var guid = new Guid(id.AsSpan(0, 16));
uint stamp = BitConverter.ToUInt32(id, 16);
if (guid != identity.Guid || stamp != codeView[0].Stamp)
{
    Console.Error.WriteLine("error: " + args[1] + " is not the symbol file of " + args[0] + ": the identifiers differ");
    return 1;
}

Array.Clear(symbols, header.IdStartOffset, id.Length);
byte[] actual = recorded.AlgorithmName.ToUpperInvariant() switch
{
    "SHA256" => SHA256.HashData(symbols),
    "SHA384" => SHA384.HashData(symbols),
    "SHA512" => SHA512.HashData(symbols),
    _ => Array.Empty<byte>(),
};
if (actual.Length == 0)
{
    Console.Error.WriteLine("error: the checksum of the symbol file is recorded with " + recorded.AlgorithmName + ", which this tool does not compute");
    return 1;
}
if (!actual.AsSpan().SequenceEqual(recorded.Checksum.AsSpan()))
{
    Console.Error.WriteLine("error: " + args[1] + " does not have the checksum that " + args[0] + " records for its symbol file");
    return 1;
}
Console.WriteLine("match=the symbol file is the one the assembly was built with");
return 0;
