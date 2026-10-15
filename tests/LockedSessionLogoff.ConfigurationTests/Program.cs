// Created: 2026-10-15 11:01 America/Los_Angeles
// Path: tests/LockedSessionLogoff.ConfigurationTests/Program.cs; Owner: maintainers
// Permissions: public read/maintainer write; Purpose: deterministic no-write tests
// Arguments: none; Usage: dotnet run with this project on Windows
using LockedSessionLogoff.Windows;
using LockedSessionLogoff.Core;

var off = new AdminSettings();
var on = off with { MonitoringRequested = true, LockedTimeoutSeconds = 20 };
var tests = new (string Name, Action Run)[]
{
    ("Missing disabled", () => Check(!new ConfigurationGate().Observe(off, null).Enabled)),
    ("Saved enabled cannot autoactivate", () => Check(new ConfigurationGate().Observe(on, "on").Mode == "AwaitingDisableThenEnable")),
    ("Explicit off then on", () => { var g = new ConfigurationGate(); g.Observe(off, "off"); Check(g.Observe(on, "on").Enabled); }),
    ("Missing then explicit on", () => { var g = new ConfigurationGate(); g.Observe(off, null); Check(g.Observe(on, "on").Enabled); }),
    ("Force dry-run", () => { var g = new ConfigurationGate(); g.Observe(off, "off"); var v = g.Observe(on with { DryRun = false }, "on"); Check(v.Enabled && v.DryRun && v.Mode == "ForcedDryRunEnforcementUnavailable"); }),
    ("Failure disarms", () => { var g = new ConfigurationGate(); g.Observe(off, "off"); g.Observe(on, "on"); Check(!g.Observe(null, null, "error").Enabled); Check(!g.Observe(on, "on").Enabled); }),
    ("Invalid timeout blocks", () => Check(!new ConfigurationGate().Observe(on with { LockedTimeoutSeconds = 0 }, "bad").Enabled)),
    ("Unsupported schema blocks", () => Check(!new ConfigurationGate().Observe(off with { SchemaVersion = 2 }, "bad").Enabled)),
    ("Null exclusions block", () => Check(!new ConfigurationGate().Observe(off with { ExcludedSids = null! }, "bad").Enabled)),
    ("Duplicate SID rejected", () => { try { (off with { ExcludedSids = ["S-1-5-18", "S-1-5-18"] }).Validate(); } catch (InvalidDataException) { return; } throw new Exception("Duplicate accepted"); }),
    ("Unimplemented exclusions block", () => { var g = new ConfigurationGate(); g.Observe(off, "off"); Check(g.Observe(on with { ExcludedSids = ["S-1-5-18"] }, "on").Mode == "BlockedUntilSidExclusionsIntegrated"); }),
    ("Unchanged reads preserve timer", () =>
    {
        var g = new ConfigurationGate(); g.Observe(off, "off");
        var first = g.Observe(on, "on"); var second = g.Observe(on with { }, "on"); Check(first == second);
        var t = new LockTimer(); var o = new MonitorOptions { Enabled = second.Enabled, DryRun = second.DryRun, Timeout = TimeSpan.FromSeconds(second.TimeoutSeconds) };
        t.Observe(1, "synthetic", LockState.Locked, TimeSpan.Zero, o);
        Check(t.Observe(1, "synthetic", LockState.Locked, TimeSpan.FromSeconds(20), o) == TimerDecision.WouldLogOff);
    }),
    ("Timeout reload", () => { var g = new ConfigurationGate(); g.Observe(off, "off"); Check(g.Observe(on with { LockedTimeoutSeconds = 45 }, "new").TimeoutSeconds == 45); }),
    ("Off immediately disables", () => { var g = new ConfigurationGate(); g.Observe(off, "off"); g.Observe(on, "on"); Check(!g.Observe(off, "off-again").Enabled); }),
    ("Changed raw has changed revision", () => { var g = new ConfigurationGate(); Check(g.Observe(off, "a").ObservedRevision != g.Observe(off, "b").ObservedRevision); })
};
var failures = 0;
foreach (var test in tests)
{
    try { test.Run(); Console.WriteLine($"PASS {test.Name}"); }
    catch (Exception e) { failures++; Console.Error.WriteLine($"FAIL {test.Name}: {e.Message}"); }
}
Console.WriteLine($"{tests.Length - failures}/{tests.Length} passed");
return failures == 0 ? 0 : 1;
static void Check(bool value) { if (!value) throw new Exception("Assertion failed"); }
