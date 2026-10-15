<!-- Created: 2026-10-15 10:43; Updated: 2026-10-15 10:58 America/Los_Angeles | Path: /VALIDATION.md | Repository: mjpcomp/locked-session-logoff | Owner: maintainers | Permissions: public read/maintainer write | Purpose: sanitized validation evidence and next implementation gate | Arguments: none | Usage: read alongside README.md, TASKS.md, SERVICE.md and ADMIN.md; distinguish reported evidence from independent verification -->
# Validation checkpoint

## 2026-10-15 10:58: administrator GUI persistence

Maintainer reports: GUI test succeeded; settings were changed, saved, and still present after closing and reopening the application. This validates GUI launch and save/reopen persistence on the current x64 test target. Exact artifact hash and CI run were not independently verified; the behavior is consistent with the administrator increment introduced in 74fc1b25fea321086e5c749583ad2b893f1fe3a2.

- [x] GUI launch, maintainer-reported success.
- [x] Configuration edited and saved, maintainer-reported success.
- [x] Close/reopen preserved saved settings, maintainer-reported success.
- [ ] UAC rejection behavior and no-write-on-launch independently exercised.
- [ ] Standard-user write denial on configuration key.
- [ ] Invalid/duplicate SID, corrupted/unknown settings and concurrent-editor tests.
- [ ] Remove/cancel behavior and preservation of unrelated registry data.
- [ ] ARM64 GUI execution.

The existing service does not consume these saved settings. Persistence is not applied monitoring/enforcement and does not validate configuration ACLs. No raw account details, paths, exclusion SIDs or settings values are retained here.

## 2026-10-15: x64 console dry-run

Evidence: maintainer pasted output from the self-contained service-host x64 diagnostic on the Windows 11 x64 deployment target. The transcript does not identify the artifact's exact commit/hash; it is consistent with the documented service-host increment introduced in c6abed0c24af74de83f5bca44194bf623b7a5b40. No independent artifact provenance verification is claimed.

Commands exercised from the extracted package:

```powershell
.\LockedSessionLogoff.Service.exe --self-check
.\LockedSessionLogoff.Service.exe --console --observe-seconds 20
```

Observed results:

- Native layout self-check passed without starting a service.
- Unlocked -> Locked -> WouldLogOff -> Unlocked sequence observed.
- WouldLogOff occurred approximately 20.07 seconds after observed Locked.
- No second WouldLogOff appeared during the remaining long locked interval.
- A later lock ended after approximately 6.02 seconds, without WouldLogOff during that short interval.
- The later lock did not immediately inherit the prior expired timer.
- All reported events were DryRun=true and Enabled=true.
- The console returned to the shell without an error shown.

These results validate one-device snapshot/timer integration and are consistent with unlock reset. They do not prove a complete fresh-threshold relock cycle, transitions between polling samples, service-delivered events, or real action execution. No user names, machine names, local paths, raw logon identities or original output are retained here.

## Prior evidence

- At a743d2d1cd29acc71b5efbcea80cdd00f9ddc069, supplied CI output confirmed x64 compilation with zero compiler warnings/errors and all 14 synthetic tests passing. ARM64 compilation was maintainer-reported; its log was not inspected.
- The earlier native x64 console probe showed Unlocked -> Locked -> Unlocked with stable diagnostic identity and no user-session error. Listener classification was subsequently corrected in source.

## Remaining integration and safety gates

- [ ] Full fresh-threshold relock cycle on a real device.
- [ ] Multiple interactive users, session-ID reuse and rapid transitions.
- [ ] Service installed/running under service identity; SCM session/power notifications.
- [ ] Sleep/hibernate/disconnect/reconnect recovery and event/poll ordering.
- [ ] Protected serialized configuration writes, service reload and effective-revision/status acknowledgment.
- [ ] Token/SID/logon identity, exclusions and final pre-action checks.
- [ ] Actual logoff, idle-lock helper, secure IPC and recovery.
- [ ] Secure installer/uninstaller and original-state ledger.
- [ ] Profile/task features, central reporting/server/authentication and remote commands.
- [ ] ARM64 device execution.

## Next increment

Connect service-side validated configuration consumption and effective revision/status to the administrator utility. Show saved intent separately from applied state. Invalid/missing/untrusted settings must leave monitoring disabled; unchanged settings must not continuously reset timers. Serialize writes and verify configuration access controls before any enforcement consumer. Add explicit settings validation/reload/failure tests.

Keep live lock/logoff unavailable until secure installation, token identity, exclusions and final-state checks are implemented. Installation must not activate monitoring or maintenance; an existing saved MonitoringRequested value must not silently turn a newly installed/upgraded service into enforcement. Define and acknowledge that transition explicitly. Runtime dry-run/debug remain requirements for the same eventual functional build.

Planning sources: README.md, TASKS.md, IDLE-LOCK.md, INTEGRATION.md, SERVICE.md, ADMIN.md, SERVER-PLAN.md, BRANDING.md. Consolidate README/TASKS links and historical source-status paragraphs in the next implementation increment. Do not repeat passed tests without relevant changes. Keep source/compiled/synthetic-tested/device-tested distinct. This documentation update changes no executable or workstation setting.
