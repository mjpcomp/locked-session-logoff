<!-- Updated: 2026-10-15 13:13 America/Los_Angeles | Path: /TASKS.md | Owner: maintainers | Permissions: public read/maintainer write | Purpose: authoritative checkpoint and next validation gates | Arguments: none | Usage: distinguish source/CI/synthetic/device evidence -->
# Current implementation tracker

## Prior round complete: SID-aware workstation dry-run lifecycle

See [sanitized checkpoint](VALIDATION-386df4b.md). Supplied evidence establishes clean install, GUI-verified reports, dry-run lock timeout/unlock, SID exclusion and removal restoring eligibility, restart OFF/ON activation gate, disable, Copy/Export, remove-settings inactive behavior, and uninstall with service/files/recovery/runtime absent and baseline restored. Exact artifact hash was not independently inspected. User is uninstalled. Live logoff was not tested or requested to finish this round.

Earlier foundation, console and configuration evidence remains in [VALIDATION.md](VALIDATION.md). These records are scoped observations, not broad production readiness or an independent CI audit.

## New increment: source present, build/device results pending

- Bounded retained diagnostics in existing protected StatusV1; up to 128 entries/32 KiB serialized history with Run IDs and start/normal-stop markers.
- Current-run snapshots remain 24 entries. Retained history is best-effort diagnostics, not a transaction/tamper-evident audit ledger.
- Friendly no-settings state, inactive timeout and explicit absent revision comparison; clearer remove-settings warning.
- GUI retained history Copy/Export; historical versus current state clearly distinguished.
- Pure diagnostics tests plus x64 disabled service restart/history/uninstall smoke.
- No live authorization/defaults, timer logic, SID policy, installer ownership locations or networking changes.

## Next targeted validation

1. New CI, tests and x64 restart-persistence smoke must pass.
2. Clean native install; monitoring stays OFF, Allow LIVE unchecked.
3. Verify retained history survives service restart, new Run ID and inactive timeout.
4. Remove settings and verify friendly inactive/absent-revision presentation.
5. Copy/Export cross-run history, then uninstall and verify original baseline and owned runtime/history removal.

Do not repeat every already-passed ordinary SID dry-run scenario just to validate diagnostics. No live test requested this round. New behavior remains pending until results supplied.

## Remaining scope

- Diagnostics device validation; corruption/retention/registry failures and power-loss behavior.
- Live logoff safe-account validation, completion reconciliation and warning/countdown UX.
- Full token/session reuse, multiple users, unlock/config/ACL/concurrency adversarial tests.
- Transactional audit/outcome ledger; full interruption/retry/recovery/upgrade/reparse hardening.
- Idle-lock helper/authenticated IPC/supervision and suspend/reconnect semantics.
- Profile inventory/guarded deletion/native retention policy and baseline restoration.
- Owned restart/shutdown scheduling, DST/missed-run/collision/reversal tests.
- Ubuntu/Laravel central reporting/auth/scoped groups/roles/privacy/retention/API; later typed remote commands with replay/expiry/revocation/approval, never arbitrary shell execution.
- ARM64 device tests, licensing/dependency/signing/release gates.
- Brand/name checks, clean branded-repository handoff, then separately approve old working repository privacy.

## Continuity/security

Current [package guide](SERVICE-INTEGRATION.md), [server plan](SERVER-PLAN.md), [branding](BRANDING.md), [idle-lock](IDLE-LOCK.md). Older README/incremental guides are historical; this checkpoint/current package guide govern the bundle. Use decoded file contents/commit patches, not SHA metadata as content. Preserve date/path/owner/permissions/purpose/arguments/usage in records. No public organization/user/machine identifiers. Keep live permit OFF by default; reporting/maintenance unimplemented. Public source writes install nothing remotely.
