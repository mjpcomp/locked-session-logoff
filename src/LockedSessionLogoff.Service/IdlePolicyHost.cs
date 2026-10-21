// Updated: 2026-10-21 11:16 America/Los_Angeles
// Path: src/LockedSessionLogoff.Service/IdlePolicyHost.cs; Owner: maintainers
// Permissions: public read/maintainer write; Purpose: saved report-only idle policy with serialized refresh and lifecycle
// Arguments: diagnostic callback; Usage: SYSTEM service; no native idle actions or helper launching
using System.IO.Pipes;
using System.Text.Json;
using LockedSessionLogoff.Windows;
namespace LockedSessionLogoff.Service;
internal sealed class IdlePolicyHost : IDisposable
{
    private readonly object policySync = new();
    private readonly NamedPipeServerStream pipe;
    private readonly CancellationTokenSource stop = new();
    private readonly Task workers;
    private readonly Action<string> note;
    private readonly string epoch = Guid.NewGuid().ToString("N");
    private readonly IdleObservationGate gate = new();
    private AgentIdlePolicy current = HostedIdlePolicy.ReadFailed();
    private int disposed, faulted; private long lastFailure = -60000;
    public IdlePolicyHost(Action<string> note)
    {
        this.note = note;
        try { pipe = AgentPipePeer.CreateServer(); } catch { stop.Dispose(); throw; }
        Refresh();
        workers = Task.WhenAll(Task.Run(RunAsync), Task.Run(WatchAsync));
        note("IdlePolicyEndpointReady report-only actions=false");
    }
    private void Failure(Exception error)
    {
        long now = Environment.TickCount64, old = Interlocked.Read(ref lastFailure);
        if (now - old < 60000 || Interlocked.CompareExchange(ref lastFailure, now, old) != old) return;
        note("IdlePolicyQueryRejected type=" + error.GetType().Name);
    }
    private AgentIdlePolicy Refresh()
    {
        lock (policySync)
        {
            AgentIdlePolicy next;
            if (Volatile.Read(ref faulted) != 0 || Volatile.Read(ref disposed) != 0)
                next = gate.Observe(null, null, Environment.TickCount64, "EndpointUnavailable");
            else
            {
                try { var saved = AdminSettingsStore.Read(); next = gate.Observe(saved.Settings, saved.Raw, Environment.TickCount64); }
                catch (Exception e) { Failure(e); next = gate.Observe(null, null, Environment.TickCount64, "ReadFailed"); }
            }
            bool changed = current.Enabled != next.Enabled || current.Mode != next.Mode
                || current.Revision != next.Revision || current.IdleTimeoutSeconds != next.IdleTimeoutSeconds;
            current = next;
            if (changed) note("IdleObservationPolicy " + JsonSerializer.Serialize(new { next.Enabled, next.Mode, next.IdleTimeoutSeconds, Revision = next.Revision, DryRun = true, ActionsEnabled = false }));
            return next;
        }
    }
    private AgentIdlePolicy Policy(AgentPeer peer) => Refresh();
    private async Task WatchAsync()
    {
        var cancel = stop.Token;
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(2));
        try { while (await timer.WaitForNextTickAsync(cancel)) Refresh(); }
        catch (OperationCanceledException) when (cancel.IsCancellationRequested) { }
    }
    private async Task RunAsync()
    {
        var cancel = stop.Token;
        while (!cancel.IsCancellationRequested)
        {
            bool connected = false, fatal = false;
            try { await pipe.WaitForConnectionAsync(cancel); connected = true; await AgentPolicyChannel.ServeOnceAsync(pipe, Policy, epoch, cancel); }
            catch (OperationCanceledException) when (cancel.IsCancellationRequested) { break; }
            catch (ObjectDisposedException) when (cancel.IsCancellationRequested) { break; }
            catch (Exception e) { Failure(e); fatal = !connected; }
            finally
            {
                if (connected && !cancel.IsCancellationRequested)
                { try { pipe.Disconnect(); } catch (Exception e) { Failure(e); fatal = true; } }
            }
            if (fatal) { Interlocked.Exchange(ref faulted, 1); Refresh(); note("IdlePolicyEndpointFaulted no-policy-available"); return; }
        }
    }
    public void Dispose()
    {
        if (Interlocked.Exchange(ref disposed, 1) != 0) return;
        stop.Cancel(); pipe.Dispose();
        bool ended;
        try { ended = workers.Wait(TimeSpan.FromSeconds(2)); } catch (AggregateException) { ended = true; }
        if (ended) stop.Dispose();
        else _ = workers.ContinueWith(t => { _ = t.Exception; stop.Dispose(); }, TaskScheduler.Default);
        note("IdlePolicyEndpointStopped");
    }
}
