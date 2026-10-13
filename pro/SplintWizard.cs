using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using System.Windows.Forms;

internal static class Theme
{
    
    internal static readonly Color Background = Color.FromArgb(15, 23, 42);
    internal static readonly Color Surface = Color.FromArgb(30, 41, 59);
    internal static readonly Color SurfaceHover = Color.FromArgb(51, 65, 85);
    internal static readonly Color Accent = Color.FromArgb(59, 130, 246);
    internal static readonly Color AccentGlow = Color.FromArgb(96, 165, 250);
    internal static readonly Color TextMain = Color.FromArgb(248, 250, 252);
    internal static readonly Color TextMuted = Color.FromArgb(148, 163, 184);
    internal static readonly Color Line = Color.FromArgb(51, 65, 85);
    
    
    internal static readonly Color Good = Color.FromArgb(16, 185, 129);
    internal static readonly Color Warn = Color.FromArgb(245, 158, 11);
    internal static readonly Color Bad = Color.FromArgb(239, 68, 68);

    internal static GraphicsPath Round(Rectangle r, int rad)
    {
        int d = rad * 2; var p = new GraphicsPath();
        p.AddArc(r.X, r.Y, d, d, 180, 90); p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
        p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90); p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
        p.CloseFigure(); return p;
    }

    internal static void Smooth(Graphics g) 
    { 
        g.SmoothingMode = SmoothingMode.AntiAlias; 
        g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit; 
        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
    }

    internal static void Shield(Graphics g, Rectangle r, bool check, bool danger = false)
    {
        float x = r.X, y = r.Y, w = r.Width, h = r.Height;
        using (var p = new GraphicsPath())
        {
            p.AddLine(x + w / 2, y, x + w, y + h * 0.14f);
            p.AddBezier(x + w, y + h * 0.14f, x + w, y + h * 0.62f, x + w * 0.78f, y + h * 0.86f, x + w / 2, y + h);
            p.AddBezier(x + w / 2, y + h, x + w * 0.22f, y + h * 0.86f, x, y + h * 0.62f, x, y + h * 0.14f);
            p.CloseFigure();
            
            Color c1 = danger ? Color.FromArgb(248, 113, 113) : AccentGlow;
            Color c2 = danger ? Color.FromArgb(185, 28, 28) : Color.FromArgb(29, 78, 216);
            
            using (var b = new LinearGradientBrush(r, c1, c2, 60f)) g.FillPath(b, p);
            using (var pen = new Pen(Color.FromArgb(60, 255, 255, 255), 2)) g.DrawPath(pen, p);
        }

        if (danger)
            using (var pen = new Pen(Color.White, Math.Max(4, w / 15)) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round })
            {
                g.DrawLine(pen, x + w * 0.35f, y + h * 0.35f, x + w * 0.65f, y + h * 0.65f);
                g.DrawLine(pen, x + w * 0.65f, y + h * 0.35f, x + w * 0.35f, y + h * 0.65f);
            }
        else if (check)
            using (var pen = new Pen(Color.White, Math.Max(4, w / 16)) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round })
                g.DrawLines(pen, new[] { new PointF(x + w * 0.3f, y + h * 0.48f), new PointF(x + w * 0.45f, y + h * 0.63f), new PointF(x + w * 0.72f, y + h * 0.34f) });
    }
}

