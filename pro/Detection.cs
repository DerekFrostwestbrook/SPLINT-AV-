using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using Microsoft.Win32;

internal sealed class FileCtx
{
    internal readonly FileInfo Info;
    internal readonly string Path, Lower, Name;
    internal readonly bool Persistent;
    private string ascii, uni, companyName;
    private int pe = -1, signed = -1;
    private double? entropy;

    internal FileCtx(FileInfo info, bool persistent)
    {
        Info = info; Path = info.FullName; Lower = Path.ToLowerInvariant(); Name = info.Name.ToLowerInvariant();
        Persistent = persistent || Lower.Contains("\\start menu\\programs\\startup\\");
    }

    internal bool InStartup { get { return Lower.Contains("\\start menu\\programs\\startup\\"); } }
    internal bool Hidden { get { return (Info.Attributes & FileAttributes.Hidden) != 0; } }
    internal bool UserWritable
    {
        get
        {
            return Lower.Contains("\\appdata\\") || Lower.Contains("\\temp\\") || Lower.Contains("\\downloads\\") ||
                   Lower.Contains("\\programdata\\") || Lower.Contains("\\users\\public\\") || Lower.Contains("\\$recycle.bin\\");
        }
    }
    internal bool IsScript { get { string e = Info.Extension.ToLowerInvariant(); return e == ".vbs" || e == ".vbe" || e == ".js" || e == ".jse" || e == ".wsf" || e == ".ps1" || e == ".bat" || e == ".cmd" || e == ".hta"; } }

    internal string CompanyName
    {
        get
        {
            if (companyName != null) return companyName;
            try { companyName = FileVersionInfo.GetVersionInfo(Path).CompanyName ?? ""; } catch { companyName = ""; }
            return companyName;
        }
    }

    internal double Entropy
    {
        get
        {
            if (entropy.HasValue) return entropy.Value;
            entropy = FileMetrics.Entropy(Path);
            if (entropy < 0) entropy = 0;
            return entropy.Value;
        }
    }

    internal bool IsPE
    {
        get
        {
            if (pe < 0)
            {
                pe = 0;
                try
                {
                    using (var fs = File.OpenRead(Path))
                    {
                        var h = new byte[64];
                        if (fs.Read(h, 0, 64) == 64 && h[0] == 'M' && h[1] == 'Z')
                        {
                            int off = BitConverter.ToInt32(h, 0x3C);
                            if (off > 0 && off < fs.Length - 4)
                            {
                                fs.Position = off; var s = new byte[4]; fs.Read(s, 0, 4);
                                if (s[0] == 'P' && s[1] == 'E' && s[2] == 0 && s[3] == 0) pe = 1;
                            }
                        }
                    }
                }
                catch { }
            }
            return pe == 1;
        }
    }

    internal bool Signed
    {
        get
        {
            if (signed < 0)
            {
                signed = 0;
                if (AuthenticodeTrust.IsTrusted(Path)) signed = 1;
            }
            return signed == 1;
        }
    }

    internal bool Suspect { get { return IsPE && (Persistent || UserWritable) && !Signed; } }

    internal bool Net
    {
        get { return Has("wininet", "winhttp", "ws2_32", "urlmon", "httpsendrequest", "urldownloadtofile", "api.telegram.org", "discord.com/api/webhooks", "smtp"); }
    }

    internal bool Has(params string[] any)
    {
        if (ascii == null)
        {
            try
            {
                byte[] b;
                using (var fs = File.OpenRead(Path))
                {
                    int len = (int)Math.Min(fs.Length, 8 * 1024 * 1024), r = 0;
                    b = new byte[len];
                    while (r < len) { int k = fs.Read(b, r, len - r); if (k <= 0) break; r += k; }
                }
                ascii = Encoding.GetEncoding(28591).GetString(b).ToLowerInvariant();
                uni = Encoding.Unicode.GetString(b).ToLowerInvariant();
            }
            catch { ascii = ""; uni = ""; }
        }
        foreach (string n in any)
            if (ascii.IndexOf(n, StringComparison.Ordinal) >= 0 || uni.IndexOf(n, StringComparison.Ordinal) >= 0) return true;
        return false;
    }
}

