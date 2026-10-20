<!-- Updated: 2026-10-20 09:36 America/Los_Angeles | Path: /IDLE-LOCK.md | Owner: maintainers | Permissions: public read/maintainer write | Purpose: staged idle-lock architecture and read-only observer guide | Arguments: observer self-check / observe idle-seconds | Usage: normal non-elevated local-console session; no enforcement yet -->
# Optional idle-to-lock stage

## Current implementation boundary

First stage is SOURCE ONLY until new CI/device evidence: pure InputIdleTimer, synthetic tests, and standalone LockedSessionLogoff.SessionAgent.exe input observer. Existing service/admin installation and machine configuration are NOT changed. Agent is separately packaged, manually started and read-only; it performs no lock/sign-out requests, writes no configuration, registers no startup task/service, uses no IPC and is not launched/supervised by the service. CLI idle threshold is diagnostic, not authoritative machine policy. SID exclusions/service configuration are not integrated into this observer yet.

Full idle-lock enforcement, authenticated IPC, launch supervision and user-facing warning/countdown remain unimplemented. The old foundation-only checkpoint is historical; TASKS.md and README.md describe current broader service validation. Do not call WouldLock a real lock or completed idle-lock feature.

## Why measure in the user session

GetLastInputInfo reports input only for its calling session, not all users, and input ticks can move backward. This agent refuses elevated administrator/SYSTEM/session-0 execution and only times a positively known active unlocked local Console session. RDP/disconnected/unknown sessions pause/reset; native input or WTS failures do not imply inactivity. It does not record keystrokes, mouse coordinates, window titles, screen content, account names or SID strings to its diagnostic output. The native adapter may query WTS snapshots locally but only the calling session is timed; no network export.

Startup, changed input tick, unlock, changed logon identity, timeout change, backward uptime, and observation gaps over 10 seconds grant a fresh window rather than using historical idle age for immediate action. 32-bit native tick subtraction handles rollover; future/ambiguous age greater than half the tick range pauses/reset timing conservatively. A changed backward input tick resets rather than extending a guessed old interval. At most one WouldLock per unchanged idle episode. Polling cannot establish every transition between samples, and synthetic input can affect the signal; no mouse-jiggler detection/tamper-proof promise.

## Standalone test after CI passes

Wait for Read-only idle observer workflow, idle tests, x64 layout check and publication. Download idle-observer-win-x64, fully extract and inspect package.json/checksums. This is NOT the integrated-test artifact and contains no installer. Keep the service uninstalled. ARM64 is cross-published only until native Surface Pro testing. Self-contained package requires no SDK/PowerShell 7 on the laptop; no network is used by this command sequence.

Open NORMAL Windows PowerShell, not Run as Administrator, at the extracted observer directory:

```powershell
Start-Transcript -Path (Join-Path (Get-Location) ('idle-observer-'+(Get-Date -Format 'yyyyMMdd-HHmmss')+'.txt')) -NoClobber
& .\LockedSessionLogoff.SessionAgent.exe --self-check | Out-Host
& .\LockedSessionLogoff.SessionAgent.exe --observe --idle-seconds 20 | Out-Host
```

Leave mouse/keyboard untouched for about 25 seconds; expect WouldLock but desktop remains unlocked. Move mouse/type and wait again; expect a new full window then one new WouldLock. Manually lock/unlock; timing should pause while locked and start fresh after unlock, not inherit expired time. Stop with Ctrl+C, then Stop-Transcript. Native executable input activity is not injected by these commands. Keep output private: transcript headers can contain account/machine names even though observer event text does not. Flash-drive round-trip command sheet can be supplied for this distinct package after CI.

No install/uninstall cycle is required. Stop observer, retain export, then remove its extracted folder if no longer needed. This does NOT uninstall any separate service installation. Do not edit the existing integrated package or run privileged CI fixtures.

## Intended later enforcement semantics

Independently enabled administrator-controlled X idle minutes and Y continuously locked minutes; installation leaves both disabled until explicitly enabled. Unlocked/no input for X -> recheck candidate -> request lock in correct user session -> positively observe actual native lock -> start separate Y timer. Manual locks use the same locked-logoff logic. Unlock/input changes reset relevant windows. WouldLock never fabricates lock state or starts a hypothetical logoff countdown. Same build retains runtime DryRun and independent Debug settings.

Service remains authoritative. Supported launch into eligible sessions and authenticated/authorized IPC must bind to actual SID/session/authentication identity, reject stale/replayed/cross-session claims, validate freshness, protect binaries/settings, avoid credentials/secrets in arguments and supervise duplicate/helper exit behavior. Recheck input/state immediately before any future lock request; API acceptance is not proof of lock. Define power, secure-desktop, RDP/disconnection, restart and retry behavior before integrating. User can terminate their own helper until supervised restart; administrators control the local trust boundary.

Track every owned launch mechanism for uninstall/reversal; preserve unrelated tasks/settings and recovery state on conflicts/partial failures. Publish x64/ARM64 and device-test separately. No silent changes to power policy/screensaver/sleep or presentation/media exceptions: watching video without input can count as idle. Profile/maintenance/server features remain separate opt-ins.

## Remaining acceptance gates

- New pure tests/CI/layout/build, then real non-elevated observer input/relock/resume tests.
- Independent validated policy/UI fields, exclusions, authenticated IPC and supervised launch.
- Native lock requests only after authority/identity/input checks; positive lock confirmation.
- Multiple sessions/RDP/security/error/replay/race/recovery/installer cleanup tests.
- Warning/countdown UX and broader release hardening.

Public API basis: https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-getlastinputinfo and https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-lockworkstation
