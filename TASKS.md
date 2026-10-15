<!-- Created: 2026-10-14 17:36; Updated: 2026-10-15 09:39 America/Los_Angeles | Path: /TASKS.md | Repository: mjpcomp/locked-session-logoff | Owner: maintainers | Permissions: public read/maintainer write | Purpose: durable checkpoint and verification tracker | Arguments: none | Usage: read with README.md and IDLE-LOCK.md; update after every increment -->
# Implementation tracker

## Latest checkpoint: resume Windows integration

2026-10-15 09:39 America/Los_Angeles. Repository main inspected: a743d2d1cd29acc71b5efbcea80cdd00f9ddc069; no intervening commits. Read planning files through commit diffs. [README.md](README.md) defines overall scope; [IDLE-LOCK.md](IDLE-LOCK.md) defines the optional per-session idle-lock stage.

Evidence for a743d2d, supplied by the maintainer on 2026-10-14: x64 build succeeded with zero compiler warnings/errors; all 14 synthetic tests passed. ARM64 compilation also succeeded according to maintainer report; no ARM64 log inspected. No independent Actions-status verification is claimed. No actual device/session integration has been tested.

No service, admin UI, session agent, installer, native adapter, or installable release exists. Nothing has been deployed. Next implementation increment: native session adapter, Windows service, elevated admin shell, and per-session agent, with enforcement disabled by default. Do not mark them complete merely because a skeleton compiles.

## Decisions

- Public, organization-neutral, no telemetry/private environment data.
- Windows 11 x64 primary; native ARM64 build with device testing separate.
- .NET 10 LTS; no initial AOT/trimming. License undecided.
- Installation enables no locked-logoff, idle-lock, profile policy, or maintenance task.
- Elevated admin controls optional behavior; same build supports dry-run/debug.
- Locked duration is not inactivity. Optional idle stage locks first; confirmed lock starts the separate logoff timer.
- Native retention policy; guarded interactive profile deletion is separate.
- Restore owned configuration on uninstall; irreversible actions cannot be undone.

## Foundation evidence

- [x] Core options with disabled default and timeout validation.
- [x] Timer source: identity/reset/exclusions/unknown states/dry-run candidates.
- [x] 14 synthetic regression cases; maintainer-provided execution log passes all.
- [x] x64 compilation; maintainer-provided log passes with zero compiler warnings/errors.
- [x] ARM64 compilation, maintainer-reported success only.
- [ ] ARM64 device execution.
- [ ] Native Windows behavior validated.

## Initial build issue and prevention

The first build failed before C# compilation because the self-test csproj XML header included a double-hyphen CLI switch. Commit a743d2d corrected only the header. Corrected XML parsing and subsequent maintainer CI logs succeeded. Validate all project XML and keep command switches outside XML comments.

Runner logs also showed Node deprecation warnings for checkout/setup-dotnet v4; these were not compiler warnings and did not fail the jobs. Review maintained action versions and pin appropriately during workflow work; no upgrade is applied by this documentation commit.

## Foundation and security

- [ ] Service/admin/agent/shared solution and target compatibility validation.
- [ ] Secure installation/config/log/ledger ACLs and authorized privileged requests.
- [ ] Persistence, reload, CLI precedence, bounded diagnostics, safe invalid-config behavior.
- [ ] Transactional original-state ledger/recovery/versioning/upgrade behavior.
- [ ] License approval and dependency review.

## Session engine and agent

- [ ] Native enumeration, actual lock state/events, stable logon identity, final pre-action recheck.
- [ ] Service lifecycle, serialized event/poll handling, real logoff/retry/dry-run logging.
- [ ] Independent idle-lock setting, correct-session input detection, non-elevated agent.
- [ ] Protected agent lifecycle/launch and authenticated session-bound IPC; no service-session idle assumptions.
- [ ] Handle tick rollover/non-incremental input timestamps, helper failure and stale reports.
- [ ] Dry-run never fabricates a lock or hypothetical logoff timer.
- [ ] Session reuse/recovery/disconnect/reconnect/unknown states/suspend behavior.
- [ ] UI enablement, two timeouts, exclusions, dry-run/debug, data-loss warnings.
- [ ] Complete the detailed [idle-lock checklist](IDLE-LOCK.md#implementation-and-acceptance-checklist).

## Profiles

- [ ] Verify retention policy registry value/type/range/applicability.
- [ ] Display/preview/apply/remove with baseline preservation and command/GUI alternatives.
- [ ] Explain registry vs Local Group Policy and native exclusion limitations.
- [ ] Inventory unknown-safe fields; optional safe size scan.
- [ ] Block loaded/special/operator/excluded profiles; confirm/re-query/provider-delete/log results.

## Scheduling

- [ ] Owned daily SYSTEM restart/shutdown task and run-status display.
- [ ] Countdown/data-loss warnings, wake option, missed-run skip.
- [ ] Collision protection and authorized replacement XML backup if supported.
- [ ] Exact command/GUI alternatives; DST/clock/sleep/missed-run tests.

## Installation, reversal, and release

- [ ] Install without activation; native architecture detection.
- [ ] Uninstall preview/baseline restoration/third-party conflict decisions.
- [ ] Remove owned service/helper launch mechanisms/tasks/files; log export/retention.
- [ ] Partial-failure records/non-success exits/retry/unattended policy.
- [ ] Repeated edits/upgrades/interrupted changes/uninstall tests.
- [ ] Self-contained packages/checksums/accurate signing labels and complete instructions.
- [ ] Privacy/dependency audit and license decision before release.

## Workstation gates: none verified

- [ ] Fresh install changes no enforcement/profile/task policy.
- [ ] Real lock timeout/unlock/relock/multiple sessions/exclusions.
- [ ] Real input cancellation/idle lock/confirmed lock handoff/dry-run behavior.
- [ ] Start during lock/sleep/hibernate/disconnect/reconnect/errors match documented rules.
- [ ] Protected profiles and unknown ages remain safe.
- [ ] Policy absent/present/repeated/external edits restore correctly.
- [ ] Task collision/countdown/missed-run/local-time behavior.
- [ ] Dry-run external policy/task boundary clear.
- [ ] Uninstall preview matches actions; failed recovery retains records.
- [ ] Standard users cannot alter privileged code/config/ledger or invoke unauthorized operations.

## Change record and continuity

2026-10-15 09:39: README.md and TASKS.md updated with supplied CI evidence, idle-lock links, and next increment. No application changes or workstation effects. Owner: maintainers; public read/maintainer write. No new command arguments. IDLE-LOCK.md's older build-status paragraph is historical and superseded by the evidence here; its feature remains unimplemented.

Use commit/PR full diffs when decoded file reads fail. Reconstruct only from a known baseline or obtain full text. Never treat SHA metadata as content or publish private correspondence. Keep source/built/synthetic-tested/device-tested states distinct after every increment.
