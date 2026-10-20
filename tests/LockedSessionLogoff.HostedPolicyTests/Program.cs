// Created: 2026-10-20 13:32 America/Los_Angeles
// Path: tests/LockedSessionLogoff.HostedPolicyTests/Program.cs; Owner: maintainers
// Permissions: public read/maintainer write; Purpose: hosting stage cannot enable idle via locked-logoff settings
// Arguments: none; Usage: pure tests; no registry/pipe/native operations
using LockedSessionLogoff.Windows;
var peer = new AgentPeer(new(1, "S-1-5-21-1-2-3-1001", 123), 42, 1234, "synthetic");
var nonce = Guid.NewGuid().ToString("N"); var epoch = Guid.NewGuid().ToString("N");
var tests = new (string Name, Action Run)[]
{
    ("Missing config disabled", () => Check(!HostedIdlePolicy.CreateDisabled(null, null).Enabled)),
    ("Ordinary config disabled", () => Check(!HostedIdlePolicy.CreateDisabled(new(), "raw").Enabled)),
    ("Locked monitoring cannot enable idle", () => Check(!HostedIdlePolicy.CreateDisabled(new() { MonitoringRequested = true }, "raw").Enabled)),
    ("Live authorization cannot enable idle", () => Check(!HostedIdlePolicy.CreateDisabled(new() { MonitoringRequested = true, AllowLiveLogoff = true, DryRun = false }, "raw").Enabled)),
    ("Failed read disabled", () => { var p = HostedIdlePolicy.ReadFailed(); Check(!p.Enabled && p.Mode == "ReadFailed"); }),
    ("No action authority in reply", () => { var r = AgentPolicyRules.ForPeer(HostedIdlePolicy.CreateDisabled(new(), "raw"), peer, nonce, epoch); Check(!r.Enabled && r.DryRun && !r.ActionsEnabled); AgentPolicyRules.ValidateReply(r, nonce); }),
    ("Exclusion retained and applied", () => { var p = HostedIdlePolicy.CreateDisabled(new() { ExcludedSids = [peer.Identity.Sid] }, "raw"); var r = AgentPolicyRules.ForPeer(p, peer, nonce, epoch); Check(!r.Enabled && r.Mode == "ExcludedBySid"); }),
    ("Exclusion array copied", () => { var list = new[] { peer.Identity.Sid }; var p = HostedIdlePolicy.CreateDisabled(new() { ExcludedSids = list }, "raw"); list[0] = "S-1-5-18"; Check(p.ExcludedSids[0] == peer.Identity.Sid); }),
    ("Revision changes with snapshot", () => Check(HostedIdlePolicy.CreateDisabled(new(), "a").Revision != HostedIdlePolicy.CreateDisabled(new(), "b").Revision))
};
int failed = 0;
foreach (var test in tests) { try { test.Run(); Console.WriteLine($"PASS {test.Name}"); } catch (Exception e) { failed++; Console.Error.WriteLine($"FAIL {test.Name}: {e.Message}"); } }
Console.WriteLine($"{tests.Length - failed}/{tests.Length} passed"); return failed == 0 ? 0 : 1;
static void Check(bool ok) { if (!ok) throw new Exception("Assertion failed"); }
