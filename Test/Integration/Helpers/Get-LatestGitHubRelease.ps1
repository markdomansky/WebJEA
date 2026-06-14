<#
.SYNOPSIS
    Builds the current WebJEA project locally for testing.

.DESCRIPTION
    This script builds the WebJEA project from the current source code using the shared
    build process (Build-WebJEA.ps1) that is also used by the GitHub Actions workflow.
    This ensures consistency between local builds and CI/CD builds.

.PARAMETER OutputPath
    Path where the build output will be created. Default: Test\Integration\build-output

.PARAMETER Version
    Optional specific version to use. If not specified, generates version from current UTC time.

.PARAMETER CreateZip
    If specified, creates a zip file of the release package.

.PARAMETER Configuration
    Build configuration. Default: Release

.EXAMPLE
    $build = & .\Build-LocalWebJEA.ps1

.EXAMPLE
    $build = & .\Build-LocalWebJEA.ps1 -CreateZip

.EXAMPLE
    $build = & .\Build-LocalWebJEA.ps1 -Version "2026.2.4.1200" -Configuration Debug

.OUTPUTS
    Hashtable with build information:
    - Version: The version number used
    - OutputPath: Path to the release package
    - ZipPath: Path to the zip file (if -CreateZip was specified)
#>
[CmdletBinding()]
param(
    [Parameter()]
    [string]$OutputPath,

    [Parameter()]
    [string]$Version,

    [Parameter()]
    [switch]$CreateZip,

    [Parameter()]
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release'
)

$ErrorActionPreference = 'Stop'

Write-Log '>> Get-LatestGitHubRelease'

# Find repository root (go up from Test\Integration\Helpers to repo root)
$repoRoot = Split-Path -Parent (Split-Path -Parent (Split-Path -Parent $PSScriptRoot))
$buildScript = Join-Path $repoRoot "Build\Build-WebJEA.ps1"

if (-not (Test-Path $buildScript)) {
    throw "Build script not found at: $buildScript"
}

Write-Log "Building WebJEA from current source..."
Write-Log "Repository root: $repoRoot"

# Set default output path if not specified
if (-not $OutputPath) {
    $OutputPath = Join-Path $repoRoot "Test\Integration\build-output"
}

# Build parameters
$buildParams = @{
    OutputPath    = $OutputPath
    Configuration = $Configuration
}

if ($Version) {
    $buildParams.Version = $Version
}

if ($CreateZip) {
    $buildParams.CreateZip = $true
}

Write-Log "Invoking shared build script..."

# Execute the shared build script
$buildResult = & $buildScript @buildParams

if (-not $buildResult) {
    throw "Build script did not return a result"
}

Write-Log "Build completed successfully"
Write-Log "Version: $($buildResult.Version)"
Write-Log "Package: $($buildResult.OutputPath)"

if ($buildResult.ZipPath) {
    Write-Log "Archive: $($buildResult.ZipPath)"
}

return $buildResult
