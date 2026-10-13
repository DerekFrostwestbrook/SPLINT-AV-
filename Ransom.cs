using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

internal sealed class Family
{
    internal string Name;
    internal string[] Ext;    
    internal string[] Notes;  
    internal Family(string name, string ext, string notes)
    {
        Name = name;
        Ext = ext.Length == 0 ? new string[0] : ext.Split(',');
        Notes = notes.Length == 0 ? new string[0] : notes.ToLowerInvariant().Split(',');
    }
}



internal static class Ransom
{
    internal const int MinFiles = 5;
    internal const string ToolsUrl = "https://www.nomoreransom.org/en/decryption-tools.html";
    internal const string IdUrl = "https://id-ransomware.malwarehunterteam.com/";

    
    internal static readonly Family[] All =
    {
        new Family("WannaCry", ".wncry,.wnry,.wcry", "@Please_Read_Me@.txt,@WanaDecryptor@.exe"),
        new Family("Locky", ".locky,.zepto,.odin,.thor,.osiris,.aesir", "_Locky_recover_instructions.txt,_HELP_instructions.html"),
        new Family("Cerber", ".cerber,.cerber2,.cerber3", "# DECRYPT MY FILES #.txt,# DECRYPT MY FILES #.html"),
        new Family("TeslaCrypt", ".ecc,.ezz,.exx,.vvv", "HELP_RESTORE_FILES.txt,RECOVER INSTRUCTIONS.txt"),
        new Family("CryptoWall", "", "HELP_DECRYPT.txt,HELP_DECRYPT.html,DECRYPT_INSTRUCTION.html"),
        new Family("STOP/Djvu", ".djvu,.djvuu,.djvus,.djvur,.nooa,.rumba,.kook,.tfude", "_readme.txt"),
        new Family("Dharma/Crysis", ".dharma,.wallet,.arena,.cezar", "FILES ENCRYPTED.txt,Info.hta"),
        new Family("Phobos", ".phobos,.eking,.eight,.devos", "info.txt,info.hta"),
        new Family("GandCrab", ".gdcb,.krab,.crab", "GDCB-DECRYPT.txt,KRAB-DECRYPT.txt,CRAB-DECRYPT.txt"),
        new Family("REvil/Sodinokibi", "", "*-readme.txt"),
        new Family("Ryuk", ".ryk,.ryuk", "RyukReadMe.txt,RyukReadMe.html"),
        new Family("Conti", ".conti", "readme.txt,CONTI_README.txt"),
        new Family("Maze", "", "DECRYPT-FILES.txt"),
        new Family("Shade/Troldesh", ".xtbl,.ytbl,.breaking_bad,.heisenberg", "readme1.txt,readme2.txt,readme3.txt"),
        new Family("Jigsaw", ".fun,.kkk,.gws,.porno", ""),
        new Family("Hermes", ".hermes", "DECRYPT_INFORMATION.html"),
        new Family("Makop", ".makop", "README_WARNING.txt"),
        new Family("MedusaLocker", ".medusa,.skynet,.bozon", "HOW_TO_RECOVER_DATA.html"),
        new Family("Akira", ".akira", "akira_readme.txt"),
        new Family("Nemty", ".nemty", "NEMTY-DECRYPT.txt"),
        new Family("Rhysida", ".rhysida", "CriticalBreachDetected.pdf"),
        new Family("Play", ".play", "ReadMe.txt")
    };

    private static Dictionary<string, Family> byExt;

    internal static Family ByExtension(string ext)
    {
        if (byExt == null)
        {
            var d = new Dictionary<string, Family>(StringComparer.OrdinalIgnoreCase);
            foreach (Family f in All) foreach (string e in f.Ext) d[e] = f;
            byExt = d;
        }
        Family r; return byExt.TryGetValue(ext, out r) ? r : null;
    }

    internal static Family ByNote(string fileName)
    {
        string n = fileName.ToLowerInvariant();
        foreach (Family f in All)
            foreach (string note in f.Notes)
                if (note.Length > 1 && note[0] == '*' ? n.EndsWith(note.Substring(1)) : n == note) return f;
        return null;
    }

    
    internal static bool NoteLooksReal(string path)
    {
        try
        {
            var fi = new FileInfo(path);
            if (fi.Length == 0 || fi.Length > 65536) return false;
            string t = File.ReadAllText(path).ToLowerInvariant();
            bool pay = t.Contains("bitcoin") || t.Contains("btc") || t.Contains(".onion") || t.Contains("tox id") || t.Contains("monero") || t.Contains("tor browser");
            bool enc = t.Contains("encrypted") || t.Contains("decrypt") || t.Contains("your files") || t.Contains("ransom");
            return pay && enc;
        }
        catch { return false; }
    }

    internal static bool LooksLikeNoteName(string name)
    {
        string n = name.ToLowerInvariant();
        string ext = Path.GetExtension(n);
        if (ext != ".txt" && ext != ".html" && ext != ".hta") return false;
        return n.Contains("readme") || n.Contains("decrypt") || n.Contains("recover") || n.Contains("restore") || n.Contains("how_to");
    }

    
    internal static double Entropy(string path)
    {
        try
        {
            byte[] b = new byte[16384]; int n;
            using (var fs = File.OpenRead(path)) n = fs.Read(b, 0, b.Length);
            if (n < 1024) return -1;
            int[] c = new int[256];
            for (int i = 0; i < n; i++) c[b[i]]++;
            double h = 0;
            for (int i = 0; i < 256; i++) if (c[i] > 0) { double p = (double)c[i] / n; h -= p * Math.Log(p, 2); }
            return h;
        }
        catch { return -1; }
    }

    
    internal static string ShadowCopies()
    {
        try
        {
            var psi = new ProcessStartInfo("vssadmin.exe", "list shadows") { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true, CreateNoWindow = true };
            using (Process p = Process.Start(psi))
            {
                string o = p.StandardOutput.ReadToEnd() + p.StandardError.ReadToEnd();
                p.WaitForExit(15000);
                return o.Length > 3000 ? o.Substring(0, 3000) + "\n..." : o;
            }
        }
        catch (Exception e) { return "Could not query shadow copies: " + e.Message; }
    }
}
