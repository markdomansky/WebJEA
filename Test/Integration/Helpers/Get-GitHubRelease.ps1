<#
.SYNOPSIS
    Fetches release information from a GitHub repository.

.DESCRIPTION
    Uses the GitHub API to retrieve release information and find a matching
    downloadable asset based on a pattern. This is used for deployment scenarios
    where you want to fetch a pre-built release from GitHub.

.PARAMETER Owner
    The GitHub repository owner/organization.

.PARAMETER Repository
    The GitHub repository name.

.PARAMETER Tag
    Optional specific release tag to fetch. If not specified, fetches the latest release.

.PARAMETER AssetPattern
    Wildcard pattern to match the asset filename. Default: *.zip

.EXAMPLE
    $release = & .\Get-GitHubRelease.ps1 -Owner 'markdomansky' -Repository 'WebJEA'

.EXAMPLE
    $release = & .\Get-GitHubRelease.ps1 -Owner 'markdomansky' -Repository 'WebJEA' -Tag 'v1.0.0'

.OUTPUTS
    Hashtable with release information:
    - TagName: Release tag
    - ReleaseName: Release name
    - AssetName: Matched asset filename
    - DownloadUrl: Direct download URL for the asset
    - Size: Asset size in bytes
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string]$Owner,

    [Parameter(Mandatory)]
    [string]$Repository,

    [Parameter()]
    [string]$Tag,

    [Parameter()]
    [string]$AssetPattern = '*.zip'
)

$ErrorActionPreference = 'Stop'

Write-Log '>> Get-GitHubRelease'

$baseUri = "https://api.github.com/repos/$Owner/$Repository/releases"

if ($Tag) {
    $releaseUri = "$baseUri/tags/$Tag"
}
else {
    $releaseUri = "$baseUri/latest"
}

Write-Log "Fetching release information from: $releaseUri"

$headers = @{
    'Accept'     = 'application/vnd.github.v3+json'
    'User-Agent' = 'PowerShell-WebJEA-Deployer'
}

try {
    $release = Invoke-RestMethod -Uri $releaseUri -Headers $headers -Method Get
}
catch {
    throw "Failed to fetch release from GitHub: $_"
}

Write-Log "Found release: $($release.tag_name) - $($release.name)"

$matchingAsset = $release.assets | Where-Object {
    $_.name -like $AssetPattern
} | Select-Object -First 1

if (-not $matchingAsset) {
    throw "No asset matching pattern '$AssetPattern' found in release $($release.tag_name)"
}

Write-Log "Found matching asset: $($matchingAsset.name)"

return @{
    TagName     = $release.tag_name
    ReleaseName = $release.name
    AssetName   = $matchingAsset.name
    DownloadUrl = $matchingAsset.browser_download_url
    Size        = $matchingAsset.size
}
