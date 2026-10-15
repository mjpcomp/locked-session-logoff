// Created: 2026-10-15 12:13 America/Los_Angeles
// Path: tests/LockedSessionLogoff.EligibilityTests/Program.cs; Owner: maintainers
// Permissions: public read/maintainer write; Purpose: synthetic no-write/no-logoff tests
// Arguments: none; Usage: dotnet run with this project on Windows
using LockedSessionLogoff.Windows;
var identity = new TokenIdentity(1, "S-1-5-21-1-2-3-1001", 123);
var locked = new SessionSnapshot(1, "Console", 0, "Synthetic", "Locked", 123, "generation", null);
bool Allow(TokenIdentity? actual, SessionSnapshot? snapshot, string[]? excluded = null) => SessionEligibility.Allows(identity, actual, snapshot, "generation", excluded ?? []);
EffectiveConfiguration Live(AdminSettings settings, bool capable = true) { var gate = new ConfigurationGate(sidCapable: true, liveCapable: capable); gate.Observe(new AdminSettings(), "off"); return gate.Observe(settings, "on"); }
var on = new AdminSettings { MonitoringRequested = true };
var tests = new (string Name, Action Run)[]
{
    ("Matching locked identity", () => Check(Allow(identity, locked))),
    ("Null token denied", () => Check(!Allow(null, locked))),
    ("Null snapshot denied", () => Check(!Allow(identity, null))),
    ("Session reuse authentication differs", () => Check(!Allow(identity with { AuthenticationId = 124 }, locked))),
    ("Different SID denied", () => Check(!Allow(identity with { Sid = "S-1-5-18" }, locked))),
    ("Different session denied", () => Check(!Allow(identity with { SessionId = 2 }, locked))),
    ("Excluded SID denied", () => Check(!Allow(identity, locked, [identity.Sid]))),
    ("Unrelated exclusion permits", () => Check(Allow(identity, locked, ["S-1-5-18"]))),
    ("Unlocked denied", () => Check(!Allow(identity, locked with { LockState = "Unlocked" }))),
    ("Unknown denied", () => Check(!Allow(identity, locked with { LockState = "Unknown" }))),
    ("Disconnected denied", () => Check(!Allow(identity, locked with { ConnectionState = 4 }))),
    ("Query error denied", () => Check(!Allow(identity, locked with { Error = "synthetic" }))),
    ("WTS generation change denied", () => Check(!Allow(identity, locked with { Identity = "new-generation" }))),
    ("Backend capability required", () => Check(Live(on with { AllowLiveLogoff = true }, false).DryRun)),
    ("Live permit required", () => Check(Live(on).DryRun)),
    ("Dry-run wins over live permit", () => Check(Live(on with { AllowLiveLogoff = true, DryRun = true }).DryRun)),
    ("Explicit live mode", () => Check(!Live(on with { AllowLiveLogoff = true, DryRun = false }).DryRun))
};
int failures = 0;
foreach (var test in tests) { try { test.Run(); Console.WriteLine($"PASS {test.Name}"); } catch (Exception e) { failures++; Console.Error.WriteLine($"FAIL {test.Name}: {e.Message}"); } }
Console.WriteLine($"{tests.Length - failures}/{tests.Length} passed"); return failures == 0 ? 0 : 1;
static void Check(bool value) { if (!value) throw new Exception("Assertion failed"); }
