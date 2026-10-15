// Updated: 2026-10-15 13:13 America/Los_Angeles
// Path: src/LockedSessionLogoff.Windows/ServiceReport.cs; Owner: maintainers
// Permissions: public read/maintainer write; Purpose: verified current status and bounded retained diagnostics
// Arguments: Write report / ReadVerified; Usage: SYSTEM writes/admin GUI reads; no enforcement authorization here
using Microsoft.Win32;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.ServiceProcess;
using System.Text;
using System.Text.Json;
namespace LockedSessionLogoff.Windows;

public sealed record ServiceReport(int Protocol, int ProcessId, long ProcessStartTicks,
    string RunId, DateTimeOffset ReportedUtc, string State, EffectiveConfiguration? Configuration, string[] RecentEvents)
{
    public RetainedEvent[] RetainedEvents { get; init; } = [];
    public string? DiagnosticsNote { get; init; }
}
public sealed record RetainedEvent(string RunId, string Text);

public static class EventHistory
{
    public const int MaxEvents = 128;
    public const int MaxBytes = 32768;
    public const int MaxText = 2048;
    public static RetainedEvent[] Merge(IEnumerable<RetainedEvent> old, IEnumerable<RetainedEvent> additions)
    {
        var rows = new List<RetainedEvent>();
        var seen = new HashSet<RetainedEvent>();
        int count = 0;
        foreach (var item in old.Concat(additions))
        {
            if (++count > 4096) throw new InvalidDataException("Excessive history input");
            if (item is null || !Guid.TryParseExact(item.RunId, "N", out _) || string.IsNullOrWhiteSpace(item.Text)
                || item.Text.Length > MaxText || item.Text.Contains('\r') || item.Text.Contains('\n') || item.Text.Contains('\0'))
                throw new InvalidDataException("Invalid diagnostic entry");
            if (seen.Add(item)) rows.Add(item);
        }
        while (rows.Count > MaxEvents) rows.RemoveAt(0);
        while (Encoding.UTF8.GetByteCount(JsonSerializer.Serialize(rows)) > MaxBytes) rows.RemoveAt(0);
        return rows.ToArray();
    }
    public static RetainedEvent[] Update(ServiceReport? previous, ServiceReport current)
    {
        Validate(current); if (previous is not null) Validate(previous);
        bool sameRun = previous?.RunId == current.RunId;
        var additions = new List<RetainedEvent>();
        if (!sameRun)
        {
            var start = new DateTime(current.ProcessStartTicks, DateTimeKind.Utc);
            additions.Add(new(current.RunId, start.ToString("O") + " ServiceRunStarted"));
        }
        var known = sameRun ? previous!.RecentEvents.ToHashSet(StringComparer.Ordinal) : new HashSet<string>(StringComparer.Ordinal);
        foreach (var line in current.RecentEvents)
            if (!known.Contains(line)) additions.Add(new(current.RunId, line));
        if (current.State == "Stopped" && (!sameRun || previous!.State != "Stopped"))
            additions.Add(new(current.RunId, current.ReportedUtc.ToString("O") + " ServiceRunStopped"));
        return Merge(previous?.RetainedEvents ?? [], additions);
    }
    public static void Validate(ServiceReport report)
    {
        if (report.Protocol != 1 || !Guid.TryParseExact(report.RunId, "N", out _) || report.ProcessStartTicks <= 0
            || report.ProcessStartTicks > DateTime.MaxValue.Ticks || report.State is not ("Running" or "Stopped")
            || report.RecentEvents is null || report.RecentEvents.Length > 24 || report.RetainedEvents is null
            || report.RetainedEvents.Length > MaxEvents)
            throw new InvalidDataException("Invalid diagnostic report");
        foreach (var line in report.RecentEvents) _ = Merge([], [new(report.RunId, line)]);
        _ = Merge(report.RetainedEvents, []);
        if (Encoding.UTF8.GetByteCount(JsonSerializer.Serialize(report.RetainedEvents)) > MaxBytes)
            throw new InvalidDataException("Oversized retained diagnostics");
    }
}

