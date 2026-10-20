<!-- Created: 2026-10-20 09:36 America/Los_Angeles | Path: /VALIDATION-0138c6c.md | Owner: maintainers | Permissions: public read/maintainer write; Purpose: sanitized direct PowerShell preview regression evidence | Arguments: none | Usage: closes targeted device regression only -->
# Windows PowerShell preview-fix workstation validation

Supplied 2026-10-20 transcript and initial/preview/final JSON directly establish native 64-bit Windows PowerShell 5.1.26100.9711 Desktop and package metadata win-x64 / 0138c6c4823778d55cc9a8c7e1ae20e4178e9e8f. These are now inspected version/package metadata, unlike the earlier test round. Exact application binary digests/CI logs were not independently checked. Raw transcript contains private account/machine/paths and is deliberately not reproduced here.

## Results

- Install preview and actual install both exit 0.
- Service Running/Disabled; effective monitoring false/dry-run true; saved monitoring false/dry-run true/live permit false; baseline present; manifest 475 files.
- Repaired installed uninstall preview exits 0 with explicit expected WhatIf operation, no false modified-file error.
- After preview: same running service run, monitoring still disabled, install/recovery present, recovery hash unchanged, configuration exact unchanged.
- Independent post-preview check: 475 files, zero missing, zero hash mismatches.
- Actual original installed uninstaller exits 0, service deletion succeeds, completion reports original baseline restoration.
- Final service/install/recovery/runtime absent; original configuration restoration true using this installation's captured HadConfig/original-value comparison.
- Transcript explicitly ends PASS and Stop-Transcript completed. Pasted JSON agrees; missing upload attachment is not missing evidence.

## Conclusion and scope

Targeted PowerShell 5.1 preview/no-mutation/normal uninstall/restoration gate PASSED for this package/environment. No live logoff needed or tested again. The previous preview failure remains a historical defect fixed by the tested increment. No claim that genuine-mismatch fixtures, all recovery/race cases, PowerShell 7 CI or ARM64 device tests were independently inspected from this transcript. Those require separate evidence. User can remain uninstalled; no repeat laptop cycle required for this build.

Source/UI next increment and standalone idle observer require their own CI/device checks. This record contains no account/machine identifiers, raw run IDs, user-local paths, configuration hashes or original config.