internal sealed class RoundButton : Control
{
    internal bool Primary; private bool hover, down;
    internal RoundButton()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.Selectable, true);
        Cursor = Cursors.Hand; Font = new Font("Segoe UI Semibold", 9.5F);
    }
    protected override void OnMouseEnter(EventArgs e) { hover = true; Invalidate(); base.OnMouseEnter(e); }
    protected override void OnMouseLeave(EventArgs e) { hover = false; down = false; Invalidate(); base.OnMouseLeave(e); }
    protected override void OnMouseDown(MouseEventArgs e) { down = true; Invalidate(); base.OnMouseDown(e); }
    protected override void OnMouseUp(MouseEventArgs e) { down = false; Invalidate(); base.OnMouseUp(e); }
    protected override void OnEnabledChanged(EventArgs e) { Invalidate(); base.OnEnabledChanged(e); }
    protected override void OnPaint(PaintEventArgs e)
    {
        Graphics g = e.Graphics; Theme.Smooth(g); g.Clear(Parent != null ? Parent.BackColor : Theme.Background);
        var r = new Rectangle(1, 1, Width - 3, Height - 3);
        using (GraphicsPath p = Theme.Round(r, 8))
        {
            if (Primary && Enabled)
            {
                Color c1 = down ? Color.FromArgb(37, 99, 235) : hover ? Theme.AccentGlow : Theme.Accent;
                Color c2 = down ? Color.FromArgb(29, 78, 216) : hover ? Theme.Accent : Color.FromArgb(30, 64, 175);
                using (var b = new LinearGradientBrush(r, c1, c2, 90f)) g.FillPath(b, p);
                using (var pen = new Pen(Color.FromArgb(80, 255, 255, 255), 1.5f)) { p.AddLine(0,0,0,0); g.DrawPath(pen, Theme.Round(new Rectangle(2,2,Width-5,Height-5), 7)); }
                TextRenderer.DrawText(g, AppSetup.T(Text), Font, r, Color.White, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            }
            else
            {
                using (var b = new SolidBrush(hover && Enabled ? Theme.SurfaceHover : Theme.Surface)) g.FillPath(b, p);
                using (var pen = new Pen(hover && Enabled ? Theme.Accent : Theme.Line, 1.5f)) g.DrawPath(pen, p);
                TextRenderer.DrawText(g, AppSetup.T(Text), Font, r, Enabled ? Theme.TextMain : Theme.Line, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            }
        }
    }
}

internal sealed class Card : Control
{
    internal string Glyph = "", Title = "", Desc = "", Big = ""; internal bool Selectable, Selected; internal Color Tint = Theme.Accent;
    private bool hover;
    internal event EventHandler Picked;
    internal Card() { SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true); }
    protected override void OnMouseEnter(EventArgs e) { if (Selectable) { hover = true; Cursor = Cursors.Hand; Invalidate(); } base.OnMouseEnter(e); }
    protected override void OnMouseLeave(EventArgs e) { hover = false; Invalidate(); base.OnMouseLeave(e); }
    protected override void OnClick(EventArgs e) { if (Selectable && Picked != null) Picked(this, e); base.OnClick(e); }
    protected override void OnPaint(PaintEventArgs e)
    {
        Graphics g = e.Graphics; Theme.Smooth(g); g.Clear(Parent != null ? Parent.BackColor : Theme.Background);
        var r = new Rectangle(2, 2, Width - 5, Height - 5);
        using (GraphicsPath p = Theme.Round(r, 12))
        {
            using (var b = new LinearGradientBrush(r, Selected ? Theme.SurfaceHover : Theme.Surface, Theme.Background, 45f)) g.FillPath(b, p);
            using (var pen = new Pen(Selected ? Tint : hover ? Color.FromArgb(120, Tint) : Theme.Line, Selected ? 2.5f : 1.5f)) g.DrawPath(pen, p);
            using (var path = new GraphicsPath()) {
                path.AddEllipse(r.X + 10, r.Y + 10, 66, 66);
                using (var pgb = new PathGradientBrush(path) { CenterColor = Color.FromArgb(40, Tint), SurroundColors = new[] { Color.Transparent } }) g.FillPath(pgb, path);
            }
        }
        var circle = new Rectangle(r.X + 20, r.Y + 20, 46, 46);
        using (var b = new SolidBrush(Color.FromArgb(30, Tint))) g.FillEllipse(b, circle);
        using (var f = new Font("Segoe MDL2 Assets", 18F))
            TextRenderer.DrawText(g, Glyph, f, circle, Tint, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
        
        if (Big.Length > 0)
            using (var f = new Font("Segoe UI", 26F, FontStyle.Bold))
                TextRenderer.DrawText(g, Big, f, new Rectangle(r.Right - 110, r.Y + 14, 96, 54), Tint, TextFormatFlags.Right | TextFormatFlags.VerticalCenter);
        
        using (var f = new Font("Segoe UI Semibold", 11.5F))
            TextRenderer.DrawText(g, AppSetup.T(Title), f, new Rectangle(r.X + 20, r.Y + 76, r.Width - 40, 26), Theme.TextMain, TextFormatFlags.Left | TextFormatFlags.EndEllipsis);
        
        using (var f = new Font("Segoe UI", 9F))
            TextRenderer.DrawText(g, AppSetup.T(Desc), f, new Rectangle(r.X + 20, r.Y + 106, r.Width - 40, r.Height - 116), Theme.TextMuted, TextFormatFlags.Left | TextFormatFlags.WordBreak);
    }
}

internal sealed class HeaderPanel : Control
{
    internal string Heading = "", Caption = ""; internal bool ThreatDetected;
    internal HeaderPanel() { SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true); }
    protected override void OnPaint(PaintEventArgs e)
    {
        Graphics g = e.Graphics; Theme.Smooth(g);
        using (var b = new LinearGradientBrush(ClientRectangle, Color.FromArgb(15, 23, 42), Color.FromArgb(30, 27, 75), 25f)) g.FillRectangle(b, ClientRectangle);
        using (var pgb = new PathGradientBrush(new GraphicsPath(new[] { new Point(Width - 100, -50), new Point(Width + 150, 100), new Point(Width - 50, 250) }, new[] { (byte)1, (byte)1, (byte)1 })))
        {
            pgb.CenterColor = Color.FromArgb(40, Theme.AccentGlow); pgb.SurroundColors = new[] { Color.Transparent };
            g.FillRectangle(pgb, ClientRectangle);
        }

        Theme.Shield(g, new Rectangle(28, 22, 42, 50), !ThreatDetected, ThreatDetected);
        using (var f = new Font("Segoe UI Semibold", 10F)) TextRenderer.DrawText(g, "SPLINT " + Splint.Tier, f, new Point(86, 22), Theme.AccentGlow);
        using (var f = new Font("Segoe UI", 18F, FontStyle.Bold)) TextRenderer.DrawText(g, AppSetup.T(Heading), f, new Point(84, 38), Theme.TextMain);
        using (var f = new Font("Segoe UI", 9.5F)) TextRenderer.DrawText(g, AppSetup.T(Caption), f, new Point(86, 72), Theme.TextMuted);
    }
}

