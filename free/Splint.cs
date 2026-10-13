using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;

internal static class Splint
{
    internal const string Tier = "FREE";
    internal const string Version = "0.2.0";
    internal const string Credits = "SPLINT FREE";
    internal static readonly string Home = AppDomain.CurrentDomain.BaseDirectory;
    internal static readonly string Data = Path.Combine(Home, "SPLINT-data");
    internal static readonly string Reports = Path.Combine(Data, "Reports");
    internal static readonly string Quarantine = Path.Combine(Data, "Quarantine");
    internal static readonly List<Finding> Findings = new List<Finding>();

    internal sealed class Finding
    {
        internal string Path;
        internal string Detail;
        internal string Digest;
        internal long Size;
        internal bool Actionable;
        internal bool HashMatch;
    }

    [STAThread]
    private static int Main(string[] args)
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        try
        {
            Directory.CreateDirectory(Data);
            if (!AppSetup.Initialize(null, Data, Tier)) return 2;
            Directory.CreateDirectory(Reports);
            Directory.CreateDirectory(Quarantine);
            Application.Run(new SplintWizard());
            return 0;
        }
        catch (Exception e)
        {
            MessageBox.Show("SPLINT could not start: " + e.Message, "SPLINT", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return 1;
        }
    }

    internal static List<string> Roots(string scanType, string customPath)
    {
        if (scanType == "Custom") return new List<string> { customPath };
        if (scanType == "Quick") return new List<string> { Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) }.Where(x => !String.IsNullOrWhiteSpace(x) && Directory.Exists(x)).ToList();
        return new List<string> { Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) }
            .Where(x => !String.IsNullOrWhiteSpace(x) && Directory.Exists(x)).ToList();
    }

    internal static ScanSummary Scan(IEnumerable<string> roots, BackgroundWorker worker)
    {
        HashDatabase.Initialize();
        var result = new ScanSummary();
        var queue = new Queue<string>(roots);
        while (queue.Count > 0 && !worker.CancellationPending)
        {
            string dir = queue.Dequeue();
            string[] files, dirs;
            try { files = Directory.GetFiles(dir); dirs = Directory.GetDirectories(dir); }
            catch { continue; }
            foreach (string sub in dirs)
            {
                try { if (!Excluded(sub) && (File.GetAttributes(sub) & FileAttributes.ReparsePoint) == 0) queue.Enqueue(sub); }
                catch { }
            }
            foreach (string file in files)
            {
                if (worker.CancellationPending) break;
                result.Checked++;
                if (result.Checked % 100 == 0) worker.ReportProgress(0, new object[] { result.Checked, file });
                try
                {
                    var info = new FileInfo(file);
                    if (info.Length > 100L * 1024 * 1024) continue;
                    string digest = Digest(file);
                    bool known = IsKnownHash(digest);
                    string detail = known
                        ? "Exact SHA-256 database match; verify the file and publisher before taking action (database entries can include dual-use software)"
                        : Heuristic(info);
                    if (detail != null) result.Items.Add(new Finding { Path = file, Detail = detail, Digest = digest, Size = info.Length, Actionable = false, HashMatch = known });
                }
                catch { }
            }
        }
        lock (Findings) { Findings.Clear(); Findings.AddRange(result.Items); }
        result.Cancelled = worker.CancellationPending;
        result.ReportPath = WriteReport(result);
        return result;
    }

    private static bool Excluded(string path)
    {
        string p = path.TrimEnd(Path.DirectorySeparatorChar).ToLowerInvariant();
        return p.StartsWith(Data.ToLowerInvariant()) || p.Contains("\\$recycle.bin") || p.Contains("\\system volume information") || p.Contains("\\windows\\winsxs");
    }

    private static string Heuristic(FileInfo f)
    {
        if (!HeuristicAnalysis.IsCandidate(f)) return null;

        string ascii = null, unicode = null;
        Func<string[], bool> has = needles =>
        {
            if (ascii == null)
            {
                try
                {
                    byte[] bytes;
                    using (var stream = File.OpenRead(f.FullName))
                    {
                        int length = (int)Math.Min(stream.Length, 1024 * 1024);
                        bytes = new byte[length];
                        int read = 0;
                        while (read < length)
                        {
                            int count = stream.Read(bytes, read, length - read);
                            if (count <= 0) break;
                            read += count;
                        }
                    }
                    ascii = Encoding.GetEncoding(28591).GetString(bytes).ToLowerInvariant();
                    unicode = Encoding.Unicode.GetString(bytes).ToLowerInvariant();
                }
                catch (IOException) { ascii = ""; unicode = ""; }
                catch (UnauthorizedAccessException) { ascii = ""; unicode = ""; }
            }
            foreach (string needle in needles)
                if (ascii.IndexOf(needle, StringComparison.Ordinal) >= 0 || unicode.IndexOf(needle, StringComparison.Ordinal) >= 0) return true;
            return false;
        };

        bool isPe = HeuristicAnalysis.IsPortableExecutable(f);
        HeuristicResult result = HeuristicAnalysis.Score(f, isPe, isPe && HeuristicAnalysis.IsTrustedSigned(f), false, has, true);
        return result.Detected ? result.Detail : null;
    }

    private static bool IsKnownHash(string value)
    {
        return HashDatabase.Contains(value);
    }

    private static string Digest(string path)
    {
        using (var stream = File.OpenRead(path))
        using (var sha = SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "");
    }

    private static string WriteReport(ScanSummary result)
    {
        string path = Path.Combine(Reports, "scan-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss") + ".txt");
        using (var w = new StreamWriter(path, false, Encoding.UTF8))
        {
            w.WriteLine("SPLINT " + Version + " | UTC " + DateTime.UtcNow.ToString("o"));
            w.WriteLine("Tier=" + Tier + " Files=" + result.Checked + " Findings=" + result.Items.Count);
            w.WriteLine("Hash database matches and heuristic scores are review-only, not confirmed malware. Verify files and publishers before taking action; false positives are possible.");
            foreach (Finding f in result.Items) w.WriteLine(f.Digest + "\t" + f.Size + "\t" + f.Detail + "\t" + f.Path);
        }
        return path;
    }

    internal static void MoveToQuarantine(Finding f)
    {
        if (!File.Exists(f.Path)) throw new IOException("The file no longer exists.");
        string win = Environment.GetFolderPath(Environment.SpecialFolder.Windows).TrimEnd('\\') + "\\";
        if (f.Path.StartsWith(win, StringComparison.OrdinalIgnoreCase) || f.Path.StartsWith(Data, StringComparison.OrdinalIgnoreCase)) throw new IOException("Protected path; no change was made.");
        string dest = Path.Combine(Quarantine, DateTime.UtcNow.ToString("yyyyMMddHHmmssfff") + "_" + Path.GetFileName(f.Path));
        File.Move(f.Path, dest);
        File.WriteAllText(dest + ".splint.txt", "Original=" + f.Path + "\nSHA256=" + f.Digest + "\nMovedUTC=" + DateTime.UtcNow.ToString("o"), Encoding.UTF8);
    }

    internal sealed class ScanSummary
    {
        internal long Checked;
        internal bool Cancelled;
        internal List<Finding> Items = new List<Finding>();
        internal string ReportPath;
    }
}
