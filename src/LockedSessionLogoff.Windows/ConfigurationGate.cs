// Created: 2026-10-15 11:01 America/Los_Angeles
// Path: src/LockedSessionLogoff.Windows/ConfigurationGate.cs; Owner: maintainers
// Permissions: public read/maintainer write; Purpose: reusable conservative activation gate
// Arguments: validated settings/raw snapshot or explicit error; Usage: serialized caller
using System.Security.Cryptography;
using System.Text;

namespace LockedSessionLogoff.Windows;

public sealed record EffectiveConfiguration(bool Enabled, bool DryRun, bool Debug,
    int TimeoutSeconds, string? ObservedRevision, string Mode);

public sealed class ConfigurationGate
{
    private bool sawDisabled;

    public EffectiveConfiguration Observe(AdminSettings? settings, string? raw, string? error = null)
    {
        if (error is not null)
        {
            sawDisabled = false;
            return Disabled("ReadOrObservationFailed");
        }
        if (raw is null)
        {
            sawDisabled = true;
            return Disabled("MissingConfiguration");
        }
        if (settings is null) { sawDisabled = false; return Disabled("InvalidConfiguration"); }
        try { settings.Validate(); }
        catch (Exception) { sawDisabled = false; return Disabled("InvalidConfiguration"); }
        var revision = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(raw)));
        if (!settings.MonitoringRequested)
        {
            sawDisabled = true;
            return new(false, true, settings.Debug, settings.LockedTimeoutSeconds, revision, "Disabled");
        }
        if (!sawDisabled)
            return new(false, true, settings.Debug, settings.LockedTimeoutSeconds, revision, "AwaitingDisableThenEnable");
        if (settings.ExcludedSids.Length != 0)
            return new(false, true, settings.Debug, settings.LockedTimeoutSeconds, revision, "BlockedUntilSidExclusionsIntegrated");
        return new(true, true, settings.Debug, settings.LockedTimeoutSeconds, revision,
            settings.DryRun ? "ConfiguredDryRun" : "ForcedDryRunEnforcementUnavailable");
    }

    private static EffectiveConfiguration Disabled(string mode) => new(false, true, false, 3600, null, mode);
}
