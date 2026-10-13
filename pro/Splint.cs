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
    internal const string Tier = "PRO";
    internal const string PlanKeyHash = "@@PLAN_HASH@@";
    internal const string Version = "0.2.0";
    internal const string Credits = "Created by Eduar";
    internal static readonly string Home = AppDomain.CurrentDomain.BaseDirectory;
    internal static readonly string Data = Path.Combine(Home, "SPLINT-data");
    internal static readonly string Reports = Path.Combine(Data, "Reports");
    internal static readonly string Quarantine = Path.Combine(Data, "Quarantine");

    internal sealed class Finding
    {
        internal string Path, Detail, Digest, Rule;
        internal long Size;
        internal Cat Kind;
        internal bool Actionable { get { return Kind == Cat.Threat || Kind == Cat.Spyware; } }
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
            if (!HasActivationConfiguration())
            {
                MessageBox.Show("This PRO build has no activation configuration. Set SPLINT_PRO_KEY_HASH before building.", "SPLINT PRO", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return 2;
            }
            if (!LicenseValid() && !Activate()) return 2;
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

    private static bool LicenseValid()
    {
        string path = Path.Combine(Data, "license.dat");
        if (!File.Exists(path)) return false;
        string[] parts = File.ReadAllLines(path);
        return parts.Length == 2 && parts[0] == Tier && FixedEquals(parts[1], PlanKeyHash);
    }

    private static bool HasActivationConfiguration()
    {
        return !String.Equals(PlanKeyHash,
            "0000000000000000000000000000000000000000000000000000000000000000",
            StringComparison.Ordinal);
    }

    private static bool Activate()
    {
        using (var dialog = new Form())
        using (var input = new TextBox())
        using (var ok = new Button())
        using (var cancel = new Button())
        {
            dialog.Text = "SPLINT " + Tier + " activation"; dialog.ClientSize = new Size(420, 145); dialog.StartPosition = FormStartPosition.CenterScreen; dialog.FormBorderStyle = FormBorderStyle.FixedDialog; dialog.MaximizeBox = false; dialog.MinimizeBox = false;
            var label = new Label { Text = "Enter your product key:", Location = new Point(18, 18), Size = new Size(380, 22) };
            input.Location = new Point(18, 47); input.Size = new Size(384, 25);
            ok.Text = "Activate"; ok.DialogResult = DialogResult.OK; ok.Location = new Point(218, 96); ok.Size = new Size(88, 30);
            cancel.Text = "Cancel"; cancel.DialogResult = DialogResult.Cancel; cancel.Location = new Point(314, 96); cancel.Size = new Size(88, 30);
            dialog.Controls.AddRange(new Control[] { label, input, ok, cancel }); dialog.AcceptButton = ok; dialog.CancelButton = cancel;
            if (dialog.ShowDialog() != DialogResult.OK) return false;
            string actual = Hash(input.Text.Trim());
            if (!FixedEquals(actual, PlanKeyHash)) { MessageBox.Show("Invalid product key.", "SPLINT", MessageBoxButtons.OK, MessageBoxIcon.Error); return false; }
            File.WriteAllText(Path.Combine(Data, "license.dat"), Tier + "\n" + actual, Encoding.ASCII);
            return true;
        }
    }

    private static string Hash(string value)
    {
        using (var sha = SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(value))).Replace("-", "");
    }

    private static bool FixedEquals(string a, string b)
    {
        if (a == null || b == null || a.Length != b.Length) return false;
        int d = 0; for (int i = 0; i < a.Length; i++) d |= char.ToUpperInvariant(a[i]) ^ char.ToUpperInvariant(b[i]); return d == 0;
    }

    internal static List<string> Roots(string scanType, string customPath)
    {
        if (scanType == "Custom") return new List<string> { customPath };
        if (scanType == "Quick") return new List<string> { Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) }.Where(x => !String.IsNullOrWhiteSpace(x) && Directory.Exists(x)).ToList();
        var roots = new List<string>();
        foreach (DriveInfo d in DriveInfo.GetDrives())
        {
            try { if (d.IsReady && d.DriveType == DriveType.Fixed) roots.Add(d.RootDirectory.FullName); }
            catch { }
        }
        return roots;
    }

    internal static ScanSummary Scan(IEnumerable<string> roots, BackgroundWorker worker)
    {
        HashDatabase.Initialize();
        var result = new ScanSummary();
        var persistent = Detection.PersistentTargets();
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
                if (result.Checked % 50 == 0) worker.ReportProgress(0, new object[] { result.Checked, file, result.Items.Count(i => i.Actionable) });
                try
                {
                    var info = new FileInfo(file);
                    if (info.Length == 0 || info.Length > 100L * 1024 * 1024 || !Detection.Candidate(info)) continue;
                    string digest = Digest(file);
                    Finding finding = IsKnownHash(digest)
                        ? new Finding { Path = file, Kind = Cat.HashMatch, Rule = "HASH-001", Detail = "Exact SHA-256 database match; verify the file and publisher before taking action (database entries can include dual-use software)", Digest = digest, Size = info.Length }
                        : Detection.Evaluate(new FileCtx(info, persistent.Contains(info.FullName)), digest);
                    if (finding != null) result.Items.Add(finding);
                }
                catch { }
            }
        }
        result.Cancelled = worker.CancellationPending;
        result.ReportPath = WriteReport(result);
        return result;
    }

    private static bool Excluded(string path)
    {
        string p = path.TrimEnd(Path.DirectorySeparatorChar).ToLowerInvariant();
        return p.StartsWith(Data.ToLowerInvariant()) || p.Contains("\\$recycle.bin") || p.Contains("\\system volume information") || p.Contains("\\windows\\winsxs");
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
            w.WriteLine("Exact rules use binary matches; heuristic scores are local weighted indicators, not a trained ML model or probability. Review heuristic findings manually; false positives are possible.");
            foreach (Finding f in result.Items) w.WriteLine(f.Kind + "\t" + f.Rule + "\t" + f.Digest + "\t" + f.Size + "\t" + f.Detail + "\t" + f.Path);
        }
        return path;
    }

    internal static void MoveToQuarantine(Finding f)
    {
        if (!f.Actionable) throw new IOException("Review-only item; no change was made.");
        if (!File.Exists(f.Path)) throw new IOException("The file no longer exists.");
        string win = Environment.GetFolderPath(Environment.SpecialFolder.Windows).TrimEnd('\\') + "\\";
        if (f.Path.StartsWith(win, StringComparison.OrdinalIgnoreCase) || f.Path.StartsWith(Data, StringComparison.OrdinalIgnoreCase)) throw new IOException("Protected path; no change was made.");
        string dest = Path.Combine(Quarantine, DateTime.UtcNow.ToString("yyyyMMddHHmmssfff") + "_" + Path.GetFileName(f.Path));
        File.Move(f.Path, dest);
        File.WriteAllText(dest + ".splint.txt", "Original=" + f.Path + "\nSHA256=" + f.Digest + "\nRule=" + f.Rule + "\nMovedUTC=" + DateTime.UtcNow.ToString("o"), Encoding.UTF8);
    }

    internal sealed class ScanSummary
    {
        internal long Checked;
        internal bool Cancelled;
        internal List<Finding> Items = new List<Finding>();
        internal string ReportPath;
        internal int Count(Cat category) { return Items.Count(i => i.Kind == category); }
    }
}
