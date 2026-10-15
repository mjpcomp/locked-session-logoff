// Created: 2026-10-15 15:48 America/Los_Angeles
// Path: src/LockedSessionLogoff.Windows/LogoffCoordinator.cs; Owner: maintainers
// Permissions: public read/maintainer write; Purpose: testable action boundary and identity-bound outcome reconciliation
// Arguments: validated candidate/config/token/snapshot and monotonic time; Usage: serialized service; injected backend in tests
using System.ComponentModel;
using System.Security.Cryptography;
using System.Text;
namespace LockedSessionLogoff.Windows;

public interface ILogoffBackend { void Request(int sessionId); }
public enum SessionObservation { Unknown, SameIdentityPresent, SessionAbsent, IdentityReplaced }
public sealed record PendingLogoff(string ActionId, TokenIdentity Target, string WtsIdentity, TimeSpan Deadline);
public sealed record ActionEvent(string Kind, int SessionId, string? ActionId = null, string? Reason = null)
{
    public override string ToString() => Kind + " session=" + SessionId
        + (ActionId is null ? "" : " action=" + ActionId) + (Reason is null ? "" : " reason=" + Reason);
}
public sealed class LogoffCoordinator(ILogoffBackend backend, bool liveCapable)
{
    public static readonly TimeSpan OutcomeWindow = TimeSpan.FromSeconds(30);
    private readonly HashSet<TokenIdentity> attempted = new();
    private readonly Dictionary<string, PendingLogoff> pending = new(StringComparer.Ordinal);
    public IReadOnlyList<PendingLogoff> Pending => pending.Values.ToArray();

    public ActionEvent Execute(TokenIdentity expected, string wtsIdentity, string? expectedRaw,
        AdminSettings latest, string? latestRaw, EffectiveConfiguration mode,
        TokenIdentity? actual, SessionSnapshot? current, TimeSpan now)
    {
        int id = expected.SessionId;
        try { latest.Validate(); } catch (Exception) { return new("CandidateBlockedConfiguration", id); }
        var revision = latestRaw is null ? null : Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(latestRaw)));
        if (!mode.Enabled || !latest.MonitoringRequested || latestRaw != expectedRaw || revision != mode.ObservedRevision
            || mode.TimeoutSeconds != latest.LockedTimeoutSeconds || (liveCapable && latestRaw is null))
            return new("CandidateBlockedConfiguration", id);
        if (!SessionEligibility.Allows(expected, actual, current, wtsIdentity, latest.ExcludedSids))
            return new("CandidateBlockedIdentityOrState", id);
        if (mode.DryRun || latest.DryRun) return new("WouldLogOff", id);
        if (!liveCapable || !latest.AllowLiveLogoff) return new("LiveActionBlocked", id);
        if (attempted.Contains(expected)) return new("LogoffDuplicateSuppressed", id);
        if (attempted.Count >= 128 || pending.Values.Any(p => p.Target.SessionId == id))
            return new("LiveActionBlocked", id, Reason: "AttemptCapacityOrSessionPending");
        attempted.Add(expected);
        var actionId = Guid.NewGuid().ToString("N");
        try { backend.Request(id); }
        catch (Exception e)
        {
            var reason = e is Win32Exception native ? "Win32=" + native.NativeErrorCode : e.GetType().Name;
            return new("LogoffRequestFailed", id, actionId, reason);
        }
        pending.Add(actionId, new(actionId, expected, wtsIdentity, now + OutcomeWindow));
        return new("LogoffRequestAccepted", id, actionId, "CompletionNotConfirmed");
    }
    public ActionEvent? Observe(string actionId, SessionObservation observation, TimeSpan now)
    {
        if (!pending.TryGetValue(actionId, out var item)) return null;
        string? kind = now >= item.Deadline ? "LogoffOutcomeUnconfirmed" : observation switch
        {
            SessionObservation.SessionAbsent => "LogoffTargetAbsentObserved",
            SessionObservation.IdentityReplaced => "LogoffTargetIdentityReplacedObserved",
            _ => null
        };
        if (kind is null) return null;
        pending.Remove(actionId);
        return new(kind, item.Target.SessionId, actionId,
            kind == "LogoffOutcomeUnconfirmed" ? "ObservationDeadline" : "ObservedAfterRequest_NotCausalProof");
    }
    public ActionEvent[] Abandon(string reason)
    {
        var result = pending.Values.Select(p => new ActionEvent("LogoffOutcomeUnconfirmed", p.Target.SessionId, p.ActionId, reason)).ToArray();
        pending.Clear(); return result;
    }
}
