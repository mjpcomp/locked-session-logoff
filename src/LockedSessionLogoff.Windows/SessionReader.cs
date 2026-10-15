// Created: 2026-10-15 09:45; Updated: 2026-10-15 09:55 America/Los_Angeles
// Path: src/LockedSessionLogoff.Windows/SessionReader.cs; Owner: maintainers
// Permissions: public read/maintainer write; Purpose: read-only local WTS snapshots
// Arguments: ReadAll takes none; Usage: Windows callers; errors remain explicit
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace LockedSessionLogoff.Windows;

public sealed record SessionSnapshot(int Id, string Station, int ConnectionState,
    string Account, string LockState, long LogonFileTime, string Identity, string? Error)
{
    public string ConnectionStateName => ConnectionState switch
    {
        0 => "Active", 1 => "Connected", 2 => "ConnectQuery", 3 => "Shadow",
        4 => "Disconnected", 5 => "Idle", 6 => "Listen", 7 => "Reset",
        8 => "Down", 9 => "Init", _ => "Unknown"
    };
    public string Kind => Id == 0 ? "Services" : ConnectionState == 6 ? "Listener"
        : string.IsNullOrEmpty(Account) ? "NoUserOrUnknown" : "User";
    public bool CanObserveTimer => Id > 0 && ConnectionState == 0 && Error is null
        && !string.IsNullOrEmpty(Identity) && (LockState is "Locked" or "Unlocked");
}

public static class SessionReader
{
    public static IReadOnlyList<SessionSnapshot> ReadAll()
    {
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException();
        CheckLayout();
        if (!WTSEnumerateSessionsW(IntPtr.Zero, 0, 1, out var buffer, out var count))
            throw new Win32Exception(Marshal.GetLastWin32Error());
        try
        {
            if (count < 0 || count > 65536) throw new InvalidOperationException("Invalid session count");
            var result = new List<SessionSnapshot>();
            var stride = Marshal.SizeOf<SessionInfo>();
            for (var i = 0; i < count; i++)
            {
                var info = Marshal.PtrToStructure<SessionInfo>(IntPtr.Add(buffer, checked(i * stride)));
                var station = Marshal.PtrToStringUni(info.Station) ?? string.Empty;
                if (info.Id == 0 || info.State == 6)
                {
                    result.Add(new(info.Id, station, info.State, string.Empty, "NotApplicable", 0, string.Empty, null));
                    continue;
                }
                result.Add(ReadOne(info, station));
            }
            return result;
        }
        finally { WTSFreeMemory(buffer); }
    }

    private static SessionSnapshot ReadOne(SessionInfo session, string station)
    {
        if (!WTSQuerySessionInformationW(IntPtr.Zero, session.Id, 25, out var data, out var bytes))
        {
            var error = Marshal.GetLastWin32Error();
            return Unknown(session, station, new Win32Exception(error).Message + $" ({error})");
        }
        try
        {
            if (bytes < Marshal.SizeOf<InfoEx>()) return Unknown(session, station, "Short WTSINFOEX buffer");
            var value = Marshal.PtrToStructure<InfoEx>(data);
            if (value.Level != 1 || value.Data.SessionId != session.Id)
                return Unknown(session, station, "Unexpected session information level or identity");
            var v = value.Data;
            var user = v.User ?? string.Empty;
            var domain = v.Domain ?? string.Empty;
            var account = user.Length == 0 ? string.Empty : domain.Length == 0 ? user : domain + "\\" + user;
            var state = v.Flags switch { 0 => "Locked", 1 => "Unlocked", _ => "Unknown" };
            var identity = user.Length == 0 || v.LogonTime <= 0 ? string.Empty
                : $"{v.SessionId}:{account}:{v.LogonTime}";
            return new(session.Id, station, v.ConnectionState, account, state, v.LogonTime, identity, null);
        }
        finally { WTSFreeMemory(data); }
    }

    private static SessionSnapshot Unknown(SessionInfo s, string station, string error)
        => new(s.Id, station, s.State, string.Empty, "Unknown", 0, string.Empty, error);

    public static void CheckLayout()
    {
        if (IntPtr.Size != 8 || Marshal.SizeOf<SessionInfo>() != 24 ||
            Marshal.SizeOf<LevelOne>() != 224 || Marshal.SizeOf<InfoEx>() != 232 ||
            Marshal.OffsetOf<InfoEx>(nameof(InfoEx.Data)).ToInt32() != 8 ||
            Marshal.OffsetOf<LevelOne>(nameof(LevelOne.LogonTime)).ToInt32() != 160)
            throw new PlatformNotSupportedException("Unexpected WTS structure layout; monitoring must remain disabled");
    }

#pragma warning disable CS0649
    [StructLayout(LayoutKind.Sequential)]
    private struct SessionInfo { public int Id; public IntPtr Station; public int State; }
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct LevelOne
    {
        public int SessionId;
        public int ConnectionState;
        public int Flags;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 33)] public string? Station;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 21)] public string? User;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 18)] public string? Domain;
        public long LogonTime;
        public long ConnectTime;
        public long DisconnectTime;
        public long LastInputTime;
        public long CurrentTime;
        public uint IncomingBytes;
        public uint OutgoingBytes;
        public uint IncomingFrames;
        public uint OutgoingFrames;
        public uint IncomingCompressedBytes;
        public uint OutgoingCompressedBytes;
    }
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct InfoEx { public uint Level; public LevelOne Data; }
#pragma warning restore CS0649
    [DllImport("wtsapi32.dll", ExactSpelling = true, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool WTSEnumerateSessionsW(IntPtr server, int reserved, int version, out IntPtr sessions, out int count);
    [DllImport("wtsapi32.dll", ExactSpelling = true, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool WTSQuerySessionInformationW(IntPtr server, int sessionId, int infoClass, out IntPtr data, out int bytes);
    [DllImport("wtsapi32.dll", ExactSpelling = true)]
    private static extern void WTSFreeMemory(IntPtr memory);
}
