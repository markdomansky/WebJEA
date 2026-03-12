<#
.SYNOPSIS
    Installs Windows Updates on a VM via PowerShell Direct session.

.DESCRIPTION
    Executes Windows Update installation on a remote VM through an existing
    PowerShell Direct session. Installs PSWindowsUpdate module if not present.

.PARAMETER Session
    An active PSSession to the target VM.

.PARAMETER Categories
    Array of update categories to install. Default: Security, Critical

.PARAMETER AutoReboot
    Whether to allow automatic reboot after updates. Default: $true

.PARAMETER TimeoutMinutes
    Maximum time to wait for updates to complete. Default: 120

.EXAMPLE
    $session = New-PSSession -VMName 'WebServer' -Credential $cred
    $result = & .\Invoke-WindowsUpdateOnVM.ps1 -Session $session

.OUTPUTS
    Hashtable with:
    - Success: Boolean indicating if update completed
    - UpdatesInstalled: Number of updates installed
    - UpdatesAvailable: Number of updates found
    - RebootRequired: Boolean indicating if reboot is needed
    - Details: Array of update details (KB, Title, Result)
    - WillReboot: Boolean if reboot was requested
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [System.Management.Automation.Runspaces.PSSession]$Session,

    [Parameter()]
    [string[]]$Categories = @('Security', 'Critical'),

    [Parameter()]
    [bool]$AutoReboot = $true,

    [Parameter()]
    [int]$TimeoutMinutes = 120
)

Write-Log '>> Invoke-WindowsUpdateOnVM'

Write-Log "Installing Windows Updates on VM (Categories: $($Categories -join ', '))..."

$updateScript = {
    param(
        [string[]]$Categories,
        [bool]$AutoReboot,
        [int]$TimeoutMinutes
    )

    $ErrorActionPreference = 'Stop'
    $results = @{
        Success          = $false
        UpdatesInstalled = 0
        UpdatesAvailable = 0
        RebootRequired   = $false
        Details          = @()
        Error            = $null
    }

    try {
        if (-not (Get-Module -ListAvailable -Name PSWindowsUpdate)) {
            Write-Output 'Installing PSWindowsUpdate module...'
            Install-PackageProvider -Name NuGet -MinimumVersion 2.8.5.201 -Force -Scope AllUsers | Out-Null
            Install-Module -Name PSWindowsUpdate -Force -Scope AllUsers -AllowClobber
        }

        Import-Module PSWindowsUpdate -Force

        Write-Output 'Scanning for available updates...'
        $updates = Get-WindowsUpdate -Category $Categories -AcceptAll -IgnoreReboot

        $results.UpdatesAvailable = ($updates | Measure-Object).Count
        Write-Output "Found $($results.UpdatesAvailable) updates available"

        if ($results.UpdatesAvailable -eq 0) {
            $results.Success = $true
            Write-Output 'No updates available'
            return $results
        }

        Write-Output 'Installing updates...'
        $installResult = Install-WindowsUpdate -Category $Categories -AcceptAll -IgnoreReboot -Confirm:$false

        $results.UpdatesInstalled = ($installResult | Where-Object { $_.Result -eq 'Installed' } | Measure-Object).Count
        $results.Details = $installResult | Select-Object KB, Title, Result

        Write-Output "Installed $($results.UpdatesInstalled) updates"

        $rebootPending = (Get-ItemProperty -Path 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\WindowsUpdate\Auto Update\RebootRequired' -ErrorAction SilentlyContinue) -or
        (Get-ItemProperty -Path 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Component Based Servicing\RebootPending' -ErrorAction SilentlyContinue)

        $results.RebootRequired = $null -ne $rebootPending
        $results.Success = $true

        if ($results.RebootRequired -and $AutoReboot) {
            Write-Output 'Reboot required. Initiating restart...'
            $results.WillReboot = $true
        }

        return $results
    }
    catch {
        $results.Error = $_.Exception.Message
        return $results
    }
}

$result = Invoke-Command -Session $Session -ScriptBlock $updateScript -ArgumentList @(
    $Categories,
    $AutoReboot,
    $TimeoutMinutes
)

if (-not $result.Success) {
    throw "Windows Update failed: $($result.Error)"
}

Write-Log 'Windows Update Results:'
Write-Log "  Updates Available: $($result.UpdatesAvailable)"
Write-Log "  Updates Installed: $($result.UpdatesInstalled)"
Write-Log "  Reboot Required: $($result.RebootRequired)"

if ($result.Details) {
    $result.Details | ForEach-Object {
        Write-Log "  - $($_.KB): $($_.Title) [$($_.Result)]"
    }
}

return $result
