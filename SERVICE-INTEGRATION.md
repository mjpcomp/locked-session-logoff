<!-- Updated: 2026-10-15 15:48 America/Los_Angeles | Path: /SERVICE-INTEGRATION.md | Owner: maintainers | Permissions: public read/maintainer write | Purpose: action/outcome test package safety and lifecycle | Arguments: WhatIf/RestoreOriginalConfiguration | Usage: controlled test laptop only for live sign-out; first-install only -->
# Identity-bound logoff outcome test bundle

## Scope and status

New source adds an injected action coordinator, fake-backend tests and bounded observation after an accepted native request. Build/CI/device results for this increment are pending. Source is not a production release or proof of successful live sign-out. Windows 11 x64 spare laptop is the immediate operator-selected target; working workstation remains uninstalled. Native ARM64 testing is deferred to an available Surface Pro later; cross-publication is not device validation.

Monitoring starts OFF, dry-run ON, and live permit OFF. No remote deployment/live action is performed by committing source. Existing SID exclusions, final native identity/state checks, startup OFF/ON activation and conservative power/gap/error disarming remain. No idle-lock helper, countdown dialog, profile cleanup, maintenance tasks, network reporting or remote commands exist.

## Action and outcome semantics

WouldLogOff never invokes the injected backend. Live requires effective enabled mode, current requested monitoring, unchanged raw configuration and matching observed hash/timeout, eligible current locked state, matching SID/authentication-LUID/session/WTS generation, live-capable service, saved AllowLiveLogoff true and Dry-run false. Restricted writer mutex still coordinates GUI edits with final reads/native request; privileged direct registry edits do not participate. There is no atomic lock-state-conditional Windows logoff API, so a narrow final-check/native-call race remains.

Live attempts receive random action IDs. LogoffRequestAccepted means backend returned without error, NOT completed sign-out. LogoffRequestFailed records exception type or Win32 code without sensitive exception message. No automatic destructive retry: one live attempt per token/logon identity per service run, even after failure or unlock/relock. Maximum 128 attempted identities per run; reaching capacity blocks further attempts. Restart plus explicit OFF/ON can permit a deliberate new test; do not use it as an unattended retry mechanism.

For an accepted request, the service observes pending targets even when monitoring is subsequently disabled. Successful enumeration without the session produces LogoffTargetAbsentObserved. A different actual token identity at the same session number produces LogoffTargetIdentityReplacedObserved. These describe observations after the request, not causal proof that the request alone ended the session. Disconnection, unknown state/token/read error, an empty user identity or an unchanged target are NOT successful completion. At a monotonic 30-second observation deadline the result is LogoffOutcomeUnconfirmed. Late observations do not retroactively confirm within the deadline. A remaining empty login-shell session may therefore produce Unconfirmed despite a user-observed sign-out; record that honestly.

Stop, power events or observation gaps abandon pending outcomes as Unconfirmed with reason. Pending operations are not resumed across service restart and never trigger repeat native requests. Abrupt crash may leave acceptance without a terminal outcome; this is not a transactional audit ledger. SCM logoff notifications are diagnostic only because session number alone does not identify the original logon.

## Diagnostics retained from previous increment

Existing protected runtime StatusV1 retains at most 128 events / 32 KiB serialized UTF-8 history, with Run IDs and start/normal-stop markers. Current-run queue is 24 events; whole report bounded to 64 KiB. Older events are evicted. Malformed previous history resets with visible warning. No new disk log location; no cryptographic/crash-proof audit promise. Live report still verifies ACL, freshness, SCM PID/process start; historic lines are not current state. Copy/Export saves displayed snapshot; user-chosen exports survive uninstall. Missing settings has friendly inactive wording, inactive timeout and explicit both-absent revisions.

## Controlled laptop procedure after CI passes

Wait for all tests, x64 disabled lifecycle smoke and packaging. Fully extract fresh integrated-test-win-x64; inspect package.json/checksums (unsigned hashes are not signing). Close old GUIs. Do not overwrite earlier installations.

From elevated native PowerShell 7 at package root:

```powershell
& "$PSHOME\pwsh.exe" -NoProfile -ExecutionPolicy Bypass -File .\Install.ps1 -WhatIf
& "$PSHOME\pwsh.exe" -NoProfile -ExecutionPolicy Bypass -File .\Install.ps1
& "$env:ProgramFiles\LockedSessionLogoff\admin\LockedSessionLogoff.Admin.exe"
```

Process-only ExecutionPolicy Bypass does not override enforced MachinePolicy/UserPolicy; use authorized policy/signing handling if blocked.

1. Keep Allow LIVE unchecked. Verify disabled/effective dry-run true. Set 20-second timeout, no exclusion for the intended test account, Dry-run checked; save OFF then ON. Lock about 30 seconds, unlock and inspect WouldLogOff. Disable afterward.
2. ONLY on the spare laptop, with all work saved and no other users' sessions at risk, explicitly choose a live test. Account used must be authorized to sign out. No user-facing countdown exists.
3. Set a comfortable 60-second timeout, check Allow LIVE, uncheck Dry-run, enable monitoring and review GUI's lost-unsaved-work confirmation. Verify ConfiguredLiveLogoff/effective dry-run false before locking. Do not turn this on on the working workstation.
4. Lock and allow the timeout. If actually signed out, sign back in and immediately disable monitoring, clear Allow LIVE and save. Export retained events. Correlate acceptance and terminal observation using action ID. If Unconfirmed/failed/no actual sign-out, do not keep retrying; export the evidence and disable live.
5. Close GUI, capture this installation's baseline, preview/remove, verify service/install/recovery/runtime absent and original configuration restored. No need to recreate settings if removed.

```powershell
$baseline = Get-Content "$env:ProgramData\LockedSessionLogoff\install.json" -Raw | ConvertFrom-Json
& "$PSHOME\pwsh.exe" -NoProfile -ExecutionPolicy Bypass -File "$env:ProgramFiles\LockedSessionLogoff\Uninstall.ps1" -RestoreOriginalConfiguration -WhatIf
& "$PSHOME\pwsh.exe" -NoProfile -ExecutionPolicy Bypass -File "$env:ProgramFiles\LockedSessionLogoff\Uninstall.ps1" -RestoreOriginalConfiguration
```

Uninstaller handles service stop/delete; explicit restoration discards test edits and restores captured original ConfigurationV1 (or removes originally absent value). It removes owned status including retained history and only removes recovery state on success; preserve recovery record on any conflict/failure, never blindly delete remnants. User exports remain untouched. First-install only: interruption/reparse/concurrency/race/recovery/upgrade hardening is still pending. Uninstall cannot reverse completed logoff or recover unsaved work.

## Validation boundaries

Prior SID-aware round: VALIDATION-386df4b.md. Prior diagnostics source 8c802fd: supplied GUI reports showed retained previous/current run and stop/start markers, monitoring stayed off, friendly missing-settings/Inactive/both-absent displays. Operator explicitly confirmed exported file and closeout completion; final uninstall checks were NOT pasted for that round. Exact artifact hashes were not independently inspected. These prior results do not establish new action/outcome code or live success. Remaining countdown/warning UX, hardening and central/idle/profile/maintenance scope are in TASKS.md and SERVER-PLAN.md.