internal sealed class Ring : Control
{
    internal float Angle, Pulse; internal bool Done; internal string Big = "0", Small = "files checked";
    internal Ring() { SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true); }
    protected override void OnPaint(PaintEventArgs e)
    {
        Graphics g = e.Graphics; Theme.Smooth(g); g.Clear(Parent != null ? Parent.BackColor : Theme.Background);
        var r = new Rectangle(14, 14, Width - 29, Height - 29);
        using (var pen = new Pen(Theme.Surface, 12)) g.DrawEllipse(pen, r);
        float breathe = (float)Math.Sin(Pulse) * 1.5f;
        using (var glow = new Pen(Color.FromArgb(40, Theme.AccentGlow), 22 + breathe) { StartCap = LineCap.Round, EndCap = LineCap.Round })
            if (Done) g.DrawEllipse(glow, r); else g.DrawArc(glow, r, Angle, 130);
        using (var pen = new Pen(Theme.AccentGlow, 10 + breathe * 0.4f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
            if (Done) g.DrawEllipse(pen, r); else g.DrawArc(pen, r, Angle, 130);
            
        using (var f = new Font("Segoe UI", 28F, FontStyle.Bold)) TextRenderer.DrawText(g, Big, f, new Rectangle(0, Height / 2 - 48, Width, 58), Theme.TextMain, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        using (var f = new Font("Segoe UI", 10F)) TextRenderer.DrawText(g, AppSetup.T(Small), f, new Rectangle(0, Height / 2 + 16, Width, 25), Theme.TextMuted, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
    }
}

internal sealed class HeroPanel : Panel
{
    internal HeroPanel() { DoubleBuffered = true; }
    protected override void OnPaint(PaintEventArgs e)
    {
        Graphics g = e.Graphics; Theme.Smooth(g);
        using (var path = new GraphicsPath()) {
            path.AddEllipse(Width - 450, -50, 500, 500);
            using (var pgb = new PathGradientBrush(path) { CenterColor = Color.FromArgb(20, Theme.AccentGlow), SurroundColors = new[] { Color.Transparent } }) g.FillPath(pgb, path);
        }
        Theme.Shield(g, new Rectangle(Width - 310, 60, 180, 220), true);
    }
}



internal sealed class SplintWizard : Form
{
    private readonly HeaderPanel header = new HeaderPanel();
    private readonly Panel[] pages = new Panel[4];
    private readonly RoundButton back = new RoundButton(), next = new RoundButton(), cancel = new RoundButton();
    private readonly Card quick = new Card(), full = new Card(), custom = new Card();
    private readonly Card cThreat = new Card(), cSpy = new Card(), cReview = new Card();
    private readonly Ring ring = new Ring();
    private readonly Label customFolder = new Label(), progressStatus = new Label(), progressCount = new Label(), resultSummary = new Label();
    private readonly DataGridView grid = new DataGridView();
    private readonly BackgroundWorker worker = new BackgroundWorker();
    private readonly Timer spin = new Timer { Interval = 16 };
    private int page;
    private string scanType = "Quick", folder;
    private Splint.ScanSummary summary;

    internal SplintWizard()
    {
        AutoScaleMode = AutoScaleMode.Dpi; AutoScaleDimensions = new SizeF(96F, 96F);
        Text = "Splint"; ClientSize = new Size(1000, 680); StartPosition = FormStartPosition.CenterScreen; BackColor = Theme.Background;
        Font = new Font("Segoe UI", 9F); FormBorderStyle = FormBorderStyle.FixedSingle; MaximizeBox = false; DoubleBuffered = true;
        
        header.SetBounds(0, 0, 1000, 104); Controls.Add(header);
        var host = new Panel { Bounds = new Rectangle(0, 104, 1000, 504), BackColor = Theme.Background }; Controls.Add(host);
        for (int i = 0; i < pages.Length; i++) { pages[i] = new Panel { Dock = DockStyle.Fill, BackColor = Theme.Background, Visible = false }; host.Controls.Add(pages[i]); }
        
        var footer = new Panel { Bounds = new Rectangle(0, 608, 1000, 72), BackColor = Theme.Surface }; Controls.Add(footer);
        footer.Paint += (s, e) => { using (var p = new Pen(Theme.Line)) e.Graphics.DrawLine(p, 0, 0, 1000, 0); };
        
        back.Text = AppSetup.T("Back"); next.Text = AppSetup.T("Next"); cancel.Text = AppSetup.T("Cancel"); next.Primary = true;
        back.SetBounds(668, 16, 100, 40); next.SetBounds(776, 16, 108, 40); cancel.SetBounds(892, 16, 92, 40);
        foreach (Control c in new Control[] { back, next, cancel }) footer.Controls.Add(c);
        back.Click += (s, e) => { if (page == 1) ShowPage(0); else if (page == 3) ShowPage(1); };
        next.Click += NextPage; cancel.Click += (s, e) => { if (worker.IsBusy) worker.CancelAsync(); else Close(); };
BuildWelcome(); BuildType(); BuildProgress(); BuildResults();
        
        spin.Tick += (s, e) => { ring.Angle = (ring.Angle + 2.2f) % 360; ring.Pulse += 0.055f; ring.Invalidate(); };
        worker.WorkerReportsProgress = true; worker.WorkerSupportsCancellation = true;
        worker.DoWork += (s, e) => e.Result = Splint.Scan((System.Collections.Generic.IEnumerable<string>)e.Argument, worker);
        worker.ProgressChanged += (s, e) =>
        {
            object[] d = (object[])e.UserState; ring.Big = Convert.ToInt64(d[0]).ToString("N0");
            header.ThreatDetected = Convert.ToInt32(d[2]) > 0; header.Invalidate();
            progressCount.Text = AppSetup.T("Checking: ") + Convert.ToString(d[1]); progressStatus.Text = Convert.ToInt32(d[2]) + AppSetup.T(" item(s) flagged so far");
        };
        worker.RunWorkerCompleted += ScanCompleted; ShowPage(0);
    }

    private Label L(string text, int x, int y, int w, int h, float size, bool bold, Color c)
    {
        return new Label { Text = AppSetup.T(text), Location = new Point(x, y), Size = new Size(w, h), BackColor = Color.Transparent, ForeColor = c, Font = new Font(bold ? "Segoe UI Semibold" : "Segoe UI", size) };
    }

    private void BuildWelcome()
    {
        var hero = new HeroPanel { Dock = DockStyle.Fill, BackColor = Theme.Background }; pages[0].Controls.Add(hero);
        hero.Controls.Add(L("Repair what an infection leaves behind.", 40, 34, 620, 44, 21F, true, Theme.TextMain));
        hero.Controls.Add(L("Splint inspects this PC for spyware and persistence indicators. Keep your antivirus running.", 42, 88, 560, 70, 10.5F, false, Theme.TextMuted));
        string[] g = { "\uE72E", "\uE7B3", "\uE785" }, t = { "Smart Heuristics", "Spyware detection", "File review" }, d = { "Analyzes file indicators and publisher information.", "Identifies keyloggers, screen capturers, and credential stealers hidden in user folders.", "Checks known signatures and publisher details." };
        for (int i = 0; i < 3; i++) hero.Controls.Add(new Card { Glyph = g[i], Title = t[i], Desc = d[i], Bounds = new Rectangle(40 + i * 296, 240, 280, 190), BackColor = Theme.Background, Tint = i == 2 ? Theme.Bad : i == 1 ? Theme.Warn : Theme.AccentGlow });
        hero.Controls.Add(L(AppSetup.T("Portable edition  \u2022  No driver  \u2022  ") + Splint.Tier, 42, 456, 600, 22, 9F, false, Theme.TextMuted));
    }

    private void BuildType()
    {
        pages[1].Controls.Add(L("Choose how thoroughly Splint should look", 40, 30, 800, 34, 15F, true, Theme.TextMain));
        var cards = new[] { quick, full, custom };
        string[] g = { "\uE945", "\uEDA2", "\uE838" }, t = { "Quick scan", "Full scan", "Custom scan" }, d = { "Your user profile, startup folders and Run key targets.", "Everything in your user profile. This can take a long time.", "Only the folder you choose." };
        for (int i = 0; i < 3; i++)
        {
            Card c = cards[i]; c.Glyph = g[i]; c.Title = t[i]; c.Desc = d[i]; c.Selectable = true; c.Bounds = new Rectangle(40 + i * 316, 90, 296, 220); c.BackColor = Theme.Background;
            c.Picked += (s, e) => { foreach (Card k in cards) { k.Selected = k == s; k.Invalidate(); } }; pages[1].Controls.Add(c);
        }
        quick.Selected = true;
        customFolder.SetBounds(40, 336, 900, 24); customFolder.ForeColor = Theme.TextMuted; customFolder.BackColor = Color.Transparent; pages[1].Controls.Add(customFolder);
    }

    private void BuildProgress()
    {
        ring.SetBounds(380, 40, 240, 240); ring.BackColor = Theme.Background; pages[2].Controls.Add(ring);
        pages[2].Controls.Add(L("Scan in progress", 0, 290, 1000, 40, 15F, true, Theme.TextMain)); ((Label)pages[2].Controls[pages[2].Controls.Count - 1]).TextAlign = ContentAlignment.MiddleCenter;
        progressStatus.SetBounds(0, 334, 1000, 30); progressStatus.TextAlign = ContentAlignment.MiddleCenter; progressStatus.ForeColor = Theme.AccentGlow; progressStatus.Font = new Font("Segoe UI Semibold", 10F); progressStatus.BackColor = Color.Transparent;
        progressCount.SetBounds(60, 368, 880, 30); progressCount.TextAlign = ContentAlignment.MiddleCenter; progressCount.ForeColor = Theme.TextMuted; progressCount.AutoEllipsis = true; progressCount.BackColor = Color.Transparent;
        pages[2].Controls.Add(progressStatus); pages[2].Controls.Add(progressCount);
    }

    private void BuildResults()
    {
        cThreat.Glyph = "\uE7BA"; cThreat.Title = "Threats"; cThreat.Desc = "Persistence and malware"; cThreat.Tint = Theme.Warn;
        cSpy.Glyph = "\uE7B3"; cSpy.Title = "Spyware"; cSpy.Desc = "Surveillance and stealers"; cSpy.Tint = Theme.AccentGlow;
        Card[] cs = { cThreat, cSpy };
        for (int i = 0; i < 2; i++) { cs[i].Bounds = new Rectangle(24 + i * 322, 6, 306, 128); cs[i].BackColor = Theme.Background; cs[i].Big = "0"; pages[3].Controls.Add(cs[i]); }
        resultSummary.SetBounds(28, 144, 940, 24); resultSummary.ForeColor = Theme.TextMain; resultSummary.Font = new Font("Segoe UI Semibold", 10F); resultSummary.BackColor = Color.Transparent; pages[3].Controls.Add(resultSummary);
        
        grid.SetBounds(28, 172, 944, 290); grid.AllowUserToAddRows = false; grid.AllowUserToDeleteRows = false; grid.RowHeadersVisible = false; grid.EnableHeadersVisualStyles = false;
        grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill; grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect; grid.BackgroundColor = Theme.Surface;
        grid.BorderStyle = BorderStyle.None; grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal; grid.GridColor = Theme.Line; grid.RowTemplate.Height = 38; grid.ColumnHeadersHeight = 38;
        grid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
        grid.ColumnHeadersDefaultCellStyle.BackColor = Theme.Background; grid.ColumnHeadersDefaultCellStyle.ForeColor = Theme.TextMuted; grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI Semibold", 9F);
        grid.DefaultCellStyle.Font = new Font("Segoe UI", 9.5F); grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(40, Theme.AccentGlow); grid.DefaultCellStyle.SelectionForeColor = Color.White; grid.DefaultCellStyle.ForeColor = Theme.TextMain; grid.DefaultCellStyle.BackColor = Theme.Surface;
        
        grid.Columns.Add(new DataGridViewCheckBoxColumn { Name = "Repair", HeaderText = AppSetup.T("Repair"), FillWeight = 9 });
        grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Type", HeaderText = AppSetup.T("Type"), FillWeight = 12, ReadOnly = true });
        grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Item", HeaderText = AppSetup.T("Item"), FillWeight = 33, ReadOnly = true });
        grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Details", HeaderText = AppSetup.T("Details"), FillWeight = 34, ReadOnly = true });
        grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Status", HeaderText = AppSetup.T("Status"), FillWeight = 12, ReadOnly = true });
        pages[3].Controls.Add(grid);
    }

    private void ShowPage(int value)
    {
        page = value; foreach (Panel p in pages) p.Visible = false; pages[page].Visible = true;
        header.Heading = new[] { "Welcome to Splint", "Scan type", "Scan in progress", "Scan results" }[page];
        header.Caption = new[] { "Repair what an infection leaves behind", "Choose how thoroughly Splint should look", "Please wait while Splint checks this PC", "Review what Splint found" }[page];
        header.Invalidate(); back.Visible = page == 1 || page == 3;
        next.Visible = page == 0 || page == 1 || (page == 3 && summary != null && summary.Items.Any(i => i.Actionable));
        next.Text = AppSetup.T(page == 3 ? "Repair" : page == 1 ? "Scan" : "Next"); cancel.Text = AppSetup.T(page == 3 ? "Close" : "Cancel");
    }

    private void NextPage(object sender, EventArgs e)
    {
        if (page == 0) { ShowPage(1); return; }
        if (page == 3) { RepairSelected(); return; }
        scanType = quick.Selected ? "Quick" : full.Selected ? "Full" : "Custom";
        if (scanType == "Custom")
            using (var picker = new FolderBrowserDialog { Description = AppSetup.T("Choose a folder to scan") })
            {
                if (picker.ShowDialog(this) != DialogResult.OK) return;
                folder = picker.SelectedPath; customFolder.Text = AppSetup.T("Selected folder: ") + folder;
            }
        var roots = Splint.Roots(scanType, folder);
        if (roots.Count == 0) { MessageBox.Show(AppSetup.T("No accessible scan locations were found."), "Splint", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
        ring.Done = false; ring.Big = "0"; progressStatus.Text = AppSetup.T("Preparing scan..."); progressCount.Text = "";
        ShowPage(2); spin.Start(); worker.RunWorkerAsync(roots);
    }

    private void ScanCompleted(object sender, RunWorkerCompletedEventArgs e)
    {
        spin.Stop(); ring.Done = true; ring.Invalidate();
        if (e.Error != null) { MessageBox.Show(AppSetup.T("The scan stopped: ") + e.Error.Message, "Splint", MessageBoxButtons.OK, MessageBoxIcon.Error); ShowPage(1); return; }
        summary = (Splint.ScanSummary)e.Result; header.ThreatDetected = summary.Items.Any(i => i.Actionable); header.Invalidate(); grid.Rows.Clear();
        foreach (Splint.Finding f in summary.Items)
        {
            int i = grid.Rows.Add(f.Actionable, f.Kind == Cat.Heuristic ? AppSetup.T("Heuristic review") : f.Kind == Cat.HashMatch ? AppSetup.T("Hash database match") : AppSetup.T(f.Kind.ToString()), f.Path, f.Detail, f.Actionable ? AppSetup.T("Found") : AppSetup.T("Review only"));
            if (!f.Actionable) grid.Rows[i].Cells[0].ReadOnly = true;
        }
        cThreat.Big = summary.Count(Cat.Threat).ToString(); cSpy.Big = summary.Count(Cat.Spyware).ToString();
        foreach (Card c in new[] { cThreat, cSpy }) c.Invalidate();
        string prefix = summary.Cancelled ? AppSetup.T("Scan canceled. Partial results: ") : "";
        int act = summary.Items.Count(i => i.Actionable); int review = summary.Items.Count(i => !i.Actionable); int hashes = summary.Count(Cat.HashMatch);
        resultSummary.Text = summary.Items.Count == 0
            ? prefix + AppSetup.T("nothing was flagged in ") + summary.Checked.ToString("N0") + AppSetup.T(" items.")
            : prefix + act + AppSetup.T(" item(s) can be repaired; ") + review + AppSetup.T(" item(s) are review only (") + hashes + AppSetup.T(" hash match(es)).");
        ShowPage(3);
    }

    private void RepairSelected()
    {
        var rows = grid.Rows.Cast<DataGridViewRow>().Where(r => Convert.ToBoolean(r.Cells[0].Value)).ToList();
        if (rows.Count == 0) { Close(); return; }
        if (MessageBox.Show(AppSetup.T("Move the selected ") + rows.Count + AppSetup.T(" item(s) into quarantine?"), AppSetup.T("Confirm repair"), MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
        int moved = 0;
        foreach (DataGridViewRow row in rows)
        {
            string path = Convert.ToString(row.Cells[2].Value);
            Splint.Finding item = summary.Items.FirstOrDefault(f => f.Actionable && String.Equals(f.Path, path, StringComparison.OrdinalIgnoreCase));
            try { if (item != null) { Splint.MoveToQuarantine(item); row.Cells[4].Value = AppSetup.T("Quarantined"); row.Cells[0].Value = false; moved++; } }
            catch (Exception ex) { row.Cells[4].Value = AppSetup.T("Could not move"); row.ErrorText = ex.Message; }
        }
        resultSummary.Text = moved + AppSetup.T(" item(s) moved to quarantine. The scan report is saved separately."); next.Visible = false;
    }
}
