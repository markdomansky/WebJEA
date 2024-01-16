$ErrorActionPreference = 'Stop'

Import-Module ..\psake\src\psake.psm1

Invoke-psake -buildfile $psscriptroot\build.psake.ps1