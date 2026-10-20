<!-- Updated: 2026-10-20 13:32 America/Los_Angeles | Path: /AGENT-POLICY.md | Owner: maintainers | Permissions: public read/maintainer write | Purpose: hosting-stage read-only IPC boundary and integration status | Arguments: service-policy/service-observe/standalone observe | Usage: internal gate; not an idle activation release -->
# Hosted read-only policy channel - idle remains disabled

## Exact status

Foundation code is now wired into installed service OnStart/OnStop/disposal and normal-user agent query/observation modes. Host creates and retains one first-instance local pipe; exchanges are bounded, client failures logged at most once per minute, accept/disconnect faults close availability rather than returning trusted data. Stop cancels IO/disposes handle with bounded worker wait. Startup pipe-creation failure fails service startup and retains installer recovery state. No helper auto-launch or startup task. Client tools remain in extracted agent-tools folder, NOT installed into Program Files; installer ownership and original configuration schema unchanged.

Hosting-stage policy is HARD-DISABLED via HostedIdlePolicy. No setting, existing live permit or caller request can enable idle observation through this host. Existing exclusions are copied/applied server-side. 300 seconds is an inactive diagnostic default, not an active idle countdown. Read failures yield disabled ReadFailed policy. This is intentional until independent saved idle settings, administrator controls and activation gates follow. Do not represent this as completed policy-controlled idle locking. Existing locked-logoff behavior is unchanged and still explicitly opt-in; keep locked monitoring/live OFF during future IPC tests.

Build, foundation CI results, native peer authentication and device operation have NOT been independently verified here. Public API lookup of foundation check-runs failed; no green build claim. No laptop test is requested for this intermediate hosting commit. Follow-up saved policy controls and native negative/security tests precede one combined flash-drive test round.

## Client modes (for later combined test)

Run NORMAL Windows PowerShell, not elevation, at extracted agent-tools folder with matching installed service build:

```powershell
& .\LockedSessionLogoff.SessionAgent.exe --service-policy
& .\LockedSessionLogoff.SessionAgent.exe --service-observe
```

One-shot query returns only verified reply or nonzero error. Service observation re-queries every observation cycle, never falls back to standalone policy or stale cached authorization, resets timing on errors, disabled/excluded reply, local lock/unknown state, policy revision or service epoch changes. In THIS stage it must stay paused/Disabled or ExcludedBySid because host cannot enable it. Ctrl+C stops; use direct native command without Out-Host to avoid unnecessary pipeline cancellation stage. Existing standalone --observe --idle-seconds 20 remains a separate diagnostic and does not consult service policy/exclusions. No mode requests a lock/sign-out or writes configuration.

## Authentication/protocol boundary

Fixed local pipe, protected DACL, first instance, no inheritance, reject-remote flag, network deny; interactive clients get individual data/synchronize rights, not create-instance. Identification-only client SQOS limits server impersonation. Bounded 4096-byte strict JSON frames, duplicate/unknown-field/root/depth checks, single-use expiring challenge, one policy query and receipt. No lock/logoff/config-write request kind.

Native server derives actual token SID/session/authentication LUID, matches kernel client PID/session/start generation and current WTS user token/local-console unlocked state, repeating checks around trusted policy evaluation. Provider executes as SYSTEM after impersonation reverts. Client checks kernel server PID against query-status-only SCM running service and process start before hello/after reply. Client claims never supply authority; authenticates user/logon, NOT helper binary or honesty of idle data. Local administrators/SYSTEM are within trust boundary; no cryptographic binary attestation or atomic race-free guarantee. No SID/token handles/usernames serialized in replies.

## Validation still required

Pure protocol and hosted-disabled policy tests do not exercise native peer checks. Existing installed CI lifecycle will execute pipe factory/host startup/stop but not a successful interactive normal-user exchange. Need normal-user access, spoofed namespace, remote/network/elevated/cross-session rejection, logon/process reuse, cancellation/disposal, malformed/flood clients, multiple users and ARM64 device tests. Single instance may be occupied until exchange timeout; no fairness/availability proof. Bounded diagnostics are not transactional audit.

Independent default-off idle controls, policy startup/off-on/error/gap handling, supervised launch, native lock recheck/confirmation and warning/countdown remain pending. Follow TASKS.md, IDLE-LOCK.md and prior scoped validation records.
