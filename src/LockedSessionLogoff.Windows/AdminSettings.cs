// Updated: 2026-10-21 11:16 America/Los_Angeles
// Path: src/LockedSessionLogoff.Windows/AdminSettings.cs; Owner: maintainers
// Permissions: public read/maintainer write; Purpose: protected settings with independent report-only idle intent
// Arguments: Save/Remove expected raw; Usage: elevated GUI writes, SYSTEM host reads; absent idle fields default OFF
using Microsoft.Win32;
using System.Security.Principal;
using System.Text.Json;
using System.Text.Json.Serialization;
namespace LockedSessionLogoff.Windows;
public sealed record AdminSettings
{
    public string ApplicationId { get; init; } = "LockedSessionLogoff";
    public int SchemaVersion { get; init; } = 1;
    public bool MonitoringRequested { get; init; }
    public bool DryRun { get; init; }
    public bool Debug { get; init; }
    public bool AllowLiveLogoff { get; init; } = false;
    public int LockedTimeoutSeconds { get; init; } = 3600;
    public bool IdleObservationRequested { get; init; } = false;
    public int IdleTimeoutSeconds { get; init; } = 300;
    public string[] ExcludedSids { get; init; } = [];
    public void Validate()
    {
        if (ApplicationId != "LockedSessionLogoff" || SchemaVersion != 1) throw new InvalidDataException("Unsupported application/schema");
        if (LockedTimeoutSeconds < 5 || LockedTimeoutSeconds > 604800) throw new InvalidDataException("Locked timeout must be 5..604800 seconds");
        if (IdleTimeoutSeconds < 5 || IdleTimeoutSeconds > 604800) throw new InvalidDataException("Idle timeout must be 5..604800 seconds");
        if (IdleObservationRequested && (!DryRun || AllowLiveLogoff)) throw new InvalidDataException("Idle observation requires Dry-run checked and Allow LIVE unchecked; idle actions are not implemented");
        if (ExcludedSids is null || ExcludedSids.Length > 256) throw new InvalidDataException("Invalid exclusions");
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var sid in ExcludedSids)
        {
            if (string.IsNullOrWhiteSpace(sid) || sid.Length > 184 || sid != sid.Trim()) throw new InvalidDataException("Invalid SID");
            var canonical = new SecurityIdentifier(sid).Value;
            if (!string.Equals(canonical, sid, StringComparison.OrdinalIgnoreCase) || !seen.Add(canonical)) throw new InvalidDataException("Invalid/duplicate SID");
        }
    }
}
public static class AdminSettingsStore
{
    public const string KeyPath = @"SOFTWARE\LockedSessionLogoff";
    public const string ValueName = "ConfigurationV1";
    public const string DisplayPath = @"HKLM\SOFTWARE\LockedSessionLogoff\ConfigurationV1 (64-bit)";
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true, UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow };
    public static (AdminSettings Settings, string? Raw) Read()
    {
        using var root = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
        using var key = root.OpenSubKey(KeyPath);
        if (key is null) return (new(), null);
        ConfigurationSecurity.Verify(key);
        var value = key.GetValue(ValueName, null, RegistryValueOptions.DoNotExpandEnvironmentNames);
        if (value is null)
        { if (key.ValueCount != 0 || key.SubKeyCount != 0) throw new InvalidDataException("Unrecognized populated application key"); return (new(), null); }
        if (value is not string raw || raw.Length > 65536 || key.GetValueKind(ValueName) != RegistryValueKind.String) throw new InvalidDataException("Invalid configuration type/size");
        var settings = JsonSerializer.Deserialize<AdminSettings>(raw, Options) ?? throw new InvalidDataException("Null configuration");
        settings.Validate(); return (settings, raw);
    }
    public static void Save(AdminSettings settings, string? expectedRaw)
    {
        RequireAdmin(); settings.Validate(); using var lease = ConfigurationSecurity.WriterLease();
        if (Read().Raw != expectedRaw) throw new IOException("Settings changed; reload first");
        using var root = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
        using var key = root.CreateSubKey(KeyPath, true) ?? throw new IOException("Cannot open key");
        ConfigurationSecurity.Verify(key); key.SetValue(ValueName, JsonSerializer.Serialize(settings, Options), RegistryValueKind.String);
    }
    public static void Remove(string? expectedRaw)
    {
        RequireAdmin(); using var lease = ConfigurationSecurity.WriterLease();
        if (Read().Raw != expectedRaw) throw new IOException("Settings changed; reload first");
        using var root = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
        using var key = root.OpenSubKey(KeyPath, true); key?.DeleteValue(ValueName, false);
    }
    private static void RequireAdmin()
    { using var identity = WindowsIdentity.GetCurrent(); if (!new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator)) throw new UnauthorizedAccessException("Elevation required"); }
}
