<!-- Updated: 2026-10-15 12:13 America/Los_Angeles | Path: /TASKS.md | Owner: maintainers | Permissions: public read/maintainer write | Purpose: authoritative current checkpoint | Arguments: none | Usage: checked means specifically evidenced; read linked guides -->
# Current implementation tracker

## Latest increment

SID-aware service/live-opt-in source and new events GUI prepared. See [SERVICE-INTEGRATION.md](SERVICE-INTEGRATION.md) for actual capabilities, safety boundaries, tests and removal. New build/device results are pending. Public edits install nothing remotely. No branded repository or identifiers renamed.

## Prior evidence (old package, not new feature validation)

- [x] 14 core synthetic tests from supplied logs; later builds maintainer-reported green.
- [x] ARM64 foundation/build compilation reported, no device validation.
- [x] x64 probe and console lock/unlock/dry-run timing.
- [x] GUI save/reopen persistence.
- [x] Configured console debug reload, enable/20-second threshold, timeout reload, disable.
- [x] Installed 299e347 service received lock/unlock notifications and emitted WouldLogOff, with enable/disable reflected.
- [x] Previewed uninstall and owned service/files/recovery/runtime removal; original preexisting configuration restored exactly, supplied verification output.
- [ ] GUI report-verification checks independently exercised (prior service evidence was also read through PowerShell).

## New code present, validation pending

- Token SID/session/authentication LUID reader with safe handle disposal and layout checks.
- Actual SID exclusions and rechecked candidate eligibility.
- Explicit default-off live permit, immutable backend capability gate and native request behind checks.
- Separate rolling Recent Service Events plus local Copy/Export.
- 17 new synthetic eligibility/permission tests and updated integrated packaging/smoke.

## Immediate gate

Verify new CI first. Clean install native package (no overwrite upgrade), verify disabled state. Test SYSTEM token queries, dry-run, actual SID exclusion, configuration/state changes and GUI events. Only then optional live test in an authorized safe test session with work saved and explicit consent. Do not infer live success from native request acceptance or synthetic tests. Record each result separately.

## Remaining scope

- [ ] New CI, x64 native identity/exclusion/live device results and lifecycle restoration.
- [ ] Full session reuse/multiple users/unlock timing races/error/config/ACL/concurrency adversarial tests.
- [ ] Logoff outcome reconciliation, warning/countdown UX, stronger persistent diagnostics/audit and transactional ledger.
- [ ] Interrupted installation/removal, retry/recovery, upgrade support, reparse/race hardening.
- [ ] Idle-lock helper, authenticated IPC, supervision, suspend/reconnect semantics.
- [ ] Profile inventory/guarded deletion/native retention-policy controls and reversal.
- [ ] Owned restart/shutdown scheduling, DST/missed-run/collision/reversal tests.
- [ ] Ubuntu/Laravel central reporting, auth/scoped groups/roles, privacy, retention and API.
- [ ] Later remote typed commands, replay/expiry/revocation, scheduling and approvals.
- [ ] ARM64 device tests, license/dependency/signing review and release gates.
- [ ] Naming/brand checks and clean branded-repo handoff; then separately approve old working repo privacy.

## Continuity

[Validation](VALIDATION.md), [current installed guide](SERVICE-INTEGRATION.md), [server/admin security](SERVER-PLAN.md), [branding](BRANDING.md), [idle-lock](IDLE-LOCK.md). README and older incremental guides contain historical implementation descriptions; current guide and this checkpoint govern this bundle.

Use commit/PR diffs for missing decoded reads; never use SHA metadata as content. Preserve timestamps/path/owner/permissions/purpose/arguments/usage in records. Keep source/built/synthetic-tested/device-tested distinct. No organization/user/machine identifiers in public records. Reporting and maintenance stay disabled/unimplemented. Live logoff defaults OFF and requires explicit new configuration; no arbitrary command execution or network listener introduced.
