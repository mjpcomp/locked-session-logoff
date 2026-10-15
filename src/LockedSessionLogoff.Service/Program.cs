// Created: 2026-10-15 09:55 America/Los_Angeles
// Path: src/LockedSessionLogoff.Service/Program.cs; Owner: maintainers
// Permissions: public read/maintainer write; Purpose: disabled SCM host/console dry-run
// Arguments: none for SCM; --self-check; --console [--observe-seconds N]
// Usage: console mode needs no installation; no modification APIs exist in this increment
using System.Diagnostics;
using System.ServiceProcess;
using System.Text.Json;
using LockedSessionLogoff.Core;
using LockedSessionLogoff.Windows;

if (args.SequenceEqual(new[] { "--self-check" }))
{
    SessionReader.CheckLayout();
    Console.WriteLine("Service layout check passed; no service started");
    return 0;
}
if (args.Length == 0)
{
    using var service = new SessionService();
    ServiceBase.Run(service);
    return 0;
}
int seconds = 60;
if (args[0] != "--console" ||
    (args.Length != 1 && (args.Length != 3 || args[1] != "--observe-seconds" ||
    !int.TryParse(args[2], out seconds))) || seconds < 5 || seconds > 604800)
{
    Console.Error.WriteLine("Usage: service via SCM, or --self-check, or --console [--observe-seconds 5..604800]");
    return 2;
}
Console.WriteLine("CONSOLE DRY-RUN ONLY. No logoff or lock APIs. Console polling cannot prove unbroken lock between samples.");
using var cancellation = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) => { e.Cancel = true; cancellation.Cancel(); };
var monitor = new SessionMonitor(new MonitorOptions
    { Enabled = true, DryRun = true, Timeout = TimeSpan.FromSeconds(seconds) }, Console.WriteLine);
try
{
    while (!cancellation.IsCancellationRequested)
    {
        monitor.Poll();
        await Task.Delay(2000, cancellation.Token);
    }
}
catch (OperationCanceledException) { }
return 0;

internal sealed class SessionService : ServiceBase
{
    private readonly SessionMonitor monitor = new(new MonitorOptions(), _ => { });
    private System.Threading.Timer? poller;
    public SessionService()
    {
        ServiceName = "LockedSessionLogoff";
        AutoLog = false;
        CanStop = true;
        CanShutdown = true;
        CanHandleSessionChangeEvent = true;
        CanHandlePowerEvent = true;
    }
    protected override void OnStart(string[] arguments)
    {
        SessionReader.CheckLayout();
        monitor.Reset();
        poller = new System.Threading.Timer(_ => monitor.Poll(), null, TimeSpan.Zero, TimeSpan.FromSeconds(2));
    }
    protected override void OnSessionChange(SessionChangeDescription change) => monitor.Forget(change.SessionId);
    protected override bool OnPowerEvent(PowerBroadcastStatus status) { monitor.Reset(); return true; }
    protected override void OnStop() { poller?.Dispose(); poller = null; monitor.Reset(); }
    protected override void OnShutdown() => OnStop();
    protected override void Dispose(bool disposing)
    {
        if (disposing) { poller?.Dispose(); poller = null; }
        base.Dispose(disposing);
    }
}

internal sealed class SessionMonitor(MonitorOptions options, Action<string> output)
{
    private readonly object gate = new();
    private readonly LockTimer timer = new();
    private readonly Dictionary<int, string> states = new();
    private readonly HashSet<int> reported = new();
    private readonly Stopwatch clock = Stopwatch.StartNew();
    private TimeSpan lastPoll;

    public void Reset()
    {
        lock (gate) { timer.Reset(); states.Clear(); reported.Clear(); lastPoll = clock.Elapsed; }
    }
    public void Forget(int id)
    {
        lock (gate) { timer.Forget(id); states.Remove(id); reported.Remove(id); }
    }
    public void Poll()
    {
        lock (gate)
        {
            try
            {
                var now = clock.Elapsed;
                if (now - lastPoll > TimeSpan.FromSeconds(10)) Reset();
                lastPoll = now;
                var sessions = SessionReader.ReadAll();
                var ids = sessions.Select(s => s.Id).ToHashSet();
                foreach (var old in states.Keys.Where(id => !ids.Contains(id)).ToArray()) Forget(old);
                foreach (var s in sessions)
                {
                    if (!s.CanObserveTimer) { Forget(s.Id); continue; }
                    var key = s.Identity + ":" + s.LockState;
                    if (!states.TryGetValue(s.Id, out var previous) || previous != key)
                    {
                        timer.Forget(s.Id);
                        reported.Remove(s.Id);
                        states[s.Id] = key;
                        Emit(s.Id, s.LockState);
                    }
                    var state = s.LockState == "Locked" ? LockState.Locked : LockState.Unlocked;
                    var decision = timer.Observe(s.Id, s.Identity, state, now, options);
                    if (decision == TimerDecision.WouldLogOff && reported.Add(s.Id)) Emit(s.Id, "WouldLogOff");
                }
            }
            catch (Exception e)
            {
                Reset();
                output(JsonSerializer.Serialize(new { TimestampUtc = DateTimeOffset.UtcNow, Event = "ObservationFailed", ErrorType = e.GetType().Name }));
            }
        }
    }
    private void Emit(int id, string state) => output(JsonSerializer.Serialize(new
        { TimestampUtc = DateTimeOffset.UtcNow, SessionId = id, Event = state, DryRun = options.DryRun, Enabled = options.Enabled }));
}
