<#
.SYNOPSIS
    Ensures a VM is running.

.DESCRIPTION
    Starts the specified VM if not already running, waits for heartbeat confirmation,
    then optionally waits an additional delay for services to initialize.

.PARAMETER VMName
    Name of the VM to start.

.PARAMETER StartupDelay
    Seconds to wait after heartbeat is OK for services to initialize. Default: 0 (no delay).

.PARAMETER TimeoutSeconds
    Maximum time to wait for the VM to reach running state. Default: 300.

.EXAMPLE
    & .\StartVM.ps1 -VMName 'DC01' -StartupDelay 60

.EXAMPLE
    & .\StartVM.ps1 -VMName 'WebServer'

.OUTPUTS
    None
#>
#Requires -Modules Hyper-V

[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string]$VMName,

    [Parameter()]
    [int]$StartupDelay = 0,

    [Parameter()]
    [int]$TimeoutSeconds = 300
)

Write-Log ">> StartVM: '$VMName'"

$vm = Get-VM -Name $VMName -ErrorAction Stop

if ($vm.State -eq 'Running') {
    Write-Log "VM '$VMName' is already running" -Level Success
    return
}

Write-Log "Starting VM '$VMName'..."
Start-VM -Name $VMName

& $psscriptroot\WaitVMReady.ps1 -VMName $VMName -TimeoutSeconds $TimeoutSeconds -startupDelay $StartupDelay
