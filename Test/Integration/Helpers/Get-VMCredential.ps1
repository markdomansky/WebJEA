<#
.SYNOPSIS
    Retrieves credentials for VM access from various sources.

.DESCRIPTION
    Attempts to retrieve credentials in the following order:
    1. From an exported credential file (CredentialFilePath)
    2. From SecretManagement module (PasswordSecretName)
    3. Interactive prompt (if not in non-interactive mode)

.PARAMETER CredentialConfig
    A hashtable containing credential configuration with keys:
    - CredentialFilePath: Path to an exported credential XML file
    - PasswordSecretName: Name of a secret in SecretManagement
    - Domain: Domain name for the user
    - Username: Username without domain

.PARAMETER NonInteractive
    If specified, will throw an error instead of prompting for credentials.

.EXAMPLE
    $cred = & .\Get-VMCredential.ps1 -CredentialConfig @{
        Domain = 'CONTOSO'
        Username = 'admin'
        PasswordSecretName = 'VMPassword'
    }

.OUTPUTS
    PSCredential
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [object]$CredentialConfig,

    [Parameter()]
    [switch]$NonInteractive
)

Write-Log ">> Get-VMCredential"

if ($CredentialConfig.CredentialFilePath -and (Test-Path $CredentialConfig.CredentialFilePath)) {
    Write-Log "Loading credentials from file: $($CredentialConfig.CredentialFilePath)"
    return Import-Clixml -Path $CredentialConfig.CredentialFilePath
}

if ($CredentialConfig.PasswordSecretName) {
    try {
        Write-Log "Attempting to retrieve secret: $($CredentialConfig.PasswordSecretName)"
        $secret = Get-Secret -Name $CredentialConfig.PasswordSecretName -ErrorAction Stop
        $username = "$($CredentialConfig.Domain)\$($CredentialConfig.Username)"
        return [PSCredential]::new($username, $secret)
    }
    catch {
        Write-Log 'SecretManagement module not available or secret not found.' -Level Warning
    }
}

if ($NonInteractive) {
    throw 'Unable to retrieve credentials and running in non-interactive mode. Configure CredentialFilePath or PasswordSecretName.'
}

$username = "$($CredentialConfig.Domain)\$($CredentialConfig.Username)"
return Get-Credential -UserName $username -Message 'Enter credentials for VM access'
