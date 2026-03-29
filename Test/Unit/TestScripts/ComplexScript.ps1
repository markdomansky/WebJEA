<#
.SYNOPSIS
Complex script.
.DESCRIPTION
This script has nested and escaped characters.
.EXAMPLE
Example usage.
.PARAMETER Param1
Description for Param1.
#>
param (
    [ValidatePattern("[a-zA-Z]")]
    [string]$Param1,
    [int]$Param2 = 42
)
Write-Output "Complex script."