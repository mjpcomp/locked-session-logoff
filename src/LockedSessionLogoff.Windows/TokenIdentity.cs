// Created: 2026-10-15 12:13 America/Los_Angeles
// Path: src/LockedSessionLogoff.Windows/TokenIdentity.cs; Owner: maintainers
// Permissions: public read/maintainer write; Purpose: read actual session token identity
// Arguments: positive session ID; Usage: trusted SYSTEM service; handles always disposed
using Microsoft.Win32.SafeHandles;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Security.Principal;
namespace LockedSessionLogoff.Windows;

public sealed record TokenIdentity(int SessionId, string Sid, long AuthenticationId)
{
    public string Key => $"{SessionId}:{Sid}:{AuthenticationId}";
}
public static class SessionTokenReader
{
    public static TokenIdentity Read(int sessionId)
    {
        if (sessionId <= 0) throw new ArgumentOutOfRangeException(nameof(sessionId));
        CheckLayout();
        if (!WTSQueryUserToken((uint)sessionId, out var token))
        { var error = Marshal.GetLastWin32Error(); token?.Dispose(); throw new Win32Exception(error); }
        using (token)
        {
            if (!GetSession(token, 12, out var actualSession, sizeof(uint), out var sessionBytes) || sessionBytes != sizeof(uint))
                throw new Win32Exception(Marshal.GetLastWin32Error());
            if (actualSession != (uint)sessionId) throw new InvalidDataException("Token session mismatch");
            if (!GetStatistics(token, 10, out var statistics, Marshal.SizeOf<Statistics>(), out var bytes) || bytes < Marshal.SizeOf<Statistics>())
                throw new Win32Exception(Marshal.GetLastWin32Error());
            using var identity = new WindowsIdentity(token.DangerousGetHandle());
            var sid = identity.User?.Value ?? throw new InvalidDataException("Token has no user SID");
            var auth = ((long)statistics.AuthenticationId.High << 32) | statistics.AuthenticationId.Low;
            if (auth == 0) throw new InvalidDataException("Unknown authentication ID");
            return new(sessionId, sid, auth);
        }
    }
    public static void CheckLayout()
    {
        if (Marshal.SizeOf<Statistics>() != 56 || Marshal.OffsetOf<Statistics>(nameof(Statistics.AuthenticationId)).ToInt32() != 8)
            throw new PlatformNotSupportedException("Unexpected token statistics layout");
    }
#pragma warning disable CS0649
    [StructLayout(LayoutKind.Sequential)] private struct Luid { public uint Low; public int High; }
    [StructLayout(LayoutKind.Sequential)] private struct Statistics
    {
        public Luid TokenId, AuthenticationId; public long ExpirationTime;
        public int TokenType, ImpersonationLevel;
        public uint DynamicCharged, DynamicAvailable, GroupCount, PrivilegeCount; public Luid ModifiedId;
    }
#pragma warning restore CS0649
    [DllImport("wtsapi32.dll", ExactSpelling = true, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)] private static extern bool WTSQueryUserToken(uint id, out SafeAccessTokenHandle token);
    [DllImport("advapi32.dll", EntryPoint = "GetTokenInformation", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)] private static extern bool GetSession(SafeAccessTokenHandle token, int kind, out uint session, int size, out int needed);
    [DllImport("advapi32.dll", EntryPoint = "GetTokenInformation", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)] private static extern bool GetStatistics(SafeAccessTokenHandle token, int kind, out Statistics statistics, int size, out int needed);
}

public static class SessionEligibility
{
    public static bool Allows(TokenIdentity? expected, TokenIdentity? actual, SessionSnapshot? current, string expectedWtsIdentity, IEnumerable<string> exclusions)
        => expected is not null && actual is not null && expected == actual && actual.SessionId > 0
        && !string.IsNullOrWhiteSpace(actual.Sid) && actual.AuthenticationId != 0
        && current is not null && current.Id == actual.SessionId && current.CanObserveTimer
        && current.LockState == "Locked" && current.Identity == expectedWtsIdentity
        && !exclusions.Contains(actual.Sid, StringComparer.OrdinalIgnoreCase);
}
