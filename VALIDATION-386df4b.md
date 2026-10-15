<!-- Created: 2026-10-15 13:13 America/Los_Angeles | Path: /VALIDATION-386df4b.md | Owner: maintainers | Permissions: public read/maintainer write | Purpose: sanitized supplied workstation evidence for SID-aware package | Arguments: none | Usage: results apply to prior test round, not new diagnostics code -->
# SID-aware package workstation checkpoint

Prior source increment: 386df4b. User tested the integrated-test-win-x64 package corresponding to this development stage; exact binary hashes/package.json commit were not independently inspected. Evidence below is supplied terminal output, copied verified GUI reports, exported event text and explicit operator confirmation on 2026-10-15. This is not an independent CI inspection, multi-user stress test or production approval.

## Observed results

- Install preview, confirmed installation and GUI launch completed; monitoring initially disabled.
- GUI displayed VERIFIED SERVICE REPORT with matching editor-loaded saved revision.
- Enabled dry-run with 20-second threshold: native lock notification, observed Locked, WouldLogOff approximately 22 seconds later, native unlock notification and observed Unlocked.
- SID exclusion generated ExcludedBySid; an excluded lock lasting roughly 33 seconds produced no new WouldLogOff.
- Service restart produced new run and AwaitingDisableThenEnable; explicit OFF then ON restored ConfiguredDryRun.
- Exclusion removal restored eligibility: observed Locked followed by WouldLogOff approximately 22 seconds later, then unlock. Earlier historical events were not new actions.
- Final save OFF produced Disabled, monitoring false/effective dry-run true.
- Operator confirmed earlier supplied reports came from Copy and final report from Export. Both outputs were readable with expected status/events.
- Operator observed events cleared on service restart. Source confirmed memory-only 24-entry queue; expected limitation, not durable logging validation.
- Remove saved settings produced MissingConfiguration, monitoring false/effective dry-run true/no revision and inactive fallback 3600-second value. Both-absent revision displayed True, motivating presentation improvement.
- After removing saved settings: captured current installation's recovery record, previewed uninstall, confirmed uninstall, service deletion succeeded and script reported original configuration baseline restored.

## Supplied final checks

ServicePresent false; InstallDirectoryPresent false; RecoveryDirectoryPresent false; RuntimeKeyPresent false; OriginalConfigurationRestored true. Last check compared the current value with captured original when present, otherwise checked absence. User remains uninstalled; extracted package and export retained as test evidence.

## Not established

Live logoff, completed-action reconciliation, warning UX, ARM64 device execution, session-ID reuse races, simultaneous users, adversarial permissions/configuration changes, interrupted removal/recovery/upgrade, persistent logging and new diagnostics behavior remain unvalidated. Ordinary single-user SID identity/candidate paths were exercised indirectly by successful WouldLogOff and exclusion outcomes; this does not prove every token edge case.

## Privacy

No raw account names/SIDs, local user paths, machine names, run IDs, configuration hashes, original configuration or raw logs are reproduced. User-selected exports remain local and outside uninstall ownership. Earlier cumulative history remains in VALIDATION.md; this supplemental record supersedes its pending workstation status for these specific prior-package paths only.
