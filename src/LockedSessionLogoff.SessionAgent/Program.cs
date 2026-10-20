// Updated: 2026-10-20 13:32 America/Los_Angeles
// Path: src/LockedSessionLogoff.SessionAgent/Program.cs; Owner: maintainers
// Permissions: public read/maintainer write; Purpose: standalone observer and verified read-only service policy client
// Arguments: --self-check; --observe --idle-seconds N; --service-policy; --service-observe
// Usage: normal non-elevated local-console user; no setting writes or lock/sign-out requests
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text.Json;
using LockedSessionLogoff.Core;
using LockedSessionLogoff.Windows;
if (args.SequenceEqual(new[] { "--self-check" }))
{ InputReader.CheckLayout(); SessionReader.CheckLayout(); Console.WriteLine("Input/WTS layouts passed; no observation or actions started"); return 0; }
bool serviceObserve = args.SequenceEqual(new[] { "--service-observe" });
bool serviceQuery = args.SequenceEqual(new[] { "--service-policy" });
int seconds = 0;
if (!serviceObserve && !serviceQuery && (args.Length != 3 || args[0] != "--observe" || args[1] != "--idle-seconds" || !int.TryParse(args[2], out seconds) || seconds < 5 || seconds > 604800))
{ Console.WriteLine("Usage: --observe --idle-seconds 20; --service-policy; --service-observe; --self-check. Read-only."); return args.Length == 0 ? 0 : 2; }
using var process = Process.GetCurrentProcess();
using var user = WindowsIdentity.GetCurrent();
if (process.SessionId <= 0 || user.User?.Value == "S-1-5-18" || new WindowsPrincipal(user).IsInRole(WindowsBuiltInRole.Administrator))
{ Console.Error.WriteLine("Run WITHOUT elevation in your local-console user session, not SYSTEM/session 0."); return 2; }
InputReader.CheckLayout(); SessionReader.CheckLayout();
string Failure(Exception e) => e is Win32Exception native ? "Win32=" + native.NativeErrorCode : e.GetType().Name;
if (serviceQuery)
{
    try { var reply = await AgentPolicyChannel.QueryAsync(); Console.WriteLine(JsonSerializer.Serialize(reply, new JsonSerializerOptions { WriteIndented = true })); return 0; }
    catch (Exception e) { Console.Error.WriteLine("No verified service policy: " + Failure(e)); return 2; }
}
Console.WriteLine($"READ-ONLY DRY-RUN session={process.SessionId} source={(serviceObserve ? "ServicePolicy" : "StandaloneDiagnostic")}. No lock/sign-out requests, writes or startup registration.");
Console.WriteLine("Service mode never falls back to standalone timing. Local active unlocked Console session only. Ctrl+C stops.");
using var stop = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) => { e.Cancel = true; stop.Cancel(); };
var timer = new InputIdleTimer(); string? lastState = null, policyKey = null;
void State(string text) { if (text == lastState) return; lastState = text; Console.WriteLine(DateTimeOffset.UtcNow.ToString("O") + " " + text); }
try
{
    while (!stop.IsCancellationRequested)
    {
        try
        {
            var s = SessionReader.ReadAll().SingleOrDefault(v => v.Id == process.SessionId);
            bool eligible = s is not null && s.CanObserveTimer && s.LockState == "Unlocked" && string.Equals(s.Station, "Console", StringComparison.OrdinalIgnoreCase);
            if (!eligible) { timer.Reset(); policyKey = null; State("Paused: session not positively known active local-console/unlocked"); }
            else
            {
                bool observe = true; int timeout = seconds;
                if (serviceObserve)
                {
                    var policy = await AgentPolicyChannel.QueryAsync(stop.Token);
                    var key = policy.ServerEpoch + ":" + policy.Revision + ":" + policy.Mode + ":" + policy.IdleTimeoutSeconds;
                    if (key != policyKey) { timer.Reset(); policyKey = key; }
                    observe = policy.Enabled; timeout = policy.IdleTimeoutSeconds;
                    if (!observe) { timer.Reset(); State("Paused: verified service policy " + policy.Mode + "; idle timeout inactive; actions=false"); }
                }
                if (observe)
                {
                    var decision = timer.Observe(Environment.TickCount64, InputReader.Read(), true, s!.Identity, TimeSpan.FromSeconds(timeout));
                    State(timer.HasFreshWindow ? "Observing fresh idle window" : "Paused: input time unavailable/uncertain");
                    if (decision == InputIdleDecision.ThresholdReached)
                        Console.WriteLine(DateTimeOffset.UtcNow.ToString("O") + $" WouldLock session={process.SessionId} threshold={timeout}s source={(serviceObserve ? "ServicePolicy" : "StandaloneDiagnostic")} (diagnostic only; no action)");
                }
            }
        }
        catch (OperationCanceledException) when (stop.IsCancellationRequested) { break; }
        catch (Exception e) { timer.Reset(); policyKey = null; State("Paused: no trusted current observation/policy " + Failure(e)); }
        await Task.Delay(1000, stop.Token);
    }
}
catch (OperationCanceledException) { }
Console.WriteLine("Observer stopped; no persistent changes to undo."); return 0;
internal static class InputReader
{
    public static void CheckLayout() { if (Marshal.SizeOf<LastInput>() != 8) throw new PlatformNotSupportedException("Unexpected input structure layout"); }
    public static uint? Read() { var input = new LastInput { Size = (uint)Marshal.SizeOf<LastInput>() }; return GetLastInputInfo(ref input) ? input.Tick : null; }
#pragma warning disable CS0649
    [StructLayout(LayoutKind.Sequential)] private struct LastInput { public uint Size, Tick; }
#pragma warning restore CS0649
    [DllImport("user32.dll", ExactSpelling = true)]
    [return: MarshalAs(UnmanagedType.Bool)] private static extern bool GetLastInputInfo(ref LastInput input);
}
