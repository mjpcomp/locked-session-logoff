<!-- Created: 2026-10-14 17:36 America/Los_Angeles | Repository: mjpcomp/locked-session-logoff | Path: /README.md | Owner: repository maintainers | Permissions: public read, authorized maintainer write | Purpose: authoritative project scope and operating guide | Arguments: none | Usage: read before implementation; update with verified behavior -->
# Locked Session Logoff

A planned Windows service and administrator utility for locked-session logoff, local-profile maintenance, and scheduled restart/shutdown management. Organization-neutral; no domain, Intune, or cloud management dependency is intended.

## Status and source of truth

Planning/documentation only. No application, installer, tests, executable, or build workflow exists yet. Features below are requirements, not claims of implemented or tested functionality. README.md defines scope and safety requirements; TASKS.md tracks implementation, verification, blockers, and next steps. Update both when behavior or status changes.

## Goals and components

- LockedSessionLogoff.Service.exe: privileged Windows service monitoring actual session lock state and logging off only eligible sessions.
- LockedSessionLogoff.Admin.exe: elevated administrator interface controlling configuration, profiles, cleanup policy, maintenance tasks, diagnostics, and uninstall preview.
- Install.ps1 and Uninstall.ps1: documented administrator-run installation/removal.
- Administrator-protected configuration, local logs, and a durable change ledger.
- C#/.NET implementation and self-contained Windows build package through GitHub Actions; initial architecture target and supported OS versions must be established and tested before release.

Installation installs components only. Monitoring remains disabled until explicitly enabled in the administrator utility. Installation must not enable profile cleanup or create restart/shutdown schedules. No telemetry or automatic uploading of configuration, logs, profile names, SIDs, or machine information.

## Locked-session logoff

Use actual Windows session lock/unlock notifications and state verification, not keyboard/mouse inactivity alone. Maintain independent timers per session. Unlock cancels/resets timing; a later lock starts a fresh interval. Recheck the specific session immediately before logoff. Handle notification/query races conservatively and document the unavoidable boundary around a logoff request. Unknown state or query failure must not cause logoff; reset timing conservatively and log the problem.

If monitoring starts while a session is already locked, count from the first positive observation, not an assumed earlier lock time. Use monotonic elapsed timing and define/test sleep, hibernation, disconnect, reconnect, service restart, and session-ID reuse behavior. Lock and disconnection are not interchangeable.

Proposed default timeout: 60 minutes after explicit enablement. Support explicit account/SID exclusions, without automatically exempting every administrator. Forced logoff may discard unsaved work; require a clear warning when enabling enforcement.

### Runtime diagnostics, not separate builds

The first executable must support both real enforcement and report-only operation without recompilation. DryRun prevents application-initiated destructive actions; Debug increases diagnostic verbosity. Their configuration and any command-line overrides must have documented precedence and administrator-controlled persistence. Proposed flags are --dry-run and --debug; these are not available commands yet. Normal enforcement is available in the same build.

Dry-run cannot suppress Windows cleanup policies or scheduled tasks that already exist. Explain this boundary prominently. Do not imply that toggling dry-run reverses external configuration.

## Profile retention policy

Prefer configuring Windows' existing restart-based stale-profile policy rather than introducing another unattended deletion engine. The admin utility must show effective settings, preview changes, apply/remove the relevant value explicitly, and preserve original state.

Local Group Policy location:

Computer Configuration > Administrative Templates > System > User Profiles > Delete user profiles older than a specified number of days on system restart.

Verify the exact registry path, value name/type, range, and OS applicability against authoritative documentation/ADMX before generating commands. Explain that direct registry configuration does not edit the Local Group Policy database and can be overwritten by policy refresh or another management system. Application exclusions do not become exclusions from Windows' built-in cleanup policy.

Show copyable, fully resolved commands as an alternative to applying changes. Clarify that retention refers to unused profiles and that deleting a local profile removes local files/settings, not the cloud identity. Shutdown must not be represented as equivalent to restart for this policy.

