#Requires -Version 5.1
#Requires -RunAsAdministrator
<# Created: 2026-10-19 13:32 America/Los_Angeles
Path: tests/PackagingLifecycle.ps1; Owner: maintainers; Permissions: public read/maintainer write
Purpose: CI-only preview/hash/lifecycle regression under PS5.1 and PS7
Arguments: PackageRoot, ExpectedMajor; Usage: disposable elevated GitHub x64 runner; never enables monitoring/live #>
[CmdletBinding()] param([Parameter(Mandatory=$true)][string]$PackageRoot,[Parameter(Mandatory=$true)][int]$ExpectedMajor)
$ErrorActionPreference='Stop'
if($env:GITHUB_ACTIONS -ne 'true'){throw 'CI-only disposable-runner test; do not run on an operator workstation.'}
if($PSVersionTable.PSVersion.Major -ne $ExpectedMajor -or -not [Environment]::Is64BitProcess){throw 'Unexpected shell version/architecture'}
$package=(Resolve-Path -LiteralPath $PackageRoot).ProviderPath
$install=Join-Path $package 'Install.ps1'
$sourceUninstall=Join-Path $package 'Uninstall.ps1'
$base=Join-Path $env:ProgramFiles 'LockedSessionLogoff'
$data=Join-Path $env:ProgramData 'LockedSessionLogoff'
$record=Join-Path $data 'install.json'
$uninstall=Join-Path $base 'Uninstall.ps1'
function Read-Config {
    $r=[Microsoft.Win32.RegistryKey]::OpenBaseKey([Microsoft.Win32.RegistryHive]::LocalMachine,[Microsoft.Win32.RegistryView]::Registry64)
    $k=$null
    try {$k=$r.OpenSubKey('SOFTWARE\LockedSessionLogoff');if($k){return $k.GetValue('ConfigurationV1',$null,[Microsoft.Win32.RegistryValueOptions]::DoNotExpandEnvironmentNames)};return $null}
    finally {if($k){$k.Dispose()};$r.Dispose()}
}
function Read-Status { return ((Get-ItemProperty -LiteralPath 'HKLM:\SOFTWARE\LockedSessionLogoff.Runtime' -Name StatusV1).StatusV1 | ConvertFrom-Json) }
function Assert-Absent {
    if((Get-Service LockedSessionLogoff -ErrorAction SilentlyContinue) -or (Test-Path $base) -or (Test-Path $data) -or (Test-Path 'HKLM:\SOFTWARE\LockedSessionLogoff.Runtime')){throw 'Owned installation/runtime remains'}
}
function Assert-Unchanged($ledger,$recordHash,$raw,$run) {
    if((Get-Service LockedSessionLogoff).Status -ne 'Running' -or (Read-Config) -cne $raw){throw 'Preview changed service/configuration'}
    if((Get-OwnedFileSha256 -LiteralPath $record) -ne $recordHash){throw 'Preview changed recovery record'}
    $status=Read-Status
    if($status.RunId -ne $run -or $status.Configuration.Enabled -ne $false){throw 'Preview restarted/activated service'}
    foreach($file in $ledger.Files){
        $p=Join-Path $base $file.Relative
        if(-not (Test-Path -LiteralPath $p) -or (Get-OwnedFileSha256 -LiteralPath $p) -ne $file.Hash){throw 'Preview changed/missed owned file'}
    }
}
$tokens=$null;$parseErrors=$null
$ast=[Management.Automation.Language.Parser]::ParseFile($sourceUninstall,[ref]$tokens,[ref]$parseErrors)
if($parseErrors.Count){throw 'Uninstaller does not parse in this shell'}
$definitions=@($ast.FindAll({param($n) $n -is [Management.Automation.Language.FunctionDefinitionAst] -and $n.Name -eq 'Get-OwnedFileSha256'},$true))
if($definitions.Count -ne 1){throw 'Hash helper missing/ambiguous'}
. ([scriptblock]::Create($definitions[0].Extent.Text))
$scratch=Join-Path ([IO.Path]::GetTempPath()) ('lsl-hash-'+[guid]::NewGuid().ToString('N'))
[IO.Directory]::CreateDirectory($scratch) | Out-Null
$owned=$false
$before=Read-Config
try {
    $fixture=Join-Path $scratch 'abc.bin'
    [IO.File]::WriteAllBytes($fixture,[Text.Encoding]::ASCII.GetBytes('abc'))
    $expected='BA7816BF8F01CFEA414140DE5DAE2223B00361A396177A9CB410FF61F20015AD'
    if((Get-OwnedFileSha256 -LiteralPath $fixture) -ne $expected){throw 'Known SHA256 failed'}
    $previewHash=& { $WhatIfPreference=$true; Get-OwnedFileSha256 -LiteralPath $fixture }
    if($previewHash -ne $expected){throw 'Hash unavailable/incorrect under WhatIf'}
    $unreadable=$false
    try { Get-OwnedFileSha256 -LiteralPath (Join-Path $scratch 'missing.bin') | Out-Null }
    catch {if($_.Exception.Message -like 'Unable to compute SHA256*'){$unreadable=$true}else{throw}}
    if(-not $unreadable){throw 'Unavailable hash not distinguished'}
    Assert-Absent
    & $install -WhatIf
    Assert-Absent
    if((Read-Config) -cne $before){throw 'Install preview changed configuration'}
    & $install -Confirm:$false
    $owned=$true
    Start-Sleep -Seconds 5
    $ledger=Get-Content -LiteralPath $record -Raw | ConvertFrom-Json
    $raw=Read-Config
    $settings=$raw | ConvertFrom-Json
    $status=Read-Status
    if($settings.MonitoringRequested -ne $false -or $settings.DryRun -ne $true -or $settings.AllowLiveLogoff -eq $true -or $status.Configuration.Enabled -ne $false){throw 'Install did not remain disabled/dry-run'}
    $recordHash=Get-OwnedFileSha256 -LiteralPath $record
    & $uninstall -RestoreOriginalConfiguration -WhatIf
    Assert-Unchanged $ledger $recordHash $raw $status.RunId
    $original=[IO.File]::ReadAllBytes($uninstall)
    try {
        [IO.File]::AppendAllText($uninstall,"`r`n# CI deliberate hash mismatch fixture`r`n")
        $rejected=$false
        try { & $uninstall -RestoreOriginalConfiguration -WhatIf }
        catch {if($_.Exception.Message -like '*Modified installed file:*'){$rejected=$true}else{throw}}
        if(-not $rejected){throw 'Real mismatch was not rejected'}
    } finally { [IO.File]::WriteAllBytes($uninstall,$original) }
    Assert-Unchanged $ledger $recordHash $raw $status.RunId
    $oldRun=$status.RunId
    Restart-Service LockedSessionLogoff
    Start-Sleep -Seconds 5
    $status=Read-Status
    if($status.RunId -eq $oldRun -or $status.Configuration.Enabled -ne $false -or @($status.RetainedEvents | Where-Object RunId -eq $oldRun).Count -lt 1){throw 'Disabled restart/history regression'}
    & $uninstall -RestoreOriginalConfiguration -WhatIf
    Assert-Unchanged $ledger $recordHash $raw $status.RunId
    & $uninstall -RestoreOriginalConfiguration -Confirm:$false
    $owned=$false
    Assert-Absent
    if((Read-Config) -cne $before){throw 'Original configuration not restored'}
    Write-Host ("PASS PS{0}: hash/WhatIf/unavailable/mismatch/no-mutation/restart/history/uninstall/baseline; {1} owned files" -f $PSVersionTable.PSVersion,@($ledger.Files).Count)
} finally {
    if($owned -and (Test-Path -LiteralPath $uninstall) -and (Test-Path -LiteralPath $record)){ & $uninstall -RestoreOriginalConfiguration -Confirm:$false }
    if(Test-Path -LiteralPath $scratch){Remove-Item -LiteralPath $scratch -Recurse -Force}
}
