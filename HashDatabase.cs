using System;
using System.IO;
using System.IO.Compression;
using System.Reflection;

internal static class HashDatabase
{
    private const int DigestLength = 32;
    private static readonly byte[] Entries = Load();

    internal static void Initialize()
    {
        if (Entries.Length == 0) throw new InvalidDataException("Embedded malware hash database is empty.");
    }

    internal static bool Contains(string hexDigest)
    {
        if (hexDigest == null || hexDigest.Length != DigestLength * 2) return false;
        for (int i = 0; i < hexDigest.Length; i++)
            if (HexNibble(hexDigest[i]) < 0) return false;

        int low = 0;
        int high = Entries.Length / DigestLength - 1;
        while (low <= high)
        {
            int middle = low + ((high - low) / 2);
            int comparison = Compare(hexDigest, middle * DigestLength);
            if (comparison == 0) return true;
            if (comparison < 0) high = middle - 1;
            else low = middle + 1;
        }
        return false;
    }

    private static byte[] Load()
    {
        Assembly assembly = Assembly.GetExecutingAssembly();
        using (Stream resource = assembly.GetManifestResourceStream("SPLINT.HASH.bin.gz"))
        {
            if (resource == null) throw new FileNotFoundException("Embedded malware hash database is missing.");
            using (var compressed = new GZipStream(resource, CompressionMode.Decompress))
            using (var expanded = new MemoryStream())
            {
                compressed.CopyTo(expanded);
                byte[] entries = expanded.ToArray();
                if (entries.Length == 0 || entries.Length % DigestLength != 0)
                    throw new InvalidDataException("Embedded malware hash database has an invalid length.");
                ValidateSorted(entries);
                return entries;
            }
        }
    }

    private static int Compare(string hexDigest, int offset)
    {
        for (int i = 0; i < DigestLength; i++)
        {
            int value = (HexNibble(hexDigest[i * 2]) << 4) | HexNibble(hexDigest[i * 2 + 1]);
            int entry = Entries[offset + i];
            if (value < entry) return -1;
            if (value > entry) return 1;
        }
        return 0;
    }

    private static int HexNibble(char value)
    {
        if (value >= '0' && value <= '9') return value - '0';
        if (value >= 'A' && value <= 'F') return value - 'A' + 10;
        if (value >= 'a' && value <= 'f') return value - 'a' + 10;
        return -1;
    }

    private static void ValidateSorted(byte[] entries)
    {
        for (int offset = DigestLength; offset < entries.Length; offset += DigestLength)
        {
            int previous = offset - DigestLength;
            for (int i = 0; i < DigestLength; i++)
            {
                if (entries[previous + i] == entries[offset + i]) continue;
                if (entries[previous + i] > entries[offset + i])
                    throw new InvalidDataException("Embedded malware hash database is not sorted.");
                break;
            }
        }
    }
}
