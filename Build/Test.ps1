param([string]$computer, [switch]$JustDeploy)

if (-not $JustDeploy)
{
    Write-Host 'Creating Session'
    $sess = New-PSSession $computer
    Write-Host "Removing $computer\c:\source"
    Invoke-Command $sess { Remove-Item 'c:\source' -Recurse -Force -ea 0 }
    Write-Host "Copying Latest Release to $computer\c:\source"
    Copy-Item -Path $psscriptroot\release -Destination 'c:\source' -ToSession $sess -Recurse
}
Write-Host "Overwriting $computer\c:\source\Deploy.ps1 with Test.Deploy.ps1"
Copy-Item -Path "$psscriptroot\Test.Deploy.ps1" -Destination 'c:\source\Deploy.ps1' -ToSession $sess -Recurse
Write-Host 'Running DSC'
Invoke-Command $sess { &  c:\source\deploy.ps1 }

Start "https://$computer.domain1.local/webjea"