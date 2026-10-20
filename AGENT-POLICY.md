<!-- Created: 2026-10-20 12:08 America/Los_Angeles | Path: /AGENT-POLICY.md | Owner: maintainers | Permissions: public read/maintainer write | Purpose: staged read-only IPC contract and security boundary | Arguments: channel APIs only, no installed CLI yet | Usage: foundation implementation; hosting/config integration follows -->
# Read-only agent policy channel foundation

## Actual status

Channel/native-peer code and pure tests are present in source. NOT yet wired into ConfiguredService, observer CLI, administrator settings/UI, installer or startup supervision. No listener starts just by loading this library and no new laptop package/test is requested for this foundation commit. CI/native security/device behavior remain pending. Do not call this completed authenticated service-agent integration.

Follow-up must host/dispose the endpoint with service lifecycle, add independently disabled validated idle policy and effective startup/error/off-on gates, expose explicit administrator controls, and add normal-user agent policy refresh with no stale-cache fallback and fresh timers on policy/service-epoch changes. No real idle lock until separate native action/confirmation/security gates pass.

## Implemented contract

Fixed local named pipe, byte mode, little-endian 4-byte length and at most 4096 UTF-8 JSON bytes. Strict mapped fields, duplicate/root/depth validation. One bounded five-second exchange: Hello -> server challenge -> GetIdlePolicy with challenge -> filtered read-only reply -> receipt. Fresh per-connection nonce, single-use/expiry validation; no caller-provided SID/session/timeout/commands accepted. Only reply contains trusted provider threshold/revision/mode; exclusions are applied on server and not sent as a list. Every accepted reply requires DryRun true / ActionsEnabled false. No lock/logoff/configuration-write request kind exists.

Native server factory requires SYSTEM, first-instance ownership, single instance, non-inheritable handle, explicit protected DACL denying NETWORK and allowing SYSTEM control/interactive data read-write+sync. PIPE_REJECT_REMOTE_CLIENTS also rejects remote connections. Client opens fixed local path with individual rights (not GENERIC_WRITE/create-instance) and identification-only SQOS. Pipe lifetime/accept loop/backoff/health reporting belongs to future host; local authenticated clients can occupy the single instance until the deadline. This is bounded denial-of-service exposure, not fairness or availability proof.

Server reads a bounded first message before impersonation, derives actual token SID/session/authentication LUID, rejects elevated/network/session-0 clients, checks kernel client PID/session/start generation, compares current WTS user token and positively known local-console unlocked state. Checks repeat around policy evaluation. Impersonation callback only queries token data; provider executes back as SYSTEM. No token handles/SIDs/usernames are serialized. Any local process in the authenticated logon may query its own filtered policy: this authenticates the user/logon, NOT the helper executable or its input truthfulness. Never elevate client idle reports into action authority.

Client requires normal user context and verifies kernel pipe server PID against running installed service via query-status-only SCM access and process start generation, before hello and after reply. Service status is not cryptographic binary attestation; local administrators/SYSTEM remain inside the trust boundary. No credentials, shared secrets or service token are passed in helper arguments.

## Remaining validation

Pure tests cover framing, truncation/size/schema/duplicate/replay/expiry, policy/exclusions/action-disabled replies and changed peer data. They do NOT execute native impersonation/ACLs/service queries. Native normal-user access, spoofed namespace, remote/network rejection, cross-session/logon changes, lifecycle cancellation/disposal, malformed/flood clients, multiple users, parser fuzzing and Windows x64/ARM64 behavior require subsequent integration tests. No claim of race-free atomic authorization or production readiness.

## Existing observer command correction

For the existing standalone observer use the long-running native command directly, without Out-Host:

```powershell
& .\LockedSessionLogoff.SessionAgent.exe --observe --idle-seconds 20
```

The earlier Ctrl+C stop message plus prompt return establishes observer shutdown; PowerShell pipeline cancellation noise was also recorded. Removing that extra pipe avoids the unnecessary pipeline stage, not a guarantee about every host. No repeat standalone test required solely for this command change. See VALIDATION-2aeffea.md and TASKS.md.
