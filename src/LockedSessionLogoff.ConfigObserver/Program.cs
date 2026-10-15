// Created: 2026-10-15 11:01 America/Los_Angeles
// Path: src/LockedSessionLogoff.ConfigObserver/Program.cs; Owner: maintainers
// Permissions: public read/maintainer write; Purpose: read saved settings and observe only
// Arguments: none; Usage: run beside admin GUI, Ctrl+C stops; no writes/actions
using System.Diagnostics;
using System.Text.Json;
using LockedSessionLogoff.Core;
using LockedSessionLogoff.Windows;

if (args.Length != 0) { Console.Error.WriteLine("Usage: LockedSessionLogoff.ConfigObserver.exe (no arguments)"); return 2; }
Console.Error.WriteLine("CONFIGURED DRY-RUN ONLY. No service installation, registry writes, locks, or logoffs.");
Console.Error.WriteLine("If already enabled, save monitoring OFF, wait for Disabled here, then save ON.");
var gate = new ConfigurationGate();
var timer = new LockTimer();
var clock = Stopwatch.StartNew();
var lastPoll = clock.Elapsed;
EffectiveConfiguration? previous = null;
var states = new Dictionary<int, string>();
var reported = new HashSet<int>();
using var stop = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) => { e.Cancel = true; stop.Cancel(); };

void ClearTimers() { timer.Reset(); states.Clear(); reported.Clear(); }
void Emit(object record) => Console.WriteLine(JsonSerializer.Serialize(record));
void Accept(EffectiveConfiguration effective)
{
    if (effective == previous) return;
    ClearTimers();
    previous = effective;
    Emit(new { TimestampUtc = DateTimeOffset.UtcNow, Event = "ConfigurationEffectiveForConsoleObserver", Configuration = effective });
}

while (!stop.IsCancellationRequested)
{
    try
    {
        var now = clock.Elapsed;
        if (now - lastPoll > TimeSpan.FromSeconds(10))
        {
            ClearTimers();
            Accept(gate.Observe(null, null, "ObservationGap"));
        }
        lastPoll = now;
        var saved = AdminSettingsStore.Read();
        var effective = gate.Observe(saved.Settings, saved.Raw);
        Accept(effective);
        if (effective.Enabled)
        {
            var snapshots = SessionReader.ReadAll();
            var ids = snapshots.Select(s => s.Id).ToHashSet();
            foreach (var id in states.Keys.Where(id => !ids.Contains(id)).ToArray())
            { timer.Forget(id); states.Remove(id); reported.Remove(id); }
            var options = new MonitorOptions { Enabled = true, DryRun = true, Debug = effective.Debug, Timeout = TimeSpan.FromSeconds(effective.TimeoutSeconds) };
            foreach (var session in snapshots)
            {
                if (!session.CanObserveTimer)
                {
                    timer.Forget(session.Id); states.Remove(session.Id); reported.Remove(session.Id);
                    if (effective.Debug && session.Error is not null)
                        Emit(new { TimestampUtc = DateTimeOffset.UtcNow, Event = "SessionQueryUnavailable", SessionId = session.Id });
                    continue;
                }
                var key = session.Identity + ":" + session.LockState;
                if (!states.TryGetValue(session.Id, out var old) || old != key)
                {
                    timer.Forget(session.Id); reported.Remove(session.Id); states[session.Id] = key;
                    Emit(new { TimestampUtc = DateTimeOffset.UtcNow, Event = session.LockState, SessionId = session.Id });
                }
                var state = session.LockState == "Locked" ? LockState.Locked : LockState.Unlocked;
                var decision = timer.Observe(session.Id, session.Identity, state, now, options);
                if (decision == TimerDecision.WouldLogOff && reported.Add(session.Id))
                    Emit(new { TimestampUtc = DateTimeOffset.UtcNow, Event = "WouldLogOff", SessionId = session.Id, effective.ObservedRevision, DryRun = true });
            }
        }
    }
    catch (Exception e)
    {
        ClearTimers();
        Accept(gate.Observe(null, null, "Failure"));
        if (previous?.Debug == true) Emit(new { TimestampUtc = DateTimeOffset.UtcNow, Event = "DiagnosticError", ErrorType = e.GetType().Name });
    }
    try { await Task.Delay(2000, stop.Token); }
    catch (OperationCanceledException) { break; }
}
return 0;
