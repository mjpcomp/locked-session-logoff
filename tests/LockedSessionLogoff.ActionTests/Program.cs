// Created: 2026-10-15 15:48 America/Los_Angeles
// Path: tests/LockedSessionLogoff.ActionTests/Program.cs; Owner: maintainers
// Permissions: public read/maintainer write; Purpose: fake backend verifies no-action paths and bounded outcomes
// Arguments: none; Usage: synthetic only; no services/registry/native logoff calls
using LockedSessionLogoff.Windows;
var tests = new (string Name, Action Run)[]
{
    ("Dry-run never calls backend", () => { var s = new Scenario(); Check(s.Fire(mode: s.Mode with { DryRun = true }).Kind == "WouldLogOff" && s.Backend.Calls == 0 && s.Actions.Pending.Count == 0); }),
    ("Saved dry-run wins", () => { var s = new Scenario(); Check(s.Fire(settings: s.Settings with { DryRun = true }).Kind == "WouldLogOff" && s.Backend.Calls == 0); }),
    ("Exclusion never calls backend", () => { var s = new Scenario(); s.Fire(settings: s.Settings with { ExcludedSids = [s.Target.Sid] }); Check(s.Backend.Calls == 0); }),
    ("Unlocked never calls backend", () => { var s = new Scenario(); s.Fire(snapshot: s.Snapshot with { LockState = "Unlocked" }); Check(s.Backend.Calls == 0); }),
    ("Unknown token never calls backend", () => { var s = new Scenario(); s.Fire(unknownToken: true); Check(s.Backend.Calls == 0); }),
    ("Unknown state never calls backend", () => { var s = new Scenario(); s.Fire(snapshot: s.Snapshot with { LockState = "Unknown" }); Check(s.Backend.Calls == 0); }),
    ("Disconnected never calls backend", () => { var s = new Scenario(); s.Fire(snapshot: s.Snapshot with { ConnectionState = 4 }); Check(s.Backend.Calls == 0); }),
    ("Different logon never calls backend", () => { var s = new Scenario(); s.Fire(token: s.Target with { AuthenticationId = 456 }); Check(s.Backend.Calls == 0); }),
    ("Changed WTS generation denied", () => { var s = new Scenario(); s.Fire(snapshot: s.Snapshot with { Identity = "changed" }); Check(s.Backend.Calls == 0); }),
    ("Changed raw configuration denied", () => { var s = new Scenario(); s.Fire(raw: "changed"); Check(s.Backend.Calls == 0); }),
    ("Missing configuration denied", () => { var s = new Scenario(); s.Fire(raw: null); Check(s.Backend.Calls == 0); }),
    ("Monitoring disabled denied", () => { var s = new Scenario(); s.Fire(settings: s.Settings with { MonitoringRequested = false }); Check(s.Backend.Calls == 0); }),
    ("Effective gate disabled denied", () => { var s = new Scenario(); s.Fire(mode: s.Mode with { Enabled = false }); Check(s.Backend.Calls == 0); }),
    ("Live permit required", () => { var s = new Scenario(); s.Fire(settings: s.Settings with { AllowLiveLogoff = false }); Check(s.Backend.Calls == 0); }),
    ("Backend capability required", () => { var s = new Scenario(false); s.Fire(); Check(s.Backend.Calls == 0); }),
    ("Live request tracked with action ID", () => { var s = new Scenario(); var r = s.Fire(); Check(r.Kind == "LogoffRequestAccepted" && s.Backend.Calls == 1 && s.Actions.Pending.Single().ActionId == r.ActionId); }),
    ("Duplicate live request suppressed", () => { var s = new Scenario(); s.Fire(); Check(s.Fire().Kind == "LogoffDuplicateSuppressed" && s.Backend.Calls == 1); }),
    ("Backend exception no destructive retry", () => { var s = new Scenario(); s.Backend.Fail = true; Check(s.Fire().Kind == "LogoffRequestFailed"); s.Fire(); Check(s.Backend.Calls == 1 && s.Actions.Pending.Count == 0); }),
    ("Accepted request not completion", () => { var s = new Scenario(); s.Fire(); Check(s.Actions.Observe(s.Id, SessionObservation.SameIdentityPresent, TimeSpan.FromSeconds(1)) is null && s.Actions.Pending.Count == 1); }),
    ("Absent target observed", () => { var s = new Scenario(); s.Fire(); Check(s.Actions.Observe(s.Id, SessionObservation.SessionAbsent, TimeSpan.FromSeconds(1))?.Kind == "LogoffTargetAbsentObserved" && s.Actions.Pending.Count == 0); }),
    ("Replacement distinctly observed", () => { var s = new Scenario(); s.Fire(); Check(s.Actions.Observe(s.Id, SessionObservation.IdentityReplaced, TimeSpan.FromSeconds(1))?.Kind == "LogoffTargetIdentityReplacedObserved"); }),
    ("Unknown is not successful completion", () => { var s = new Scenario(); s.Fire(); Check(s.Actions.Observe(s.Id, SessionObservation.Unknown, TimeSpan.FromSeconds(1)) is null); }),
    ("Deadline unknown is unconfirmed", () => { var s = new Scenario(); s.Fire(); Check(s.Actions.Observe(s.Id, SessionObservation.Unknown, TimeSpan.FromSeconds(30))?.Kind == "LogoffOutcomeUnconfirmed" && s.Actions.Pending.Count == 0); }),
    ("Late absence not claimed within deadline", () => { var s = new Scenario(); s.Fire(); Check(s.Actions.Observe(s.Id, SessionObservation.SessionAbsent, TimeSpan.FromSeconds(31))?.Kind == "LogoffOutcomeUnconfirmed"); }),
    ("Disable does not erase pending outcome", () => { var s = new Scenario(); s.Fire(); s.Fire(mode: s.Mode with { Enabled = false }); Check(s.Actions.Observe(s.Id, SessionObservation.SessionAbsent, TimeSpan.FromSeconds(1))?.Kind == "LogoffTargetAbsentObserved"); }),
    ("Stop abandons rather than confirms", () => { var s = new Scenario(); s.Fire(); Check(s.Actions.Abandon("ServiceStopped").Single().Kind == "LogoffOutcomeUnconfirmed" && s.Actions.Pending.Count == 0); }),
    ("Unknown action ignored", () => { var s = new Scenario(); Check(s.Actions.Observe("unknown", SessionObservation.SessionAbsent, TimeSpan.Zero) is null); })
};
int failures = 0;
foreach (var test in tests) { try { test.Run(); Console.WriteLine($"PASS {test.Name}"); } catch (Exception e) { failures++; Console.Error.WriteLine($"FAIL {test.Name}: {e.Message}"); } }
Console.WriteLine($"{tests.Length - failures}/{tests.Length} passed"); return failures == 0 ? 0 : 1;
static void Check(bool value) { if (!value) throw new Exception("Assertion failed"); }
internal sealed class FakeBackend : ILogoffBackend
{
    public int Calls; public bool Fail;
    public void Request(int sessionId) { Calls++; if (Fail) throw new InvalidOperationException("Synthetic failure"); }
}
internal sealed class Scenario
{
    public readonly FakeBackend Backend = new();
    public readonly LogoffCoordinator Actions;
    public readonly TokenIdentity Target = new(1, "S-1-5-21-1-2-3-1001", 123);
    public readonly SessionSnapshot Snapshot = new(1, "Console", 0, "Synthetic", "Locked", 123, "generation", null);
    public readonly AdminSettings Settings = new() { MonitoringRequested = true, AllowLiveLogoff = true, DryRun = false, LockedTimeoutSeconds = 20 };
    public readonly EffectiveConfiguration Mode;
    public Scenario(bool capability = true)
    {
        Actions = new(Backend, capability);
        var gate = new ConfigurationGate(sidCapable: true, liveCapable: true);
        gate.Observe(new AdminSettings(), "off"); Mode = gate.Observe(Settings, "saved");
    }
    public string Id => Actions.Pending.Single().ActionId;
    public ActionEvent Fire(AdminSettings? settings = null, EffectiveConfiguration? mode = null, TokenIdentity? token = null,
        SessionSnapshot? snapshot = null, string? raw = "saved", bool unknownToken = false)
        => Actions.Execute(Target, Snapshot.Identity, "saved", settings ?? Settings, raw, mode ?? Mode,
            unknownToken ? null : token ?? Target, snapshot ?? Snapshot, TimeSpan.Zero);
}