internal static class Detection
{
    private sealed class Rule
    {
        internal string Id, Name; internal Cat Cat; internal Func<FileCtx, bool> Test;
    }

    private static Rule R(string id, Cat cat, string name, Func<FileCtx, bool> test)
    {
        return new Rule { Id = id, Cat = cat, Name = name, Test = test };
    }

    private static readonly HashSet<string> Exec = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    { ".exe", ".dll", ".scr", ".sys", ".com", ".bat", ".cmd", ".vbs", ".vbe", ".js", ".jse", ".wsf", ".ps1", ".hta", ".jar" };

    private static readonly HashSet<string> SystemNames = new HashSet<string>
    { "svchost.exe", "lsass.exe", "csrss.exe", "winlogon.exe", "services.exe", "smss.exe", "wininit.exe", "spoolsv.exe", "taskhost.exe", "taskhostw.exe", "dwm.exe", "conhost.exe" };

    private static readonly string[] DocExt = { ".pdf.", ".doc.", ".docx.", ".xls.", ".xlsx.", ".jpg.", ".jpeg.", ".png.", ".txt.", ".zip." };

    private static readonly Rule[] Rules =
    {

        R("SPY-001", Cat.Spyware, "Keylogger: keyboard capture APIs + network + persistence/user-writable location",
            c => c.Suspect && c.Net && ((c.Has("setwindowshookex") && c.Has("getkeyboardstate", "mapvirtualkey", "getkeynametext", "callnexthookex"))
                 || (c.Has("getasynckeystate") && c.Has("getforegroundwindow") && c.Has("getwindowtext")))),

        R("SPY-002", Cat.Spyware, "Screen spying: desktop capture APIs + network",
            c => c.Suspect && c.Net && c.Has("bitblt") && c.Has("getdesktopwindow", "getdc") && c.Has("gdiplus", "createcompatiblebitmap")),

        R("SPY-003", Cat.Spyware, "Clipboard spying: clipboard listener + network",
            c => c.Suspect && c.Net && c.Has("addclipboardformatlistener", "setclipboardviewer") && c.Has("getclipboarddata")),

        R("SPY-004", Cat.Spyware, "Webcam/microphone capture + network",
            c => c.Suspect && c.Net && c.Has("capcreatecapturewindow", "avicap32", "waveinopen", "ksproxy")),

        R("SPY-005", Cat.Spyware, "Browser credential stealer: browser stores + key decryption",
            c => c.Suspect && c.Has("login data") && c.Has("local state", "logins.json", "key4.db", "cookies.sqlite")
                 && c.Has("cryptunprotectdata", "encrypted_key", "os_crypt")),

        R("SPY-006", Cat.Spyware, "Data exfiltration via Discord/Telegram with credential or wallet access",
            c => c.Suspect && c.Has("discord.com/api/webhooks", "api.telegram.org/bot") && c.Has("login data", "leveldb", "cookies.sqlite", "wallet.dat", "tokens")),

        R("PERS-001", Cat.Threat, "Hidden unsigned executable in a user-writable folder",
            c => c.IsPE && c.UserWritable && c.Hidden && !c.Signed && c.CompanyName == ""),

        R("PERS-002", Cat.Threat, "Startup script with a download/obfuscation cradle",
            c => c.InStartup && c.IsScript && !c.Signed && c.Has("downloadstring", "downloadfile", "frombase64string", "-encodedcommand", "mshta", "-urlcache", "invoke-expression", "iex(")),

        R("PERS-003", Cat.Threat, "Unsigned executable in a Startup folder",
            c => c.InStartup && c.IsPE && !c.Signed && c.CompanyName == ""),

        R("PERS-004", Cat.Threat, "Run-key target is an unsigned executable in a user-writable folder",
            c => c.Persistent && !c.InStartup && c.UserWritable && c.IsPE && !c.Signed && c.CompanyName == ""),

        R("PERS-005", Cat.Threat, "Unsigned driver in a user-writable folder",
            c => c.Info.Extension.ToLowerInvariant() == ".sys" && c.UserWritable && c.IsPE && !c.Signed),

        R("MASQ-001", Cat.Threat, "Executable impersonating a Windows system process outside the Windows folder",
            c => SystemNames.Contains(c.Name) && !c.Lower.Contains("\\windows\\") && c.IsPE && !c.Signed),

        R("DECOY-001", Cat.Threat, "Executable disguised with a document double extension",
            c => (c.Info.Extension.ToLowerInvariant() == ".exe" || c.Info.Extension.ToLowerInvariant() == ".scr") && HasDocExt(c.Name) && c.IsPE && !c.Signed)
    };

