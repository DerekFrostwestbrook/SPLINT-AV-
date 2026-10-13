using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

internal static class AuthenticodeTrust
{
    private const uint UiChoiceNone = 2;
    private const uint RevocationChecksNone = 0;
    private const uint UnionChoiceFile = 1;
    private const uint UnionChoiceCatalog = 2;
    private const uint StateActionIgnore = 0;
    private const uint CacheOnlyUrlRetrieval = 0x00001000;
    private const uint NoSignature = 0x800B0100;
    private const int MaxPath = 260;

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct WinTrustFileInfo
    {
        internal uint StructSize;
        internal IntPtr FilePath;
        internal IntPtr FileHandle;
        internal IntPtr KnownSubject;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct WinTrustCatalogInfo
    {
        internal uint StructSize;
        internal uint CatalogVersion;
        internal IntPtr CatalogPath;
        internal IntPtr MemberTag;
        internal IntPtr MemberPath;
        internal IntPtr MemberFile;
        internal IntPtr CalculatedHash;
        internal uint HashSize;
        internal IntPtr CatalogContext;
        internal IntPtr CatalogAdmin;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct CatalogInfo
    {
        internal uint StructSize;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = MaxPath)]
        internal string CatalogPath;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct WinTrustData
    {
        internal uint StructSize;
        internal IntPtr PolicyCallbackData;
        internal IntPtr SipClientData;
        internal uint UiChoice;
        internal uint RevocationChecks;
        internal uint UnionChoice;
        internal IntPtr FileOrCatalogInfo;
        internal uint StateAction;
        internal IntPtr StateData;
        internal IntPtr UrlReference;
        internal uint ProviderFlags;
        internal uint UiContext;
        internal IntPtr SignatureSettings;
    }

    [DllImport("wintrust.dll", ExactSpelling = true, PreserveSig = true)]
    private static extern uint WinVerifyTrust(IntPtr window, ref Guid action, IntPtr trustData);

    [DllImport("wintrust.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CryptCATAdminAcquireContext2(out IntPtr admin, IntPtr subsystem, string hashAlgorithm, IntPtr strongHashPolicy, uint flags);

    [DllImport("wintrust.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CryptCATAdminReleaseContext(IntPtr admin, uint flags);

    [DllImport("wintrust.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CryptCATAdminCalcHashFromFileHandle2(IntPtr admin, IntPtr file, ref uint hashSize, IntPtr hash, uint flags);

    [DllImport("wintrust.dll", SetLastError = true)]
    private static extern IntPtr CryptCATAdminEnumCatalogFromHash(IntPtr admin, IntPtr hash, uint hashSize, uint flags, ref IntPtr previousCatalog);

    [DllImport("wintrust.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CryptCATCatalogInfoFromContext(IntPtr catalog, IntPtr catalogInfo, uint flags);

    [DllImport("wintrust.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CryptCATAdminReleaseCatalogContext(IntPtr admin, IntPtr catalog, uint flags);

    internal static bool IsTrusted(string path)
    {
        if (String.IsNullOrEmpty(path) || !File.Exists(path)) return false;

        uint fileStatus;
        try { fileStatus = VerifyEmbeddedSignature(path); }
        catch (DllNotFoundException) { return false; }
        catch (EntryPointNotFoundException) { return false; }
        catch (IOException) { return false; }
        catch (UnauthorizedAccessException) { return false; }

        if (fileStatus == 0) return true;
        if (fileStatus != NoSignature) return false;

        try { return VerifyCatalogSignature(path); }
        catch (DllNotFoundException) { return false; }
        catch (EntryPointNotFoundException) { return false; }
        catch (IOException) { return false; }
        catch (UnauthorizedAccessException) { return false; }
    }

    private static uint VerifyEmbeddedSignature(string path)
    {
        IntPtr pathPointer = IntPtr.Zero;
        IntPtr fileInfoPointer = IntPtr.Zero;
        IntPtr trustDataPointer = IntPtr.Zero;
        try
        {
            pathPointer = Marshal.StringToCoTaskMemUni(path);
            var fileInfo = new WinTrustFileInfo
            {
                StructSize = (uint)Marshal.SizeOf(typeof(WinTrustFileInfo)),
                FilePath = pathPointer
            };
            fileInfoPointer = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(WinTrustFileInfo)));
            Marshal.StructureToPtr(fileInfo, fileInfoPointer, false);
            trustDataPointer = CreateTrustData(UnionChoiceFile, fileInfoPointer);
            Guid action = new Guid("00AAC56B-CD44-11D0-8CC2-00C04FC295EE");
            return WinVerifyTrust(IntPtr.Zero, ref action, trustDataPointer);
        }
        finally
        {
            if (trustDataPointer != IntPtr.Zero) Marshal.FreeHGlobal(trustDataPointer);
            if (fileInfoPointer != IntPtr.Zero) Marshal.FreeHGlobal(fileInfoPointer);
            if (pathPointer != IntPtr.Zero) Marshal.FreeCoTaskMem(pathPointer);
        }
    }

    private static bool VerifyCatalogSignature(string path)
    {
        IntPtr admin = IntPtr.Zero;
        IntPtr hashPointer = IntPtr.Zero;
        var catalogs = new List<IntPtr>();
        try
        {
            if (!CryptCATAdminAcquireContext2(out admin, IntPtr.Zero, "SHA256", IntPtr.Zero, 0)) return false;

            using (FileStream file = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
            {
                SafeFileHandle safeHandle = file.SafeFileHandle;
                uint hashSize = 0;
                if (!CryptCATAdminCalcHashFromFileHandle2(admin, safeHandle.DangerousGetHandle(), ref hashSize, IntPtr.Zero, 0) || hashSize == 0)
                    return false;

                hashPointer = Marshal.AllocHGlobal((int)hashSize);
                if (!CryptCATAdminCalcHashFromFileHandle2(admin, safeHandle.DangerousGetHandle(), ref hashSize, hashPointer, 0))
                    return false;

                byte[] hash = new byte[hashSize];
                Marshal.Copy(hashPointer, hash, 0, hash.Length);
                string memberTag = BitConverter.ToString(hash).Replace("-", "");
                IntPtr previous = IntPtr.Zero;
                IntPtr catalog;
                while ((catalog = CryptCATAdminEnumCatalogFromHash(admin, hashPointer, hashSize, 0, ref previous)) != IntPtr.Zero)
                {
                    catalogs.Add(catalog);
                    if (VerifyCatalog(admin, catalog, path, memberTag, safeHandle.DangerousGetHandle(), hashPointer, hashSize))
                        return true;
                    previous = catalog;
                }
            }
            return false;
        }
        finally
        {
            foreach (IntPtr catalog in catalogs)
                CryptCATAdminReleaseCatalogContext(admin, catalog, 0);
            if (hashPointer != IntPtr.Zero) Marshal.FreeHGlobal(hashPointer);
            if (admin != IntPtr.Zero) CryptCATAdminReleaseContext(admin, 0);
        }
    }

    private static bool VerifyCatalog(IntPtr admin, IntPtr catalog, string path, string memberTag, IntPtr memberFile, IntPtr hash, uint hashSize)
    {
        IntPtr catalogInfoPointer = IntPtr.Zero;
        IntPtr catalogPathPointer = IntPtr.Zero;
        IntPtr memberTagPointer = IntPtr.Zero;
        IntPtr memberPathPointer = IntPtr.Zero;
        IntPtr trustDataPointer = IntPtr.Zero;
        try
        {
            var catalogInfo = new CatalogInfo { StructSize = (uint)Marshal.SizeOf(typeof(CatalogInfo)) };
            catalogInfoPointer = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(CatalogInfo)));
            Marshal.StructureToPtr(catalogInfo, catalogInfoPointer, false);
            if (!CryptCATCatalogInfoFromContext(catalog, catalogInfoPointer, 0)) return false;
            catalogInfo = (CatalogInfo)Marshal.PtrToStructure(catalogInfoPointer, typeof(CatalogInfo));
            if (String.IsNullOrEmpty(catalogInfo.CatalogPath)) return false;

            catalogPathPointer = Marshal.StringToCoTaskMemUni(catalogInfo.CatalogPath);
            memberTagPointer = Marshal.StringToCoTaskMemUni(memberTag);
            memberPathPointer = Marshal.StringToCoTaskMemUni(path);
            var catalogTrustInfo = new WinTrustCatalogInfo
            {
                StructSize = (uint)Marshal.SizeOf(typeof(WinTrustCatalogInfo)),
                CatalogPath = catalogPathPointer,
                MemberTag = memberTagPointer,
                MemberPath = memberPathPointer,
                MemberFile = memberFile,
                CalculatedHash = hash,
                HashSize = hashSize,
                CatalogAdmin = admin
            };
            IntPtr catalogTrustInfoPointer = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(WinTrustCatalogInfo)));
            try
            {
                Marshal.StructureToPtr(catalogTrustInfo, catalogTrustInfoPointer, false);
                trustDataPointer = CreateTrustData(UnionChoiceCatalog, catalogTrustInfoPointer);
                Guid action = new Guid("00AAC56B-CD44-11D0-8CC2-00C04FC295EE");
                return WinVerifyTrust(IntPtr.Zero, ref action, trustDataPointer) == 0;
            }
            finally
            {
                Marshal.FreeHGlobal(catalogTrustInfoPointer);
            }
        }
        finally
        {
            if (trustDataPointer != IntPtr.Zero) Marshal.FreeHGlobal(trustDataPointer);
            if (catalogInfoPointer != IntPtr.Zero) Marshal.FreeHGlobal(catalogInfoPointer);
            if (catalogPathPointer != IntPtr.Zero) Marshal.FreeCoTaskMem(catalogPathPointer);
            if (memberTagPointer != IntPtr.Zero) Marshal.FreeCoTaskMem(memberTagPointer);
            if (memberPathPointer != IntPtr.Zero) Marshal.FreeCoTaskMem(memberPathPointer);
        }
    }

    private static IntPtr CreateTrustData(uint unionChoice, IntPtr info)
    {
        var trustData = new WinTrustData
        {
            StructSize = (uint)Marshal.SizeOf(typeof(WinTrustData)),
            UiChoice = UiChoiceNone,
            RevocationChecks = RevocationChecksNone,
            UnionChoice = unionChoice,
            FileOrCatalogInfo = info,
            StateAction = StateActionIgnore,
            ProviderFlags = CacheOnlyUrlRetrieval
        };
        IntPtr pointer = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(WinTrustData)));
        Marshal.StructureToPtr(trustData, pointer, false);
        return pointer;
    }
}
