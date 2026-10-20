// Updated: 2026-10-20 13:32 America/Los_Angeles
// Path: src/LockedSessionLogoff.Service/Program.cs; Owner: maintainers
// Permissions: public read/maintainer write; Purpose: existing guarded logoff plus disabled read-only policy hosting
// Arguments: SCM none; --self-check; --console [--observe-seconds N]
// Usage: installed SYSTEM service; console never starts policy endpoint or live actions
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.ServiceProcess;
using System.Text.Json;
using LockedSessionLogoff.Core;
using LockedSessionLogoff.Windows;
using LockedSessionLogoff.Service;
if (args.SequenceEqual(new[] { "--self-check" })) { SessionReader.CheckLayout(); SessionTokenReader.CheckLayout(); Console.WriteLine("WTS/token layouts passed; no service started"); return 0; }
if (args.Length == 0) { using var service = new ConfiguredService(); ServiceBase.Run(service); return 0; }
int seconds = 60;
if (args[0] != "--console" || (args.Length != 1 && (args.Length != 3 || args[1] != "--observe-seconds" || !int.TryParse(args[2], out seconds))) || seconds < 5 || seconds > 604800) return 2;
Console.WriteLine("DRY-RUN console. Token queries need SYSTEM; use installed service for SID tests. No live actions or policy endpoint in console.");
using var stop = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) => { e.Cancel = true; stop.Cancel(); };
var console = new ConfiguredMonitor(false, seconds);
try { while (!stop.IsCancellationRequested) { console.Poll(); await Task.Delay(2000, stop.Token); } } catch (OperationCanceledException) { }
return 0;

