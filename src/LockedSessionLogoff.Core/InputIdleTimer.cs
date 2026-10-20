// Created: 2026-10-20 09:36 America/Los_Angeles
// Path: src/LockedSessionLogoff.Core/InputIdleTimer.cs; Owner: maintainers
// Permissions: public read/maintainer write; Purpose: pure conservative idle candidate timing
// Arguments: uptime, nullable native input tick, known-unlocked identity, timeout
// Usage: serialized caller; emits candidates only, never performs OS actions
namespace LockedSessionLogoff.Core;
public enum InputIdleDecision { None, ThresholdReached }
public sealed class InputIdleTimer
{
    public const long MaximumObservationGapMilliseconds = 10000;
    private long anchor, lastNow, threshold;
    private uint lastInput;
    private string? identity;
    private bool fired;
    public bool HasFreshWindow { get; private set; }
    public void Reset() { HasFreshWindow = false; fired = false; identity = null; }
    public InputIdleDecision Observe(long nowMilliseconds, uint? inputTick, bool knownUnlocked, string? logonIdentity, TimeSpan timeout)
    {
        if (timeout < TimeSpan.FromSeconds(5) || timeout > TimeSpan.FromDays(7))
        { Reset(); throw new ArgumentOutOfRangeException(nameof(timeout)); }
        if (nowMilliseconds < 0 || inputTick is null || !knownUnlocked || string.IsNullOrWhiteSpace(logonIdentity))
        { Reset(); return InputIdleDecision.None; }
        var age = unchecked((uint)nowMilliseconds - inputTick.Value);
        if (age > 0x7fffffffU) { Reset(); return InputIdleDecision.None; }
        var required = (long)Math.Ceiling(timeout.TotalMilliseconds);
        if (!HasFreshWindow || identity != logonIdentity || lastInput != inputTick.Value || threshold != required
            || nowMilliseconds < lastNow || nowMilliseconds - lastNow > MaximumObservationGapMilliseconds)
        {
            HasFreshWindow = true; anchor = nowMilliseconds; identity = logonIdentity;
            lastInput = inputTick.Value; threshold = required; fired = false;
        }
        lastNow = nowMilliseconds;
        if (fired || nowMilliseconds - anchor < threshold) return InputIdleDecision.None;
        fired = true; return InputIdleDecision.ThresholdReached;
    }
}
