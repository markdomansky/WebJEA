<#
.SYNOPSIS
    Manages VM snapshots and applies Windows Updates for nightly maintenance.

.DESCRIPTION
    Entry point that invokes psake tasks defined in integration.psake.ps1.
    Performs nightly maintenance tasks for the WebJEA test environment:
    1. Reverts the VM to a clean baseline snapshot
    2. Applies Windows Updates (Security and Critical by default)
    3. Creates a new snapshot after updates are applied
    4. Removes the old snapshot

    Designed to be run via Task Scheduler on a nightly basis.

.PARAMETER ConfigPath
    Path to the configuration JSON file. Defaults to config.json in the script directory.

.PARAMETER SkipWindowsUpdate
    Skip Windows Update installation (useful for quick snapshot refresh).

.PARAMETER Force
    Skip confirmation prompts and use non-interactive credential retrieval.

.EXAMPLE
    .\Update-VMSnapshot.ps1

.EXAMPLE
    .\Update-VMSnapshot.ps1 -SkipWindowsUpdate

.EXAMPLE
    .\Update-VMSnapshot.ps1 -ConfigPath .\custom-config.json -Force

.NOTES
    Requires Hyper-V PowerShell module and psake module.
    Requires PSWindowsUpdate module on the target VM for Windows Update operations.
    Must be run on the Hyper-V host with Administrator privileges.
#>
#Requires -Version 5.1
#Requires -Modules Hyper-V
#Requires -RunAsAdministrator

[CmdletBinding()]
param(
    [Parameter()]
    [ValidateScript({ Test-Path $_ -PathType Leaf })]
    [string]$ConfigPath = "$PSScriptRoot\config.json",

    [Parameter()]
    [switch]$SkipWindowsUpdate
)

$ErrorActionPreference = 'Stop'

if (-not (Test-Path $ConfigPath -PathType Leaf)) { throw "Configuration file not found at path: $ConfigPath" }

if (-not (Get-Module -ListAvailable -Name psake)) {
    Write-Host 'psake module not found. Installing from PSGallery...' -ForegroundColor Yellow
    Install-Module -Name psake -Scope CurrentUser -Force
}
Import-Module psake -ErrorAction Stop

$psakeParams = @{
    buildFile  = "$PSScriptRoot\integration.psake.ps1"
    taskList   = @('SnapshotMaintenance')
    properties = @{
        configPath        = $ConfigPath
        helpersPath       = if ($PSScriptRoot) { Join-Path $PSScriptRoot 'Helpers' } else { Join-Path (Get-Location) 'Helpers' }
        skipWindowsUpdate = [bool]$SkipWindowsUpdate
    }
    nologo     = $true
}

Invoke-psake @psakeParams

if (-not $psake.build_success) {
    exit 1
}

exit 0