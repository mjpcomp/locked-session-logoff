// Created: 2026-10-14 17:44 America/Los_Angeles
// Path: src/LockedSessionLogoff.Core/LockTimer.cs; Owner: maintainers
// Permissions: public read/maintainer write; Purpose: side-effect-free session timer
// Arguments: Observe takes a stable session identity, state, monotonic elapsed time and options
// Usage: call from one serialized monitoring loop; Windows integration must recheck before action
namespace LockedSessionLogoff.Core;

public enum LockState { Unknown, Unlocked, Locked }
public enum TimerDecision { None, WouldLogOff, LogOffCandidate }

public sealed record MonitorOptions
{
    public bool Enabled { get; init; } = false;
    public bool DryRun { get; init; } = false;
    public bool Debug { get; init; } = false;
    public TimeSpan Timeout { get; init; } = TimeSpan.FromMinutes(60);

    public void Validate()
    {
        if (Timeout <= TimeSpan.Zero || Timeout > TimeSpan.FromDays(7))
            throw new ArgumentOutOfRangeException(nameof(Timeout));
    }
}

public sealed class LockTimer
{
    private sealed record Entry(string Identity, TimeSpan Started, TimeSpan LastObserved);
    private readonly Dictionary<int, Entry> entries = new();
    private MonitorOptions? previousOptions;

    public TimerDecision Observe(int sessionId, string identity, LockState state,
        TimeSpan monotonicNow, MonitorOptions options, bool excluded = false)
    {
        ArgumentNullException.ThrowIfNull(options);
        options.Validate();
        ArgumentException.ThrowIfNullOrWhiteSpace(identity);
        if (sessionId < 0) throw new ArgumentOutOfRangeException(nameof(sessionId));
        if (monotonicNow < TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(monotonicNow));
        if (previousOptions != options)
        {
            entries.Clear();
            previousOptions = options;
        }
        if (!options.Enabled || excluded || state != LockState.Locked)
        {
            entries.Remove(sessionId);
            return TimerDecision.None;
        }
        if (!entries.TryGetValue(sessionId, out var entry) ||
            entry.Identity != identity || monotonicNow < entry.LastObserved)
        {
            entries[sessionId] = new(identity, monotonicNow, monotonicNow);
            return TimerDecision.None;
        }
        entries[sessionId] = entry with { LastObserved = monotonicNow };
        if (monotonicNow - entry.Started < options.Timeout) return TimerDecision.None;
        return options.DryRun ? TimerDecision.WouldLogOff : TimerDecision.LogOffCandidate;
    }

    public void Forget(int sessionId) => entries.Remove(sessionId);
    public void Reset() => entries.Clear();
}
