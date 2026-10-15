// Created: 2026-10-15 10:46 America/Los_Angeles
// Path: src/LockedSessionLogoff.Windows/AdminSettings.cs; Owner: maintainers
// Permissions: public read/maintainer write; Purpose: validated machine configuration editor
// Arguments: Read none; Save settings and expected raw value; Remove expected raw value
// Usage: elevated admin UI only writes; current service does NOT read this configuration
using Microsoft.Win32;
using System.Security.Principal;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace LockedSessionLogoff.Windows;

public sealed record AdminSettings
{
    public string ApplicationId { get; init; } = "LockedSessionLogoff";
    public int SchemaVersion { get; init; } = 1;
    public bool MonitoringRequested { get; init; } = false;
    public bool DryRun { get; init; } = false;
    public bool Debug { get; init; } = false;
    public int LockedTimeoutSeconds { get; init; } = 3600;
    public string[] ExcludedSids { get; init; } = [];

    public void Validate()
    {
        if (ApplicationId != "LockedSessionLogoff" || SchemaVersion != 1)
            throw new InvalidDataException("Unsupported application or settings version");
        if (LockedTimeoutSeconds < 5 || LockedTimeoutSeconds > 604800)
            throw new InvalidDataException("Timeout must be 5 through 604800 seconds");
        if (ExcludedSids is null || ExcludedSids.Length > 256)
            throw new InvalidDataException("At most 256 exclusion SIDs are allowed");
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var sid in ExcludedSids)
        {
            if (string.IsNullOrWhiteSpace(sid) || sid.Length > 184 || sid != sid.Trim())
                throw new InvalidDataException("Exclusions must be canonical SID strings");
            var canonical = new SecurityIdentifier(sid).Value;
            if (!string.Equals(canonical, sid, StringComparison.OrdinalIgnoreCase) || !seen.Add(canonical))
                throw new InvalidDataException("Invalid or duplicate exclusion SID");
        }
    }
}

public static class AdminSettingsStore
{
    public const string KeyPath = @"SOFTWARE\LockedSessionLogoff";
    public const string ValueName = "ConfigurationV1";
    public const string DisplayPath = @"HKEY_LOCAL_MACHINE\SOFTWARE\LockedSessionLogoff\ConfigurationV1 (64-bit view)";
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow
    };

    public static (AdminSettings Settings, string? Raw) Read()
    {
        using var root = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
        using var key = root.OpenSubKey(KeyPath, false);
        if (key is null) return (new(), null);
        var raw = key.GetValue(ValueName, null, RegistryValueOptions.DoNotExpandEnvironmentNames);
        if (raw is null)
        {
            if (key.ValueCount != 0 || key.SubKeyCount != 0)
                throw new InvalidDataException("Unrecognized existing application key; not modifying it");
            return (new(), null);
        }
        if (raw is not string text || key.GetValueKind(ValueName) != RegistryValueKind.String || text.Length > 65536)
            throw new InvalidDataException("Unsupported configuration type or size");
        var settings = JsonSerializer.Deserialize<AdminSettings>(text, JsonOptions)
            ?? throw new InvalidDataException("Configuration is null");
        settings.Validate();
        return (settings, text);
    }

    public static void Save(AdminSettings settings, string? expectedRaw)
    {
        RequireAdministrator();
        settings.Validate();
        if (Read().Raw != expectedRaw) throw new IOException("Settings changed; reload before saving");
        using var root = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
        using var key = root.CreateSubKey(KeyPath, true) ?? throw new IOException("Cannot open configuration key");
        key.SetValue(ValueName, JsonSerializer.Serialize(settings, JsonOptions), RegistryValueKind.String);
    }

    public static void Remove(string? expectedRaw)
    {
        RequireAdministrator();
        if (Read().Raw != expectedRaw) throw new IOException("Settings changed; reload before removing");
        using var root = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
        using var key = root.OpenSubKey(KeyPath, true);
        key?.DeleteValue(ValueName, false);
    }

    private static void RequireAdministrator()
    {
        using var identity = WindowsIdentity.GetCurrent();
        if (!new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator))
            throw new UnauthorizedAccessException("Administrator elevation required");
    }
}
