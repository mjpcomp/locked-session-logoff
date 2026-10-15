<!-- Updated: 2026-10-15 13:13 America/Los_Angeles | Path: /SERVICE-INTEGRATION.md | Owner: maintainers | Permissions: public read/maintainer write | Purpose: retained diagnostics bundle safety/testing/lifecycle | Arguments: WhatIf/RestoreOriginalConfiguration | Usage: read before installation; first-install only -->
# Retained diagnostics test bundle

## Scope and defaults

Controlled test package, not production release. Existing SID/logon identity, exclusions and explicit live permit are unchanged. New source clarifies missing settings and retains bounded diagnostics across service process restarts. New CI/device validation is pending; earlier workstation results do not validate this new implementation.

Installation writes monitoring OFF and dry-run ON. AllowLiveLogoff defaults OFF when absent. Keep Allow LIVE unchecked during this round. No idle-lock helper, profile cleanup, maintenance schedule, central reporting, remote commands or network listeners exist. Product names/identifiers are unchanged. No overwrite upgrade support: uninstall earlier package first.

## Retained diagnostics

StatusV1 in the existing protected 64-bit HKLM application runtime key now includes RetainedEvents. The SYSTEM writer merges previous history with new current-run events, tags entries by Run ID, and records run start/normal stop. Retention is at most 128 entries AND 32 KiB of serialized UTF-8 history, evicting oldest entries. Current-run RecentEvents remains capped at 24 for snapshot compatibility. Whole serialized report is capped at 64 KiB. Repeated polling does not intentionally append duplicate snapshots. Only changed history/stop triggers an explicit registry flush.

No new disk log file or registry location is added. Retention survives ordinary service stop/start through the existing registry value, not by restoring timers or activation state. No transaction/crash-proof/tamper-evident audit guarantee is made. If earlier report/history is malformed or unsupported, history is restarted with a visible DiagnosticsNote; the warning persists in later reports. Oldest entries also disappear at retention limits. Privileged administrators remain inside the trust boundary. Abrupt crashes can lack a stop marker or lose not-yet-published events. Diagnostic persistence is not a pre-action audit ledger or proof of completed logoff.

GUI shows retained historical runs separately labeled within the event text; a fresh verified current report is still required. Historical entries are NOT fresh state. When the live report becomes unavailable, retained displayed text is explicitly only the last snapshot. Copy/Export saves that displayed report/history. Restarting GUI/service does not delete retained history; uninstall removes the entire owned StatusV1 value, including history. User-selected export files remain outside uninstall ownership.

Missing saved settings now reads 'No saved settings - monitoring disabled'. Live timeout displays Inactive whenever observation is disabled. Both absent revisions are labeled explicitly instead of a misleading True. Removing settings is not uninstall and does not restore the captured pre-install baseline. With no saved configuration, GUI defaults the editable dry-run checkbox on; no enforcement setting is silently enabled.

## New targeted test round

Wait for Integrated service test bundle CI and x64 disabled restart/history/uninstall smoke to pass. Fully extract integrated-test-win-x64 and inspect package.json/checksums. Unsigned checksums are not code signing. Close old GUIs/consoles. From elevated native PowerShell 7 at package root:

```powershell
& "$PSHOME\pwsh.exe" -NoProfile -ExecutionPolicy Bypass -File .\Install.ps1 -WhatIf
& "$PSHOME\pwsh.exe" -NoProfile -ExecutionPolicy Bypass -File .\Install.ps1
& "$env:ProgramFiles\LockedSessionLogoff\admin\LockedSessionLogoff.Admin.exe"
```

Process-only execution policy override does not bypass enforced MachinePolicy/UserPolicy. Follow authorized signing/policy handling if blocked.

1. Verify disabled state, Allow LIVE unchecked, effective dry-run true and timeout Inactive.
2. Copy/Export a report. Restart the service while monitoring remains OFF. Verify a new Run ID and prior-run retained events plus run markers; no monitoring activation.
3. Remove saved settings; expect friendly inactive message, no observed revision, 'Both absent' explanation and no active timeout. No need to recreate configuration to uninstall.
4. Copy/Export again to verify retained cross-run context and current state. No live logoff test required.
5. Close GUI, capture this installation's baseline, preview/remove, and compare restored original configuration.

```powershell
$baseline = Get-Content "$env:ProgramData\LockedSessionLogoff\install.json" -Raw | ConvertFrom-Json
& "$PSHOME\pwsh.exe" -NoProfile -ExecutionPolicy Bypass -File "$env:ProgramFiles\LockedSessionLogoff\Uninstall.ps1" -RestoreOriginalConfiguration -WhatIf
& "$PSHOME\pwsh.exe" -NoProfile -ExecutionPolicy Bypass -File "$env:ProgramFiles\LockedSessionLogoff\Uninstall.ps1" -RestoreOriginalConfiguration
```

RestoreOriginalConfiguration acknowledges discarding test-time edits and restores the captured original ConfigurationV1 (or removes it if originally absent). Uninstaller stops/deletes the owned service, removes owned files/status/history and removes recovery state only on success. Verify service/install/recovery/runtime absent and original raw configuration restored, using fresh baseline from THIS installation. Unexpected data/conflicts/partial failures require preserving the recovery record, not blind deletion/reinstallation. Interruption/reparse/race/upgrade hardening remains pending. Uninstall cannot reverse completed logoffs or recover unsaved work.

## Unchanged live-action boundary

Only installed SYSTEM service can execute live logoff, and only after explicit monitoring activation, Allow LIVE checked, Dry-run unchecked, validated token SID/session/authentication LUID, exclusions and final configuration/native lock-state checks. GUI writers coordinate through a restricted mutex; privileged direct registry tools do not. Failed/unknown/disconnected identities/states are blocked. Service startup with monitoring already ON still requires OFF then ON. Power/gap/error disarming remains conservative. An unavoidable check-to-native-call race remains; do not claim atomic locked-only enforcement. WTSLogoffSession acceptance is not confirmed completion. No warning countdown or outcome reconciliation exists yet. Live logoff remains unvalidated on the workstation and is NOT requested for this diagnostics round.

## Evidence and remaining work

See VALIDATION-386df4b.md for supplied workstation checks of the prior SID-aware package, including Copy/Export, exclusion removal, restart activation, removal of settings and uninstall restoration. Exact binary hashes/package.json commit were not independently inspected; do not overstate artifact provenance. Old memory-only log reset was expected and motivated this increment. New tests cover history bounds/dedup/restart merge and presentation; CI additionally checks actual service restart retention while disabled. Native ARM64 execution remains untested. Full durable audit/outcome ledger, warning UI, adversarial token/state/ACL/config races, recovery/upgrade, idle-lock/server scope remain in TASKS.md and SERVER-PLAN.md.
