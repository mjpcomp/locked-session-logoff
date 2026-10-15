<!-- Created: 2026-10-15 10:43 America/Los_Angeles | Path: /VALIDATION.md | Repository: mjpcomp/locked-session-logoff | Owner: maintainers | Permissions: public read/maintainer write | Purpose: sanitized validation evidence and next implementation gate | Arguments: none | Usage: read alongside README.md, TASKS.md and SERVICE.md; distinguish reported evidence from independent verification -->
# Validation checkpoint

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

## Not yet validated or implemented

- [ ] Full fresh-threshold relock cycle on a real device.
- [ ] Multiple interactive users, session-ID reuse and rapid transitions.
- [ ] Service installed/running under service identity; SCM session/power notification behavior.
- [ ] Sleep/hibernate/disconnect/reconnect recovery and event/poll ordering.
- [ ] Administrator application and protected configuration persistence/reload.
- [ ] Token/SID/logon identity, exclusions and final pre-action checks.
- [ ] Actual logoff, idle-lock helper, secure IPC and recovery.
- [ ] Secure installer/uninstaller and original-state ledger.
- [ ] Profile/task features, central reporting/server/authentication and remote commands.
- [ ] ARM64 device execution.

## Next increment

Build the local administrator utility and protected validated configuration, retaining monitoring-disabled installation defaults. Do not enable actual logoff before identity/final checks and secure installation are implemented. Dry-run/debug must remain runtime options in the same eventual functional build, not require recompilation.

Planning sources: README.md, TASKS.md, IDLE-LOCK.md, INTEGRATION.md, SERVICE.md, SERVER-PLAN.md, BRANDING.md. Next consolidation should link this record from README/TASKS and reconcile their historical source-status paragraphs. Do not repeat already-passed tests without a relevant change; retain distinct source/compiled/synthetic-tested/device-tested states. This documentation commit changes no executable or workstation setting.
