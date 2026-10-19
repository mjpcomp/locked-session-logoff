#Requires -Version 5.1
#Requires -RunAsAdministrator
<# Updated: 2026-10-19 13:32 America/Los_Angeles
Path: packaging/Uninstall.ps1; Owner: maintainers; Permissions: public read/maintainer write
Purpose: preview-safe integrity checks and baseline restoration; Arguments: RestoreOriginalConfiguration, WhatIf, Confirm
Usage: installed script; read-only hashing runs under WhatIf; all removal remains behind ShouldProcess #>
[CmdletBinding(SupportsShouldProcess=$true, ConfirmImpact='High')] param([switch]$RestoreOriginalConfiguration)
$ErrorActionPreference='Stop'
function Get-OwnedFileSha256 {
    [CmdletBinding()] param([Parameter(Mandatory=$true)][string]$LiteralPath)
    $stream=$null; $sha=$null
    try {
        $sha=[Security.Cryptography.SHA256]::Create()
        $stream=[IO.File]::Open($LiteralPath,[IO.FileMode]::Open,[IO.FileAccess]::Read,[IO.FileShare]::Read)
        $digest=[BitConverter]::ToString($sha.ComputeHash($stream)).Replace('-','')
        if($digest -notmatch '^[0-9A-F]{64}$'){throw 'Invalid SHA256 result'}
        return $digest
    } catch { throw "Unable to compute SHA256 for '$LiteralPath' ($($_.Exception.GetType().Name)); integrity not established." }
    finally { if($stream){$stream.Dispose()}; if($sha){$sha.Dispose()} }
}
$base=Join-Path $env:ProgramFiles 'LockedSessionLogoff'
$data=Join-Path $env:ProgramData 'LockedSessionLogoff'
$record=Join-Path $data 'install.json'
$root=[Microsoft.Win32.RegistryKey]::OpenBaseKey([Microsoft.Win32.RegistryHive]::LocalMachine,[Microsoft.Win32.RegistryView]::Registry64)
try {
    if(-not [Environment]::Is64BitProcess){throw 'Use native 64-bit PowerShell.'}
    $ledger=Get-Content $record -Raw | ConvertFrom-Json
    if($ledger.Protocol -ne 1 -or $ledger.Base -ne $base){throw 'Unexpected install record.'}
    foreach($p in @($base,$data)){if((Get-Item $p -Force).Attributes -band [IO.FileAttributes]::ReparsePoint){throw 'Reparse path rejected.'}}
    # Only this read-only query/import suppresses inherited WhatIf, not the removal scope.
    $svc=& { $WhatIfPreference=$false; Get-CimInstance Win32_Service -Filter "Name='LockedSessionLogoff'" -ErrorAction Stop }
    $exe=Join-Path $base 'service\LockedSessionLogoff.Service.exe'
    if($svc -and $svc.PathName -ne ('"'+$exe+'"')){throw 'Service path changed; refusing to stop another service.'}
    foreach($file in $ledger.Files){
        $full=[IO.Path]::GetFullPath((Join-Path $base $file.Relative))
        if(-not $full.StartsWith($base+'\',[StringComparison]::OrdinalIgnoreCase)){throw 'Invalid file manifest path.'}
        if($file.Hash -isnot [string] -or $file.Hash -notmatch '^[0-9A-Fa-f]{64}$'){throw 'Invalid SHA256 in installation record; recovery record retained.'}
        if(Test-Path $full){
            if((Get-Item $full -Force).Attributes -band [IO.FileAttributes]::ReparsePoint){throw 'Reparse file rejected.'}
            $actual=Get-OwnedFileSha256 -LiteralPath $full
            if($actual -ne $file.Hash){throw "Modified installed file: $full. Recovery record retained; restore/review before uninstall."}
        }
    }
    $key=$root.OpenSubKey('SOFTWARE\LockedSessionLogoff');$current=if($key){$key.GetValue('ConfigurationV1',$null,[Microsoft.Win32.RegistryValueOptions]::DoNotExpandEnvironmentNames)}else{$null};if($key){$key.Dispose()}
    $conflict=($null -ne $current -and $current -ne $ledger.InstalledConfig)
    Write-Host "Will stop/delete owned service, remove manifest-owned binaries and retained runtime diagnostics, and restore original ConfigurationV1 (or remove it if originally absent). Recovery record: $record"
    Write-Host 'Completed actions cannot be undone. No OS cleanup policy, maintenance tasks or persistent disk log files are created by this build.'
    if($conflict -and -not $RestoreOriginalConfiguration){throw 'Configuration changed after install. Close the GUI. Review original/current settings; use -RestoreOriginalConfiguration only to explicitly discard later edits and restore the captured baseline.'}
    if(-not $PSCmdlet.ShouldProcess($base,'Uninstall owned components and restore captured configuration baseline')){return}
    if($svc){
        if((Get-Service LockedSessionLogoff).Status -ne 'Stopped'){Stop-Service LockedSessionLogoff;(Get-Service LockedSessionLogoff).WaitForStatus('Stopped',[TimeSpan]::FromSeconds(20))}
        & sc.exe delete LockedSessionLogoff | Out-Host;if($LASTEXITCODE -ne 0){throw 'Service deletion failed.'}
    }
    $key=$root.CreateSubKey('SOFTWARE\LockedSessionLogoff')
    if($ledger.HadConfig){$key.SetValue('ConfigurationV1',[string]$ledger.OriginalConfig,[Microsoft.Win32.RegistryValueKind]::String)}else{$key.DeleteValue('ConfigurationV1',$false)}
    $empty=($key.ValueCount -eq 0 -and $key.SubKeyCount -eq 0);$key.Dispose();if($empty){$root.DeleteSubKey('SOFTWARE\LockedSessionLogoff',$false)}
    $runtime=$root.OpenSubKey('SOFTWARE\LockedSessionLogoff.Runtime',$true)
    if($runtime){$runtime.DeleteValue('StatusV1',$false);$empty=($runtime.ValueCount -eq 0 -and $runtime.SubKeyCount -eq 0);$runtime.Dispose();if($empty){$root.DeleteSubKey('SOFTWARE\LockedSessionLogoff.Runtime',$false)}}
    foreach($file in $ledger.Files){$full=Join-Path $base $file.Relative;if(Test-Path $full){Remove-Item -LiteralPath $full -Force}}
    Get-ChildItem $base -Directory -Recurse | Sort-Object { $_.FullName.Length } -Descending | ForEach-Object {if(-not (Get-ChildItem $_.FullName -Force)){Remove-Item $_.FullName}}
    if(Get-ChildItem $base -Force){throw 'Unlisted files remain; review them. Recovery record retained.'}
    Remove-Item $base
    Remove-Item $record -Force
    if(-not (Get-ChildItem $data -Force)){Remove-Item $data}
    Write-Host 'Uninstall completed; original configuration baseline restored.'
} catch { Write-Error "Uninstall incomplete: $($_.Exception.Message). Preserve the recovery record; resolve conflict and retry.";throw }
finally {$root.Dispose()}
