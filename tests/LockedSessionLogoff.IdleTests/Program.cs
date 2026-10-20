// Created: 2026-10-20 09:36 America/Los_Angeles
// Path: tests/LockedSessionLogoff.IdleTests/Program.cs; Owner: maintainers
// Permissions: public read/maintainer write; Purpose: synthetic idle fresh-window/reset/rollover tests
// Arguments: none; Usage: no Windows queries, user-input injection or OS actions
using LockedSessionLogoff.Core;
var tests = new (string Name, Action Run)[]
{
    ("Stale initial input grants fresh window", () => { var t = new InputIdleTimer(); None(O(t, 100000, 1)); }),
    ("No early threshold", () => { var t = new InputIdleTimer(); O(t, 0); None(O(t, 4999)); Hit(O(t, 5000)); }),
    ("Input change resets window", () => { var t = new InputIdleTimer(); O(t, 0); None(O(t, 4000, 4000)); None(O(t, 8000, 4000)); Hit(O(t, 9000, 4000)); }),
    ("One candidate per unchanged episode", () => { var t = new InputIdleTimer(); O(t, 0); Hit(O(t, 5000)); None(O(t, 7000)); }),
    ("New input permits later candidate", () => { var t = new InputIdleTimer(); O(t, 0); Hit(O(t, 5000)); None(O(t, 6000, 6000)); Hit(O(t, 11000, 6000)); }),
    ("Lock cancels, unlock grants fresh time", () => { var t = new InputIdleTimer(); O(t, 0); None(O(t, 4000, unlocked: false)); None(O(t, 5000)); None(O(t, 9999)); Hit(O(t, 10000)); }),
    ("Unknown input cancels", () => { var t = new InputIdleTimer(); O(t, 0); None(O(t, 4000, null)); None(O(t, 5000)); Hit(O(t, 10000)); }),
    ("Unknown identity cancels", () => { var t = new InputIdleTimer(); O(t, 0); None(O(t, 4000, identity: "")); None(O(t, 5000)); Hit(O(t, 10000)); }),
    ("Logon generation change resets", () => { var t = new InputIdleTimer(); O(t, 0); None(O(t, 4000, identity: "new")); None(O(t, 8999, identity: "new")); Hit(O(t, 9000, identity: "new")); }),
    ("Timeout change resets", () => { var t = new InputIdleTimer(); O(t, 0); None(O(t, 4000, seconds: 6)); None(O(t, 9000, seconds: 6)); Hit(O(t, 10000, seconds: 6)); }),
    ("Observation gap grants fresh window", () => { var t = new InputIdleTimer(); O(t, 0); None(O(t, 11000)); None(O(t, 15000)); Hit(O(t, 16000)); }),
    ("Backward uptime resets", () => { var t = new InputIdleTimer(); O(t, 1000, 500); None(O(t, 500, 500)); None(O(t, 5499, 500)); Hit(O(t, 5500, 500)); }),
    ("Native 32-bit tick rollover", () => { var t = new InputIdleTimer(); long start = (long)uint.MaxValue - 999; var input = unchecked((uint)start); O(t, start, input); None(O(t, start + 4999, input)); Hit(O(t, start + 5000, input)); }),
    ("Non-incremental input resets", () => { var t = new InputIdleTimer(); O(t, 10000, 9000); None(O(t, 12000, 8000)); None(O(t, 16999, 8000)); Hit(O(t, 17000, 8000)); }),
    ("Future/ambiguous input does not count", () => { var t = new InputIdleTimer(); None(O(t, 1000, 2000)); None(O(t, 6000, 2000)); Hit(O(t, 11000, 2000)); }),
    ("Explicit reset discards timer", () => { var t = new InputIdleTimer(); O(t, 0); t.Reset(); None(O(t, 5000)); Hit(O(t, 10000)); }),
    ("Invalid timeout rejected", () => { var t = new InputIdleTimer(); try { O(t, 0, seconds: 1); } catch (ArgumentOutOfRangeException) { return; } throw new Exception("Expected rejection"); })
};
int failures = 0;
foreach (var test in tests) { try { test.Run(); Console.WriteLine($"PASS {test.Name}"); } catch (Exception e) { failures++; Console.Error.WriteLine($"FAIL {test.Name}: {e.Message}"); } }
Console.WriteLine($"{tests.Length - failures}/{tests.Length} passed"); return failures == 0 ? 0 : 1;
static InputIdleDecision O(InputIdleTimer t, long now, uint? input = 0, bool unlocked = true, string identity = "synthetic", int seconds = 5) => t.Observe(now, input, unlocked, identity, TimeSpan.FromSeconds(seconds));
static void None(InputIdleDecision d) { if (d != InputIdleDecision.None) throw new Exception("Early/unexpected candidate"); }
static void Hit(InputIdleDecision d) { if (d != InputIdleDecision.ThresholdReached) throw new Exception("Missing candidate"); }
