<!-- Updated: 2026-10-15 12:13 America/Los_Angeles | Path: /SERVICE-INTEGRATION.md | Owner: maintainers | Permissions: public read/maintainer write | Purpose: SID-aware service and optional live-test safety/lifecycle guide | Arguments: scripts WhatIf/Confirm/RestoreOriginalConfiguration | Usage: read fully before installation or live enablement -->
# SID-aware service test bundle

## Current boundary

New source adds actual token SID/authentication-ID checks, working SID exclusions, separate rolling Recent Service Events with Copy/Export, and explicitly opt-in live logoff. New CI/device results are pending. No idle-lock helper, profile cleanup policy/deletion, scheduled restart/shutdown, central network reporting or remote commands exist yet. Product identifiers remain unchanged; branding is pending. This is a controlled test package, not a production release.

## Safety defaults and live meaning

Installation still writes monitoring OFF and dry-run ON. AllowLiveLogoff defaults false when absent, including earlier saved settings. No new enforcement is authorized by installing/upgrading code. Service startup with saved monitoring already ON requires OFF then ON. Power events/gaps/errors disarm conservatively. Old standalone consoles are not live-capable; installed service is the only live backend.

LIVE requires all of: monitoring requested, startup activation gate satisfied, Allow LIVE checked, Dry-run unchecked, and current eligibility checks passing. GUI confirmation explicitly warns of forced sign-out and lost unsaved work. No pre-logoff countdown dialog exists yet. Do NOT enable live while testing unsaved production work or another person's session without authorization. Begin with dry-run. An administrator controlling the configuration/service is inside the local trust boundary.

## Identity and candidate checks

Trusted SYSTEM service uses WTSQueryUserToken; token handles are disposed. TokenSessionId must match requested session, and identity uses account SID plus authentication LUID, not username or session ID alone. Failed identity queries block/reset that session rather than falling back to a guessed account. Account SID exclusions apply across that account's sessions. Console token queries may fail because they lack SYSTEM/TCB privilege; use the installed service, not privilege workarounds.

At timeout the service takes the restricted configuration-writer mutex, rereads current configuration, rereads token identity, queries current WTS state, and rejects changed identity/generation/configuration, unlocked/disconnected/unknown/error sessions and excluded SIDs. GUI writers coordinate with that mutex; privileged direct registry tools do not. SCM notifications reset affected timers and native state is rechecked. Windows provides no atomic lock-state-conditional logoff API: an unavoidable narrow race remains between final checks and native request. Do not claim a mathematical guarantee that a user cannot unlock in that interval.

Dry-run emits WouldLogOff only after candidate checks. Live uses asynchronous WTSLogoffSession. LogoffRequestAccepted means Windows accepted the request, NOT confirmed successful completion; inspect subsequent SessionLogoff/session disappearance. Failure records one attempt per observed lock episode; no automatic destructive retry loop. Token mismatch/exclusion/unknown states reset timing. Exclusion/identity block reasons are deduplicated per session. No raw token handles, authentication IDs, usernames or SIDs are added to the recent-event strings.

## Install and test in this order

The prior test installation should already be uninstalled. First-install only: no overwrite/upgrade support. Wait for Integrated service test bundle to pass tests, x64 smoke, and packaging. Extract integrated-test-win-x64 fully and inspect package.json/checksums. Unsigned artifacts and hashes are not code signing. Close old GUIs/consoles.

From elevated native PowerShell 7 at package root, one-process override if unsigned scripts are blocked:

```powershell
& "$PSHOME\pwsh.exe" -NoProfile -ExecutionPolicy Bypass -File .\Install.ps1 -WhatIf
& "$PSHOME\pwsh.exe" -NoProfile -ExecutionPolicy Bypass -File .\Install.ps1
& "$env:ProgramFiles\LockedSessionLogoff\admin\LockedSessionLogoff.Admin.exe"
```