    private static bool HasDocExt(string name)
    {
        foreach (string d in DocExt) if (name.Contains(d)) return true;
        return false;
    }

    internal static bool Candidate(FileInfo f) { return Exec.Contains(f.Extension); }

    internal static Splint.Finding Evaluate(FileCtx c, string digest)
    {
        Cat? cat = null; var names = new List<string>(); var ids = new List<string>();
        foreach (Rule r in Rules)
        {
            bool hit; try { hit = r.Test(c); } catch { hit = false; }
            if (!hit) continue;
            if (cat == null) cat = r.Cat;
            names.Add(r.Name); ids.Add(r.Id);
        }
        if (cat == null)
        {
            HeuristicResult heuristic = HeuristicAnalysis.Score(c.Info, c.IsPE, c.Signed, c.Persistent, (Func<string[], bool>)c.Has, false, true);
            if (!heuristic.Detected) return null;
            return new Splint.Finding { Path = c.Path, Detail = heuristic.Detail, Rule = "HEUR-SCORE", Digest = digest, Size = c.Info.Length, Kind = Cat.Heuristic };
        }
        return new Splint.Finding { Path = c.Path, Detail = string.Join("; ", names.ToArray()), Rule = string.Join(",", ids.ToArray()), Digest = digest, Size = c.Info.Length, Kind = cat.Value };
    }

    internal static HashSet<string> PersistentTargets()
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        string[] subs = { @"Software\Microsoft\Windows\CurrentVersion\Run", @"Software\Microsoft\Windows\CurrentVersion\RunOnce", @"Software\WOW6432Node\Microsoft\Windows\CurrentVersion\Run" };
        foreach (RegistryKey hive in new[] { Registry.CurrentUser, Registry.LocalMachine })
            foreach (string sub in subs)
            {
                try
                {
                    using (RegistryKey k = hive.OpenSubKey(sub))
                    {
                        if (k == null) continue;
                        foreach (string n in k.GetValueNames())
                        {
                            string p = ExtractPath(Convert.ToString(k.GetValue(n)));
                            if (p != null) set.Add(p);
                        }
                    }
                }
                catch { }
            }
        return set;
    }

    private static string ExtractPath(string cmd)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(cmd)) return null;
            cmd = Environment.ExpandEnvironmentVariables(cmd.Trim());
            string p;
            if (cmd[0] == '"') { int e = cmd.IndexOf('"', 1); if (e < 0) return null; p = cmd.Substring(1, e - 1); }
            else { int i = cmd.IndexOf(".exe", StringComparison.OrdinalIgnoreCase); p = i < 0 ? cmd : cmd.Substring(0, i + 4); }
            return System.IO.Path.IsPathRooted(p) ? System.IO.Path.GetFullPath(p) : null;
        }
        catch { return null; }
    }

}
