<!-- Created: 2026-10-15 09:45 America/Los_Angeles | Path: /INTEGRATION.md | Owner: maintainers | Permissions: public read/maintainer write | Purpose: native session probe checkpoint/test guide | Arguments: watch or layout-only flags as documented below | Usage: read before running diagnostic packages -->
# Native session adapter: read-only diagnostic

## Status

This increment adds a native WTS snapshot adapter and console diagnostic, not the service or admin application. Source is prepared; compilation and package existence require a successful Actions run. No device testing is claimed. README.md and TASKS.md remain the broader project specification; this file records this new increment and its current boundary. Read IDLE-LOCK.md as well. Next tracker maintenance must link this guide and reconcile source status.

Existing core evidence: x64 compilation and 14/14 synthetic tests passed in maintainer-provided logs at a743d2d; ARM64 compilation was maintainer-reported. That evidence does not validate the new native code.

## What it does

Enumerates local Windows sessions; queries connection state, account, lock flag, logon time, and a diagnostic identity based on session/account/logon time. Frees native buffers; checks sizes/layout and buffer length; reports individual query failures as Unknown with error details. Unknown/empty identities are not eligible for future enforcement. The identity remains a diagnostic key: before enforcement, validate token/logon identity and event/poll ordering.

Read-only polling can miss unlock/relock transitions between snapshots. Service lock/unlock notifications and final identity/state rechecks are still mandatory. Disconnected state is distinct from locked state. No logoff, lock, policy write, profile deletion, service installation, scheduling, or telemetry occurs. Console output includes account/session information; do not post it publicly without redaction.

## Obtain and run after CI succeeds

Open the Native session diagnostic packages workflow in GitHub Actions. Download session-probe-win-x64 or session-probe-win-arm64 for the native OS architecture, then extract. These are unsigned self-contained diagnostics, not installers or production releases. Review SHA256SUMS.txt; checksums are not code signing.

From an elevated PowerShell window in the extracted directory:

```powershell
.\LockedSessionLogoff.Probe.exe
.\LockedSessionLogoff.Probe.exe --watch
```

The first command prints one snapshot; the second prints every two seconds until Ctrl+C. Elevation may be needed to inspect other users' sessions; it does not authorize any changes because this executable has no modification APIs. Redirect locally if desired:

```powershell
.\LockedSessionLogoff.Probe.exe --watch > .\session-probe.jsonl
```

The first stdout line is a diagnostic banner, followed by JSON snapshots; the output file is not strictly JSONL until that banner is removed. Delete local captures when finished; do not commit them.

## Local test

1. Confirm the executable reports the expected native architecture.
2. Start watch mode; note your own session/account and Unlocked flag.
3. Press Win+L, wait at least ten seconds, unlock, then stop with Ctrl+C.
4. Review snapshots for Unlocked -> Locked -> Unlocked on the same logon identity.
5. With another user session, verify independent IDs/accounts and connection states.
6. Share only redacted state/error excerpts if something is Unknown or mismatched.

A successful layout check or build is not proof of correct session semantics. ARM64 must be run on a real ARM64 machine before marking that architecture device-tested. The workflow validates layout only on x64, publishes both architectures, and includes the guide/checksums. It runs the existing core synthetic tests without querying hosted-runner account data into public logs.

## Remaining implementation

- [ ] Verify new CI and artifact existence.
- [ ] Validate real x64 and ARM64 lock-state snapshots.
- [ ] Service lifecycle and serialized lock/unlock/suspend notification handling.
- [ ] Stable token/logon identity, secure configuration and exclusions, final pre-logoff recheck.
- [ ] Elevated administrator UI and runtime enablement/dry-run/debug.
- [ ] Session agent input-idle detection, authenticated IPC and lifecycle.
- [ ] Install/uninstall with no automatic enforcement; original-state ledger before policy/task changes.

No installable enforcement utility exists in this increment. The previous safe-default and reversible-uninstall requirements remain unchanged. Source files and workflow have headers documenting purpose, arguments, ownership and usage. Public read/maintainer write permissions refer to repository files, not workstation ACLs; protected installation ACLs remain to be implemented. No license decision or signing arrangement is implied.
