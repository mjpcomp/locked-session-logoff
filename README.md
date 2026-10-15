<!-- Created: 2026-10-14 17:36; Updated: 2026-10-15 09:39 America/Los_Angeles | Repository: mjpcomp/locked-session-logoff | Path: /README.md | Owner: maintainers | Permissions: public read/maintainer write | Purpose: authoritative scope and operating guide | Arguments: none | Usage: read with TASKS.md and IDLE-LOCK.md before changes -->
# Locked Session Logoff

Organization-neutral Windows service and administrator utility project for locked-session logoff, local-profile maintenance, and scheduled restart/shutdown. No domain, Intune, or cloud-management dependency is intended.

## Current status

Foundation source: side-effect-free C# timer library, 14 synthetic executable self-tests, and a GitHub Actions workflow. For commit a743d2d1cd29acc71b5efbcea80cdd00f9ddc069, maintainer-supplied logs confirm successful x64 compilation with zero compiler warnings/errors and 14/14 self-tests passing. The maintainer also reports ARM64 compilation succeeded; its log has not been independently inspected. These are foundation checks, not Windows integration tests.

No Windows service, administrator UI, native session adapter, session helper, installer, actual logoff, profile deletion, or maintenance task implementation exists yet. No installable release exists. See [TASKS.md](TASKS.md) for evidence and next steps and [IDLE-LOCK.md](IDLE-LOCK.md) for the optional idle-to-lock stage. The supplement's earlier unverified-build checkpoint is historical; the evidence above supersedes that build-status statement only.

Targets: Windows 11 x64 and Windows 11 ARM64, .NET 10 LTS. ARM64 compilation and ARM64 device testing are separate milestones. No Windows 10 or 32-bit target is planned initially. Future packages are self-contained and architecture-specific.

## Foundation developer commands

With the .NET 10 SDK, from the repository root:

```powershell
dotnet run --project tests/LockedSessionLogoff.SelfTests/LockedSessionLogoff.SelfTests.csproj -c Release
dotnet build tests/LockedSessionLogoff.SelfTests/LockedSessionLogoff.SelfTests.csproj -c Release -r win-x64
dotnet build tests/LockedSessionLogoff.SelfTests/LockedSessionLogoff.SelfTests.csproj -c Release -r win-arm64
```

These run synthetic tests/builds, not a session watcher. The self-test executable has no privileged or destructive actions and is not a dotnet test framework project. CI currently publishes no downloadable application.

## Intended components and safe defaults

- LockedSessionLogoff.Service.exe: privileged session monitoring/logoff.
- LockedSessionLogoff.Admin.exe: elevated configuration, profile/task management, diagnostics, and uninstall preview.
- LockedSessionLogoff.SessionAgent.exe: non-elevated per-session input-idle detection and interactive lock requests; see [IDLE-LOCK.md](IDLE-LOCK.md).
- Install.ps1 / Uninstall.ps1: administrator-run deployment/removal with recovery.
- Protected configuration, bounded local logs, and a transactional original-state ledger.

Installation only installs. Locked-logoff and idle-lock monitoring remain disabled until explicitly enabled through the admin utility. Profile cleanup and restart/shutdown tasks require separate opt-in. No telemetry or automatic uploads of logs, profile names/SIDs, configuration, or machine information.

## Locked-session and idle-lock requirements

Actual Windows lock/unlock events and state verification, not generic inactivity. Independent per-session timers; unlock resets; relock starts fresh. Recheck identity/state immediately before action. Unknown state/query failure resets timing and blocks logoff. Starting during a lock counts only from first observed lock. Track stable logon identity, not session ID alone. Define/test sleep/hibernate/disconnect/reconnect semantics; disconnection is not lock.

Use monotonic timing and serialized observations. The current core accepts monotonic elapsed values from a future adapter; it does not query Windows or prove uninterrupted state between polls. Native notifications and race handling remain required. Core decisions are candidates only; final state/identity checks remain mandatory. Reset on configuration changes conservatively. Bound repeated-candidate logging/retries.

