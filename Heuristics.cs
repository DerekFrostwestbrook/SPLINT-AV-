using System;
using System.Collections.Generic;
using System.IO;


internal enum Cat { Threat, Spyware, Heuristic, HashMatch }

internal sealed class HeuristicResult
{
    internal bool Detected;
    internal string Detail;
}

internal static class HeuristicAnalysis
{
    private static readonly HashSet<string> CandidateExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    { ".exe", ".dll", ".scr", ".sys", ".com", ".bat", ".cmd", ".vbs", ".vbe", ".js", ".jse", ".wsf", ".ps1", ".hta", ".jar" };

    internal static HeuristicResult Score(FileInfo info, bool? isPe = null, bool? signedResult = null,
        bool persistent = false, Func<string[], bool> has = null, bool advanced = false, bool maxTier = false)
    {
        if (info == null) return new HeuristicResult { Detected = false };

        int score = 0;
        var reasons = new List<string>();
        string lower = info.FullName.ToLowerInvariant();
        bool userWritable = lower.Contains("\\appdata\\") || lower.Contains("\\temp\\") || lower.Contains("\\downloads\\") ||
                            lower.Contains("\\programdata\\") || lower.Contains("\\users\\public\\") || lower.Contains("\\$recycle.bin\\");
        bool inStartup = lower.Contains("\\start menu\\programs\\startup\\");
        bool hidden = (info.Attributes & FileAttributes.Hidden) != 0;
        bool pe = isPe ?? IsPortableExecutable(info);
        bool signed = signedResult ?? IsTrustedSigned(info);

        if (userWritable && !signed) { score += 20; reasons.Add("Unsigned in user dir"); }
        if (inStartup || persistent) { score += 15; reasons.Add("Startup persistence"); }
        if (hidden) { score += 10; reasons.Add("Hidden attribute"); }

        if (pe)
        {
            double entropy = GetEntropy(info.FullName);
            if (entropy > 7.4) { score += 25; reasons.Add(string.Format("High entropy ({0:F1})", entropy)); }
        }

        string company = "";
        try { company = (System.Diagnostics.FileVersionInfo.GetVersionInfo(info.FullName).CompanyName ?? "").ToLowerInvariant(); } catch { }
        if (company.Contains("microsoft") || company.Contains("google") || company.Contains("mozilla") ||
            company.Contains("valve") || company.Contains("adobe") || company.Contains("apple") ||
            company.Contains("intel") || company.Contains("amd") || company.Contains("nvidia") || company.Contains("electronic arts"))
            score -= 50;

        if (lower.Contains("node_modules") || lower.Contains("electron") || lower.Contains("unityengine") || lower.Contains("epic games"))
            score -= 40;

        if (advanced && has != null && has(new[] { "virtualalloc", "createremotethread", "writeprocessmemory", "setwindowshookex" }))
        {
            score += 20;
            reasons.Add("Process injection/hook APIs");
        }

        if (score >= 65)
            return new HeuristicResult { Detected = true, Detail = "Score " + score + ": " + string.Join(", ", reasons.ToArray()) };
        return new HeuristicResult { Detected = false };
    }

    internal static bool IsCandidate(FileInfo file)
    {
        return file != null && CandidateExtensions.Contains(file.Extension);
    }

    internal static bool IsPortableExecutable(FileInfo file)
    {
        if (file == null) return false;
        try
        {
            using (var stream = File.OpenRead(file.FullName))
            {
                var header = new byte[64];
                if (stream.Read(header, 0, header.Length) != header.Length || header[0] != 'M' || header[1] != 'Z') return false;
                int offset = BitConverter.ToInt32(header, 60);
                if (offset <= 0 || offset >= stream.Length - 4) return false;
                stream.Position = offset;
                var signature = new byte[4];
                return stream.Read(signature, 0, signature.Length) == signature.Length &&
                       signature[0] == 'P' && signature[1] == 'E' && signature[2] == 0 && signature[3] == 0;
            }
        }
        catch { return false; }
    }

    internal static bool IsTrustedSigned(FileInfo file)
    {
        if (file == null) return false;
        try { return AuthenticodeTrust.IsTrusted(file.FullName); }
        catch { return false; }
    }

    private static double GetEntropy(string path)
    {
        try
        {
            byte[] bytes = new byte[16384];
            int length;
            using (var stream = File.OpenRead(path)) length = stream.Read(bytes, 0, bytes.Length);
            if (length < 1024) return 0;

            int[] counts = new int[256];
            for (int i = 0; i < length; i++) counts[bytes[i]]++;
            double entropy = 0;
            for (int i = 0; i < counts.Length; i++)
            {
                if (counts[i] == 0) continue;
                double probability = (double)counts[i] / length;
                entropy -= probability * Math.Log(probability, 2);
            }
            return entropy;
        }
        catch { return 0; }
    }
}
