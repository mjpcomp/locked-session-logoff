<!-- Updated: 2026-10-20 09:36 America/Los_Angeles | Path: /TASKS.md | Owner: maintainers | Permissions: public read/maintainer write; Purpose: completed preview gate and next idle-observer increment | Arguments: none | Usage: keep source/CI/device/operator evidence separate -->
# Current implementation tracker

## Closed targeted Windows PowerShell 5.1 preview gate

[0138c6c workstation evidence](VALIDATION-0138c6c.md): supplied transcript explicitly identifies PS5.1.26100.9711, native 64-bit and matching package commit. Install preview/install/uninstall preview/actual uninstall exit 0. Preview kept same running service, monitoring off, configuration/recovery unchanged, 475 files present/matching. Final service/install/recovery/runtime absent and baseline restoration true. No repeat live action requested/performed in this round. Exact binary hashes/CI runs not independently inspected. Both machines remain uninstalled.

Earlier [live/outcome round](VALIDATION-f6af81c.md), [SID round](VALIDATION-386df4b.md), [foundation](VALIDATION.md). Diagnostics 8c802fd cross-run history/friendly state directly shown; export and earlier closeout operator-confirmed. The OLD PS5 preview failure remains historical, not retroactively passed.

## Next functional increment: read-only session idle observer

Source prepared: pure conservative InputIdleTimer and tests, standalone normal-user local-console SessionAgent, separate Read-only idle observer workflow/artifacts. It only reports WouldLock. NO native lock/logoff requests, IPC, service/installer/config changes, startup tasks or supervision. CLI threshold is diagnostic, not machine policy/exclusion integration. Build/tests/device results pending.

Fresh windows after startup/input/unlock/identity/config/gaps; null/future/ambiguous input or unknown/non-console state pauses/reset. Native input tick rollover and backward values tested synthetically. One candidate per unchanged episode. No keyboard/mouse/window-content logging. This is phase one toward authenticated per-session idle-lock and warning helper architecture, NOT completed idle-lock enforcement.

## Immediate gate

Approve source, verify new observer CI/tests/x64 layouts. Then standalone idle-observer-win-x64 on laptop in NORMAL non-elevated console: 20-second untouched period -> WouldLock without locking, input reset, manual lock/unlock fresh window, Ctrl+C stop. No install/uninstall or live logoff required. Supply another flash-drive command sheet once artifact exists. ARM64 Surface Pro device test later; cross-publication is not native validation.

## Remaining scope

- New idle source/CI/device results; sleep/resume/native input uncertainties and polling limitations.
- Validated independent idle policy/UI/exclusions; authenticated identity-bound IPC and supervised launch, real lock request/recheck/confirmation, duplicate/replay/stale/cross-session defenses and ownership cleanup.
- Warning/countdown UX before shared-workstation readiness.
- Full token/session reuse, multiple users, unlock/disconnect/config/ACL/concurrency adversarial tests.
- Diagnostic corruption/retention/power-loss tests and transactional outcome/audit ledger/crash reconciliation.
- Interrupted installation/removal, retry/recovery/upgrade/reparse/race hardening.
- Profile inventory/guarded deletion/native retention policy/baseline reversal.
- Owned maintenance tasks, DST/missed-run/collision/warnings/reversal.
- Ubuntu/Laravel reporting/auth/groups/roles/privacy/retention/API; later typed remote commands with expiry/replay/revocation/approval, never arbitrary shell execution.
- ARM64 native tests, signing/licensing/dependency/release gates, branding/clean repo handoff and separately approved old-repo privacy.

## Continuity

[README](README.md), [idle architecture/current standalone guide](IDLE-LOCK.md), [integrated guide](SERVICE-INTEGRATION.md), [server](SERVER-PLAN.md), [branding](BRANDING.md). Integrated guide still documents the independently packaged service lifecycle; it is not the observer launch guide. Preserve date/path/owner/permissions/purpose/arguments/usage. Read actual source/patches, never SHA metadata as content. Public records omit operator identifiers/raw logs. No automatic deployment or silent authority/enforcement changes.
