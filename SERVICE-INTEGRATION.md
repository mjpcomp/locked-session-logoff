<!-- Updated: 2026-10-19 13:32 America/Los_Angeles | Path: /SERVICE-INTEGRATION.md | Owner: maintainers | Permissions: public read/maintainer write | Purpose: dual-shell preview-fix test bundle guide | Arguments: WhatIf/RestoreOriginalConfiguration | Usage: first-install only, no live repeat required -->
# PowerShell preview compatibility test bundle

## What changed / what did not

New uninstaller computes SHA256 using a read-only .NET file stream rather than preview-sensitive Get-FileHash helper behavior. It distinguishes unavailable hashing/invalid manifest digests from genuine file mismatches. Only the read-only CIM query/import runs with locally suppressed inherited WhatIf, avoiding alias-import preview interference. The outer ShouldProcess gate is unchanged and still guards service deletion, registry restoration and file removal. No global disabling of WhatIf or integrity bypass. New fix CI/device results remain pending.

Application/live behavior is unchanged. Monitoring defaults OFF, dry-run ON, AllowLiveLogoff OFF. Do not repeat live sign-out for this packaging fix. Prior controlled laptop evidence is in VALIDATION-f6af81c.md; old PS5 preview failed although original normal uninstall/restoration passed.

## CI coverage

Disposable elevated x64 runner executes the packaging lifecycle separately under native Windows PowerShell 5.1 and PowerShell 7. Fixtures test a known SHA256 with inherited WhatIf, unavailable hash reporting, no-change install/uninstall previews, deliberate installed-file mismatch rejection followed by exact fixture restoration, disabled restart/history retention, real uninstall and original baseline restoration. Tests never enable monitoring/live and refuse ordinary operator-workstation execution. ARM64 is cross-published, not device-tested. Passing CI is not proof of full interruption/reparse/concurrency/upgrade hardening.

## Targeted laptop check after CI passes

No current installation should remain. Wait for application tests, BOTH shell lifecycle steps, x64 self-check and packaging to pass. Extract fresh integrated-test-win-x64; inspect package.json/checksums. Unsigned hashes are not code signing. Do not mix scripts from older packages or edit installed files/manifests.

Use elevated native 64-bit Windows PowerShell 5.1 on the spare laptop. Confirm [Environment]::Is64BitProcess is True. At package root:

```powershell
& "$PSHOME\powershell.exe" -NoProfile -ExecutionPolicy Bypass -File .\Install.ps1 -WhatIf
& "$PSHOME\powershell.exe" -NoProfile -ExecutionPolicy Bypass -File .\Install.ps1
& "$env:ProgramFiles\LockedSessionLogoff\admin\LockedSessionLogoff.Admin.exe"
```

Verify fresh disabled status, effective dry-run true and Allow LIVE unchecked. Keep monitoring OFF. Close GUI, capture THIS installation's baseline and run preview:

```powershell
$baseline = Get-Content "$env:ProgramData\LockedSessionLogoff\install.json" -Raw | ConvertFrom-Json
& "$PSHOME\powershell.exe" -NoProfile -ExecutionPolicy Bypass -File "$env:ProgramFiles\LockedSessionLogoff\Uninstall.ps1" -RestoreOriginalConfiguration -WhatIf
```

Preview should reach the explicit uninstall WhatIf message without false modified-file error. Service must remain running and owned files/recovery/configuration remain. Do not intentionally alter installed files on the laptop; CI owns the mutation fixture. If preview fails, preserve recovery state and report it rather than bypassing guards. If preview passes, run the same uninstall command without -WhatIf, confirm, then verify service/install/recovery/runtime absent and original configuration restored using the captured HadConfig/original value. No manual service stop required. PowerShell 7 uses pwsh.exe instead of powershell.exe. Process-only execution-policy override does not defeat enforced Group Policy.

## Existing application boundaries

Only installed SYSTEM backend can issue live logoff, after activation, unchanged configuration, matching SID/session/authentication LUID/WTS identity, exclusions and locked-state checks plus explicit live permit with dry-run off. No atomic check-to-logoff guarantee; narrow race remains. No countdown UX. Requests carry action IDs; accepted is not completed. Pending observation uses 30-second deadline, target absent/replaced observations or Unconfirmed, including stop/power/gap abandonment. One live attempt per logon identity per service run, maximum 128; no automatic destructive retries, no restart-resumption ledger.

Protected runtime history remains bounded to 128 entries/32 KiB, current queue 24 and full report 64 KiB. Historical runs are labeled; current reports verify ACL/age/SCM PID/process start. Malformed history warns/resets; abrupt crash can lose outcome/context. Not tamper-evident/transactional audit. Copy/Export is user-directed; exports remain outside uninstall ownership. Uninstaller removes owned runtime history, service/files and successful recovery state, restores original ConfigurationV1 or absence; completed actions cannot be undone.

First-install only, no overwrite upgrades. Preserve recovery record on conflicts/partial failures. No idle-lock/profile cleanup/maintenance schedules/network reporting/remote commands yet. Signing/licensing/recovery/upgrade and broader release/security gates remain in TASKS.md and SERVER-PLAN.md.
