// Created: 2026-10-20 09:36 America/Los_Angeles
// Path: src/LockedSessionLogoff.SessionAgent/Program.cs; Owner: maintainers
// Permissions: public read/maintainer write; Purpose: standalone read-only own-session idle observation
// Arguments: --self-check; --observe --idle-seconds N; Usage: NORMAL, non-elevated user console; Ctrl+C stops
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Principal;
using LockedSessionLogoff.Core;
using LockedSessionLogoff.Windows;
if (args.SequenceEqual(new[] { "--self-check" }))
{ InputReader.CheckLayout(); SessionReader.CheckLayout(); Console.WriteLine("Input/WTS layouts passed; no observation or actions started"); return 0; }
if (args.Length != 3 || args[0] != "--observe" || args[1] != "--idle-seconds"
    || !int.TryParse(args[2], out var seconds) || seconds < 5 || seconds > 604800)
{ Console.WriteLine("Usage: --observe --idle-seconds 20 (5..604800); or --self-check. Read-only, no OS actions."); return args.Length == 0 ? 0 : 2; }
using var process = Process.GetCurrentProcess();
using var user = WindowsIdentity.GetCurrent();
if (process.SessionId <= 0 || user.User?.Value == "S-1-5-18"
    || new WindowsPrincipal(user).IsInRole(WindowsBuiltInRole.Administrator))
{ Console.Error.WriteLine("Run in your local-console user session WITHOUT elevation; not SYSTEM/session 0."); return 2; }
InputReader.CheckLayout(); SessionReader.CheckLayout();
Console.WriteLine($"READ-ONLY DRY-RUN session={process.SessionId} idle={seconds}s. No lock/sign-out requests, config writes, IPC or startup registration.");
Console.WriteLine("Local active Console session only. Startup/unlock/input changes/unknown state/gaps start a fresh window. Ctrl+C stops.");
using var stop = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) => { e.Cancel = true; stop.Cancel(); };
var timer = new InputIdleTimer(); string? lastState = null;
void State(string text) { if (text == lastState) return; lastState = text; Console.WriteLine(DateTimeOffset.UtcNow.ToString("O") + " " + text); }
try
{
    while (!stop.IsCancellationRequested)
    {
        try
        {
            var s = SessionReader.ReadAll().SingleOrDefault(v => v.Id == process.SessionId);
            var eligible = s is not null && s.CanObserveTimer && s.LockState == "Unlocked"
                && string.Equals(s.Station, "Console", StringComparison.OrdinalIgnoreCase);
            if (!eligible) { timer.Reset(); State("Paused: session not positively known active local-console/unlocked"); }
            else
            {
                var input = InputReader.Read();
                var decision = timer.Observe(Environment.TickCount64, input, true, s!.Identity, TimeSpan.FromSeconds(seconds));
                State(timer.HasFreshWindow ? "Observing fresh idle window" : "Paused: input time unavailable/uncertain");
                if (decision == InputIdleDecision.ThresholdReached)
                    Console.WriteLine(DateTimeOffset.UtcNow.ToString("O") + $" WouldLock session={process.SessionId} threshold={seconds}s (diagnostic only; no action)");
            }
        }
        catch (Exception e) { timer.Reset(); State("Paused: native observation failed type=" + e.GetType().Name); }
        await Task.Delay(1000, stop.Token);
    }
}
catch (OperationCanceledException) { }
Console.WriteLine("Observer stopped; no persistent changes to undo."); return 0;
internal static class InputReader
{
    public static void CheckLayout() { if (Marshal.SizeOf<LastInput>() != 8) throw new PlatformNotSupportedException("Unexpected input structure layout"); }
    public static uint? Read()
    { var input = new LastInput { Size = (uint)Marshal.SizeOf<LastInput>() }; return GetLastInputInfo(ref input) ? input.Tick : null; }
#pragma warning disable CS0649
    [StructLayout(LayoutKind.Sequential)] private struct LastInput { public uint Size, Tick; }
#pragma warning restore CS0649
    [DllImport("user32.dll", ExactSpelling = true)]
    [return: MarshalAs(UnmanagedType.Bool)] private static extern bool GetLastInputInfo(ref LastInput input);
}
