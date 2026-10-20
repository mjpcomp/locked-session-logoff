<!-- Updated: 2026-10-20 12:08 America/Los_Angeles | Path: /TASKS.md | Owner: maintainers | Permissions: public read/maintainer write | Purpose: observer evidence and read-only IPC foundation gate | Arguments: none | Usage: distinguish source/CI/native/device/operator evidence -->
# Current implementation tracker

## Scoped completed rounds

[Preview fix](VALIDATION-0138c6c.md): supplied exact PS5.1/package metadata; zero exit previews/install/uninstall, 475 files preserved/matching, recovery/config unchanged and final absence/baseline restoration. [Live/outcome](VALIDATION-f6af81c.md), [SID](VALIDATION-386df4b.md), [foundation](VALIDATION.md). Earlier diagnostics history/friendly state directly shown; export/earlier closeout operator-confirmed.

[Standalone observer](VALIDATION-2aeffea.md): native layouts pass, WouldLock without automatic lock, manual lock pause/unlock fresh ~20-second window and stop/prompt return directly shown; no-repeat/input-reset/no-automatic-lock details operator-confirmed. Ctrl+C also produced PowerShell pipeline-stopped noise. Use direct long-running native command per AGENT-POLICY.md rather than unnecessary Out-Host pipe. No repeat laptop round needed for that change. Both service installations remain removed; observer stopped.

## New source: IPC foundation, NOT active integration

[Contract/security/status](AGENT-POLICY.md): bounded strict read-only framing/challenge/receipt; native local pipe ACL/first-instance/remote rejection; actual client token/PID/WTS binding and installed-service PID/start verification; server-side policy/exclusion filtering and action-disabled replies. Pure in-memory tests and separate CI workflow. No auto-started endpoint or changes to service/agent CLI/installer/configuration. Native peer checks and new tests have NOT yet been executed/validated.

## Next engineering gate (no operator test yet)

Approve foundation, verify compile/protocol tests; then wire listener lifecycle/cancellation, independent default-off idle settings and administrator controls, agent service-policy refresh/fresh-window resets, and installed normal-user/negative security tests. Keep idle actions disabled. Only after that integrated package passes CI supply one flash-drive test sheet; avoid a laptop test for every internal substep. Authenticates user/logon, not helper binary or honesty of idle data. Never accept client WouldLock as privileged action authority.

## Remaining requirements

- Native IPC/spoof/network/cross-session/replay/flood/disposal/error and Windows x64/ARM64 integration evidence.
- Policy startup/off-on/error/gap gates, exclusions, authenticated supervised launch, native lock recheck and positive lock confirmation.
- User warning/countdown UX; full token/session reuse/multiple-user/unlock/disconnect/config/ACL/concurrency tests.
- Diagnostics corruption/retention/power loss; transactional audit/outcome/crash ledger.
- Interrupted install/removal, recovery/retry/upgrade/reparse/race hardening and helper ownership cleanup.
- Profile inventory/guarded deletion/native retention/baseline reversal; owned maintenance/DST/missed-run/collision/warnings/reversal.
- Ubuntu/Laravel reporting/auth/groups/roles/privacy/retention/API; typed commands with expiry/replay/revocation/approval, never arbitrary shell execution.
- ARM64 Surface Pro device tests later; signing/licensing/dependencies/release gates; branding/clean repo handoff and separately approved old-repo privacy.

## Continuity

README.md, IDLE-LOCK.md, SERVICE-INTEGRATION.md and SERVER-PLAN.md retain component/roadmap requirements; this checkpoint and AGENT-POLICY.md clarify newer evidence and IPC foundation boundaries. Preserve dates/path/owner/permissions/purpose/arguments/usage. Read actual source/patches, not SHA metadata as content. No public identifiers/raw operator logs or automatic deployment.
