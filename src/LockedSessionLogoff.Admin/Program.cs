// Updated: 2026-10-15 11:21 America/Los_Angeles
// Path: src/LockedSessionLogoff.Admin/Program.cs; Owner: maintainers
// Permissions: public read/maintainer write; Purpose: saved versus service-reported state
// Arguments: none; Usage: elevated executable; confirmed saves only
using System.Security.Cryptography;
using System.Text;
using LockedSessionLogoff.Windows;
namespace LockedSessionLogoff.Admin;
internal static class Program
{
    [STAThread] private static void Main() { ApplicationConfiguration.Initialize(); Application.Run(new AdminForm()); }
}
internal sealed class AdminForm : Form
{
    private readonly CheckBox enabled = new() { Text = "Request monitoring", AutoSize = true };
    private readonly CheckBox dry = new() { Text = "Dry-run requested (this build always forces dry-run)", AutoSize = true };
    private readonly CheckBox debug = new() { Text = "Debug requested", AutoSize = true };
    private readonly NumericUpDown timeout = new() { Minimum = 5, Maximum = 604800, Value = 3600 };
    private readonly TextBox exclusions = new() { Multiline = true, Width = 700, Height = 55, ScrollBars = ScrollBars.Vertical };
    private readonly TextBox live = new() { Multiline = true, ReadOnly = true, Width = 1050, Height = 155, ScrollBars = ScrollBars.Vertical };
    private readonly Label message = new() { AutoSize = true, MaximumSize = new Size(1050, 0) };
    private readonly DataGridView sessions = new() { Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.DisplayedCells };
    private readonly Button save = new() { Text = "Save", AutoSize = true };
    private readonly Button remove = new() { Text = "Remove saved settings", AutoSize = true };
    private readonly System.Windows.Forms.Timer refresh = new() { Interval = 3000 };
    private string? expectedRaw;
    private bool loaded;
    public AdminForm()
    {
        Text = "Locked Session Logoff - Service integration (dry-run only)";
        Size = new Size(1180, 900); MinimumSize = new Size(900, 750); StartPosition = FormStartPosition.CenterScreen;
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1, Padding = new Padding(12) };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize)); root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        var panel = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Top, FlowDirection = FlowDirection.TopDown, WrapContents = false };
        panel.Controls.Add(new Label { Text = "DRY-RUN ONLY: no lock/logoff, profile cleanup, restart or reporting actions.", ForeColor = Color.DarkRed, AutoSize = true });
        panel.Controls.AddRange([enabled, dry, debug, new Label { Text = "Timeout (seconds):", AutoSize = true }, timeout,
            new Label { Text = "Exclusion SIDs, one per line. Nonempty exclusions BLOCK observation until SID integration.", AutoSize = true }, exclusions]);
        var buttons = new FlowLayoutPanel { AutoSize = true };
        var reload = new Button { Text = "Reload/discard edits", AutoSize = true };
        buttons.Controls.AddRange([save, reload, remove]); panel.Controls.Add(buttons); panel.Controls.Add(message); panel.Controls.Add(live);
        root.Controls.Add(panel, 0, 0); root.Controls.Add(sessions, 0, 1); Controls.Add(root);
        save.Click += (_, _) => Save();
        reload.Click += (_, _) => { if (MessageBox.Show(this, "Discard edits and reload?", "Reload", MessageBoxButtons.YesNo) == DialogResult.Yes) LoadSettings(); };
        remove.Click += (_, _) =>
        {
            if (!loaded || expectedRaw is null || MessageBox.Show(this, "Remove only the saved application configuration? Service observation will disable. No Windows policy/task is changed.", "Remove", MessageBoxButtons.YesNo) != DialogResult.Yes) return;
            try { AdminSettingsStore.Remove(expectedRaw); LoadSettings(); } catch (Exception e) { message.Text = e.Message; }
        };
        Shown += (_, _) => { LoadSettings(); RefreshLive(); refresh.Start(); };
        refresh.Tick += (_, _) => RefreshLive();
        FormClosed += (_, _) => { refresh.Stop(); refresh.Dispose(); };
    }
    private void LoadSettings()
    {
        try
        {
            var r = AdminSettingsStore.Read(); expectedRaw = r.Raw; enabled.Checked = r.Settings.MonitoringRequested;
            dry.Checked = r.Settings.DryRun; debug.Checked = r.Settings.Debug; timeout.Value = r.Settings.LockedTimeoutSeconds; exclusions.Lines = r.Settings.ExcludedSids;
            loaded = true; save.Enabled = true; remove.Enabled = expectedRaw is not null; message.Text = "Loaded saved settings; check service report below for effective state.";
        }
        catch (Exception e) { loaded = false; save.Enabled = false; remove.Enabled = false; message.Text = "Settings blocked: " + e.Message; }
    }
    private void Save()
    {
        if (!loaded) return;
        try
        {
            var s = new AdminSettings { MonitoringRequested = enabled.Checked, DryRun = dry.Checked, Debug = debug.Checked, LockedTimeoutSeconds = decimal.ToInt32(timeout.Value),
                ExcludedSids = exclusions.Lines.Select(v => v.Trim()).Where(v => v.Length > 0).ToArray() };
            s.Validate();
            var text = $"Save to {AdminSettingsStore.DisplayPath}?\nMonitoring={s.MonitoringRequested}, DryRun={s.DryRun}, Debug={s.Debug}, Timeout={s.LockedTimeoutSeconds}s\nExclusions:\n{string.Join("\n", s.ExcludedSids)}\nInstalled service may reload observation. NO live actions exist in this build.";
            if (MessageBox.Show(this, text, "Confirm save", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
            AdminSettingsStore.Save(s, expectedRaw); LoadSettings(); RefreshLive();
        }
        catch (Exception e) { message.Text = "Not saved: " + e.Message; }
    }
    private void RefreshLive()
    {
        try
        {
            var r = ServiceReportStore.ReadVerified();
            var hash = expectedRaw is null ? null : Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(expectedRaw)));
            var c = r.Configuration;
            live.Text = $"SERVICE REPORT (protected registry + SCM PID/start-time/freshness checked)\r\nRun: {r.RunId}  Report UTC: {r.ReportedUtc:O}\r\nMode: {c?.Mode}  Observation: {c?.Enabled}  Effective dry-run: {c?.DryRun}  Timeout: {c?.TimeoutSeconds}s\r\nObserved revision: {c?.ObservedRevision}\r\nMatches editor-loaded saved revision: {hash == c?.ObservedRevision} (unsaved edits are not applied)\r\n" + string.Join("\r\n", r.RecentEvents ?? []);
        }
        catch (Exception e) { live.Text = "NO VERIFIED FRESH SERVICE REPORT: " + e.Message + "\r\nSaved settings alone do not prove active monitoring."; }
        try { sessions.DataSource = SessionReader.ReadAll().Select(s => new { s.Id, s.Kind, s.Account, s.ConnectionStateName, s.LockState, s.Error }).ToArray(); }
        catch (Exception e) { sessions.DataSource = null; message.Text = "Session read failed: " + e.GetType().Name; }
    }
}
