<!-- Updated: 2026-10-21 11:16 America/Los_Angeles | Path: /AGENT-POLICY.md | Owner: maintainers | Permissions: public read/maintainer write | Purpose: saved report-only idle settings and effective-policy guards | Arguments: GUI plus service-policy/service-observe | Usage: controlled x64 combined test after CI, no idle lock actions -->
# Service-managed report-only idle observation

## Current source and evidence

Saved idle intent/timeout and separate administrator GUI controls now feed an independent service IdleObservationGate. Source/build/native/device results for THIS increment remain pending. Maintainer explicitly reported all builds/checks green for previous hosting commit 7933adf; logs were not independently inspected. That is not native normal-user exchange evidence.

IdleObservationRequested defaults false and IdleTimeoutSeconds defaults 300, including older saved JSON/installer-created configuration with absent fields. Both live in existing protected ConfigurationV1, preserving the existing baseline/restore ownership model; no extra registry values or schema upgrade mechanism. Do not mix older binaries that reject unknown fields with new saved JSON. First-install only; no overwrite upgrade.

Idle observation is independent from MonitoringRequested/LockedTimeoutSeconds. To request idle observation, Dry-run MUST be checked and Allow LIVE unchecked; invalid combinations are rejected before save and by service reads. Shared SID exclusions affect both stages. Reply always requires DryRun true / ActionsEnabled false. Enabling idle observation authorizes local WouldLock output only, never locks, simulated lock state or logoff. Existing locked-logoff live behavior for valid idle-OFF configurations is unchanged.

## Activation and failure rules

Service observes configuration on startup, every two seconds and during authenticated queries, under a serialized policy lock. Saved idle ON at startup reports AwaitingDisableThenEnable until service observes idle OFF then ON. Config read/validation failure disarms to ReadFailed; recovery with idle still ON requires OFF/ON. Backward service monotonic time or refresh gap over ten seconds disarms; if current saved intent is OFF, that valid OFF observation can rearm a later ON. Missing configuration is disabled. Removing saved settings disables both requests; it is not uninstall/restoration.

Normal native client queries still require current local-console unlocked logon identity. Per-client locked/disconnected/unknown/connection failures do not globally require admin OFF/ON: client resets its local timer, does not use stale data, and needs a fresh verified reply/window after recovery. Policy revision, timeout or server epoch changes reset client timing. Dedicated short suspend/power-notification semantics, secure desktop and RDP remain unvalidated; do not infer coverage from the long-gap guard.

IdleObservationPolicy service events report effective enabled/mode/timeout/revision without account/SID data. GUI saved checkbox is intent, not activation proof; main verified status panel is explicitly LOCKED-LOGOFF state. Inspect idle policy events AND authenticated agent reply for effective idle state. Agent WouldLock remains local output, not a privileged report/control message to the service.

## Combined laptop round (only after new CI passes)

Use fresh integrated-test-win-x64 with matching package metadata. Keep locked monitoring OFF, Dry-run checked, Allow LIVE unchecked. No repeat live logoff. Service/admin install/removal still elevated; agent runs from extracted agent-tools in NORMAL PowerShell, never elevation. Client tools are not automatically installed/launched. No helper startup task or supervision yet.

1. Clean install, verify initial disabled idle/locked intent and unchecked live permit.
2. From normal shell query --service-policy before enabling: require a VERIFIED successful reply with Enabled false / DryRun true / ActionsEnabled false. If query fails, STOP and return error plus service events; do not treat failure as a disabled-policy pass. This is the first native normal-user exchange validation.
3. Start --service-observe directly (no Out-Host). Enable only IDLE observation at 20 seconds in elevated GUI; wait for effective ConfiguredDryRun. Expect WouldLock without locking.
4. Change idle threshold, verify fresh timing; disable, verify no candidates. Add intended user's SID to shared exclusions, reenable and verify ExcludedBySid/no candidate; remove exclusion and verify fresh eligibility.
5. Restart service with saved idle ON; verify AwaitingDisableThenEnable/no candidate, then GUI idle OFF, wait for effective Disabled, idle ON to rearm.
6. Test stopping service/connection unavailability resets client and does not fall back to standalone. Reconnect requires fresh verified policy/window; restart also requires OFF/ON. Elevated client rejection is distinct from successful normal-user query. Do not run privileged CI attack fixtures on laptop.
7. Disable both intents, leave live unchecked, stop agent, export events/transcript and close GUI. Capture this installation's baseline, preview/remove and verify owned absence/restoration using current integrated guide. No manual file/manifest changes. A full two-console flash-drive sheet will be supplied once CI passes.

Commands (not an automatic test script):

```powershell
& .\LockedSessionLogoff.SessionAgent.exe --service-policy
& .\LockedSessionLogoff.SessionAgent.exe --service-observe
```

## Unchanged security boundary / remaining validation

Fixed local first-instance pipe, protected DACL/network deny/reject-remote, identification-only client SQOS, bounded strict 4096-byte JSON frames, expiring single-use challenge and receipt. Native token/PID/start/session/current WTS checks repeat; client checks kernel server PID against query-status-only running SCM service and process start. Authenticates user/logon, NOT helper executable or honesty of input. Local admin/SYSTEM trusted; no cryptographic binary or atomic race-free proof. No lock/logoff/config-write RPC.

Pure new tests exercise settings/default/legacy JSON, independent gate restart/read error/gap/time bounds/revision/exclusion and action-disabled replies. They do not prove native ACL/impersonation/client access. Successful/negative native connection, spoofed namespace, remote/elevated/cross-session, replay/flood, cancellation/disposal, multiple users and ARM64 device cases remain gates. Existing lifecycle CI exercises SYSTEM pipe creation/start/stop only, not interactive normal-user authentication.

No actual idle enforcement, countdown/warning UX, supervised launch, profile/maintenance/server features yet. Native lock recheck/confirmation and full ownership/recovery/upgrade/adversarial release gates remain. Bounded retained diagnostics are not a transactional/tamper-evident audit ledger. Never publish raw operator identifiers/transcripts.
