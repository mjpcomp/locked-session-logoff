<!-- Updated: 2026-10-20 09:36 America/Los_Angeles | Path: /README.md | Owner: maintainers | Permissions: public read/maintainer write | Purpose: current scope, safe component boundaries and roadmap | Arguments: see component guides | Usage: read with TASKS.md and IDLE-LOCK.md -->
# Locked Session Logoff

Organization-neutral Windows service/admin utility project for locked-session logoff, optional idle-lock, local-profile maintenance and scheduled restart/shutdown. No domain/Intune/cloud-management dependency is intended. Current packages are controlled tests, not a production release.

## Implemented versus validated

Service/admin packages implement protected configuration, native session state/events, SID/logon identity checks and exclusions, runtime dry-run, explicit default-off live permit, bounded diagnostics, action/outcome observations and baseline-preserving first-install/removal. Supplied Windows 11 x64 evidence includes ordinary SID dry-run lifecycle, retained history, a controlled live request followed by target absence, and uninstall restoration. PowerShell 5.1 preview fix 0138c6c now has supplied exact shell/package metadata, successful preview with 475 files/recovery/configuration preserved, and successful actual removal/baseline verification. Evidence scope/limitations are in [TASKS.md](TASKS.md), [VALIDATION-f6af81c.md](VALIDATION-f6af81c.md), [VALIDATION-0138c6c.md](VALIDATION-0138c6c.md) and earlier records.

NEW standalone idle observer source and tests are NOT yet build/device validated. It measures own-session input and reports WouldLock only: no locking/sign-out, startup registration, IPC or service policy integration. See [IDLE-LOCK.md](IDLE-LOCK.md). It is a separate artifact and needs no service installation.

No countdown/warning helper, actual idle-lock enforcement, profile management/retention changes, maintenance tasks, central reporting or remote commands exist yet. First-install/removal scripts are not an upgrade/recovery-complete transactional installer. Native ARM64 execution remains untested; cross-publication is separate. Targets are Windows 11 x64/ARM64, self-contained .NET 10 packages; no Windows 10/32-bit release target.

## Components and guides

- Integrated service/admin: [SERVICE-INTEGRATION.md](SERVICE-INTEGRATION.md); elevated deployment/configuration, SYSTEM action backend.
- Standalone non-elevated read-only observer: [IDLE-LOCK.md](IDLE-LOCK.md), Read-only idle observer workflow/artifacts.
- [Server/security plan](SERVER-PLAN.md), [branding](BRANDING.md), [current tasks](TASKS.md).

Installation starts monitoring OFF, dry-run ON, live authorization OFF. Real sign-out can lose unsaved work and requires explicit consent in controlled tests. Native request acceptance is not completed-action proof. Final identity/state checks reduce but cannot eliminate check-to-native-call races. Pending outcomes and bounded retained history are not tamper-evident/transactional audit. No arbitrary shell commands/network listener or automatic uploads.

## Developer entry points

With .NET 10 SDK at repository root:

```powershell
dotnet run --project tests/LockedSessionLogoff.SelfTests -c Release
dotnet run --project tests/LockedSessionLogoff.ConfigurationTests -c Release
dotnet run --project tests/LockedSessionLogoff.EligibilityTests -c Release
dotnet run --project tests/LockedSessionLogoff.DiagnosticsTests -c Release
dotnet run --project tests/LockedSessionLogoff.ActionTests -c Release
dotnet run --project tests/LockedSessionLogoff.IdleTests -c Release
```

These are executable synthetic tests, not dotnet test framework projects. Integrated CI also has disposable x64 install/preview/mismatch/restart/removal fixtures under Windows PowerShell 5.1 and PowerShell 7. Never run CI-only PackagingLifecycle.ps1 on an operator machine. New observer CI publishes separately and only self-checks layouts on x64; actual user idle behavior requires device evidence.

## Requirements still governing future work

Locked timers use observed lock/unlock, monotonic timing, stable identity and conservative unknown/error/reset behavior, not generic inactivity. Optional idle stage must confirm a REAL lock before starting separate locked timing, never fabricate lock in dry-run. Fresh idle time follows startup/unlock/reconfiguration; no keylogging/content collection or jiggler/tamper-proof claims. Service-authoritative authenticated IPC and protected/supervised per-user launch remain mandatory before enforcement. Do not silently change power/screensaver/media/presentation policy.

Future profile retention uses the native restart-based stale-profile policy, not a competing unattended deletion engine. Preserve original value/type/data; distinguish direct registry edits from Local Group Policy and policy refresh. App exclusions do not exempt profiles from native policy. Future profile inventory must preserve unknown dates/status; size scans on demand and reparse/permission-safe. Delete through Windows profile management, never raw folder/AppData wiping; block loaded/special/system/operator/excluded profiles, requery/confirm exact selections and record individual outcomes. Local profile removal does not delete cloud identity.

Future daily SYSTEM restart/shutdown tasks require explicit opt-in, owned names, inspection/preview, next/last/result, missed-run/DST/collision handling, warning/countdown/data-loss consent, exact copyable commands and original XML backup before any authorized replacement. Never overwrite unrelated tasks silently.

Uninstall restores captured configuration, not completed actions or lost work. Preserve protected recovery state on conflicts/partial failure, respect subsequent edits and define retry/upgrade behavior. Keep standard users from modifying privileged settings/binaries/ledger and authenticate all future privileged IPC. Licensing/dependency/signing and full release/adversarial gates remain open. Unsigned checksums are not signing. Never publish private operator transcripts/identifiers/credentials.

Read current docs/source before increments; record date/path/owner/permissions/purpose/arguments/usage. Use decoded file contents/commit patches, not SHA metadata as content. Validate XML; XML comments cannot contain double-hyphen command switches. Repository writes install nothing on operator devices.
