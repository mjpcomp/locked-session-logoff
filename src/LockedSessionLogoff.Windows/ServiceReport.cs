// Created: 2026-10-15 11:21 America/Los_Angeles
// Path: src/LockedSessionLogoff.Windows/ServiceReport.cs; Owner: maintainers
// Permissions: public read/maintainer write; Purpose: protected local service status
// Arguments: Write report; ReadVerified none; Usage: SYSTEM writes/admin GUI verifies
using Microsoft.Win32;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.ServiceProcess;
using System.Text.Json;
namespace LockedSessionLogoff.Windows;

public sealed record ServiceReport(int Protocol, int ProcessId, long ProcessStartTicks,
    string RunId, DateTimeOffset ReportedUtc, string State, EffectiveConfiguration? Configuration, string[] RecentEvents);

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
        key.SetValue("StatusV1", JsonSerializer.Serialize(report), RegistryValueKind.String);
    }
    public static ServiceReport ReadVerified()
    {
        using var root = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
        using var key = root.OpenSubKey(KeyPath) ?? throw new IOException("No installed service report");
        ConfigurationSecurity.Verify(key);
        if (key.GetValue("StatusV1") is not string raw || raw.Length > 65536) throw new IOException("No valid service report");
        var report = JsonSerializer.Deserialize<ServiceReport>(raw) ?? throw new IOException("Null service report");
        var age = DateTimeOffset.UtcNow - report.ReportedUtc;
        if (report.Protocol != 1 || age > TimeSpan.FromSeconds(15) || age < TimeSpan.FromSeconds(-2)) throw new IOException("Service report stale/unsupported");
        using var controller = new ServiceController("LockedSessionLogoff");
        if (controller.Status != ServiceControllerStatus.Running ||
            !QueryServiceStatusEx(controller.ServiceHandle.DangerousGetHandle(), 0, out var native, Marshal.SizeOf<NativeStatus>(), out _) || native.ProcessId != report.ProcessId)
            throw new IOException("Report process does not match running service");
        using var process = Process.GetProcessById(report.ProcessId);
        if (process.StartTime.ToUniversalTime().Ticks != report.ProcessStartTicks) throw new IOException("Report belongs to an earlier process");
        return report;
    }
#pragma warning disable CS0649
    [StructLayout(LayoutKind.Sequential)]
    private struct NativeStatus
    {
        public uint ServiceType, CurrentState, ControlsAccepted, Win32ExitCode, ServiceSpecificExitCode, CheckPoint, WaitHint;
        public int ProcessId;
        public uint ServiceFlags;
    }
#pragma warning restore CS0649
    [DllImport("advapi32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool QueryServiceStatusEx(IntPtr service, int level, out NativeStatus status, int size, out int needed);
}
