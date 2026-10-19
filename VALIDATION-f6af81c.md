<!-- Created: 2026-10-19 13:32 America/Los_Angeles | Path: /VALIDATION-f6af81c.md | Owner: maintainers | Permissions: public read/maintainer write | Purpose: sanitized live/outcome/PS5 lifecycle evidence | Arguments: none | Usage: scoped supplied evidence; preview defect is not a pass -->
# Controlled Windows 11 x64 laptop live-test checkpoint

Development stage f6af81c; exact artifact hash/package.json commit and shell build were not independently inspected. Operator reported the laptop used default Windows PowerShell 5.x. Reports/terminal output supplied 2026-10-19 establish the results below, not production readiness or native ARM64 execution. No raw user/machine identifiers, SIDs, run/action IDs, local user paths, configuration hashes or original configuration are published.

## Application evidence

- Verified current report, saved revision matching and disabled initial configuration.
- Dry-run at 20 seconds: observed lock then WouldLogOff roughly 20 seconds later, followed by unlock.
- Explicit live mode at 60 seconds: observed lock, request accepted roughly 62 seconds later.
- Native SessionLogoff notification for the target roughly 6 seconds after acceptance.
- LogoffTargetAbsentObserved roughly 10 seconds after acceptance, correlated with the SAME action ID. This records target absence after the request, not independent causal proof.
- New session connected/logged on and was observed unlocked. One live request shown; no destructive repeat in supplied interval.
- Final monitoring disabled/effective dry-run true/inactive timeout with matching saved revision. Final status does not independently display saved AllowLiveLogoff.

## Preview failure and integrity investigation

Windows PowerShell uninstall -WhatIf emitted module alias/provider-property WhatIf messages and reported the installed uninstaller modified. Independent read-only verification outside preview found 475 files checked, zero missing, zero hash mismatches. This strongly implicates the preview-sensitive hashing path, but exact helper behavior was not independently reproduced on that device. Old preview remains FAILED, not a compatibility pass.

## Actual uninstall and verified restoration

Operator captured this installation's protected baseline record, ran the ORIGINAL installed uninstaller without WhatIf and with explicit RestoreOriginalConfiguration, confirmed removal, received successful service deletion and completion. No script/manifest modification or guard bypass was requested. Supplied final checks: service absent, install directory absent, recovery directory absent, runtime key absent and original configuration restored according to captured HadConfig/original-value comparison. This is direct verification, not only an operator closeout statement. Test laptop and working machine remain uninstalled.

## New fix validation pending

New direct read-only SHA256 helper, scoped read-only CIM import handling and dual-shell CI fixtures require their own CI/device results. Prior normal uninstall success does not validate repaired preview. Genuine mismatch must still block. Do not retrofit edited uninstallers or hashes into existing installations. Remaining live-edge/race/recovery/upgrade/audit/countdown and ARM64 tests are tracked in TASKS.md.
