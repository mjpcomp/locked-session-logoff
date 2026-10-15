<!-- Updated: 2026-10-15 12:13 America/Los_Angeles | Path: /VALIDATION.md | Owner: maintainers | Permissions: public read/maintainer write | Purpose: sanitized cumulative validation evidence | Arguments: none | Usage: distinguish supplied evidence, reports and untested new paths -->
# Validation record

## Foundation and standalone diagnostics

At a743d2d, supplied CI logs confirmed x64 compilation with zero compiler warnings/errors and 14/14 core tests. ARM64 compilation was maintainer-reported, not device-tested. Earlier native x64 probe showed Unlocked -> Locked -> Unlocked with stable diagnostic identity and no user-session error. Listener classification was then corrected.

Service-host console output supplied 2026-10-15: layout self-check passed; Locked -> WouldLogOff approximately 20.07 seconds later -> Unlocked. A subsequent lock ended approximately 6.02 seconds later without a false timeout; it did not immediately inherit an expired earlier timer. Full fresh-threshold relock was not shown. Console polling cannot prove transitions between snapshots.

Administrator GUI: maintainer reported launch, edit/save and close/reopen persistence. Artifact provenance was consistent with 74fc1b2 but exact GUI artifact hash was not independently inspected. UAC rejection, corruption and standard-user write tests were not thereby established.

## Configuration bridge

Supplied output at the configured observer stage showed debug true/false/true reload; enablement from saved settings; 20-second locked timeout with WouldLogOff about 20.05 seconds later; unlock; reload of timeout to 200 seconds without process restart; subsequent disable. No full 200-second timer test was shown. This validates console consumption only, not native service/IPC security.

## Installed service package 299e347

Maintainer reported all builds passed after redundant threading package reference was removed. Package.json and installation baseline identified 299e3471e5e92f93ab386e5bb334c2b0377d9605. Supplied output showed preview, successful install, owned files/rollback record and running service. Native SCM SessionLock/SessionUnlock events, Locked/Unlocked and WouldLogOff were recorded in the service report, with GUI-saved 20-second configuration accepted and later disabled. WouldLogOff appeared about 22 seconds after observed Locked, not an exact deadline guarantee.

The report was retrieved through PowerShell; this alone does not establish that the GUI's ACL/PID/start/freshness verification passed. Recent events were initially hard to find; no Event Viewer/persistent logging existed. New UI addresses discoverability but requires validation.

## Uninstall and baseline restoration, 2026-10-15

Supplied baseline indicated Protocol 1, HadConfig true, Installed phase and commit 299e347. Unsigned script was initially blocked. An explicit child PowerShell invocation with process-only ExecutionPolicy Bypass ran preview and then confirmed uninstall. No permanent policy change was requested in the instructions; enforced group policy cannot be overridden by that option.

Post-uninstall supplied checks:

- ServicePresent: false.
- InstallDirectoryPresent: false.
- RecoveryDirectoryPresent: false.
- RuntimeKeyPresent: false.
- OriginalConfigurationRestored: true, exact case-sensitive string comparison to captured pre-install value.

This is a successful tested uninstall/restoration path, not proof of interruption/modified-file/adversarial recovery. User remains uninstalled awaiting next package. Raw paths, usernames, machine names, SIDs, run IDs, original config and raw logs are deliberately omitted.

## New increment not yet validated

SID/token identity, actual SID exclusions, final candidate checks, live permit/logoff, new events GUI, and new native/CI tests are source additions only until explicitly verified. Prior successes do not transfer to them. Synthetic tests never invoke native logoff. Native request acceptance is not confirmation of completed logoff.

Next: new CI, clean installed dry-run and SID exclusion tests, visible event Copy/Export, then optional authorized safe-account live test and lifecycle restoration. Keep all work saved before enabling live. Existing installer is first-install only; no overwrite upgrade. See SERVICE-INTEGRATION.md and TASKS.md for current behavior, residual race/installer limitations and full pending scope.
