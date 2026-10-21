<!-- Updated: 2026-10-21 11:16 America/Los_Angeles | Path: /TASKS.md | Owner: maintainers | Permissions: public read/maintainer write | Purpose: saved idle-control implementation and combined native test gate | Arguments: none | Usage: separate source/maintainer CI/native/operator/device evidence -->
# Current implementation tracker

## Prior scoped evidence

VALIDATION-0138c6c.md records exact PS5.1/package metadata, preview preservation/475 matching files and actual uninstall/baseline restoration. VALIDATION-f6af81c.md records controlled live request, same-action target absence and normal restoration; historical preview failure is not retroactively passed. VALIDATION-2aeffea.md records standalone read-only observer checks and Ctrl+C shell noise, with operator-only timing confirmations distinguished. Earlier SID/foundation records remain. Service installations removed; observer stopped.

Maintainer explicitly reported all builds/checks for 7933adfde71c2b205e705eff9fec42090825dc92 green on 2026-10-20. Treat as maintainer CI confirmation, not independently inspected logs or successful native normal-user authentication.

## New source, execution pending

- IdleObservationRequested default OFF and independent 300-second idle timeout in existing protected ConfigurationV1.
- Separate GUI idle intent/timeout, shared SID exclusions, complete save preview and explicit locked-state versus idle effective-state labeling.
- Report-only idle requires Dry-run ON / live permit OFF; no changes to valid old idle-OFF live-logoff behavior.
- Independent idle gate: startup saved ON waits OFF/ON; config errors and long/backward service observation reset activation; policy refresh every two seconds and on query.
- Effective idle policy events and existing authenticated client refresh/reset/no-stale fallback. No idle lock/sign-out RPC/actions, fabricated lock state, agent auto-start or supervision.
- New pure idle-policy validation/gate tests and existing suites in integrated CI. No successful native client access claimed.

## Immediate gate

Approve source and verify ALL new CI including dual-shell lifecycle. Then provide ONE two-console flash-drive sheet: elevated install/GUI and NORMAL-user agent, successful verified disabled query first, idle-only enable/threshold/reset/disable/shared exclusion/restart OFF-ON/connection loss, no automatic lock, export and uninstall/baseline checks. STOP on failed native handshake, return diagnostics rather than calling it a disabled pass. No repeat live logoff. No new laptop action until fresh CI passes.

## Remaining scope

- Native normal-user/negative/spoof/network/elevated/cross-session/process-logon reuse/flood/disposal/error/multiple-user evidence.
- Short suspend/power events/secure desktop/RDP semantics beyond conservative long-gap resetting.
- Authenticated supervised launch/ownership cleanup; real idle lock recheck and positive confirmation; warning/countdown UX.
- Full locked action/config/ACL/concurrency adversarial tests and diagnostics corruption/retention/power loss/transactional outcome/crash ledger.
- Interrupted install/removal, recovery/retry/upgrade/reparse/race hardening.
- Profile inventory/guarded deletion/native retention/baseline reversal; maintenance/DST/missed-run/collision/warnings/reversal.
- Ubuntu/Laravel reporting/auth/groups/roles/privacy/retention/API; typed commands with expiry/replay/revocation/approval, never arbitrary shell.
- ARM64 Surface Pro device validation later; signing/licensing/dependencies/release gates, branding/clean handoff and separately approved old-repo privacy.

## Continuity

AGENT-POLICY.md is current integrated idle guide; README/IDLE-LOCK/SERVICE-INTEGRATION/SERVER-PLAN retain other requirements where not superseded. Preserve dates/path/owner/permissions/purpose/arguments/usage. Read real source/patches, not SHA metadata as content. Omit private operator identifiers/raw logs. Public source writes install nothing on operator devices.
