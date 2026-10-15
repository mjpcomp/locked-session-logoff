<!-- Created: 2026-10-14 17:36; Updated: 2026-10-14 17:44 America/Los_Angeles | Repository: mjpcomp/locked-session-logoff | Path: /README.md | Owner: maintainers | Permissions: public read/maintainer write | Purpose: authoritative scope and operating guide | Arguments: none | Usage: read with TASKS.md before changes -->
# Locked Session Logoff

Organization-neutral Windows service and administrator utility project for locked-session logoff, local-profile maintenance, and scheduled restart/shutdown. No domain, Intune, or cloud-management dependency is intended.

## Current status

First foundation increment: side-effect-free C# timer library, deterministic executable self-tests, and a GitHub Actions workflow. No Windows service, administrator UI, native session adapter, installer, logoff execution, profile deletion, or maintenance task implementation exists yet. No installable release exists. Compilation and test results must be checked after the commit; source presence is not test evidence. TASKS.md is the durable checkpoint.

Initial targets: Windows 11 x64 and Windows 11 ARM64, .NET 10 LTS. ARM64 compilation and ARM64 device testing are separate milestones. No Windows 10 or 32-bit target is planned initially. Planned release packages are self-contained and architecture-specific.

## Foundation developer commands

With the .NET 10 SDK, from the repository root:

```powershell
dotnet run --project tests/LockedSessionLogoff.SelfTests/LockedSessionLogoff.SelfTests.csproj -c Release
dotnet build tests/LockedSessionLogoff.SelfTests/LockedSessionLogoff.SelfTests.csproj -c Release -r win-x64
dotnet build tests/LockedSessionLogoff.SelfTests/LockedSessionLogoff.SelfTests.csproj -c Release -r win-arm64
```

These run synthetic tests/builds, not a session watcher. The self-test executable has no privileged or destructive actions. It is not a dotnet test framework project. CI currently publishes no downloadable application.

## Intended components and safe defaults

- LockedSessionLogoff.Service.exe: privileged session monitoring/logoff.
- LockedSessionLogoff.Admin.exe: elevated configuration, profile management, task management, diagnostics, and uninstall preview.
- Install.ps1 / Uninstall.ps1: administrator-run deployment/removal with recovery.
- Protected configuration, bounded local logs, and a transactional original-state ledger.

Installation only installs; monitoring stays disabled until enabled through the admin utility. Profile cleanup and restart/shutdown tasks require separate opt-in. No telemetry or automatic uploads of logs, profile names/SIDs, configuration, or machine information.

## Locked-session requirements

Actual Windows lock/unlock events and state verification, not generic inactivity. Independent per-session timers; unlock resets; relock starts fresh. Recheck identity/state immediately before action. Unknown state/query failure resets timing and blocks logoff. Starting during a lock counts only from first observed lock. Track stable logon identity, not session ID alone. Define and test sleep/hibernate/disconnect/reconnect semantics; disconnection is not lock.

Use monotonic timing and serialized observations. The current core accepts monotonic elapsed values from a future adapter; it does not query Windows or prove uninterrupted state between polls. Native notifications and race handling remain required. Core decisions are candidates only, never permission to bypass the final recheck. Reset on configuration changes conservatively. Repeated candidates require adapter-level bounded logging/retry handling.

Proposed timeout: 60 minutes. Explicit account/SID exclusions, not automatic exemption of all admins. Same build supports enforcement and runtime DryRun; independent Debug controls verbosity. Proposed --dry-run/--debug flags and persistence/precedence are not implemented yet. Dry-run suppresses our destructive actions, not external policies/tasks already installed. Enabling forced logoff requires an unsaved-work warning.

## Profile retention

Configure native restart-based stale-profile policy; do not create a competing unattended deletion engine. Display current settings, preview/apply/remove, preserve original state, and provide exact copyable commands. Verify registry value/type/range/OS applicability before implementing.

Group Policy: Computer Configuration > Administrative Templates > System > User Profiles > Delete user profiles older than a specified number of days on system restart.

Direct registry configuration is not editing the Local Group Policy database and can be overwritten by policy refresh. App exclusions do not exempt profiles from the native policy. Retention means unused days; shutdown is not represented as equivalent to restart. Deleting a local profile does not delete its cloud identity.

## Interactive profile management

Show account when resolvable, SID, path, reported last use/age, loaded/special status, exclusions, and eligibility reasons. Unknown values remain unknown; unknown dates are excluded from age selection. Optional size scans run on demand and safely handle permissions/reparse points.

Delete through Windows profile management, never raw folder deletion. Block loaded/special/system/current-operator/excluded profiles; re-query before action, confirm exact selections, handle races, and log each result. Whole-profile deletion removes local files/settings, not just caches. Generic AppData wiping is out of scope.

## Maintenance task management

Preview/create/inspect/update/remove an owned daily SYSTEM task. Restart or shutdown, local time, warning/countdown, wake option, next/last run and result. Skip missed runs by default; test DST/clock changes. A positive shutdown countdown implies forced app closure; show the data-loss warning. Refuse silent collisions with unrelated tasks. If replacement is supported, require explicit approval and original XML backup/restore. Provide equivalent commands and Task Scheduler instructions.

## Uninstall and reversal

Persist original absent/existing value/type/data before first edit, preserve the baseline across repeated edits, and record last-written state transactionally. Protect privileged binaries/configuration/ledger from standard-user changes. Validate privileged requests.

Preview every stop/remove/restore/retain/conflict. Stop enforcement first; remove service, owned tasks, installed files/configuration. Restore original policy values or remove only values originally absent, never unrelated keys. Detect later third-party edits and ask rather than overwrite silently. Offer log export/retention. On partial failure, retain protected recovery records, return failure, and provide retry instructions; define unattended behavior. Upgrades must preserve original baselines.

Uninstall cannot recover deleted profiles or unsaved work. It reverses configuration, not completed destructive actions. Installation/removal commands will be documented only when implemented.

## Build, release, and continuity

GitHub Actions compiles foundation targets and runs synthetic x64 self-tests. Future self-contained service/admin packages need checksums and accurate signing labels; unsigned is not signed. Build success is not workstation validation. Select a license with maintainer approval before release; review dependencies. Never publish credentials, signing keys, customer/tenant details, or private logs.

Read README.md and TASKS.md before each increment. Record changed paths, timestamps, purpose, ownership/permissions, arguments/usage, evidence, blockers, and next step. Add headers where formats permit; document non-comment formats in tracking records.

Connector workaround: if a file read returns only blob metadata, use commit/PR diffs. A SHA is not content. Reconstruct complete files only from a known baseline or obtain full text; do not overwrite unknown content or loop metadata-only reads. Never publish private support correspondence.
