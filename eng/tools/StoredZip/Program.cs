using System.Text;

// Writes a ZIP archive that is the same, byte for byte, whenever it is made from the same files: on any
// operating system, with any version of the runtime, at any time.
//
//   StoredZip <archive to create> <folder whose contents go into it>
//
// What makes it reproducible:
//   - the entries are the files under the folder, in ordinal order of their paths, with "/" as the separator;
//   - there are no entries for folders;
//   - every file is stored, not compressed, so no compressor and no compression level has a say;
//   - every entry has the same fixed time, the earliest a ZIP archive can express, 1980-01-01 00:00:00;
//   - no entry has attributes, an extra field or a comment, and the archive names no operating system.
//
// The .NET class library can write ZIP archives, but it records the operating system it ran on and the file
// permissions of that system in every entry. The format is simple enough to write directly, which is what
// this does: a local header and the bytes of each file, then the central directory, then its end record.
// The archive is for a few small files; it refuses anything the plain format cannot hold.

if (args.Length != 2)
{
    Console.Error.WriteLine("usage: StoredZip <archive to create> <folder whose contents go into it>");
    return 64;
}

string archive = Path.GetFullPath(args[0]);
string folder = Path.GetFullPath(args[1]);
if (!Directory.Exists(folder))
{
    Console.Error.WriteLine("error: there is no folder " + folder);
    return 1;
}
if (File.Exists(archive))
{
    Console.Error.WriteLine("error: " + archive + " exists already");
    return 1;
}

string[] names = Directory.GetFiles(folder, "*", SearchOption.AllDirectories)
    .Select(file => Path.GetRelativePath(folder, file).Replace('\\', '/'))
    .OrderBy(name => name, StringComparer.Ordinal)
    .ToArray();
if (names.Length == 0 || names.Length > ushort.MaxValue)
{
    Console.Error.WriteLine("error: " + folder + " has no files, or too many for this archive format");
    return 1;
}
foreach (string name in names)
{
    if (name.Any(character => character < ' ' || character > '~'))
    {
        Console.Error.WriteLine("error: the name of " + name + " has a character outside printable ASCII");
        return 1;
    }
}

const ushort VersionNeeded = 10;       // 1.0: stored files, no folders, no ZIP64
const ushort FixedTime = 0;            // 00:00:00
const ushort FixedDate = 0x0021;       // 1980-01-01
uint[] crcTable = Enumerable.Range(0, 256).Select(index =>
{
    uint value = (uint)index;
    for (int bit = 0; bit < 8; bit++)
    {
        value = (value & 1) != 0 ? 0xEDB88320u ^ (value >> 1) : value >> 1;
    }
    return value;
}).ToArray();

var centralDirectory = new MemoryStream();
using (var output = new FileStream(archive, FileMode.CreateNew, FileAccess.Write))
using (var writer = new BinaryWriter(output))
using (var directory = new BinaryWriter(centralDirectory))
{
    foreach (string name in names)
    {
        byte[] content = File.ReadAllBytes(Path.Combine(folder, name.Replace('/', Path.DirectorySeparatorChar)));
        byte[] nameBytes = Encoding.ASCII.GetBytes(name);
        uint crc = 0xFFFFFFFFu;
        foreach (byte value in content)
        {
            crc = crcTable[(crc ^ value) & 0xFF] ^ (crc >> 8);
        }
        crc ^= 0xFFFFFFFFu;
        long offset = output.Position;
        if (offset + content.Length > uint.MaxValue - 1_000_000)
        {
            Console.Error.WriteLine("error: the archive would be too large for this format");
            return 1;
        }

        // Local file header.
        writer.Write(0x04034B50u);
        writer.Write(VersionNeeded);
        writer.Write((ushort)0);            // flags
        writer.Write((ushort)0);            // method: stored
        writer.Write(FixedTime);
        writer.Write(FixedDate);
        writer.Write(crc);
        writer.Write((uint)content.Length); // compressed size
        writer.Write((uint)content.Length); // size
        writer.Write((ushort)nameBytes.Length);
        writer.Write((ushort)0);            // extra field length
        writer.Write(nameBytes);
        writer.Write(content);

        // Central directory entry.
        directory.Write(0x02014B50u);
        directory.Write(VersionNeeded);     // made by: version 1.0, system 0, which every reader accepts
        directory.Write(VersionNeeded);
        directory.Write((ushort)0);         // flags
        directory.Write((ushort)0);         // method: stored
        directory.Write(FixedTime);
        directory.Write(FixedDate);
        directory.Write(crc);
        directory.Write((uint)content.Length);
        directory.Write((uint)content.Length);
        directory.Write((ushort)nameBytes.Length);
        directory.Write((ushort)0);         // extra field length
        directory.Write((ushort)0);         // comment length
        directory.Write((ushort)0);         // disk number
        directory.Write((ushort)0);         // internal attributes
        directory.Write(0u);                // external attributes
        directory.Write((uint)offset);
        directory.Write(nameBytes);
    }

    long directoryOffset = output.Position;
    directory.Flush();
    writer.Write(centralDirectory.ToArray());

    // End of central directory.
    writer.Write(0x06054B50u);
    writer.Write((ushort)0);                // this disk
    writer.Write((ushort)0);                // disk with the directory
    writer.Write((ushort)names.Length);
    writer.Write((ushort)names.Length);
    writer.Write((uint)centralDirectory.Length);
    writer.Write((uint)directoryOffset);
    writer.Write((ushort)0);                // comment length
}

Console.WriteLine("wrote " + Path.GetFileName(archive) + ": " + names.Length + " files, " + new FileInfo(archive).Length + " bytes");
return 0;
