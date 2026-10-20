<!-- Created: 2026-10-20 12:08 America/Los_Angeles | Path: /VALIDATION-2aeffea.md | Owner: maintainers | Permissions: public read/maintainer write | Purpose: sanitized standalone idle-observer evidence | Arguments: none | Usage: distinguish transcript evidence from operator confirmations -->
# Read-only idle observer checkpoint

Supplied transcript identifies Windows PowerShell 5.1.26100.9711 Desktop and package metadata 2aeffea8174c65b22499386562c6ccc09df9eeef, win-x64, ReadOnlyIdleObserver, ActionsEnabled false. Native input/WTS layout check exits 0. Observation loop ran; source's elevated/SYSTEM refusal was not taken, consistent with ordinary user context. Exact binary hashes/CI logs were not independently inspected. No private transcript identifiers are reproduced.

## Evidence

- Initial fresh-window message then WouldLock about 24 seconds later at configured 20 seconds. Initial message is not necessarily the last-input reset time; no early threshold shown.
- Operator explicitly confirmed WouldLock without automatic locking, no repeat during continued untouched wait, and new input followed by fresh wait/new candidate. Input resets are not individually logged, so timing for that stage is operator-confirmed rather than fully reconstructed from transcript.
- Manual-lock stage logged paused state; observation resumed after unlock. Next WouldLock about 20.05 seconds after resumed fresh window.
- Observer printed normal stop message, prompt returned and subsequent notes/transcript completion succeeded. PowerShell additionally logged two pipeline-stopped messages after Ctrl+C on the piped native command; record successful observer shutdown with shell cancellation noise, not 'no errors whatsoever'. Operator stop confirmation agrees.

## Scope

Ordinary local-console read-only idle path PASSED for shown/confirmed checks. Not actual idle locking, authenticated IPC, saved machine-policy integration, sleep/resume, RDP, multiple users, ARM64 or general race/recovery validation. No installation/uninstallation needed for observer. New IPC foundation requires independent CI/native/integration evidence; operator need not repeat this standalone round for the foundation commit.

Record is sanitized: no account/machine names, user-local paths, raw run IDs, SID data or original private transcript. Earlier preview validation remains VALIDATION-0138c6c.md.