## Interactive profile management

Display account name when resolvable, SID, profile path, reported last-use time, age, loaded status, special/system status, exclusions, and eligibility reasons. Unknown names/dates must remain explicitly unknown. Optional size scans run on demand and handle permissions/reparse points safely.

Use Windows profile-management mechanisms, not raw recursive directory deletion. Block loaded, special/system, current operator, and excluded profiles. Re-query immediately before deletion, handle races/failures, and require confirmation of the exact selected profiles. Unknown last-use dates are excluded from automatic age selection. Log outcomes individually. Do not call whole-profile deletion cache clearing. Generic AppData wiping is out of scope.

## Scheduled restart/shutdown

Admin utility can preview, create, inspect, update, and remove its own named daily maintenance task. Options: restart or shutdown, local time, warning/countdown, wake from sleep, and explicit forced-close/data-loss acknowledgement. Show next run, last run, and last result. Do not catch up missed runs by default. Test daylight-saving and clock-change behavior. A shutdown countdown greater than zero implicitly forces application closure; explain this accurately.

Use a suitable SYSTEM task principal. Refuse silent overwrite of existing unrelated tasks. If replacement is explicitly supported, require backup and a clear restore decision. Provide equivalent PowerShell commands and Task Scheduler instructions.

## Installation and intended operating sequence

No executable or installation commands are available yet. Do not run placeholder commands as if implemented. Intended sequence:

1. Download a verified build artifact and inspect checksums/signing status.
2. Install as administrator; monitoring and optional maintenance remain disabled.
3. Open the elevated admin utility, review defaults/exclusions, and enable dry-run monitoring.
4. Test lock/unlock, relock, multiple sessions, failures, and timeout behavior.
5. Explicitly enable enforcement after reviewing unsaved-work warnings.
6. Independently opt into profile retention and/or a daily maintenance task.
7. Use uninstall preview before removal.

A successful compilation is not Windows integration validation. Unsigned builds must be labeled unsigned. No signing keys, credentials, private logs, organization-specific details, or tenant identifiers belong in source/build artifacts. Include checksums, dependency/license review, and an explicit license decision before release.

## Reversible changes and uninstall

Persist original state before the first modification and record each successful change transactionally. Preserve whether a value was absent, its type/data, original task definitions if replacement was authorized, and the last value written by this utility. Repeated edits must not replace the original baseline. Protect executable/configuration/ledger locations from standard-user modification and service privilege escalation.

Before uninstall, show exactly what will be stopped, removed, restored, retained, or unresolved. Stop enforcement first. Remove the service, application-owned tasks, installed files, and application-owned configuration. Restore prior policy values, removing only values originally absent; do not delete unrelated registry keys.

Compare current state with the utility's last-written state. If another administrator changed it, flag a conflict and request a decision rather than overwriting silently. Offer log export. Remove logs/state after successful restoration unless explicitly retained. On partial failure, retain protected recovery information and return a non-success result with recovery instructions. Support repeat/recovery runs and define safe unattended-uninstall behavior.

Uninstall cannot recover deleted profiles or unsaved work lost through logoff/restart/shutdown. It restores configuration, not completed destructive actions.

## Contributor continuity and connector reads

Read README.md and TASKS.md before changes; distinguish planned, implemented, built, and Windows-tested states. Record dates, tests, failures, blockers, and the next step. Files/scripts must include creation/update timestamp, location, purpose, owner/permissions, arguments, and usage where the format allows comments; document non-comment formats in the tracking records.

Known connector workaround: file-read operations may return success and a blob SHA while omitting decoded content. Use a commit diff or pull-request diff to obtain text. A SHA is not file content. Diffs are contextual changes, not necessarily complete current files: reconstruct from a known baseline or request the full file from the maintainer when necessary. Never overwrite unknown content based on metadata alone. Keep private support correspondence out of this public repository.
