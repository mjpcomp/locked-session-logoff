<!-- Updated: 2026-10-19 13:32 America/Los_Angeles | Path: /TASKS.md | Owner: maintainers | Permissions: public read/maintainer write | Purpose: live-test checkpoint and preview regression gate | Arguments: none | Usage: distinguish direct/operator/source/CI/device evidence -->
# Current implementation tracker

## Completed scoped evidence

[SID-aware round](VALIDATION-386df4b.md), earlier [foundation/configuration](VALIDATION.md). Diagnostics 8c802fd: supplied verified reports established cross-restart history/run markers/disabled state/friendly missing settings/inactive timeout/both-absent display. Export and closeout operator-confirmed; final removal checks were not pasted for that earlier round.

[Live/outcome/PowerShell laptop checkpoint](VALIDATION-f6af81c.md): supplied dry-run, explicit live request, SessionLogoff, same-action target-absence observation and new sign-in; final disable; independent 475-file check with zero missing/mismatched; original normal Windows PowerShell uninstall and DIRECT final verification of service/files/recovery/runtime absence and baseline restoration. Exact shell build/artifact hashes/CI runs not independently inspected. No blanket production/security claim.

Old Windows PowerShell -WhatIf uninstall FAILED with false modified-file indication; never count it as passed. Preview-sensitive read path strongly implicated, exact helper behavior not independently reproduced on device. Both test laptop and working machine currently uninstalled. ARM64 device testing deferred until an available Surface Pro; cross-publication is not device execution.

## New fix: source present, CI/device pending

- Read-only .NET SHA256 helper works independently of WhatIf-sensitive PowerShell provider helpers.
- Unavailable digest / invalid manifest digest distinguished from real mismatch; genuine mismatch still blocks before changes.
- Only read-only CIM query/import locally suppresses inherited WhatIf; outer removal gate unchanged.
- Explicit Windows PowerShell 5.1 AND PowerShell 7 CI lifecycle regression: known hash, inherited WhatIf, unavailable hash, no-mutation previews, deliberate mismatch rejection/restoration, disabled restart/history and real uninstall/baseline restoration. Fixtures only on disposable CI runner.
- No C# service/live policy/default or network changes.

## Next gate

Approve commit, inspect CI including BOTH shell lifecycle steps. Then one short Windows PowerShell 5.1 laptop packaging check with monitoring/live OFF: clean install, successful uninstall preview leaving service/files/config intact, actual uninstall and baseline verification. No repeat live logoff or full SID timing round required. Remain uninstalled until fresh package passes CI.

## Remaining scope

- New preview fix CI/device results; broader failure/corruption/retention/permission tests.
- Countdown/warning UX before shared-workstation readiness; none exists now.
- Full session/token reuse, simultaneous users, unknown/disconnected/unlock/config/ACL/concurrency adversarial tests.
- Transactional audit/outcome ledger, crash/restart reconciliation; bounded diagnostics are not that ledger.
- Interrupted install/removal, retry/recovery/upgrade/reparse/race hardening.
- Idle-lock helper/authenticated IPC/supervision/suspend-reconnect semantics.
- Profile inventory/guarded deletion/native retention policy/baseline reversal.
- Owned restart/shutdown scheduling/DST/missed-run/collision/reversal tests.
- Ubuntu/Laravel reporting/auth/scoped groups/roles/privacy/retention/API; later typed remote commands with expiry/replay/revocation/approval, never arbitrary shell execution.
- ARM64 native tests, signing/dependency/licensing/release gates; naming/brand/clean repo handoff and separate approval before old repo privacy change.

## Continuity/security

[Current guide](SERVICE-INTEGRATION.md), [server](SERVER-PLAN.md), [branding](BRANDING.md), [idle-lock](IDLE-LOCK.md). Older incremental guides/README are historical where superseded. Read real source/patches, not SHA metadata as content. Preserve dates/path/owner/permissions/purpose/arguments/usage; no public account/machine/organization identifiers or raw operator logs. Public source changes install nothing on operator devices. Monitoring/live authorization off by default; no network listener or remote deployment introduced.
