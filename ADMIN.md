<!-- Created: 2026-10-15 10:46 America/Los_Angeles | Path: /ADMIN.md | Owner: maintainers | Permissions: public read/maintainer write | Purpose: configuration UI boundary/test guide | Arguments: executable takes none | Usage: read before running or saving settings -->
# Administrator utility: configuration editor increment

## Status

Adds elevated WinForms GUI, local service status, read-only refreshed sessions, and validated machine-wide settings persistence. Source is prepared; compilation/artifacts/UI testing remain unverified until CI and workstation tests. No installer, service settings consumer, applied-revision acknowledgment, actual logoff/lock, idle helper, profile/task changes, central reporting or remote actions are added.

The existing service host remains unchanged and does NOT consume this configuration. The UI explicitly says configuration only; saved MonitoringRequested is intent, not an effective enablement. Do not describe this package as a complete enforcement release. Connecting the service, proving ACLs and adding an applied-status channel are the next increment.

## Configuration and security boundary

64-bit registry value: HKEY_LOCAL_MACHINE\SOFTWARE\LockedSessionLogoff\ConfigurationV1, REG_SZ JSON. Same native view on x64/ARM64. Contains only app/schema, monitoring request, dry-run/debug, timeout seconds and exclusion SIDs; no credentials. Defaults request no monitoring and timeout 3600 seconds. Save/remove explicitly require administrator elevation. The executable embeds requireAdministrator.

Relies on Windows HKLM inherited access controls; no custom ACL rewrite is performed. Validate standard users cannot modify this key on each supported deployment before any consumer can enforce actions. Custom/broken ACLs are not repaired or certified by this build. No credentials, endpoints or executable paths should be put in this value. Future installer must validate/protect configuration and binaries.

Unknown schema/application/fields, wrong type, malformed/oversized data, invalid timeout, duplicate/invalid SIDs, and unrecognized populated keys without our value cause a visible error, not a silent default overwrite. Missing configuration displays defaults without creating a key. One JSON value is written per save. A changed-value check detects common stale editor conflicts but is NOT atomic compare-and-swap; serialize writers before enabling an enforcement consumer. This build does not auto-repair corrupted data.

Remove deletes only ConfigurationV1 after confirmation; leaves the key and unrelated data. No OS profile-retention policy or scheduled tasks are touched. This is not the planned full uninstall. Credentials and protected original-state ledger remain future work. Exclusion SIDs are validated and stored, not currently enforced.

## Download/run after CI passes

Open Administrator utility packages for the approved commit. Extract admin-win-x64 or admin-win-arm64 completely. Packages are unsigned/self-contained; inspect checksums, which do not replace signing. Run LockedSessionLogoff.Admin.exe, accept the administrator prompt, and review the top configuration-only warning. Rejecting elevation should prevent launch. No command arguments.

## Workstation test checklist

- [ ] GUI opens after elevation; no settings are created just by opening it.
- [ ] Service status is not installed/unavailable unless independently installed; never equate running with applied settings.
- [ ] Session table shows console lock/unlock and non-user listener/services classifications.
- [ ] Adjust timeout/dry-run/debug; Save displays exact settings/target and requires confirmation.
- [ ] Close/reopen confirms persistence without activating actions.
- [ ] Duplicate/invalid SID input rejected; numeric limits enforced.
- [ ] Cancel save/removal leaves configuration unchanged.
- [ ] Concurrent editor changes prompt reload when detected; do not claim atomic locking.
- [ ] Remove confirmation deletes only our value; reopen shows disabled defaults.
- [ ] Standard-user write attempt to the machine key fails on test machine.
- [ ] Corrupt/unknown settings show blocked state, not accidental replacement.
- [ ] ARM64 GUI/device verification separately.

If no values existed before this test, removing saved configuration restores the application's disabled default behavior; the empty application key may remain. Manual cleanup of malformed settings requires a reviewed registry backup/recovery procedure, not blind deletion. Do not paste captures containing usernames into public issues without redaction.

## Implementation record and next steps

Files: Windows/AdminSettings.cs; Admin project/manifest/Program.cs; .github/workflows/admin.yml; ADMIN.md. Owner maintainers, repository public read/maintainer write. Local operation requires elevation; no workstation changes occur from merely committing files. Registry writes happen only when the operator confirms Save/Remove. No service start/stop controls or automatic writes on refresh.

Next: add serialized secure configuration consumption and effective revision/status, then secure installer and final token/session checks before live logoff. Add tests for settings validation and persistence/failure cases; current CI runs existing core tests only, not new UI/settings behavior. Consolidate README/TASKS links to all increment guides and validation evidence before a release. Keep service/console/UI/platform-server states distinct. SysIdle remains only a naming candidate; identifiers are unchanged.
