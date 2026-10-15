<!-- Updated: 2026-10-15 11:21 America/Los_Angeles | Path: /TASKS.md | Owner: maintainers | Permissions: public read/maintainer write | Purpose: authoritative current checkpoint | Arguments: none | Usage: consult all linked guides; checked means specifically evidenced -->
# Current implementation tracker

## Latest increment

Installed dry-run service integration source prepared: see [SERVICE-INTEGRATION.md](SERVICE-INTEGRATION.md). This replaces earlier source-status checkpoints, not their evidence. Build/CI smoke/device validation of this new increment remains pending. Nothing is installed remotely by repository edits.

## Prior evidenced passes

- [x] 14 core synthetic tests and x64 compilation from supplied CI logs.
- [x] ARM64 foundation compilation, maintainer-reported only.
- [x] x64 probe lock/unlock snapshots on one workstation.
- [x] x64 console timer long/short lock dry-run.
- [x] Administrator GUI save/reopen persistence, maintainer-reported.
- [x] Configured console debug reload, enable/20-second timeout, unlock, timeout reload to 200, disable from supplied output.
- [ ] Installed service configuration/status/events/lifecycle on actual device.
- [ ] ARM64 device execution.

## Immediate next gate

Verify integrated CI including x64 isolated install/uninstall smoke test before attempting a workstation install. Then follow SERVICE-INTEGRATION.md to test disabled installation, service-reported observed revision, SCM session events, configuration reload, and previewed uninstall/baseline recovery. Do not repeat unrelated passed console tests.

## Remaining scope

- [ ] Configuration corruption/ACL/concurrency tests and additional automated security/lifecycle coverage.
- [ ] Native token/logon identity and exclusion SID enforcement; final pre-action checks.
- [ ] Functional logoff, same-build runtime dry-run/debug, explicit enablement.
- [ ] Idle-lock session helper, authenticated IPC, supervision, suspend/reconnect behavior.
- [ ] Durable bounded diagnostics/audit, transactional change ledger, interruption recovery and upgrade support.
- [ ] Profile inventory/guarded deletion/native retention-policy management.
- [ ] Owned scheduled restart/shutdown and reversal.
- [ ] Ubuntu/Laravel reporting/dashboard/API, scoped roles/groups/external authentication.
- [ ] Later secured remote commands, scheduling, replay/revocation safeguards.
- [ ] License decision, dependency/signing review, checksums and production release gates.
- [ ] Branding clearance/new clean branded repository/migration; no identifier change yet.

## Planning index and continuity

[README](README.md), [validation](VALIDATION.md), [current service integration](SERVICE-INTEGRATION.md), [server/security](SERVER-PLAN.md), [branding](BRANDING.md), [idle-lock](IDLE-LOCK.md). Historical incremental guides: [probe](INTEGRATION.md), [service host](SERVICE.md), [admin editor](ADMIN.md), [configuration bridge](CONFIG-INTEGRATION.md).

Public repository and examples remain organization-neutral. Local enforcement/reporting/maintenance default disabled. No live lock/logoff or maintenance APIs in current code. Keep source/compiled/synthetic-tested/installed-device-tested separate. Use commit/PR diffs for dropped decoded file reads; never treat a SHA as content. Headers/records must retain timestamp, path, ownership/permissions, purpose, arguments/usage.

This increment changes Windows library/project/settings/security/status, service and GUI code, packaging scripts/workflow, and this checkpoint plus service guide. Registry/application effects occur only when an operator runs scripts or confirms GUI writes. Current bundle's restoration conflicts are conservative and documented; incomplete uninstall must retain recovery state.
