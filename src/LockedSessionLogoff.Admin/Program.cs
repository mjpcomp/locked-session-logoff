// Updated: 2026-10-21 11:16 America/Los_Angeles
// Path: src/LockedSessionLogoff.Admin/Program.cs; Owner: maintainers
// Permissions: public read/maintainer write; Purpose: separate saved idle observation and locked-logoff controls
// Arguments: none; Usage: elevated GUI; idle intent never grants lock/sign-out authority
using System.Text;
using LockedSessionLogoff.Windows;
namespace LockedSessionLogoff.Admin;
internal static class Program { [STAThread] private static void Main() { ApplicationConfiguration.Initialize(); Application.Run(new AdminForm()); } }
internal sealed class AdminForm : Form
{
    private readonly CheckBox enabled = new() { Text = "Request LOCKED-session logoff monitoring", AutoSize = true };
    private readonly CheckBox idle = new() { Text = "Request IDLE observation (report only; never locks)", AutoSize = true };
    private readonly CheckBox dry = new() { Text = "Dry-run (required for idle observation; suppresses live logoff)", AutoSize = true };
    private readonly CheckBox permit = new() { Text = "Allow LIVE logoff - unsaved work may be lost", AutoSize = true, ForeColor = Color.DarkRed };
    private readonly CheckBox debug = new() { Text = "Debug requested", AutoSize = true };
    private readonly NumericUpDown timeout = new() { Minimum = 5, Maximum = 604800, Value = 3600 };
    private readonly NumericUpDown idleTimeout = new() { Minimum = 5, Maximum = 604800, Value = 300 };
    private readonly TextBox exclusions = new() { Multiline = true, Width = 700, Height = 50, ScrollBars = ScrollBars.Vertical };
    private readonly TextBox live = new() { Multiline = true, ReadOnly = true, Dock = DockStyle.Fill, ScrollBars = ScrollBars.Vertical };
    private readonly TextBox eventText = new() { Multiline = true, ReadOnly = true, Dock = DockStyle.Fill, ScrollBars = ScrollBars.Both, WordWrap = false };
    private readonly GroupBox eventGroup = new() { Text = "Retained Service Events (historical runs labeled; not audit)", Dock = DockStyle.Fill };
    private readonly Label message = new() { AutoSize = true, MaximumSize = new Size(1050, 0) };
    private readonly DataGridView sessions = new() { Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.DisplayedCells };
    private readonly Button save = new() { Text = "Save", AutoSize = true }, remove = new() { Text = "Remove saved settings (disable both requests)", AutoSize = true };
    private readonly System.Windows.Forms.Timer refresh = new() { Interval = 3000 };
    private string? expectedRaw; private bool loaded;
    public AdminForm()
    {
        Text = "Locked Session Logoff - service-managed idle observation test"; Size = new Size(1180, 1020); MinimumSize = new Size(900, 850); StartPosition = FormStartPosition.CenterScreen; AutoScroll = true;
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 4, ColumnCount = 1, Padding = new Padding(12), AutoScroll = true };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize)); root.RowStyles.Add(new RowStyle(SizeType.Absolute, 170)); root.RowStyles.Add(new RowStyle(SizeType.Absolute, 220)); root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        var panel = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Top, FlowDirection = FlowDirection.TopDown, WrapContents = false };
        panel.Controls.Add(new Label { Text = "IDLE is observation-only. No idle lock/countdown/profile/maintenance actions. Locked LIVE logoff is a separate opt-in.", AutoSize = true, ForeColor = Color.DarkRed });
        panel.Controls.AddRange([dry, permit, debug, enabled, new Label { Text = "LOCKED timeout (seconds):", AutoSize = true }, timeout, idle, new Label { Text = "IDLE observation timeout (seconds):", AutoSize = true }, idleTimeout, new Label { Text = "Shared SID exclusions, one per line. Obtain SID under the intended user's account.", AutoSize = true }, exclusions]);
        var buttons = new FlowLayoutPanel { AutoSize = true }; var reload = new Button { Text = "Reload/discard edits", AutoSize = true };
        buttons.Controls.AddRange([save, reload, remove]); panel.Controls.Add(buttons); panel.Controls.Add(message);
        var eventButtons = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 35 };
        var copy = new Button { Text = "Copy report/events", AutoSize = true }; var export = new Button { Text = "Export report/events", AutoSize = true };
        eventButtons.Controls.AddRange([copy, export]); eventGroup.Controls.Add(eventText); eventGroup.Controls.Add(eventButtons);
        root.Controls.Add(panel, 0, 0); root.Controls.Add(live, 0, 1); root.Controls.Add(eventGroup, 0, 2); root.Controls.Add(sessions, 0, 3); Controls.Add(root);
        copy.Click += (_, _) => { try { Clipboard.SetText(live.Text + "\r\n\r\n" + eventText.Text); } catch (Exception e) { message.Text = e.Message; } };
        export.Click += (_, _) =>
        {
            using var dialog = new SaveFileDialog { Filter = "Text files|*.txt", FileName = "service-events.txt", OverwritePrompt = true };
            if (dialog.ShowDialog(this) != DialogResult.OK) return;
            try { File.WriteAllText(dialog.FileName, live.Text + "\r\n\r\n" + eventText.Text, Encoding.UTF8); } catch (Exception e) { message.Text = e.Message; }
        };
        save.Click += (_, _) => Save();
        reload.Click += (_, _) => { if (MessageBox.Show(this, "Discard edits and reload?", "Reload", MessageBoxButtons.YesNo) == DialogResult.Yes) LoadSettings(); };
        remove.Click += (_, _) =>
        {
            if (!loaded || expectedRaw is null || MessageBox.Show(this, "Delete saved configuration and disable locked monitoring AND idle observation? This is not uninstall/baseline restoration.", "Remove saved settings", MessageBoxButtons.YesNo) != DialogResult.Yes) return;
            try { AdminSettingsStore.Remove(expectedRaw); LoadSettings(); RefreshLive(); } catch (Exception e) { message.Text = e.Message; }
        };
        Shown += (_, _) => { LoadSettings(); RefreshLive(); refresh.Start(); }; refresh.Tick += (_, _) => RefreshLive();
        FormClosed += (_, _) => { refresh.Stop(); refresh.Dispose(); };
    }
    private void LoadSettings()
    {
        try
        {
            var r = AdminSettingsStore.Read(); expectedRaw = r.Raw; enabled.Checked = r.Settings.MonitoringRequested; dry.Checked = r.Raw is null || r.Settings.DryRun;
            permit.Checked = r.Settings.AllowLiveLogoff; debug.Checked = r.Settings.Debug; timeout.Value = r.Settings.LockedTimeoutSeconds;
            idle.Checked = r.Settings.IdleObservationRequested; idleTimeout.Value = r.Settings.IdleTimeoutSeconds; exclusions.Lines = r.Settings.ExcludedSids;
            loaded = true; save.Enabled = true; remove.Enabled = expectedRaw is not null;
            message.Text = r.Raw is null ? "No saved settings. Both requests disabled; Save creates settings, not the pre-install baseline." : "Saved intent loaded. For effective idle mode inspect IdleObservationPolicy events or the authenticated agent reply.";
        }
        catch (Exception e) { loaded = false; save.Enabled = false; remove.Enabled = false; message.Text = "Settings blocked: " + e.Message; }
    }
    private void Save()
    {
        if (!loaded) return;
        try
        {
            var s = new AdminSettings { MonitoringRequested = enabled.Checked, IdleObservationRequested = idle.Checked, IdleTimeoutSeconds = decimal.ToInt32(idleTimeout.Value), DryRun = dry.Checked, AllowLiveLogoff = permit.Checked, Debug = debug.Checked, LockedTimeoutSeconds = decimal.ToInt32(timeout.Value), ExcludedSids = exclusions.Lines.Select(v => v.Trim()).Where(v => v.Length > 0).ToArray() }; s.Validate();
            var warning = s.AllowLiveLogoff && !s.DryRun ? "LIVE LOGOFF AUTHORIZED: locked users may be forcibly signed out; unsaved work can be lost. No countdown exists yet." : "No live logoff authorized by these settings. IDLE remains report-only.";
            var text = $"{warning}\nSave to {AdminSettingsStore.DisplayPath}?\nLocked request={s.MonitoringRequested}, Locked timeout={s.LockedTimeoutSeconds}s\nIdle observation request={s.IdleObservationRequested}, Idle timeout={s.IdleTimeoutSeconds}s\nDryRun={s.DryRun}, LivePermit={s.AllowLiveLogoff}, Debug={s.Debug}\nShared exclusions:\n{string.Join("\n", s.ExcludedSids)}";
            if (MessageBox.Show(this, text, "Confirm machine settings", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
            AdminSettingsStore.Save(s, expectedRaw); LoadSettings(); RefreshLive();
        }
        catch (Exception e) { message.Text = "Not saved: " + e.Message; }
    }
    private void RefreshLive()
    {
        try
        {
            var r = ServiceReportStore.ReadVerified(); var c = r.Configuration;
            live.Text = $"VERIFIED CURRENT SERVICE REPORT: {r.ReportedUtc:O}\r\nRun: {r.RunId} LOCKED-LOGOFF state: {ServiceDisplay.Mode(c)}\r\nLocked mode: {c?.Mode} Locked monitoring: {c?.Enabled} Effective dry-run: {c?.DryRun} Locked timeout: {ServiceDisplay.Timeout(c)}\r\nObserved revision: {c?.ObservedRevision ?? "(none)"}\r\nMatches editor-loaded revision: {ServiceDisplay.Revision(loaded, expectedRaw, c)}; unsaved edits not applied.\r\nIDLE effective mode: see IdleObservationPolicy events / authenticated agent. Saved checkbox is intent, not activation proof.";
            if (r.DiagnosticsNote is not null) live.AppendText("\r\nDIAGNOSTICS WARNING: " + r.DiagnosticsNote);
            eventGroup.Text = "Retained Service Events (up to 128 / 32 KiB; historical runs labeled; not audit)";
            var text = string.Join("\r\n", r.RetainedEvents.Select(e => "[run " + e.RunId + "] " + e.Text)); if (eventText.Text != text) eventText.Text = text;
        }
        catch (Exception e) { live.Text = "NO VERIFIED FRESH SERVICE REPORT: " + e.Message; eventGroup.Text = "LAST DISPLAYED EVENT SNAPSHOT ONLY; current report unavailable/stale"; }
        try { sessions.DataSource = SessionReader.ReadAll().Select(s => new { s.Id, s.Kind, s.Account, s.ConnectionStateName, s.LockState, s.Error }).ToArray(); }
        catch (Exception e) { sessions.DataSource = null; message.Text = "Session read failed: " + e.GetType().Name; }
    }
}
