<!-- Created: 2026-10-14 17:48 America/Los_Angeles | Path: /IDLE-LOCK.md | Repository: mjpcomp/locked-session-logoff | Owner: maintainers | Permissions: public read/maintainer write | Purpose: additive idle-lock scope and implementation tracker | Arguments: none | Usage: read alongside README.md and TASKS.md before implementation -->
# Optional idle-to-lock stage

## Status and checkpoint

Specification only; not implemented. This document supplements README.md and TASKS.md and must be consulted with them. Next documentation maintenance must add explicit links from both files. Foundation commit 934dc785888bed16ac816c71c70bc1d33b72c136 contains timer core, synthetic self-tests, and CI definition only; build results and Windows integration remain unverified here. No installable utility exists.

## Behavior

Add an independently enabled idle-lock option controlled by the elevated administrator application. Installation leaves it disabled. Configure X minutes of no session input before requesting a lock; configure Y minutes continuously locked before logoff separately. Do not log off directly because the idle threshold expired.

Unlocked/no input for X -> request lock -> positively observe actual lock -> start Y-minute locked timer. Manual locks use the same locked timer. Unlock resets locked timing. New input resets idle timing. Unknown input/state, helper failure, or identity mismatch must not be treated as proof of inactivity. Start/restart/unlock/reconfiguration must grant a fresh conservative idle window rather than lock immediately from stale historical data. Define/test suspend/resume semantics.

DryRun suppresses idle-lock requests and logoff actions; Debug independently controls detail. A would-lock decision must not fabricate a lock event or start a hypothetical logoff countdown. If a real manual lock occurs during dry-run, the normal locked timer can report would-logoff. Log stage, session, reason, and result without recording keystrokes, mouse coordinates, window titles, or content. Bound logs and retries.

## Architecture

GetLastInputInfo returns input information for the calling session, not all sessions. LockWorkStation requires an interactive-desktop process; success means a request was initiated, not proof of lock. Add LockedSessionLogoff.SessionAgent.exe, a small non-elevated helper in each eligible interactive user session. Use supported service-to-session launch and lifecycle management; never query service session 0 and assume it represents an employee. The service remains authoritative for policy and locked-session logoff.

Authenticate/authorize IPC, bind connections to actual session/logon identity, reject stale or cross-session reports and replayed requests, validate timestamps, and avoid accepting caller-provided session identity as authority. Protect binaries/configuration and service-side settings from standard users. Do not place service credentials or secrets in helper arguments. Confirm lock through native session events/state before starting the locked timer. Recheck state/input before requesting lock and handle input races conservatively.

Handle 32-bit input tick rollover and documented non-incremental input timestamps safely. Test local console and define RDP/disconnected-session support explicitly. Supervise helper startup/exit and avoid duplicate agents. Do not promise tamper-proof enforcement: a local administrator can disable the utility, a user may terminate their own helper until supervision restarts it, and synthetic input may prevent an input-idle threshold. No keylogging or mouse-jiggler detection is in scope.

Input-idle policy is independent of screensaver/sleep preferences. It does not require changing those settings. Do not silently modify power policies, screensaver settings, or media/presentation exceptions. Viewing video or a presentation without input may count as idle; make the policy explicit. Future exceptions require explicit design, not automatic promises.

## Installation, uninstall, and packaging

Include native x64 and ARM64 helper builds with matching service/admin packages. Installation alone enables no idle-lock enforcement. The admin utility controls enablement, timeout, exclusions, dry-run/debug, and diagnostics. Track helper launch mechanisms as owned changes. Uninstall stops monitoring/agents and removes owned launch mechanisms, binaries, configuration, and logs according to the existing preview/recovery policy. Preserve unrelated tasks/settings and flag conflicts. No change to irreversible-action warnings.

## Implementation and acceptance checklist

- [ ] Link this supplement from README.md and TASKS.md in next documentation update.
- [ ] Extend validated configuration with independent idle-lock enablement and timeout.
- [ ] Add session agent project, protected deployment, secure launch/lifecycle, and authenticated IPC.
- [ ] Measure input in the correct session; handle errors, tick rollover, and non-incremental timestamps.
- [ ] Idle threshold produces lock candidate only; recheck input/state before action.
- [ ] Verify actual lock before starting locked-logoff timing.
- [ ] Test new input cancellation, manual lock, unlock/relock, multiple sessions, exclusions, and helper recovery.
- [ ] Test dry-run: no lock, no simulated locked state, real manual lock still monitored.
- [ ] Test sleep/resume, service restart, session identity reuse, secure desktop, and remote-session policy.
- [ ] Admin UI clearly distinguishes X idle minutes and Y locked minutes.
- [ ] Verify disabled screensaver/sleep does not disable the independent policy.
- [ ] Test installer/uninstaller agent cleanup and partial-failure recovery.
- [ ] Build both architectures; validate on real devices separately.

## Public API references

- https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-getlastinputinfo
- https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-lockworkstation
- https://devblogs.microsoft.com/oldnewthing/20260618-00/?p=112444/

No organization-specific information or private support correspondence belongs in this document or implementation.
