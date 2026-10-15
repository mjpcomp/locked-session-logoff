#Requires -Version 5.1
#Requires -RunAsAdministrator
<# Created: 2026-10-15 11:21 America/Los_Angeles
Path: packaging/Install.ps1; Owner: maintainers; Permissions: public read/maintainer write
Purpose: first-install only, disabled monitoring; Arguments: WhatIf/Confirm
Usage: run extracted package Install.ps1; refuses existing service/directories #>
[CmdletBinding(SupportsShouldProcess=$true, ConfirmImpact='High')] param()
$ErrorActionPreference='Stop'
$base=Join-Path $env:ProgramFiles 'LockedSessionLogoff'
$data=Join-Path $env:ProgramData 'LockedSessionLogoff'
$root=[Microsoft.Win32.RegistryKey]::OpenBaseKey([Microsoft.Win32.RegistryHive]::LocalMachine,[Microsoft.Win32.RegistryView]::Registry64)
try {
    if (-not [Environment]::Is64BitProcess) { throw 'Use native 64-bit PowerShell.' }
    $meta=Get-Content (Join-Path $PSScriptRoot 'package.json') -Raw | ConvertFrom-Json
    $native=if($env:PROCESSOR_ARCHITEW6432){$env:PROCESSOR_ARCHITEW6432}else{$env:PROCESSOR_ARCHITECTURE}
    $expected=if($native -eq 'ARM64'){'win-arm64'}elseif($native -eq 'AMD64'){'win-x64'}else{throw 'Unsupported architecture'}
    if($meta.Runtime -ne $expected){throw 'Package architecture does not match native OS.'}
    if((Get-Service LockedSessionLogoff -ErrorAction SilentlyContinue) -or (Test-Path $base) -or (Test-Path $data)){throw 'Existing service/install directory: upgrade is not supported by this first installer.'}
    $runtime=$root.OpenSubKey('SOFTWARE\LockedSessionLogoff.Runtime'); if($runtime){$runtime.Dispose();throw 'Runtime key already exists; refusing to overwrite.'}
    $key=$root.OpenSubKey('SOFTWARE\LockedSessionLogoff'); $original=$null; $had=$false
    if($key){
        if($key.GetValueNames() -contains 'ConfigurationV1'){
            if($key.GetValueKind('ConfigurationV1') -ne [Microsoft.Win32.RegistryValueKind]::String){throw 'Unsupported existing configuration type.'}
            $original=[string]$key.GetValue('ConfigurationV1'); $had=$true
        }; $key.Dispose()
    }
    $config=[ordered]@{ApplicationId='LockedSessionLogoff';SchemaVersion=1;MonitoringRequested=$false;DryRun=$true;Debug=$false;LockedTimeoutSeconds=3600;ExcludedSids=@()} | ConvertTo-Json -Compress
    Write-Host "Will install SYSTEM service, binaries at $base, protected rollback record at $data, and disabled dry-run configuration. No maintenance/reporting is enabled."
    if(-not $PSCmdlet.ShouldProcess($base,'Install disabled dry-run service and preserve previous configuration')){return}
    foreach($path in @($base,$data)){
        New-Item -ItemType Directory -Path $path | Out-Null
        $acl=New-Object System.Security.AccessControl.DirectorySecurity
        $acl.SetSecurityDescriptorSddlForm('O:BAG:BAD:P(A;OICI;FA;;;SY)(A;OICI;FA;;;BA)')
        Set-Acl -LiteralPath $path -AclObject $acl
        if((Get-Item $path -Force).Attributes -band [IO.FileAttributes]::ReparsePoint){throw 'Reparse install path rejected.'}
        if(Get-ChildItem $path -Force){throw 'Install directory not empty.'}
    }
    Copy-Item (Join-Path $PSScriptRoot 'service') $base -Recurse
    Copy-Item (Join-Path $PSScriptRoot 'admin') $base -Recurse
    Copy-Item (Join-Path $PSScriptRoot 'Uninstall.ps1') $base
    $files=@(Get-ChildItem $base -File -Recurse | ForEach-Object {
        if($_.Attributes -band [IO.FileAttributes]::ReparsePoint){throw 'Reparse file rejected.'}
        [ordered]@{Relative=$_.FullName.Substring($base.Length+1);Hash=(Get-FileHash $_.FullName -Algorithm SHA256).Hash}
    })
    $ledger=[ordered]@{Protocol=1;Base=$base;HadConfig=$had;OriginalConfig=$original;InstalledConfig=$config;Files=$files;Commit=$meta.Commit;Phase='Prepared'}
    $ledger | ConvertTo-Json -Depth 6 | Set-Content (Join-Path $data 'install.json') -Encoding UTF8
    $key=$root.CreateSubKey('SOFTWARE\LockedSessionLogoff');$key.SetValue('ConfigurationV1',$config,[Microsoft.Win32.RegistryValueKind]::String);$key.Dispose()
    $key=$root.CreateSubKey('SOFTWARE\LockedSessionLogoff.Runtime');$key.Dispose()
    $exe=Join-Path $base 'service\LockedSessionLogoff.Service.exe'
    New-Service -Name LockedSessionLogoff -DisplayName 'Locked Session Logoff (dry-run)' -BinaryPathName ('"'+$exe+'"') -StartupType Automatic | Out-Null
    Start-Service LockedSessionLogoff
    (Get-Service LockedSessionLogoff).WaitForStatus('Running',[TimeSpan]::FromSeconds(20))
    $ledger.Phase='Installed';$ledger | ConvertTo-Json -Depth 6 | Set-Content (Join-Path $data 'install.json') -Encoding UTF8
    Write-Host "Installed. Open $base\admin\LockedSessionLogoff.Admin.exe. Monitoring is disabled. Rollback: $base\Uninstall.ps1"
} catch {
    Write-Error "Installation incomplete: $($_.Exception.Message). Preserve $data and use the recorded uninstall/recovery procedure; do not blindly rerun or delete directories."
    throw
} finally { $root.Dispose() }
