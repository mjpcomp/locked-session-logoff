<!-- Created: 2026-10-14 17:36 America/Los_Angeles | Repository: mjpcomp/locked-session-logoff | Path: /TASKS.md | Owner: repository maintainers | Permissions: public read, authorized maintainer write | Purpose: authoritative implementation and validation tracker | Arguments: none | Usage: update after each work increment; do not mark unverified work complete -->
# Tasks and implementation record

## Checkpoint: documentation bootstrap

As of 2026-10-14 17:36 America/Los_Angeles: public repository created; this initial commit establishes planning documents only. No executable, source implementation, installer, workflow, or successful build exists. Nothing has been deployed. No Windows behavior has been tested.

Next step: review scope, select the supported Windows/.NET target and architecture, then prepare the first source implementation and build workflow for review. Do not claim an artifact until a successful build and artifact existence are verified.

## Agreed decisions

- Public, organization-neutral repository; no customer/tenant/machine data.
- Installation alone enables no enforcement, cleanup policy, or restart/shutdown schedule.
- Elevated admin utility controls all optional behavior.
- Same build supports enforcement, runtime dry-run, and independent debug verbosity.
- Session timeout is continuously locked time, not generic inactivity.
- Profile retention uses the native restart-based policy; interactive profile deletion is separate.
- Uninstall previews and reverses owned configuration changes, preserving later third-party changes unless explicitly overridden.
- Destructive actions already completed cannot be undone by uninstall.

## Milestone 1: implementation foundations

- [ ] Verify supported Windows versions/editions and select maintained .NET target.
- [ ] Establish solution, service, elevated admin UI, shared library, and tests.
- [ ] Define service identity, installation paths, ACLs, secure configuration, and privileged interface authorization.
- [ ] Define config schema, validation, runtime reload, argument precedence, and monitoring-disabled installation default.
- [ ] Design transactional original-state ledger, ownership markers, versioning, and recovery.
- [ ] Select license with maintainer approval; review dependencies.

## Milestone 2: locked-session engine

- [ ] Implement session enumeration, actual lock state, and lock/unlock notifications.
- [ ] Independent monotonic timers; unlock/reset, relock, and immediate pre-action recheck.
- [ ] Handle unknown/query errors safely; no guessed historical lock time.
- [ ] Handle logoff/relogon, session-ID reuse, disconnect/reconnect, and service recovery.
- [ ] Define/test sleep/hibernate elapsed-time semantics and timer races.
- [ ] Explicit exclusions and unsaved-work warnings.
- [ ] Dry-run suppresses logoff; debug provides bounded local diagnostics.
- [ ] Tests for threshold boundaries, cancellation, failures, and multiple sessions.

## Milestone 3: profile administration

- [ ] Verify native policy registry details, ranges, OS support, and restart semantics.
- [ ] Read/display policy; preview/apply/remove with baseline preservation.
- [ ] Explain registry versus Local Group Policy database and exclusion limitations.
- [ ] Generate exact copyable commands and GUI instructions.
- [ ] Inventory account/SID/path/last-use/age/loaded/special/protection status.
- [ ] Unknown timestamps excluded from age selection; optional safe size scan.
- [ ] Confirm selected deletions; re-query; use profile provider, not raw folder removal.
- [ ] Block loaded/special/operator/excluded profiles; log individual results.

## Milestone 4: maintenance scheduling

- [ ] Daily restart/shutdown task with local-time scheduling and explicit countdown/data-loss warning.
- [ ] Wake option, SYSTEM principal, missed-run skip default, and status display.
- [ ] Protect unrelated tasks; implement collision handling and original XML backup only if replacement is supported.
- [ ] Preview/copy commands and document Task Scheduler navigation.
- [ ] Verify daylight-saving, sleep, powered-off/missed-run, and active-session behavior.

## Milestone 5: install, rollback, and uninstall

- [ ] Install scripts/UI integration; install does not activate enforcement or maintenance.
- [ ] Preview removal/restoration and identify irreversible prior actions.
- [ ] Restore absent/existing registry baselines without deleting unrelated values.
- [ ] Detect third-party edits and request conflict decisions.
- [ ] Remove owned tasks/service/files; offer log export and retention choice.
- [ ] Retain recovery state on partial failure; non-success exit codes.
- [ ] Test repeated configuration, upgrade, interrupted apply, uninstall retry, and unattended policy.

## Milestone 6: build and release

- [ ] GitHub Actions Windows restore/build/test/publish/package workflow.
- [ ] Self-contained initial Windows architecture build and checksums.
- [ ] Verify artifact existence; label signing status accurately.
- [ ] Add exact installation/configuration/CLI/uninstall instructions only after implemented.
- [ ] Audit repository/artifacts for private information, credentials, and unintended logs.
- [ ] Release notes distinguish compilation, unit tests, and workstation validation.

## Required workstation acceptance tests

- [ ] Fresh install changes no enforcement/profile/task policy.
- [ ] Lock past threshold: dry-run records intended action; enforcement logs off only correct session.
- [ ] Unlock before threshold cancels; relock gets a fresh timer.
- [ ] Multiple sessions and account exclusions behave independently.
- [ ] Monitoring starts during lock; no invented historical duration.
- [ ] Sleep/hibernate/disconnect/reconnect and query failures match documented rules.
- [ ] Loaded/special/current operator profiles cannot be deleted.
- [ ] Unknown dates remain unknown and are not age-selected.
- [ ] Existing policy absent/present, repeated edits, and external edits restore safely.
- [ ] Task collisions, countdown, missed runs, and local-time changes tested.
- [ ] Dry-run boundary for pre-existing Windows policy/tasks is explicit.
- [ ] Uninstall preview matches actual actions; failures preserve recovery state.
- [ ] Standard users cannot alter privileged binaries/config/ledger or invoke unauthorized operations.

## Working protocol and file-read workaround

Read these documents at the start of each increment. When connector file reads return only blob metadata, use commit or pull-request diffs. Reconstruct full text only from a known baseline; otherwise obtain the complete file. Do not repeat metadata-only reads indefinitely or treat a SHA as content. Do not publish private support correspondence.

For every subsequent increment record: timestamp, files changed, purpose, arguments/usage changes, permissions impact, implemented status, build/test evidence, blockers, and next step. Check boxes represent verified completion, not intended work.
