<!-- Created: 2026-10-15 09:55 America/Los_Angeles | Path: /SERVICE.md | Owner: maintainers | Permissions: public read/maintainer write | Purpose: service increment checkpoint and console test guide | Arguments: console/observe-seconds/self-check as below | Usage: consult with README.md, TASKS.md, IDLE-LOCK.md, INTEGRATION.md -->
# Service host and real-session dry-run

## Status and sanitized evidence

Maintainer tested the prior x64 probe on 2026-10-15: an interactive console session reported Unlocked -> Locked -> Unlocked, with unchanged diagnostic identity and no user-session errors. This is a one-device console snapshot pass, not service-account/event/enforcement or ARM64 validation. No account names, paths or raw identity values are included here.

This increment adds service-host source and fixes listener/services classification. Compilation and package availability must be verified after CI. Service/admin/helper integration is not complete. No logoff or lock function is imported or called. A functional enforcement release still requires admin controls, token/logon identity validation, final checks, persistence/security, installation and rollback.

## Native classification fix

Session 0 and WTSListen entries are explicitly non-user with NotApplicable lock state and no fabricated query error. Genuine user query/identity errors remain visible as Unknown. Snapshots add readable connection state, Kind and CanObserveTimer. The latter is read-only timer eligibility, not authority to log off. Only active connected user sessions with known identity/state qualify for this diagnostic. Disconnected-user enforcement is not implemented.

## Service architecture

A ServiceBase host accepts session-change and power notifications. Session changes reset the affected timer; power changes reset all timers conservatively. Polling and event handling share a serialized gate. Gaps over ten seconds reset all timing. Service mode always uses Enabled=false, accepts no runtime enablement/configuration yet, and has no persistent logging or external status interface. AutoLog=false prevents implicit event-source setup. Do not install this host manually: a secure installer and admin utility are not provided yet.

Console mode intentionally enables only synthetic decisions over real snapshots with DryRun=true. It prints state transitions and once-per-lock WouldLogOff. It does not receive SCM session notifications, so rapid unlock/relock between polls may be missed. Do not treat console output as proof of continuous-lock enforcement. Any LogOffCandidate result is never executed by this increment.

## Test after Service host diagnostics workflow succeeds

Download service-host-win-x64, extract all files, and open elevated PowerShell in that directory. The self-contained unsigned package needs no .NET runtime installation. Inspect SHA256SUMS.txt; hashes are not signing.

```powershell
.\LockedSessionLogoff.Service.exe --self-check
.\LockedSessionLogoff.Service.exe --console --observe-seconds 20
```

Lock for more than 20 seconds, unlock, stop with Ctrl+C, and inspect for Locked, WouldLogOff, Unlocked. Repeat with an unlock before the threshold: no WouldLogOff should occur for that short interval. This is a dry-run console test and cannot sign anyone out. Session IDs are printed but account names are omitted. No files/settings/services/tasks are created by console mode. Calling the executable without flags expects SCM and is not a console test.

The updated probe is still packaged by its original workflow and can show the new listener classification. ARM64 packages are compilation targets only until run on real hardware.

## Remaining work

- [ ] Verify service-host compilation/self-check/artifacts on CI.
- [ ] Verify console dry-run timing on a real x64 workstation.
- [ ] Secure installer/uninstaller and admin UI; monitoring stays disabled at install.
- [ ] Real SCM lifecycle/events under service identity; event/poll ordering and suspend handling.
- [ ] Token/SID/logon identity checks, exclusions and final pre-action verification.
- [ ] Actual logoff action gated by explicit enablement; runtime dry-run/debug/config reload.
- [ ] Session agent, idle-lock detection, authenticated IPC and lifecycle.
- [ ] Bounded persistent diagnostics/status and original-state ledger.
- [ ] Profile/task features and reversal remain pending.
- [ ] ARM64 device validation.

Next documentation consolidation must link this file and INTEGRATION.md from README.md/TASKS.md and update their source-status paragraphs. These additive increment records do not override safe defaults or claim pending features are complete. No workstation changes have been made remotely. Repository maintainer permissions are not a substitute for the workstation ACL implementation. License remains undecided; no production release is implied.
