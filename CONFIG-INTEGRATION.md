<!-- Created: 2026-10-15 11:01 America/Los_Angeles | Path: /CONFIG-INTEGRATION.md | Owner: maintainers | Permissions: public read/maintainer write | Purpose: safe saved-config consumer test checkpoint | Arguments: observer takes none | Usage: read with ADMIN.md, SERVICE.md and VALIDATION.md before testing -->
# Saved configuration integration: console bridge

## Scope and honest boundary

This increment implements a reusable activation gate and a configured console observer using the same saved settings as the admin GUI. It is a safe integration test, NOT the installed service configuration consumer or GUI applied-status channel. Existing Service and Admin executables are unchanged. Their statements that the current service does not consume saved settings remain true. No installer, actions or live enforcement exists. New source/build/tests require CI verification; no new device pass is claimed.

The observer reads the machine registry but writes nothing. The existing GUI writes only on confirmed Save/Remove. Console emits ConfigurationEffectiveForConsoleObserver, with a SHA-256 fingerprint of the raw snapshot and its accepted/blocked mode. This is not authenticated service acknowledgment. No raw settings, usernames or logon identities are printed by this observer; session IDs and errors by category may appear. No telemetry.

## Safety and activation

- Missing/invalid/read-error settings disable observation and reset timing.
- Startup with saved monitoring enabled waits for a valid monitoring OFF state followed by ON; it cannot autoactivate from that old request. If settings start absent/off, a later explicit ON can enable dry-run.
- After a read/observation failure, require OFF then ON again. Gaps over ten seconds disarm/reset conservatively.
- DryRun is always forced true, even when saved false. Mode states that enforcement is unavailable. No lock/logoff imports or execution.
- Configured exclusion SIDs currently block all observation rather than silently ignore exclusions. Actual SID resolution/exclusion integration remains pending.
- Accept changed configuration fingerprints/mode once; unchanged snapshots do not reset timers each poll. Changed timeout/debug/raw contents conservatively reset timing.
- Session state/identity changes reset that session; unknown/ineligible sessions are forgotten. Missing sessions are forgotten. One WouldLogOff per observed lock interval.
- Console uses polling, so fast unlock/relock between samples is not guaranteed to be observed. It cannot prove continuous-lock enforcement.

Only validated snapshots from AdminSettingsStore.Read should be passed to ConfigurationGate; its raw/settings arguments are one coherent read. Synthetic tests use simple raw labels intentionally. Fingerprints identify observed bytes, not signatures or configuration provenance.

## Test after Configured observer integration succeeds

Extract configured-observer-win-x64 fully and open PowerShell there. Use elevation if registry/session access requires it. Packages are unsigned/self-contained with checksums. No arguments:

```powershell
.\LockedSessionLogoff.ConfigObserver.exe
```

1. Open the existing Admin GUI beside it. Do not manually install a service.
2. If monitoring was already checked, uncheck and Save. Wait for the observer to print Disabled.
3. Set timeout to 20 seconds, leave exclusions empty, select dry-run, check monitoring, and Save.
4. Expect ConfiguredDryRun with Enabled=true and a configuration fingerprint.
5. Lock for at least 25 seconds, unlock; expect Locked, WouldLogOff, Unlocked.
6. Change timeout in GUI and Save; expect a new fingerprint and accepted timeout without restarting the observer.
7. Uncheck monitoring and Save; expect Disabled and no timer decisions.
8. Ctrl+C stops. Optionally remove saved configuration using the GUI's explicit confirmation.

Do not deliberately corrupt production settings for a test. Failure tests use synthetic inputs in CI. No live actions occur even if DryRun is unchecked; the observer clearly reports ForcedDryRunEnforcementUnavailable. GUI still reports only saved settings, not this console's applied state.

## New automated coverage

15 deterministic no-write cases: missing/default disabled, old enabled startup block, explicit activation, forced dry-run, failure disarm, invalid timeout/schema/null exclusions, duplicate SID rejection, unsupported exclusions block, unchanged-read stability, timeout reload, disable and revision change. Existing 14 core cases remain separate. The workflow executes both suites on hosted x64 and cross-publishes x64/ARM64; it does not run the observer against runner settings or record runner sessions.

## Remaining work before service consumption/enforcement

- [ ] Verify new test/build/artifacts and GUI-to-observer reload on a device.
- [ ] Serialize configuration writers; verify/protect registry ACLs and handle conflicts. Existing optimistic writes are not atomic CAS.
- [ ] Connect tested gate to installed service with explicit activation lifecycle, stable identity and secure settings.
- [ ] Authenticated local status channel and saved-versus-applied revision in GUI, including freshness/process identity.
- [ ] Native SID/logon-token identity, exclusions, event ordering and final state checks.
- [ ] Secure installer/uninstaller, bounded logs and original-state ledger.
- [ ] Functional live actions gated by explicit consent; runtime dry-run/debug in same eventual release.

Before a functional release consolidate README.md/TASKS.md links and source status. Read this guide with IDLE-LOCK.md, INTEGRATION.md, SERVICE.md, ADMIN.md, VALIDATION.md, SERVER-PLAN.md and BRANDING.md. This commit does not rename product identifiers, provision server/domain infrastructure or change workstation settings. Service integration remains incomplete until explicitly implemented and tested.
