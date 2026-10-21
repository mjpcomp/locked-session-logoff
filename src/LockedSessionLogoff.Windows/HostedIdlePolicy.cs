// Updated: 2026-10-21 11:16 America/Los_Angeles
// Path: src/LockedSessionLogoff.Windows/HostedIdlePolicy.cs; Owner: maintainers
// Permissions: public read/maintainer write; Purpose: independent idle activation with conservative config/time guards
// Arguments: trusted settings/raw, monotonic ms, optional read error; Usage: serialized host; never authorizes OS actions
using System.Security.Cryptography;
using System.Text;
namespace LockedSessionLogoff.Windows;
public static class HostedIdlePolicy
{
    // Retained hard-disabled factory for earlier hosting-stage regression cases.
    public static AgentIdlePolicy CreateDisabled(AdminSettings? settings, string? raw)
    {
        if (settings is null || raw is null) return new(false, 300, null, "Disabled", []);
        settings.Validate();
        var revision = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(raw)));
        return new(false, 300, revision, "Disabled", settings.ExcludedSids.ToArray());
    }
    public static AgentIdlePolicy ReadFailed() => new(false, 300, null, "ReadFailed", []);
}
public sealed class IdleObservationGate
{
    private bool sawDisabled; private long? lastObservation;
    public AgentIdlePolicy Observe(AdminSettings? settings, string? raw, long nowMilliseconds, string? error = null)
    {
        bool discontinuity = nowMilliseconds < 0 || (lastObservation.HasValue &&
            (nowMilliseconds < lastObservation.Value || nowMilliseconds - lastObservation.Value > 10000));
        if (discontinuity) sawDisabled = false;
        lastObservation = nowMilliseconds >= 0 ? nowMilliseconds : null;
        if (error is not null || nowMilliseconds < 0) return Fail();
        if (raw is null)
        {
            if (settings?.IdleObservationRequested == true) return Fail();
            sawDisabled = true; return new(false, 300, null, "Disabled", []);
        }
        if (settings is null) return Fail();
        try { settings.Validate(); } catch (Exception) { return Fail(); }
        var revision = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(raw)));
        var exclusions = settings.ExcludedSids.ToArray();
        if (!settings.IdleObservationRequested)
        { sawDisabled = true; return new(false, settings.IdleTimeoutSeconds, revision, "Disabled", exclusions); }
        if (!sawDisabled) return new(false, settings.IdleTimeoutSeconds, revision, "AwaitingDisableThenEnable", exclusions);
        return new(true, settings.IdleTimeoutSeconds, revision, "ConfiguredDryRun", exclusions);
    }
    private AgentIdlePolicy Fail() { sawDisabled = false; return HostedIdlePolicy.ReadFailed(); }
}