Enforced MachinePolicy/UserPolicy is not overridden; use signing/authorized policy handling in that case, not a workaround to defeat it.

1. Verify fresh service report, Disabled/monitoring false, effective dry-run true, Allow LIVE unchecked.
2. Keep Allow LIVE unchecked and Dry-run checked, exclusions empty; save OFF, then ON with a 20-second timeout.
3. Lock for at least 25 seconds and unlock. Inspect the clearly labeled Recent Service Events panel for lock/WouldLogOff/unlock. Copy/Export operates on displayed local snapshots; events are rolling 24 entries, not persistent Event Viewer/audit logs. Stale retained text is explicitly labeled last snapshot.
4. Obtain the intended test user's SID using whoami /user in THAT user's session (not an alternate administrator account). Add SID to exclusions, Save, repeat lock test; expect ExcludedBySid and no WouldLogOff for that session. Remove exclusion and Save for a later nonexcluded test.
5. Verify disable, timeout reload, stale-report behavior and service restart gate.
6. ONLY after those pass, optional live test on an authorized disposable/test session with all work saved: set a comfortable timeout, check Allow LIVE, uncheck Dry-run, explicitly confirm, verify ConfiguredLiveLogoff/effective dry-run false, then lock. Expected outcome is actual sign-out. Test excluded SID remains protected separately.
7. After testing, disable monitoring and clear Allow LIVE. Do not leave an unvalidated live policy on a shared production station.

GUI SID entry is manual intentionally. Elevating with a different administrator identity can make that GUI account's whoami SID different from the employee to exclude. No guessed username-to-SID matching.

## Status and uninstall

Protected local runtime registry report still validates ACL, age, protocol, SCM PID and process start. It is not cryptographic attestation; no fresh verified report means unavailable/unknown. Observed revision alone does not imply active eligibility; inspect mode and block events. This package adds no service control API accessible to ordinary users or network listeners.

Installer/uninstaller remain baseline-preserving first-install scripts. Review preview; installation captures earlier ConfigurationV1 in protected ProgramData install.json. Later GUI edits intentionally cause a conservative restoration conflict. Close GUI, retain the baseline for comparison, then explicitly restore it when removing:

```powershell
& "$PSHOME\pwsh.exe" -NoProfile -ExecutionPolicy Bypass -File "$env:ProgramFiles\LockedSessionLogoff\Uninstall.ps1" -RestoreOriginalConfiguration -WhatIf
& "$PSHOME\pwsh.exe" -NoProfile -ExecutionPolicy Bypass -File "$env:ProgramFiles\LockedSessionLogoff\Uninstall.ps1" -RestoreOriginalConfiguration
```

Only owned files/service/status are removed; baseline config restored or originally absent value removed. Unrelated data preserved; conflicts/partial failures retain recovery state. Verify service/install/recovery/runtime absent and original configuration equal. Uninstall cannot recover lost unsaved work or reverse completed logoffs. User-chosen exported event files remain outside uninstall ownership. No persistent service log files exist to export automatically. Interruption/reparse/race/upgrade/concurrency/recovery hardening is still pending; do not blindly delete remnants or rerun failed installs.

## Validation status

Prior package 299e347 had maintainer-reported green builds; supplied workstation output confirmed installation, service lock/unlock events, would-logoff, enable/disable, previewed uninstall, removal of service/install/recovery/runtime and exact restoration of preexisting configuration. These do not validate the new token/exclusion/live paths. CI adds 17 synthetic eligibility/live-permission cases to existing 14 core and 15 configuration cases; synthetic tests call no real logoff. Native token layout is checked by x64 self-check. ARM64 remains cross-published until device-tested.

Next gates: new CI/smoke; real SYSTEM token read; dry-run exclusion/candidate checks; GUI Copy/Export; optional explicit live test; uninstall restoration; more adversarial state/configuration/permission/race tests. Full durable logs/ledger, warning UI, upgrades, idle-lock and server scope remain tracked in TASKS.md and SERVER-PLAN.md.
