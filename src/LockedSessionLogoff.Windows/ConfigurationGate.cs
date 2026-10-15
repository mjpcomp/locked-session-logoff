// Updated: 2026-10-15 12:13 America/Los_Angeles
// Path: src/LockedSessionLogoff.Windows/ConfigurationGate.cs; Owner: maintainers
// Permissions: public read/maintainer write; Purpose: activation and immutable backend capabilities
// Arguments: sidCapable/liveCapable are constructor capabilities, not settings
// Usage: serialized service; standalone diagnostic defaults cannot authorize live actions
using System.Security.Cryptography;
using System.Text;
namespace LockedSessionLogoff.Windows;
public sealed record EffectiveConfiguration(bool Enabled, bool DryRun, bool Debug, int TimeoutSeconds, string? ObservedRevision, string Mode);
public sealed class ConfigurationGate(bool sidCapable = false, bool liveCapable = false)
{
    private bool sawDisabled;
    public EffectiveConfiguration Observe(AdminSettings? settings, string? raw, string? error = null)
    {
        if (error is not null) { sawDisabled = false; return Disabled("ReadOrObservationFailed"); }
        if (raw is null) { sawDisabled = true; return Disabled("MissingConfiguration"); }
        if (settings is null) { sawDisabled = false; return Disabled("InvalidConfiguration"); }
        try { settings.Validate(); } catch (Exception) { sawDisabled = false; return Disabled("InvalidConfiguration"); }
        var revision = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(raw)));
        if (!settings.MonitoringRequested) { sawDisabled = true; return new(false, true, settings.Debug, settings.LockedTimeoutSeconds, revision, "Disabled"); }
        if (!sawDisabled) return new(false, true, settings.Debug, settings.LockedTimeoutSeconds, revision, "AwaitingDisableThenEnable");
        if (settings.ExcludedSids.Length != 0 && !sidCapable) return new(false, true, settings.Debug, settings.LockedTimeoutSeconds, revision, "BlockedUntilSidExclusionsIntegrated");
        var live = liveCapable && settings.AllowLiveLogoff && !settings.DryRun;
        return new(true, !live, settings.Debug, settings.LockedTimeoutSeconds, revision, live ? "ConfiguredLiveLogoff" : settings.DryRun ? "ConfiguredDryRun" : "ForcedDryRunEnforcementUnavailable");
    }
    private static EffectiveConfiguration Disabled(string mode) => new(false, true, false, 3600, null, mode);
}