public static class ServiceDisplay
{
    public static string Mode(EffectiveConfiguration? c) => c?.Mode switch
    {
        "MissingConfiguration" => "No saved settings - monitoring disabled",
        "Disabled" => "Monitoring disabled",
        "AwaitingDisableThenEnable" => "Waiting for monitoring OFF then ON after restart/disarm",
        "ConfiguredDryRun" => "Dry-run monitoring active",
        "ConfiguredLiveLogoff" => "LIVE logoff authorized",
        _ => c?.Mode ?? "Unknown - no configuration report"
    };
    public static string Timeout(EffectiveConfiguration? c) => c is null ? "Unknown" : !c.Enabled ? "Inactive" : c.TimeoutSeconds + "s";
    public static string Revision(bool loaded, string? raw, EffectiveConfiguration? c)
    {
        if (!loaded || c is null) return "Unavailable";
        if (raw is null && c.Mode == "MissingConfiguration") return "Both absent - no saved settings";
        if (c.ObservedRevision is null) return "Unavailable - service has no observed revision";
        var hash = raw is null ? null : Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(Encoding.UTF8.GetBytes(raw)));
        return hash == c.ObservedRevision ? "Yes" : "No - reload or wait for service";
    }
}

public static class ServiceReportStore
{
    public const string KeyPath = @"SOFTWARE\LockedSessionLogoff.Runtime";
    public static void Write(ServiceReport report)
    {
        using var identity = WindowsIdentity.GetCurrent();
        if (identity.User?.Value != "S-1-5-18") throw new UnauthorizedAccessException("SYSTEM report writer required");
        using var root = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
        using var key = root.OpenSubKey(KeyPath, true) ?? throw new IOException("Runtime key not installed");
        ConfigurationSecurity.Verify(key);
        ServiceReport? previous = null;
        RetainedEvent[] history;
        string? note;
        try
        {
            var value = key.GetValue("StatusV1", null, RegistryValueOptions.DoNotExpandEnvironmentNames);
            if (value is not null)
            {
                if (value is not string old || old.Length > 65536 || Encoding.UTF8.GetByteCount(old) > 65536
                    || key.GetValueKind("StatusV1") != RegistryValueKind.String) throw new InvalidDataException("Invalid earlier report");
                previous = JsonSerializer.Deserialize<ServiceReport>(old) ?? throw new InvalidDataException("Null earlier report");
            }
            history = EventHistory.Update(previous, report);
            note = previous?.DiagnosticsNote;
        }
        catch (Exception e) when (e is JsonException or InvalidDataException or ArgumentException or OverflowException)
        {
            previous = null;
            history = EventHistory.Update(null, report);
            note = "Earlier retained diagnostics could not be read; history was restarted. This is not a complete audit trail.";
        }
        var stored = report with { RetainedEvents = history, DiagnosticsNote = note };
        var raw = JsonSerializer.Serialize(stored);
        if (Encoding.UTF8.GetByteCount(raw) > 65536) throw new InvalidDataException("Report too large");
        key.SetValue("StatusV1", raw, RegistryValueKind.String);
        if (previous is null || !history.SequenceEqual(previous.RetainedEvents) || report.State == "Stopped") key.Flush();
    }
    public static ServiceReport ReadVerified()
    {
        using var root = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
        using var key = root.OpenSubKey(KeyPath) ?? throw new IOException("No installed service report");
        ConfigurationSecurity.Verify(key);
        if (key.GetValue("StatusV1") is not string raw || raw.Length > 65536 || Encoding.UTF8.GetByteCount(raw) > 65536) throw new IOException("No valid service report");
        var report = JsonSerializer.Deserialize<ServiceReport>(raw) ?? throw new IOException("Null service report");
        EventHistory.Validate(report);
        var age = DateTimeOffset.UtcNow - report.ReportedUtc;
        if (report.State != "Running" || age > TimeSpan.FromSeconds(15) || age < TimeSpan.FromSeconds(-2)) throw new IOException("Service report stale/not running");
        using var controller = new ServiceController("LockedSessionLogoff");
        if (controller.Status != ServiceControllerStatus.Running ||
            !QueryServiceStatusEx(controller.ServiceHandle.DangerousGetHandle(), 0, out var native, Marshal.SizeOf<NativeStatus>(), out _) || native.ProcessId != report.ProcessId)
            throw new IOException("Report process does not match running service");
        using var process = Process.GetProcessById(report.ProcessId);
        if (process.StartTime.ToUniversalTime().Ticks != report.ProcessStartTicks) throw new IOException("Report belongs to an earlier process");
        return report;
    }
#pragma warning disable CS0649
    [StructLayout(LayoutKind.Sequential)] private struct NativeStatus
    {
        public uint ServiceType, CurrentState, ControlsAccepted, Win32ExitCode, ServiceSpecificExitCode, CheckPoint, WaitHint;
        public int ProcessId; public uint ServiceFlags;
    }
#pragma warning restore CS0649
    [DllImport("advapi32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)] private static extern bool QueryServiceStatusEx(IntPtr service, int level, out NativeStatus status, int size, out int needed);
}
