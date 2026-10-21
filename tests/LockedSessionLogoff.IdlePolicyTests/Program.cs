// Created: 2026-10-21 11:16 America/Los_Angeles
// Path: tests/LockedSessionLogoff.IdlePolicyTests/Program.cs; Owner: maintainers
// Permissions: public read/maintainer write; Purpose: independent default-off policy, restart/error/gap and exclusion cases
// Arguments: none; Usage: synthetic only, native authentication remains separate validation
using System.Text.Json;
using LockedSessionLogoff.Windows;
var off = new AdminSettings { DryRun = true, IdleTimeoutSeconds = 20 };
var on = off with { IdleObservationRequested = true };
var peer = new AgentPeer(new(1, "S-1-5-21-1-2-3-1001", 123), 42, 1234, "synthetic");
AgentIdlePolicy Arm(IdleObservationGate g) { g.Observe(off, "off", 0); return g.Observe(on, "on", 1000); }
var tests = new (string Name, Action Run)[]
{
    ("Default idle OFF", () => { var s = new AdminSettings(); Check(!s.IdleObservationRequested && s.IdleTimeoutSeconds == 300); }),
    ("Legacy JSON idle OFF", () => { var s = JsonSerializer.Deserialize<AdminSettings>("{\"MonitoringRequested\":false}")!; Check(!s.IdleObservationRequested && s.IdleTimeoutSeconds == 300); }),
    ("Missing configuration disabled", () => Check(!new IdleObservationGate().Observe(null, null, 0).Enabled)),
    ("Saved ON cannot autoactivate", () => Check(new IdleObservationGate().Observe(on, "on", 0).Mode == "AwaitingDisableThenEnable")),
    ("OFF then ON enables report-only policy", () => { var p = Arm(new()); Check(p.Enabled && p.Mode == "ConfiguredDryRun" && p.IdleTimeoutSeconds == 20); }),
    ("Locked monitoring not required", () => { Check(!on.MonitoringRequested); Check(Arm(new()).Enabled); }),
    ("Locked monitoring cannot enable idle", () => Check(!new IdleObservationGate().Observe(off with { MonitoringRequested = true }, "locked-on", 0).Enabled)),
    ("New service gate requires rearm", () => { Arm(new()); Check(!new IdleObservationGate().Observe(on, "on", 0).Enabled); }),
    ("Read error disarms", () => { var g = new IdleObservationGate(); Arm(g); Check(g.Observe(null, null, 2000, "Synthetic").Mode == "ReadFailed"); Check(g.Observe(on, "on", 3000).Mode == "AwaitingDisableThenEnable"); }),
    ("OFF ON recovers after error", () => { var g = new IdleObservationGate(); Arm(g); g.Observe(null, null, 2000, "Synthetic"); g.Observe(off, "off", 3000); Check(g.Observe(on, "on", 4000).Enabled); }),
    ("Long service observation gap disarms", () => { var g = new IdleObservationGate(); Arm(g); Check(g.Observe(on, "on", 12000).Mode == "AwaitingDisableThenEnable"); }),
    ("Backward service clock disarms", () => { var g = new IdleObservationGate(); Arm(g); Check(!g.Observe(on, "on", 500).Enabled); }),
    ("Negative time rejected safely", () => Check(new IdleObservationGate().Observe(on, "on", -1).Mode == "ReadFailed")),
    ("Missing raw cannot arm supplied ON", () => Check(new IdleObservationGate().Observe(on, null, 0).Mode == "ReadFailed")),
    ("Unsafe dry-run combination rejected", () => Reject(() => (on with { DryRun = false }).Validate())),
    ("Live permit incompatible with idle observation", () => Reject(() => (on with { AllowLiveLogoff = true }).Validate())),
    ("Idle bounds rejected", () => { Reject(() => (off with { IdleTimeoutSeconds = 4 }).Validate()); Reject(() => (off with { IdleTimeoutSeconds = 604801 }).Validate()); }),
    ("Idle bounds accepted", () => { (off with { IdleTimeoutSeconds = 5 }).Validate(); (off with { IdleTimeoutSeconds = 604800 }).Validate(); }),
    ("Timeout/raw change updates revision", () => { var g = new IdleObservationGate(); var first = Arm(g); var next = g.Observe(on with { IdleTimeoutSeconds = 40 }, "new-raw", 2000); Check(next.Enabled && next.IdleTimeoutSeconds == 40 && next.Revision != first.Revision); }),
    ("Shared exclusion copied and filtered", () => { var g = new IdleObservationGate(); g.Observe(off, "off", 0); var a = new[] { peer.Identity.Sid }; var p = g.Observe(on with { ExcludedSids = a }, "excluded", 1000); a[0] = "S-1-5-18"; var r = AgentPolicyRules.ForPeer(p, peer, Guid.NewGuid().ToString("N"), Guid.NewGuid().ToString("N")); Check(!r.Enabled && r.Mode == "ExcludedBySid" && r.DryRun && !r.ActionsEnabled); }),
    ("Enabled idle still never grants actions", () => { var r = AgentPolicyRules.ForPeer(Arm(new()), peer, Guid.NewGuid().ToString("N"), Guid.NewGuid().ToString("N")); Check(r.Enabled && r.DryRun && !r.ActionsEnabled); })
};
int failed = 0;
foreach (var test in tests) { try { test.Run(); Console.WriteLine($"PASS {test.Name}"); } catch (Exception e) { failed++; Console.Error.WriteLine($"FAIL {test.Name}: {e.Message}"); } }
Console.WriteLine($"{tests.Length - failed}/{tests.Length} passed"); return failed == 0 ? 0 : 1;
static void Check(bool ok) { if (!ok) throw new Exception("Assertion failed"); }
static void Reject(Action action) { try { action(); } catch (InvalidDataException) { return; } throw new Exception("Expected rejection"); }
