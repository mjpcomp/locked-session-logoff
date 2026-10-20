// Created: 2026-10-20 13:32 America/Los_Angeles
// Path: src/LockedSessionLogoff.Service/IdlePolicyHost.cs; Owner: maintainers
// Permissions: public read/maintainer write; Purpose: service-owned local read-only endpoint lifecycle
// Arguments: bounded diagnostic callback; Usage: SYSTEM OnStart/OnStop only; policy always disabled
using System.Diagnostics;
using System.IO.Pipes;
using LockedSessionLogoff.Windows;
namespace LockedSessionLogoff.Service;
internal sealed class IdlePolicyHost : IDisposable
{
    private readonly NamedPipeServerStream pipe;
    private readonly CancellationTokenSource stop = new();
    private readonly Task worker;
    private readonly Action<string> note;
    private readonly string epoch = Guid.NewGuid().ToString("N");
    private int disposed; private long lastFailure = -60000;
    public IdlePolicyHost(Action<string> note)
    {
        this.note = note;
        try { pipe = AgentPipePeer.CreateServer(); } catch { stop.Dispose(); throw; }
        worker = Task.Run(RunAsync);
        note("IdlePolicyEndpointReady policy=Disabled actions=false hosting-stage-only");
    }
    private void Failure(Exception error)
    {
        long now = Environment.TickCount64, old = Interlocked.Read(ref lastFailure);
        if (now - old < 60000 || Interlocked.CompareExchange(ref lastFailure, now, old) != old) return;
        note("IdlePolicyQueryRejected type=" + error.GetType().Name);
    }
    private AgentIdlePolicy Policy(AgentPeer peer)
    {
        try { var current = AdminSettingsStore.Read(); return HostedIdlePolicy.CreateDisabled(current.Settings, current.Raw); }
        catch (Exception e) { Failure(e); return HostedIdlePolicy.ReadFailed(); }
    }
    private async Task RunAsync()
    {
        var cancel = stop.Token;
        while (!cancel.IsCancellationRequested)
        {
            bool connected = false, fatal = false;
            try
            {
                await pipe.WaitForConnectionAsync(cancel); connected = true;
                await AgentPolicyChannel.ServeOnceAsync(pipe, Policy, epoch, cancel);
            }
            catch (OperationCanceledException) when (cancel.IsCancellationRequested) { break; }
            catch (ObjectDisposedException) when (cancel.IsCancellationRequested) { break; }
            catch (Exception e) { Failure(e); fatal = !connected; }
            finally
            {
                if (connected && !cancel.IsCancellationRequested)
                { try { pipe.Disconnect(); } catch (Exception e) { Failure(e); fatal = true; } }
            }
            if (fatal) { note("IdlePolicyEndpointFaulted no-policy-available"); return; }
        }
    }
    public void Dispose()
    {
        if (Interlocked.Exchange(ref disposed, 1) != 0) return;
        stop.Cancel(); pipe.Dispose();
        bool ended;
        try { ended = worker.Wait(TimeSpan.FromSeconds(2)); } catch (AggregateException) { ended = true; }
        if (ended) stop.Dispose();
        else _ = worker.ContinueWith(t => { _ = t.Exception; stop.Dispose(); }, TaskScheduler.Default);
        note("IdlePolicyEndpointStopped");
    }
}
