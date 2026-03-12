<#
.SYNOPSIS
    Deploys WebJEA to a Hyper-V VM using PowerShell Direct.

.DESCRIPTION
    Entry point that invokes psake tasks defined in integration.psake.ps1.
    Deploys WebJEA to the target VM using a locally built package by default.
    For integration testing, run Build-WebJEA.ps1 first to create the package.

    Alternatively, use -UseGitHubBuild to download the latest release from GitHub instead.

.PARAMETER ConfigPath
    Path to the configuration JSON file. Defaults to config.json in the script directory.

.PARAMETER UseGitHubBuild
    Download and deploy from GitHub instead of using the local build.
    By default, the script uses the local build created by Build-WebJEA.ps1.

.EXAMPLE
    .\Build-WebJEA.ps1
    .\Deploy-WebJEA.ps1

.EXAMPLE
    .\Deploy-WebJEA.ps1 -UseGitHubBuild

.EXAMPLE
    .\Deploy-WebJEA.ps1 -ConfigPath .\custom-config.json

.NOTES
    Requires Hyper-V PowerShell module and psake module.
    Must be run on the Hyper-V host with appropriate permissions.
    The target VM must support PowerShell Direct (Windows 10/Server 2016+).
#>
#Requires -Version 5.1
#Requires -Modules "Hyper-V", "psake"

[CmdletBinding()]
param(
    [Parameter()]
    [ValidateScript({ Test-Path $_ -PathType Leaf })]
    [string]$ConfigPath = ("$PSScriptRoot\config.json"),

    [Parameter()]
    [switch]$UseGitHubBuild,

    [Parameter()]
    [switch]$SkipBuild
)

$ErrorActionPreference = 'Stop'

if (-not (Get-Module -ListAvailable -Name psake)) {
    Write-Host 'psake module not found. Installing from PSGallery...' -ForegroundColor Yellow
    Install-Module -Name psake -Scope CurrentUser -Force
}
Import-Module psake -ErrorAction Stop

$psakeParams = @{
    buildFile  = "$PSScriptRoot\integration.psake.ps1"
    taskList   = @('Deploy')
    properties = @{
        configPath     = $ConfigPath
        helpersPath    = Join-Path $PSScriptRoot 'Helpers'
        useGitHubBuild = [bool]$UseGitHubBuild
        skipBuild      = [bool]$SkipBuild
    }
    nologo     = $true
}

Invoke-psake @psakeParams

if (-not $psake.build_success) {
    exit 1
}

exit 0
