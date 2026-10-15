<!-- Created: 2026-10-14 17:36; Updated: 2026-10-14 17:44 America/Los_Angeles | Path: /TASKS.md | Repository: mjpcomp/locked-session-logoff | Owner: maintainers | Permissions: public read/maintainer write | Purpose: durable checkpoint and verification tracker | Arguments: none | Usage: update after every increment; checked means verified -->
# Implementation tracker

## Latest checkpoint: first source foundation

2026-10-14 17:44 America/Los_Angeles. This commit adds core timer source, 14 deterministic self-tests, and foundation CI. Compilation/tests have NOT been verified at preparation time. Check Actions after commit. No service/admin/installer/native adapter exists; nothing is deployable and no workstation has changed.

Next: verify CI, fix failures before expanding; implement native session identity/state/events and a service plus elevated administrator shell. Do not present synthetic self-tests as a deployable utility.

## Decisions

- Public and organization-neutral; no telemetry/private environment details.
- Windows 11 x64 primary; Windows 11 ARM64 native build, device validation tracked separately.
- .NET 10 LTS; no initial AOT/trimming. License remains undecided.
- Installation enables no enforcement, profile policy, or maintenance task.
- Elevated admin utility controls behavior. Same build supports dry-run and debug.
- Continuous locked duration, not inactivity. Normal timeout proposed at 60 minutes.
- Native profile-retention policy; interactive profile deletion is separate.
- Reversible owned configuration; completed destructive actions cannot be undone.

## Implemented source, awaiting build evidence

- [x] Core options with disabled default and timeout validation.
- [x] Serialized timer decisions with per-session identity, relock/reset, unknown-state/exclusion handling, and dry-run candidates.
- [x] Synthetic regression runner source: 14 cases.
- [x] Workflow definition: Windows x64 execution and win-x64/win-arm64 compilation.
- [ ] Successful CI compilation and all self-tests verified.
- [ ] ARM64 device execution verified.

## Foundation and security

- [ ] Service/admin/shared solution integration; framework/OS compatibility validation.
- [ ] Protected installation/config/log/ledger ACLs and privileged-request authorization.
- [ ] Configuration persistence, runtime reload, CLI precedence, bounded logging.
- [ ] Original-state transactional ledger, recovery, versioning, upgrade behavior.
- [ ] License approval and dependency review.

## Session engine

- [ ] Native enumeration, lock/unlock notifications, verified identity and final pre-logoff recheck.
- [ ] Actual logoff, failure/retry handling and dry-run logging.
- [ ] Notifications serialized with polling; unlock/relock between polls handled.
- [ ] Session-ID reuse, service recovery, disconnect/reconnect, unknown state.
- [ ] Define/test sleep/hibernate elapsed semantics, timing races, and suspend resets.
- [ ] Admin UI enablement, exclusions, timeout, dry-run/debug, and unsaved-work warning.

## Profiles

- [ ] Verify native retention policy registry value/type/range and applicability.
- [ ] Display/preview/apply/remove policy with original-state preservation.
- [ ] Explain direct registry vs Local Group Policy and native exclusion limitations.
- [ ] Exact commands and GUI navigation.
- [ ] Profile inventory with unknown values preserved; optional safe size scan.
- [ ] Block loaded/special/operator/excluded profiles; confirm selections/re-query/provider deletion/log results.

## Scheduling

- [ ] Owned daily restart/shutdown SYSTEM task and next/last/result display.
- [ ] Countdown/force-close warnings, wake option, skip missed runs.
- [ ] Collision protection, authorized replacement XML backup if supported.
- [ ] Equivalent commands/GUI instructions; DST/clock/sleep/missed-run tests.

## Installation, uninstall, release

- [ ] Install without activation; x64/ARM64 package detection.
- [ ] Uninstall preview, baseline restoration, third-party conflict decisions.
- [ ] Remove owned service/tasks/files; offer log export/retention.
- [ ] Failure recovery records, non-success exits, retry and unattended policy.
- [ ] Test repeated edits, upgrades, interrupted changes/uninstall.
- [ ] Publish self-contained application packages/checksums; accurate signing labels.
- [ ] Privacy/dependency audit; full usage instructions and release notes.

## Workstation acceptance gates (none verified)

- [ ] Fresh install modifies no enforcement/profile/task policy.
- [ ] Actual locked timeout, unlock cancellation, relock fresh timer, multiple sessions and exclusions.
- [ ] Start during lock, sleep/hibernate/disconnect/reconnect/errors follow documented rules.
- [ ] Loaded/special/operator profiles protected; unknown age not auto-selected.
- [ ] Policy absent/present/repeated edits/external edits restore safely.
- [ ] Task collisions/countdown/missed runs/local-time changes behave as documented.
- [ ] Dry-run boundary for external Windows policies/tasks is clear.
- [ ] Uninstall preview matches actions; failures retain recovery state.
- [ ] Standard users cannot modify privileged code/config/ledger or invoke unauthorized actions.

## File/change record and read workaround

New: src/LockedSessionLogoff.Core/{LockedSessionLogoff.Core.csproj,LockTimer.cs}; tests/LockedSessionLogoff.SelfTests/{LockedSessionLogoff.SelfTests.csproj,Program.cs}; .github/workflows/foundation.yml. Updated: README.md, TASKS.md. Ownership: repository maintainers; public read/maintainer write. Purpose/arguments/usage documented in each header. No installer paths/permissions have been applied.

Use commit/PR full diffs when connector file reads omit decoded text. Reconstruct only from a known baseline or request full text. Never treat SHA metadata as content or publish private support correspondence. Record implemented/built/tested separately after each increment.
