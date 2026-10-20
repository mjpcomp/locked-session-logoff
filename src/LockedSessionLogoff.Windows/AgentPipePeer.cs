// Created: 2026-10-20 12:08 America/Los_Angeles
// Path: src/LockedSessionLogoff.Windows/AgentPipePeer.cs; Owner: maintainers
// Permissions: public read/maintainer write; Purpose: Windows local pipe ACL, process/service and token peer binding
// Arguments: connected pipe; Usage: future SYSTEM host/normal-user client; no static listener startup
using Microsoft.Win32.SafeHandles;
using System.ComponentModel;
using System.Diagnostics;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Security.Principal;
namespace LockedSessionLogoff.Windows;
public readonly record struct PipeServiceInstance(int ProcessId, long ProcessStartTicks);
public static class AgentPipePeer
{
    public const string PipeName = "LockedSessionLogoff.IdlePolicy.v1";
    private const string PipePath = @"\\.\pipe\LockedSessionLogoff.IdlePolicy.v1";
    // Network denied; SYSTEM full control; interactive clients data read/write+sync only, NOT create-instance.
    private const string Sddl = "O:SYG:SYD:P(D;;GA;;;S-1-5-2)(A;;GA;;;SY)(A;;0x00100003;;;IU)";
    public static void RequireSystem()
    { using var id = WindowsIdentity.GetCurrent(); if (id.User?.Value != "S-1-5-18") throw new UnauthorizedAccessException("SYSTEM required"); }
    public static void RequireNormalUser()
    {
        using var id = WindowsIdentity.GetCurrent(); using var p = Process.GetCurrentProcess();
        if (p.SessionId <= 0 || id.User is null || id.User.Value == "S-1-5-18" || new WindowsPrincipal(id).IsInRole(WindowsBuiltInRole.Administrator))
            throw new UnauthorizedAccessException("Non-elevated interactive user required");
    }
    public static NamedPipeServerStream CreateServer()
    {
        RequireSystem();
        if (!ConvertStringSecurityDescriptorToSecurityDescriptorW(Sddl, 1, out var descriptor, out _)) throw Error();
        try
        {
            var security = new SecurityAttributes { Length = Marshal.SizeOf<SecurityAttributes>(), Descriptor = descriptor, Inherit = false };
            var handle = CreateNamedPipeW(PipePath, 0x40080003, 8, 1, 4096, 4096, 0, ref security);
            if (handle.IsInvalid) { var e = Error(); handle.Dispose(); throw e; }
            try { return new NamedPipeServerStream(PipeDirection.InOut, true, false, handle); }
            catch { handle.Dispose(); throw; }
        } finally { LocalFree(descriptor); }
    }
    public static async Task<NamedPipeClientStream> ConnectAsync(CancellationToken cancel)
    {
        while (true)
        {
            cancel.ThrowIfCancellationRequested();
            // Individual data rights avoid GENERIC_WRITE/create-instance; identification-only SQOS; no inheritance.
            var handle = CreateFileW(PipePath, 0x00100003, 0, IntPtr.Zero, 3, 0x40110000, IntPtr.Zero);
            if (!handle.IsInvalid)
            { try { return new NamedPipeClientStream(PipeDirection.InOut, true, true, handle); } catch { handle.Dispose(); throw; } }
            int error = Marshal.GetLastWin32Error(); handle.Dispose();
            if (error is not (2 or 231)) throw new Win32Exception(error);
            await Task.Delay(50, cancel);
        }
    }
    public static AgentPeer ReadClient(NamedPipeServerStream pipe)
    {
        RequireSystem();
        if (!GetNamedPipeClientProcessId(pipe.SafePipeHandle, out var pid)) throw Error();
        TokenIdentity? token = null;
        pipe.RunAsClient(() =>
        {
            using var id = WindowsIdentity.GetCurrent(true) ?? throw new UnauthorizedAccessException("No impersonated client");
            if (id.Groups?.Contains(new SecurityIdentifier("S-1-5-2")) == true || new WindowsPrincipal(id).IsInRole(WindowsBuiltInRole.Administrator))
                throw new UnauthorizedAccessException("Network/elevated client rejected");
            token = Inspect(id);
        });
        RequireSystem();
        var identity = token ?? throw new UnauthorizedAccessException("Unknown token");
        using var process = Process.GetProcessById(checked((int)pid));
        if (process.SessionId != identity.SessionId || identity != SessionTokenReader.Read(identity.SessionId))
            throw new UnauthorizedAccessException("Client does not match current logon");
        var state = SessionReader.ReadAll().SingleOrDefault(s => s.Id == identity.SessionId);
        if (state is null || !state.CanObserveTimer || state.LockState != "Unlocked" || !string.Equals(state.Station, "Console", StringComparison.OrdinalIgnoreCase))
            throw new UnauthorizedAccessException("No eligible local-console session");
        return new(identity, process.Id, process.StartTime.ToUniversalTime().Ticks, state.Identity);
    }
    public static PipeServiceInstance ReadServiceInstance(NamedPipeClientStream pipe)
    {
        if (!GetNamedPipeServerProcessId(pipe.SafePipeHandle, out var pipePid)) throw Error();
        var manager = OpenSCManagerW(null, null, 1); if (manager == IntPtr.Zero) throw Error();
        IntPtr service = IntPtr.Zero;
        try
        {
            service = OpenServiceW(manager, "LockedSessionLogoff", 4); if (service == IntPtr.Zero) throw Error();
            if (!QueryServiceStatusEx(service, 0, out var state, Marshal.SizeOf<ServiceStatus>(), out _)) throw Error();
            if (state.CurrentState != 4 || state.ProcessId == 0 || state.ProcessId != pipePid)
                throw new UnauthorizedAccessException("Pipe is not the running installed service");
            using var process = Process.GetProcessById(checked((int)pipePid));
            return new(process.Id, process.StartTime.ToUniversalTime().Ticks);
        } finally { if (service != IntPtr.Zero) CloseServiceHandle(service); CloseServiceHandle(manager); }
    }
    private static TokenIdentity Inspect(WindowsIdentity id)
    {
        if (Marshal.SizeOf<Statistics>() != 56 || Marshal.OffsetOf<Statistics>(nameof(Statistics.AuthenticationId)).ToInt32() != 8)
            throw new PlatformNotSupportedException("Unexpected token layout");
        if (!GetSession(id.AccessToken, 12, out var session, 4, out var count)) throw Error();
        if (count != 4 || session == 0 || session > int.MaxValue) throw new InvalidDataException("Invalid token session");
        if (!GetStatistics(id.AccessToken, 10, out var statistics, Marshal.SizeOf<Statistics>(), out count)) throw Error();
        var auth = ((long)statistics.AuthenticationId.High << 32) | statistics.AuthenticationId.Low;
        var sid = id.User?.Value;
        if (count < 56 || auth == 0 || string.IsNullOrEmpty(sid) || sid == "S-1-5-18") throw new InvalidDataException("Invalid user token");
        return new((int)session, sid, auth);
    }
    private static Win32Exception Error() => new(Marshal.GetLastWin32Error());
#pragma warning disable CS0649
    [StructLayout(LayoutKind.Sequential)] private struct SecurityAttributes { public int Length; public IntPtr Descriptor; [MarshalAs(UnmanagedType.Bool)] public bool Inherit; }
    [StructLayout(LayoutKind.Sequential)] private struct Luid { public uint Low; public int High; }
    [StructLayout(LayoutKind.Sequential)] private struct Statistics { public Luid TokenId, AuthenticationId; public long Expiration; public int Type, Level; public uint Charged, Available, Groups, Privileges; public Luid Modified; }
    [StructLayout(LayoutKind.Sequential)] private struct ServiceStatus { public uint Type, CurrentState, Accepted, Exit, SpecificExit, Checkpoint, Wait, ProcessId, Flags; }
#pragma warning restore CS0649
    [DllImport("advapi32.dll", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool ConvertStringSecurityDescriptorToSecurityDescriptorW(string text, uint revision, out IntPtr descriptor, out uint bytes);
    [DllImport("kernel32.dll", ExactSpelling = true)] private static extern IntPtr LocalFree(IntPtr memory);
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)] private static extern SafePipeHandle CreateNamedPipeW(string name, uint openMode, uint pipeMode, uint instances, uint output, uint input, uint timeout, ref SecurityAttributes security);
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)] private static extern SafePipeHandle CreateFileW(string name, uint access, uint share, IntPtr security, uint creation, uint flags, IntPtr template);
    [DllImport("kernel32.dll", ExactSpelling = true, SetLastError = true)] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool GetNamedPipeClientProcessId(SafePipeHandle pipe, out uint pid);
    [DllImport("kernel32.dll", ExactSpelling = true, SetLastError = true)] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool GetNamedPipeServerProcessId(SafePipeHandle pipe, out uint pid);
    [DllImport("advapi32.dll", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)] private static extern IntPtr OpenSCManagerW(string? machine, string? database, uint access);
    [DllImport("advapi32.dll", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)] private static extern IntPtr OpenServiceW(IntPtr manager, string name, uint access);
    [DllImport("advapi32.dll", ExactSpelling = true)] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool CloseServiceHandle(IntPtr handle);
    [DllImport("advapi32.dll", ExactSpelling = true, SetLastError = true)] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool QueryServiceStatusEx(IntPtr service, int level, out ServiceStatus status, int size, out int needed);
    [DllImport("advapi32.dll", EntryPoint = "GetTokenInformation", SetLastError = true)] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool GetSession(SafeAccessTokenHandle token, int kind, out uint session, int size, out int needed);
    [DllImport("advapi32.dll", EntryPoint = "GetTokenInformation", SetLastError = true)] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool GetStatistics(SafeAccessTokenHandle token, int kind, out Statistics statistics, int size, out int needed);
}