internal sealed class ConfiguredService : ServiceBase
{
    private readonly ConfiguredMonitor monitor = new(true);
    private System.Threading.Timer? poller; private IdlePolicyHost? idle;
    public ConfiguredService() { ServiceName = "LockedSessionLogoff"; AutoLog = false; CanStop = true; CanShutdown = true; CanHandleSessionChangeEvent = true; CanHandlePowerEvent = true; }
    protected override void OnStart(string[] args)
    {
        SessionReader.CheckLayout(); SessionTokenReader.CheckLayout();
        idle = new IdlePolicyHost(monitor.IdleEvent);
        try { poller = new(_ => monitor.Poll(), null, TimeSpan.Zero, TimeSpan.FromSeconds(2)); }
        catch { idle.Dispose(); idle = null; throw; }
    }
    protected override void OnSessionChange(SessionChangeDescription c) => monitor.SessionChanged(c.SessionId, c.Reason.ToString());
    protected override bool OnPowerEvent(PowerBroadcastStatus status) { monitor.PowerChanged(); return true; }
    protected override void OnStop() { poller?.Dispose(); poller = null; idle?.Dispose(); idle = null; monitor.Stop(); }
    protected override void OnShutdown() => OnStop();
    protected override void Dispose(bool disposing) { if (disposing) { poller?.Dispose(); idle?.Dispose(); monitor.Stop(); } base.Dispose(disposing); }
}
internal sealed class ConfiguredMonitor(bool publish, int? consoleSeconds = null)
{
    private readonly object sync = new();
    private readonly ConfigurationGate gate = new(sidCapable: true, liveCapable: publish);
    private readonly LogoffCoordinator actions = new(new NativeLogoffBackend(), liveCapable: publish);
    private readonly LockTimer timer = new(); private readonly Stopwatch clock = Stopwatch.StartNew();
    private readonly Dictionary<int, string> states = new(), blocked = new(); private readonly HashSet<int> reported = new();
    private readonly Queue<string> events = new(); private readonly string runId = Guid.NewGuid().ToString("N");
    private readonly long processStart = Process.GetCurrentProcess().StartTime.ToUniversalTime().Ticks;
    private EffectiveConfiguration? effective; private TimeSpan lastPoll; private bool stopped;
    private void Clear() { timer.Reset(); states.Clear(); blocked.Clear(); reported.Clear(); }
    private void Event(string text) { var line = DateTimeOffset.UtcNow.ToString("O") + " " + text; events.Enqueue(line); while (events.Count > 24) events.Dequeue(); if (!publish) Console.WriteLine(line); }
    public void IdleEvent(string text) { lock (sync) { if (!stopped) Event(text); } }
    private void Accept(EffectiveConfiguration next) { if (effective == next) return; Clear(); effective = next; Event("Configuration " + JsonSerializer.Serialize(next)); }
    private void Forget(int id) { timer.Forget(id); states.Remove(id); reported.Remove(id); }
    private void Block(int id, string reason) { Forget(id); if (!blocked.TryGetValue(id, out var old) || old != reason) Event(reason + " session=" + id); blocked[id] = reason; }
    private void AbandonOutcomes(string reason) { foreach (var result in actions.Abandon(reason)) Event(result.ToString()); }
    private void Reconcile(IReadOnlyList<SessionSnapshot>? snapshots, TimeSpan now)
    {
        foreach (var pending in actions.Pending)
        {
            var observation = SessionObservation.Unknown;
            if (snapshots is not null)
            {
                var s = snapshots.SingleOrDefault(v => v.Id == pending.Target.SessionId);
                if (s is null) observation = SessionObservation.SessionAbsent;
                else if (s.Error is null && !string.IsNullOrEmpty(s.Identity))
                {
                    try { var actual = SessionTokenReader.Read(s.Id); observation = actual != pending.Target ? SessionObservation.IdentityReplaced : s.Identity == pending.WtsIdentity ? SessionObservation.SameIdentityPresent : SessionObservation.Unknown; }
                    catch (Exception) { observation = SessionObservation.Unknown; }
                }
            }
            var result = actions.Observe(pending.ActionId, observation, now);
            if (result is not null) Event(result.ToString());
        }
    }
    public void SessionChanged(int id, string reason) { lock (sync) { if (stopped) return; Forget(id); blocked.Remove(id); Event("SessionEvent " + id + " " + reason); } }
    public void PowerChanged() { lock (sync) { if (stopped) return; AbandonOutcomes("PowerEvent"); Clear(); Accept(gate.Observe(null, null, "Power")); Event("PowerEvent: disarmed"); } }
    public void Poll()
    {
        lock (sync)
        {
            if (stopped) return;
            try
            {
                var now = clock.Elapsed;
                if (now - lastPoll > TimeSpan.FromSeconds(10)) { AbandonOutcomes("ObservationGap"); Clear(); gate.Observe(null, null, "Gap"); } lastPoll = now;
                var saved = consoleSeconds.HasValue ? (Settings: new AdminSettings { MonitoringRequested = true, DryRun = true, LockedTimeoutSeconds = consoleSeconds.Value }, Raw: (string?)null) : AdminSettingsStore.Read();
                var next = consoleSeconds.HasValue ? new EffectiveConfiguration(true, true, false, consoleSeconds.Value, null, "ConsoleDryRun") : gate.Observe(saved.Settings, saved.Raw);
                Accept(next);
                if (next.Enabled || actions.Pending.Count > 0)
                {
                    var snapshots = SessionReader.ReadAll(); Reconcile(snapshots, clock.Elapsed);
                    if (next.Enabled)
                    {
                        var ids = snapshots.Select(s => s.Id).ToHashSet();
                        foreach (var id in states.Keys.Concat(blocked.Keys).Distinct().Where(id => !ids.Contains(id)).ToArray()) { Forget(id); blocked.Remove(id); }
                        var options = new MonitorOptions { Enabled = true, DryRun = next.DryRun, Debug = next.Debug, Timeout = TimeSpan.FromSeconds(next.TimeoutSeconds) };
                        foreach (var s in snapshots)
                        {
                            if (!s.CanObserveTimer) { Forget(s.Id); continue; }
                            TokenIdentity identity;
                            try { identity = SessionTokenReader.Read(s.Id); } catch (Exception e) { Block(s.Id, "IdentityUnavailable:" + e.GetType().Name); continue; }
                            if (saved.Settings.ExcludedSids.Contains(identity.Sid, StringComparer.OrdinalIgnoreCase)) { Block(s.Id, "ExcludedBySid"); continue; }
                            blocked.Remove(s.Id);
                            var key = s.Identity + ":" + identity.Key + ":" + s.LockState;
                            if (!states.TryGetValue(s.Id, out var old) || old != key) { Forget(s.Id); states[s.Id] = key; Event(s.LockState + " session=" + s.Id); }
                            var state = s.LockState == "Locked" ? LockState.Locked : LockState.Unlocked;
                            var decision = timer.Observe(s.Id, identity.Key, state, now, options);
                            if (decision == TimerDecision.None || reported.Contains(s.Id)) continue;
                            try
                            {
                                using var lease = ConfigurationSecurity.WriterLease();
                                var latest = consoleSeconds.HasValue ? saved : AdminSettingsStore.Read();
                                if (latest.Raw != saved.Raw || (!consoleSeconds.HasValue && !latest.Settings.MonitoringRequested)) { Clear(); Event("CandidateBlockedConfigurationChanged"); break; }
                                var actual = SessionTokenReader.Read(s.Id);
                                var current = SessionReader.ReadAll().SingleOrDefault(v => v.Id == s.Id);
                                var result = actions.Execute(identity, s.Identity, saved.Raw, latest.Settings, latest.Raw, next, actual, current, clock.Elapsed);
                                reported.Add(s.Id); Event(result.ToString());
                                if (result.Kind.StartsWith("CandidateBlocked", StringComparison.Ordinal)) { timer.Forget(s.Id); states.Remove(s.Id); }
                            }
                            catch (Exception e) { reported.Add(s.Id); Event("CandidateOrLogoffFailed session=" + s.Id + " type=" + e.GetType().Name); }
                        }
                    }
                }
                Report("Running");
            }
            catch (Exception e) { Clear(); Accept(gate.Observe(null, null, "Failure")); Event("Blocked:" + e.GetType().Name); Reconcile(null, clock.Elapsed); try { Report("Running"); } catch { } }
        }
    }
    private void Report(string state) { if (publish) ServiceReportStore.Write(new(1, Environment.ProcessId, processStart, runId, DateTimeOffset.UtcNow, state, effective, events.ToArray())); }
    public void Stop() { lock (sync) { if (stopped) return; stopped = true; AbandonOutcomes("ServiceStopped"); Clear(); effective = gate.Observe(null, null, "Stop"); try { Report("Stopped"); } catch { } } }
}
internal sealed class NativeLogoffBackend : ILogoffBackend
{
    public void Request(int id)
    {
        using var identity = WindowsIdentity.GetCurrent();
        if (id <= 0 || identity.User?.Value != "S-1-5-18") throw new UnauthorizedAccessException("SYSTEM and user session required");
        if (!WTSLogoffSession(IntPtr.Zero, (uint)id, false)) throw new Win32Exception(Marshal.GetLastWin32Error());
    }
    [DllImport("wtsapi32.dll", ExactSpelling = true, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)] private static extern bool WTSLogoffSession(IntPtr server, uint session, [MarshalAs(UnmanagedType.Bool)] bool wait);
}
