<#
.SYNOPSIS
    Shuts down VMs that were not running at the start of the script.

.DESCRIPTION
    Uses the initial states hashtable from Start-RequiredVMs.ps1 to determine
    which VMs should be shut down. Attempts graceful shutdown via PowerShell Direct
    if credentials are provided, otherwise forces shutdown.

.PARAMETER InitialStates
    Hashtable from Start-RequiredVMs.ps1 with initial VM states.

.PARAMETER DomainControllerVMName
    Name of the Domain Controller VM.

.PARAMETER WebServerVMName
    Name of the Web Server VM.

.PARAMETER Credential
    Optional credential for graceful shutdown via PowerShell Direct.

.EXAMPLE
    & .\Stop-VMsIfNotRunningInitially.ps1 -InitialStates $states -DomainControllerVMName 'DC01' -WebServerVMName 'WebServer'
#>
#Requires -Modules Hyper-V

[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [hashtable]$InitialStates,

    [Parameter(Mandatory)]
    [string]$DomainControllerVMName,

    [Parameter(Mandatory)]
    [string]$WebServerVMName,

    [Parameter()]
    [PSCredential]$Credential
)

Write-Log '>> Stop-VMsIfNotRunningInitially'

# Shut down web server first
if ($InitialStates.WebServer -ne 'Running') {
    Write-Log 'Web Server was not running initially. Shutting down...'
    $webVM = Get-VM -Name $WebServerVMName -ErrorAction SilentlyContinue

    if ($webVM -and $webVM.State -eq 'Running') {
        try {
            if ($Credential) {
                $session = New-PSSession -VMName $WebServerVMName -Credential $Credential -ErrorAction SilentlyContinue
                if ($session) {
                    Invoke-Command -Session $session -ScriptBlock { Stop-Computer -Force } -ErrorAction SilentlyContinue
                    Remove-PSSession -Session $session -ErrorAction SilentlyContinue
                    Start-Sleep -Seconds 30
                }
            }

            $webVM = Get-VM -Name $WebServerVMName
            if ($webVM.State -ne 'Off') {
                Stop-VM -Name $WebServerVMName -Force -TurnOff
            }
            Write-Log 'Web Server VM shut down' -Level Success
        }
        catch {
            Write-Log "Failed to shut down Web Server VM: $_" -Level Warning
        }
    }
}

# Shut down DC
if ($InitialStates.DomainController -ne 'Running') {
    Write-Log 'Domain Controller was not running initially. Shutting down...'
    $dcVM = Get-VM -Name $DomainControllerVMName -ErrorAction SilentlyContinue

    if ($dcVM -and $dcVM.State -eq 'Running') {
        try {
            Stop-VM -Name $DomainControllerVMName -Force -TurnOff
            Write-Log 'Domain Controller VM shut down' -Level Success
        }
        catch {
            Write-Log "Failed to shut down Domain Controller VM: $_" -Level Warning
        }
    }
}
