// Created: 2026-10-14 17:44 America/Los_Angeles
// Path: tests/LockedSessionLogoff.SelfTests/Program.cs; Owner: maintainers
// Permissions: public read/maintainer write; Purpose: deterministic timer regression tests
// Arguments: none; Usage: dotnet run --project tests/LockedSessionLogoff.SelfTests -c Release
using LockedSessionLogoff.Core;

var tests = new (string Name, Action Run)[]
{
    ("Safe defaults", () => { var o = new MonitorOptions(); Check(!o.Enabled && !o.DryRun && !o.Debug); }),
    ("Threshold boundary", () => { var t = new LockTimer(); var o = Active(); Check(See(t, 0, o) == TimerDecision.None); Check(See(t, 59, o) == TimerDecision.None); Check(See(t, 60, o) == TimerDecision.LogOffCandidate); }),
    ("Dry-run candidate only", () => { var t = new LockTimer(); var o = Active() with { DryRun = true }; See(t, 0, o); Check(See(t, 60, o) == TimerDecision.WouldLogOff); }),
    ("Unlock and relock", () => { var t = new LockTimer(); var o = Active(); See(t, 0, o); See(t, 40, o, LockState.Unlocked); See(t, 50, o); Check(See(t, 109, o) == TimerDecision.None); Check(See(t, 110, o) == TimerDecision.LogOffCandidate); }),
    ("Unknown resets", () => { var t = new LockTimer(); var o = Active(); See(t, 0, o); See(t, 59, o, LockState.Unknown); Check(See(t, 61, o) == TimerDecision.None); Check(See(t, 120, o) == TimerDecision.None); }),
    ("Independent sessions", () => { var t = new LockTimer(); var o = Active(); See(t, 0, o); See(t, 30, o, id: 2); Check(See(t, 60, o) == TimerDecision.LogOffCandidate); Check(See(t, 60, o, id: 2) == TimerDecision.None); }),
    ("Identity reuse resets", () => { var t = new LockTimer(); var o = Active(); See(t, 0, o); Check(See(t, 60, o, identity: "new-logon") == TimerDecision.None); }),
    ("Exclusion resets", () => { var t = new LockTimer(); var o = Active(); See(t, 0, o); Check(See(t, 60, o, excluded: true) == TimerDecision.None); Check(See(t, 61, o) == TimerDecision.None); }),
    ("Disabled resets", () => { var t = new LockTimer(); var o = Active(); See(t, 0, o); Check(See(t, 60, o with { Enabled = false }) == TimerDecision.None); Check(See(t, 61, o) == TimerDecision.None); }),
    ("Runtime option change resets", () => { var t = new LockTimer(); var o = Active(); See(t, 0, o); Check(See(t, 60, o with { DryRun = true }) == TimerDecision.None); }),
    ("No historical lock guess", () => { var t = new LockTimer(); Check(See(t, 10000, Active()) == TimerDecision.None); }),
    ("Clock regression resets", () => { var t = new LockTimer(); var o = Active(); See(t, 100, o); Check(See(t, 50, o) == TimerDecision.None); Check(See(t, 109, o) == TimerDecision.None); }),
    ("Forget and reset", () => { var t = new LockTimer(); var o = Active(); See(t, 0, o); t.Forget(1); Check(See(t, 60, o) == TimerDecision.None); t.Reset(); Check(See(t, 120, o) == TimerDecision.None); }),
    ("Invalid timeout rejected", () => { try { (Active() with { Timeout = TimeSpan.Zero }).Validate(); } catch (ArgumentOutOfRangeException) { return; } throw new Exception("Invalid timeout accepted"); })
};
int failed = 0;
foreach (var test in tests)
{
    try { test.Run(); Console.WriteLine($"PASS {test.Name}"); }
    catch (Exception e) { failed++; Console.Error.WriteLine($"FAIL {test.Name}: {e.Message}"); }
}
Console.WriteLine($"{tests.Length - failed}/{tests.Length} passed");
return failed == 0 ? 0 : 1;

static MonitorOptions Active() => new() { Enabled = true, Timeout = TimeSpan.FromSeconds(60) };
static void Check(bool value) { if (!value) throw new Exception("Assertion failed"); }
static TimerDecision See(LockTimer timer, double seconds, MonitorOptions options,
    LockState state = LockState.Locked, int id = 1, string identity = "test-logon", bool excluded = false)
    => timer.Observe(id, identity, state, TimeSpan.FromSeconds(seconds), options, excluded);
