<!-- Created: 2026-10-15 11:21 America/Los_Angeles | Path: /SERVICE-INTEGRATION.md | Owner: maintainers | Permissions: public read/maintainer write | Purpose: installed dry-run integration, recovery and test guide | Arguments: install/uninstall WhatIf/Confirm and explicit restore switch | Usage: read fully before local installation -->
# Installed service integration - dry-run only

## Status and evidence

This increment connects ConfigurationGate to the actual service, adds protected local status with SCM PID/process-start/freshness checks, serialized configuration writes, administrator UI status, and a first-install/uninstall bundle. New build/smoke/device results are unverified until CI and workstation tests. No live lock/logoff, idle helper, profile policy, restart task, network reporting, or remote commands exist. All observation is forced dry-run. Exclusion lists still block observation until SID resolution is integrated.

Maintainer's prior configuration-observer test passed: debug toggled and reloaded; monitoring enabled from saved settings; a 20-second threshold produced WouldLogOff about 20.05 seconds after lock; unlock detected; timeout changed to 200 without restart; monitoring then disabled. This is console evidence, not installed-service validation. No private identifiers/output retained.

## Configuration/status and security

Machine configuration remains ConfigurationV1 under the existing 64-bit HKLM application key. Reads reject unsafe owners and ACLs granting untrusted writes. GUI writes use an administrator/SYSTEM-only named mutex plus stale-value checks; direct edits by privileged external tools remain outside that protocol. Unknown schema/type/values remain blocked. ACL verification is conservative; do not weaken permissions checks to get a test passing.

Service status uses HKLM\SOFTWARE\LockedSessionLogoff.Runtime\StatusV1. SYSTEM writes only, using the precreated runtime key. GUI verifies its ACL, report age (15 seconds), protocol, SCM running-process ID and exact process-start ticks. A privileged administrator can still alter registry/service configuration; this is a local administrative trust boundary, not a cryptographic attestation or remote API. Reports contain configuration fingerprint and bounded recent event strings, not account names/raw logon identities. Registry size is bounded on reads. Status is not persistent audit logging.

The GUI identifies observed revision and whether it matches the editor-loaded saved revision. Observed does not mean a blocked configuration was activated: check Mode and Observation. No fresh verified report means unknown/not verified, never healthy/enabled by inference. Old admin binaries do not participate in the new writer lock; close/delete old extracted copies and use this bundle only.

Startup with an already enabled saved request requires OFF then ON after startup. Installer writes disabled defaults even if a prior GUI-only configuration existed and captures the previous string for restoration. Power events and long gaps disarm conservatively; this test build requires reactivation after them. Unchanged reads do not reset timers. SCM session events reset affected session timers, capturing transitions a console poll might miss.

## Install after integrated workflow passes

First-install only, not upgrade. Extract integrated-dryrun-win-x64 (or native ARM64) fully, review SHA256SUMS.txt and scripts. Packages are unsigned: hashes do not replace signing. Close prior GUI/observer processes. Open native elevated PowerShell at the package root.

```powershell
.\Install.ps1 -WhatIf
.\Install.ps1
```

Review confirmation. Creates only owned Program Files/ProgramData directories, SYSTEM service, runtime key and disabled application configuration. Existing service/directories/runtime key cause refusal, not overwrite. Previous application ConfigurationV1 string is captured in protected ProgramData install.json. No unrelated profile policy/power setting/task is changed. Installation scripts do not run automatically merely because an artifact is downloaded.

Open the installed admin executable under Program Files\LockedSessionLogoff\admin. Verify a fresh report with Disabled/Observation=false. Then save OFF, wait for Disabled, set timeout 20, exclusions empty, dry-run checked, and save ON. Lock for at least 25 seconds, unlock, and inspect recent service events/ WouldLogOff. Change timeout without restarting service; confirm observed revision/mode. Test OFF cancels observation. Stop/start service through Windows Services and verify stale status is not presented as fresh.

## Uninstall and recovery

```powershell
& "$env:ProgramFiles\LockedSessionLogoff\Uninstall.ps1" -WhatIf
& "$env:ProgramFiles\LockedSessionLogoff\Uninstall.ps1"
```

Preview lists owned service/files/runtime report and baseline configuration restoration. Installed file hashes and service binary path must match. Only manifest-owned files are deleted; unlisted/modified files cause a retained recovery record rather than recursive deletion. No persistent logs exist in this build, so there is no log archive to export yet.

Any post-install configuration edit, including a legitimate GUI save, is conservatively treated as a restoration conflict. After reviewing the captured baseline and current configuration and closing the GUI, explicitly acknowledge discarding later edits with:

```powershell
& "$env:ProgramFiles\LockedSessionLogoff\Uninstall.ps1" -RestoreOriginalConfiguration
```

This restores the original ConfigurationV1 string or removes only that value if originally absent; unrelated registry values are preserved. An absent current value is treated as already removed. Empty owned keys/directories can be removed. Recovery state is deleted only after successful completion; partial failures retain it and must not be called a clean uninstall. Repeated removal/recovery must be tested. Do not blindly delete leftovers or rerun installation after failure. No existing executable upgrade is supported; uninstall/review first.

Limitations: first installer uses inherited registry ACLs, which service verifies rather than rewriting. It does not automatically rollback every interrupted install; a phase/baseline/file record is retained once prepared. Failure before that record may require reviewed manual removal of newly created empty/partial owned directories. No domain/branding rename is included. Future production installer needs broader interruption, reparse/race, permission, upgrade, concurrent-tool and recovery testing. This is a controlled test bundle, not a production release.

## Gates

- [ ] CI core/configuration tests, compilation, x64 isolated install/uninstall smoke pass.
- [ ] Real installed service under SYSTEM: fresh verified report and disabled installation.
- [ ] GUI enable/disable/timeout reload and SCM lock/unlock events.
- [ ] Standard-user write denial and status spoof/stale/PID-reuse tests.
- [ ] Install/uninstall baseline restoration, conflicts, modified files and partial recovery.
- [ ] ARM64 device lifecycle.
- [ ] SID/token identity/exclusions/final action checks before live logoff.
- [ ] Idle agent/IPC, full logs/ledger, profile/task and server features remain pending.

Read with README.md, TASKS.md, VALIDATION.md and SERVER-PLAN.md. This guide supersedes the earlier no-service-consumer descriptions in ADMIN.md/SERVICE.md for this new bundle only; old standalone artifacts retain their old behavior.
