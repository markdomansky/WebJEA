<#
.SYNOPSIS
    Waits for a VM to be running and have a healthy heartbeat.

.DESCRIPTION
    Polls the VM state and heartbeat integration service until the VM is ready
    or the timeout is reached.

.PARAMETER VMName
    Name of the VM to wait for.

.PARAMETER TimeoutSeconds
    Maximum time to wait. Default: 300 seconds.

.EXAMPLE
    & .\WaitVMReady.ps1 -VMName 'WebServer' -TimeoutSeconds 300

.OUTPUTS
    Returns $true if VM is ready, throws on timeout.
#>
#Requires -Modules Hyper-V

[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string]$VMName,

    [Parameter()]
    [int]$TimeoutSeconds = 300,

    [Parameter()]
    [int]$StartupDelay = 0
)

Write-Log 'WaitVMReady'

Write-Log "  Waiting for VM '$VMName' to be ready..."
$stopwatch = [System.Diagnostics.Stopwatch]::StartNew()

$vm = Get-VM -Name $VMName -ErrorAction SilentlyContinue
$heartbeat = $null #will get later
while ($vm.State -ne 'Running' -or $heartbeat.PrimaryStatusDescription -ne 'OK')
{
    Start-Sleep -Seconds 5
    $vm = Get-VM -Name $VMName -ErrorAction SilentlyContinue

    $heartbeat = $vm | Get-VMIntegrationService -Name Heartbeat -ErrorAction SilentlyContinue
    if ($vm.State -eq 'Running' -and $heartbeat.PrimaryStatusDescription -eq 'OK') {
        Write-Log "  VM '$VMName' is ready (heartbeat OK)" -Level Success

        if ($StartupDelay -gt 0)
        {
            Write-Log "  Waiting $StartupDelay seconds for services to initialize..."
            Start-Sleep -Seconds $StartupDelay
        }

        break
    }

    if ($stopwatch.Elapsed.TotalSeconds -ge $TimeoutSeconds)
    {
        throw "Timeout waiting for VM '$VMName' to start after $TimeoutSeconds seconds"
    }
}
