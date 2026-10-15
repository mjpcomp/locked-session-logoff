// Created: 2026-10-15 10:46 America/Los_Angeles
// Path: src/LockedSessionLogoff.Admin/Program.cs; Owner: maintainers
// Permissions: public read/maintainer write; Purpose: elevated settings/session dashboard
// Arguments: none; Usage: launch executable, review, explicitly save/remove settings
using System.ServiceProcess;
using LockedSessionLogoff.Windows;

namespace LockedSessionLogoff.Admin;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new AdminForm());
    }
}

internal sealed class AdminForm : Form
{
    private readonly CheckBox enabled = new() { Text = "Request monitoring (not applied by current service)", AutoSize = true };
    private readonly CheckBox dryRun = new() { Text = "Dry-run / report-only", AutoSize = true };
    private readonly CheckBox debug = new() { Text = "Verbose diagnostics", AutoSize = true };
    private readonly NumericUpDown timeout = new() { Minimum = 5, Maximum = 604800, Value = 3600, Width = 120 };
    private readonly TextBox exclusions = new() { Multiline = true, ScrollBars = ScrollBars.Vertical, Height = 75, Width = 550 };
    private readonly Label service = new() { AutoSize = true };
    private readonly Label status = new() { AutoSize = true, MaximumSize = new Size(1050, 0) };
    private readonly DataGridView sessions = new()
    {
        Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false, AllowUserToDeleteRows = false,
        AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.DisplayedCells, AutoGenerateColumns = true
    };
    private readonly Button save = new() { Text = "Save configuration", AutoSize = true };
    private readonly Button remove = new() { Text = "Remove saved configuration", AutoSize = true };
    private readonly System.Windows.Forms.Timer refresh = new() { Interval = 3000 };
    private string? expectedRaw;
    private bool loaded;

    public AdminForm()
    {
        Text = "Locked Session Logoff - Administrator (configuration stage)";
        Size = new Size(1160, 780);
        MinimumSize = new Size(850, 650);
        StartPosition = FormStartPosition.CenterScreen;
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, Padding = new Padding(12) };
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        var settingsPanel = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Top, FlowDirection = FlowDirection.TopDown, WrapContents = false };
        settingsPanel.Controls.Add(new Label { Text = "CONFIGURATION ONLY: saves are NOT consumed by the current service. No lock/logoff, profile or task actions.", AutoSize = true, ForeColor = Color.DarkRed, MaximumSize = new Size(1050, 0) });
        settingsPanel.Controls.Add(service);
        settingsPanel.Controls.Add(enabled);
        settingsPanel.Controls.Add(dryRun);
        settingsPanel.Controls.Add(debug);
        settingsPanel.Controls.Add(new Label { Text = "Locked timeout in seconds (5 through 604800):", AutoSize = true });
        settingsPanel.Controls.Add(timeout);
        settingsPanel.Controls.Add(new Label { Text = "Exclusion SIDs, one per line (stored only; enforcement not implemented):", AutoSize = true });
        settingsPanel.Controls.Add(exclusions);
        var buttons = new FlowLayoutPanel { AutoSize = true, WrapContents = true };
        var reload = new Button { Text = "Reload saved settings", AutoSize = true };
        buttons.Controls.AddRange([save, reload, remove]);
        settingsPanel.Controls.Add(buttons);
        settingsPanel.Controls.Add(new Label { Text = AdminSettingsStore.DisplayPath, AutoSize = true, MaximumSize = new Size(1050, 0) });
        layout.Controls.Add(settingsPanel, 0, 0);
        layout.Controls.Add(sessions, 0, 1);
        layout.Controls.Add(status, 0, 2);
        Controls.Add(layout);
        save.Click += (_, _) => SaveSettings();
        reload.Click += (_, _) =>
        {
            if (MessageBox.Show(this, "Discard unsaved edits and reload?", "Reload", MessageBoxButtons.YesNo) == DialogResult.Yes) LoadSettings();
        };
        remove.Click += (_, _) => RemoveSettings();
        refresh.Tick += (_, _) => RefreshStatus();
        Shown += (_, _) => { LoadSettings(); RefreshStatus(); refresh.Start(); };
        FormClosed += (_, _) => { refresh.Stop(); refresh.Dispose(); };
    }

    private void LoadSettings()
    {
        try
        {
            var result = AdminSettingsStore.Read();
            expectedRaw = result.Raw;
            enabled.Checked = result.Settings.MonitoringRequested;
            dryRun.Checked = result.Settings.DryRun;
            debug.Checked = result.Settings.Debug;
            timeout.Value = result.Settings.LockedTimeoutSeconds;
            exclusions.Lines = result.Settings.ExcludedSids;
            loaded = true; save.Enabled = true; remove.Enabled = expectedRaw is not null;
            status.Text = expectedRaw is null ? "Defaults displayed; no saved configuration. No writes on launch." : "Saved configuration loaded. Not applied by current service.";
        }
        catch (Exception e)
        {
            loaded = false; save.Enabled = false; remove.Enabled = false;
            status.Text = "Configuration blocked: " + e.Message + ". Existing data was not replaced.";
        }
    }

    private AdminSettings Edited() => new()
    {
        MonitoringRequested = enabled.Checked, DryRun = dryRun.Checked, Debug = debug.Checked,
        LockedTimeoutSeconds = decimal.ToInt32(timeout.Value),
        ExcludedSids = exclusions.Lines.Select(s => s.Trim()).Where(s => s.Length > 0).ToArray()
    };

    private void SaveSettings()
    {
        if (!loaded) return;
        try
        {
            var settings = Edited(); settings.Validate();
            var summary = $"Save machine configuration?\nMonitoring requested: {settings.MonitoringRequested}\nDry-run: {settings.DryRun}\nDebug: {settings.Debug}\nTimeout: {settings.LockedTimeoutSeconds} seconds\nExcluded SIDs:\n{string.Join("\n", settings.ExcludedSids)}\n\nWrites only {AdminSettingsStore.DisplayPath}.\nCurrent service does not apply these settings; no actions will run.";
            if (MessageBox.Show(this, summary, "Confirm configuration write", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
            AdminSettingsStore.Save(settings, expectedRaw);
            LoadSettings();
        }
        catch (Exception e) { MessageBox.Show(this, e.Message, "Configuration not saved", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }

    private void RemoveSettings()
    {
        if (!loaded || expectedRaw is null) return;
        if (MessageBox.Show(this, "Remove only the saved ConfigurationV1 value?\nThe key and unrelated values remain. This is not uninstall and changes no Windows policy or task.",
            "Confirm removal", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
        try { AdminSettingsStore.Remove(expectedRaw); LoadSettings(); }
        catch (Exception e) { MessageBox.Show(this, e.Message, "Configuration not removed"); }
    }

    private void RefreshStatus()
    {
        try
        {
            using var controller = new ServiceController("LockedSessionLogoff");
            controller.Refresh(); service.Text = "Windows service: " + controller.Status + " (status only; does not prove settings applied)";
        }
        catch (InvalidOperationException) { service.Text = "Windows service: not installed or unavailable; no installer in this package."; }
        catch (Exception e) { service.Text = "Service status unavailable: " + e.GetType().Name; }
        try
        {
            sessions.DataSource = SessionReader.ReadAll().Select(s => new
            { s.Id, s.Kind, s.Station, s.Account, s.ConnectionStateName, s.LockState, s.CanObserveTimer, s.Error }).ToArray();
        }
        catch (Exception e) { sessions.DataSource = null; status.Text = "Session refresh failed: " + e.Message; }
    }
}
