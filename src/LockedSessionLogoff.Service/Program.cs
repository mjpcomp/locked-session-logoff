// Updated: 2026-10-15 11:21 America/Los_Angeles
// Path: src/LockedSessionLogoff.Service/Program.cs; Owner: maintainers
// Permissions: public read/maintainer write; Purpose: configured dry-run SCM service
// Arguments: SCM none; --self-check; --console [--observe-seconds N]
// Usage: installed by Install.ps1; no live action APIs
using System.Diagnostics;
using System.ServiceProcess;
using System.Text.Json;
using LockedSessionLogoff.Core;
using LockedSessionLogoff.Windows;

if (args.SequenceEqual(new[] { "--self-check" })) { SessionReader.CheckLayout(); Console.WriteLine("Service layout check passed; no service started"); return 0; }
if (args.Length == 0) { using var service = new ConfiguredService(); ServiceBase.Run(service); return 0; }
int seconds = 60;
if (args[0] != "--console" || (args.Length != 1 && (args.Length != 3 || args[1] != "--observe-seconds" || !int.TryParse(args[2], out seconds))) || seconds < 5 || seconds > 604800) return 2;
Console.WriteLine("CONSOLE DRY-RUN: polling only; not service notification validation");
using var stop = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) => { e.Cancel = true; stop.Cancel(); };
var console = new ConfiguredMonitor(false, seconds);
try { while (!stop.IsCancellationRequested) { console.Poll(); await Task.Delay(2000, stop.Token); } }
catch (OperationCanceledException) { }
return 0;

internal sealed class ConfiguredService : ServiceBase
{
    private readonly ConfiguredMonitor monitor = new(true);
    private System.Threading.Timer? poller;
    public ConfiguredService()
    {
        ServiceName = "LockedSessionLogoff"; AutoLog = false; CanStop = true; CanShutdown = true;
        CanHandleSessionChangeEvent = true; CanHandlePowerEvent = true;
    }
    protected override void OnStart(string[] args)
    {
        SessionReader.CheckLayout();
        poller = new System.Threading.Timer(_ => monitor.Poll(), null, TimeSpan.Zero, TimeSpan.FromSeconds(2));
    }
    protected override void OnSessionChange(SessionChangeDescription change) => monitor.SessionChanged(change.SessionId, change.Reason.ToString());
    protected override bool OnPowerEvent(PowerBroadcastStatus status) { monitor.PowerChanged(); return true; }
    protected override void OnStop() { poller?.Dispose(); poller = null; monitor.Stop(); }
    protected override void OnShutdown() => OnStop();
    protected override void Dispose(bool disposing) { if (disposing) { poller?.Dispose(); monitor.Stop(); } base.Dispose(disposing); }
}

internal sealed class ConfiguredMonitor(bool publish, int? consoleSeconds = null)
{
    private readonly object sync = new();
    private readonly ConfigurationGate gate = new();
    private readonly LockTimer timer = new();
    private readonly Stopwatch clock = Stopwatch.StartNew();
    private readonly Dictionary<int, string> states = new();
    private readonly HashSet<int> reported = new();
    private readonly Queue<string> events = new();
    private readonly string runId = Guid.NewGuid().ToString("N");
    private readonly long processStart = Process.GetCurrentProcess().StartTime.ToUniversalTime().Ticks;
    private EffectiveConfiguration? effective;
    private TimeSpan lastPoll;
    private bool stopped;
    private void Clear() { timer.Reset(); states.Clear(); reported.Clear(); }
    private void Event(string text)
    {
        var line = DateTimeOffset.UtcNow.ToString("O") + " " + text;
        events.Enqueue(line); while (events.Count > 24) events.Dequeue();
        if (!publish) Console.WriteLine(line);
    }
    private void Accept(EffectiveConfiguration next)
    {
        if (effective == next) return; Clear(); effective = next;
        Event("Configuration " + JsonSerializer.Serialize(next));
    }
    public void SessionChanged(int id, string reason)
    {
        lock (sync) { if (stopped) return; timer.Forget(id); states.Remove(id); reported.Remove(id); Event("SessionEvent " + id + " " + reason); }
    }
    public void PowerChanged()
    {
        lock (sync) { if (stopped) return; Clear(); Accept(gate.Observe(null, null, "PowerEvent")); Event("PowerEvent: timing disarmed"); }
    }
    public void Poll()
    {
        lock (sync)
        {
            if (stopped) return;
            try
            {
                var now = clock.Elapsed;
                if (now - lastPoll > TimeSpan.FromSeconds(10)) { Clear(); gate.Observe(null, null, "Gap"); }
                lastPoll = now;
                EffectiveConfiguration next;
                if (consoleSeconds.HasValue) next = new(true, true, false, consoleSeconds.Value, null, "ConsoleDryRun");
                else { var saved = AdminSettingsStore.Read(); next = gate.Observe(saved.Settings, saved.Raw); }
                Accept(next);
                if (next.Enabled)
                {
                    var snapshots = SessionReader.ReadAll();
                    var ids = snapshots.Select(s => s.Id).ToHashSet();
                    foreach (var id in states.Keys.Where(id => !ids.Contains(id)).ToArray()) { timer.Forget(id); states.Remove(id); reported.Remove(id); }
                    var options = new MonitorOptions { Enabled = true, DryRun = true, Debug = next.Debug, Timeout = TimeSpan.FromSeconds(next.TimeoutSeconds) };
                    foreach (var s in snapshots)
                    {
                        if (!s.CanObserveTimer) { timer.Forget(s.Id); states.Remove(s.Id); reported.Remove(s.Id); continue; }
                        var key = s.Identity + ":" + s.LockState;
                        if (!states.TryGetValue(s.Id, out var old) || old != key)
                        { timer.Forget(s.Id); reported.Remove(s.Id); states[s.Id] = key; Event(s.LockState + " session=" + s.Id); }
                        var state = s.LockState == "Locked" ? LockState.Locked : LockState.Unlocked;
                        if (timer.Observe(s.Id, s.Identity, state, now, options) == TimerDecision.WouldLogOff && reported.Add(s.Id)) Event("WouldLogOff session=" + s.Id);
                    }
                }
                Report("Running");
            }
            catch (Exception e)
            {
                Clear(); Accept(gate.Observe(null, null, "Failure")); Event("Blocked: " + e.GetType().Name);
                try { Report("Running"); } catch { }
            }
        }
    }
    private void Report(string state)
    {
        if (publish) ServiceReportStore.Write(new(1, Environment.ProcessId, processStart, runId, DateTimeOffset.UtcNow, state, effective, events.ToArray()));
    }
    public void Stop()
    {
        lock (sync)
        {
            if (stopped) return; stopped = true; Clear(); effective = gate.Observe(null, null, "Stop");
            try { Report("Stopped"); } catch { }
        }
    }
}