Proposed locked timeout: 60 minutes. Explicit account/SID exclusions, not automatic exemption of all admins. Same build supports runtime DryRun and independent Debug verbosity. CLI flags, persistence, and precedence remain to be implemented. Dry-run suppresses our actions, not external policies/tasks already installed. Enabling forced logoff requires an unsaved-work warning.

Optional idle stage: X minutes with no session input -> request lock -> confirm actual lock -> start separate Y-minute locked-logoff timer. Do not invent a locked state in dry-run. Agent startup/restart/unlock grants a fresh conservative idle window. No keylogging, screen-content collection, mouse-jiggler detection, or promise of tamper-proof enforcement. Full architecture, privacy, IPC, and recovery requirements are in [IDLE-LOCK.md](IDLE-LOCK.md).

## Profile retention

Configure native restart-based stale-profile policy; do not create a competing unattended deletion engine. Display settings, preview/apply/remove, preserve original state, and provide exact copyable commands. Verify registry value/type/range/OS applicability before implementing.

Group Policy: Computer Configuration > Administrative Templates > System > User Profiles > Delete user profiles older than a specified number of days on system restart.

Direct registry configuration does not edit the Local Group Policy database and can be overwritten by policy refresh. App exclusions do not exempt profiles from the native policy. Retention means unused days; shutdown is not equivalent to restart. Deleting a local profile does not delete its cloud identity.

## Interactive profile management

Show account when resolvable, SID, path, reported last use/age, loaded/special status, exclusions, and eligibility reasons. Unknown values remain unknown; unknown dates are excluded from age selection. Optional size scans run on demand and handle permissions/reparse points safely.

Delete through Windows profile management, never raw folder deletion. Block loaded/special/system/current-operator/excluded profiles; re-query before action, confirm exact selections, handle races, and log individual outcomes. Whole-profile deletion removes local files/settings, not just caches. Generic AppData wiping is out of scope.

## Maintenance task management

Preview/create/inspect/update/remove an owned daily SYSTEM task. Restart/shutdown, local time, warning/countdown, wake option, next/last run and result. Skip missed runs by default; test DST/clock changes. Positive shutdown countdown implies forced application closure: show the data-loss warning. Refuse silent collisions with unrelated tasks; authorized replacement, if supported, requires original XML backup/restore. Provide equivalent commands and Task Scheduler instructions.

## Uninstall and reversal

Persist original absent/existing value/type/data before first edit, preserve baseline across repeated edits/upgrades, and record last-written state transactionally. Protect privileged binaries/configuration/ledger from standard-user changes. Authorize privileged requests and bind helper IPC to actual session/logon identity.

Preview every stop/remove/restore/retain/conflict. Stop enforcement and helpers first; remove service, owned helper launch mechanisms/tasks, installed files/configuration. Restore original policy values or remove only originally absent values, never unrelated keys. Detect later third-party edits and ask rather than overwrite silently. Offer log export/retention. On partial failure, retain protected recovery records, return failure, and provide retry instructions; define unattended behavior.

Uninstall cannot recover deleted profiles or unsaved work. It reverses configuration, not completed destructive actions. Installation/removal commands will be documented only when implemented.

## Build, release, and continuity

Foundation CI compiles both targets and runs synthetic x64 tests. Future self-contained packages need checksums and accurate signing labels. Build success is not workstation validation. Select a license with maintainer approval and review dependencies before release. Never publish credentials, signing keys, customer/tenant details, or private logs.

Read README.md, TASKS.md, and IDLE-LOCK.md before each increment. Record paths, timestamps, purpose, ownership/permissions, arguments/usage, evidence, blockers, and next step. Add file headers where formats permit; document non-comment formats in tracking records. XML comments must not contain double-hyphen command switches: validate project XML before publishing changes.

Connector workaround: use commit/PR diffs when file reads omit decoded content. A SHA is not content. Reconstruct full files only from a known baseline or obtain full text. Never overwrite unknown content or loop metadata-only reads. Do not publish private support correspondence.
