// Created: 2026-10-20 13:32 America/Los_Angeles
// Path: src/LockedSessionLogoff.Windows/HostedIdlePolicy.cs; Owner: maintainers
// Permissions: public read/maintainer write; Purpose: deliberately disabled hosting-stage policy
// Arguments: trusted settings/raw snapshot; Usage: SYSTEM host, pure tests; NOT saved idle enablement
using System.Security.Cryptography;
using System.Text;
namespace LockedSessionLogoff.Windows;
public static class HostedIdlePolicy
{
    public static AgentIdlePolicy CreateDisabled(AdminSettings? settings, string? raw)
    {
        if (settings is null || raw is null) return new(false, 300, null, "Disabled", []);
        settings.Validate();
        var revision = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(raw)));
        return new(false, 300, revision, "Disabled", settings.ExcludedSids.ToArray());
    }
    public static AgentIdlePolicy ReadFailed() => new(false, 300, null, "ReadFailed", []);
}
