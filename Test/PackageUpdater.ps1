<#
.SYNOPSIS
    Updates NuGet packages and DOMPurify to the latest stable versions at least MinAgeHours old.

.DESCRIPTION
    Scans each package in WebJEA\packages.config and queries the NuGet v3 API for the latest
    stable version that is at least MinAgeHours old. Uses nuget.exe to apply updates, then
    patches Web.config appSettings (jQueryVersion, jQueryUIVersion) and assembly binding
    redirects to match the new package versions.

    Also queries the NPM registry for the latest stable DOMPurify version that is at least
    MinAgeHours old, downloads purify.min.js from unpkg, and removes the DownloadDomPurify
    MSBuild target from WebJEA.vbproj so the build no longer fetches an unversioned copy.

    Writes a git commit message to the output stream summarising all changes made.

.PARAMETER SolutionPath
    Path to the WebJEA.sln solution file. Defaults to ..\WebJEA\WebJEA.sln relative to this
    script's directory.

.PARAMETER MinAgeHours
    Minimum age in hours a package version must have before it qualifies for update. Default: 72.

.OUTPUTS
    [string] A git commit message summarising applied updates, or 'No package updates found.'

.EXAMPLE
    .\PackageUpdater.ps1

    Updates all packages using defaults (72-hour minimum age, auto-detected solution path).

.EXAMPLE
    .\PackageUpdater.ps1 -MinAgeHours 999999 -WhatIf

    Dry-run: shows what would be updated without modifying any files.

.NOTES
    Requires nuget.exe in PATH or a common install location.
    Requires internet access to query api.nuget.org, registry.npmjs.org, and unpkg.com.
#>
#Requires -Version 5.1

[CmdletBinding(SupportsShouldProcess)]
param (
    [Parameter()]
    [string]$SolutionPath = (Join-Path $PSScriptRoot '..\WebJEA\WebJEA.sln'),

    [Parameter()]
    [ValidateRange(0, 2147483647)]
    [int]$MinAgeHours = 72
)

begin
{

    $script:PSDefaultParameterValues = @{ 'Write-Log:Stream' = 'Verbose' } #default to verbose stream
    function Write-Log
    {
        [CmdletBinding()]
        param(
            [Parameter(Position = 0)]
            [string]$Message = '',

            [Parameter()]
            [ValidateSet('Information', 'Warning', 'Error', 'Success')]
            [string]$Level = 'Information'
        )

        $timestamp = Get-Date -Format 'yyyy-MM-dd HH:mm:ss'

        $color = switch ($Level)
        {
            'Information' { 'White' }
            'Warning' { 'Yellow' }
            'Error' { 'Red' }
            'Success' { 'Green' }
        }

        $prefix = switch ($Level)
        {
            'Information' { '[INFO]' }
            'Warning' { '[WARN]' }
            'Error' { '[ERROR]' }
            'Success' { '[OK]' }
        }

        Write-Host "$timestamp $prefix $Message" -ForegroundColor $color

    }
    function FindNugetExe
    {
        <#
        .SYNOPSIS
            Returns the path to nuget.exe from PATH or well-known install locations.
            Throws if nuget.exe cannot be found.
        #>
        [CmdletBinding()]
        param ()
        begin {}
        process
        {
            $fromPath = Get-Command 'nuget' -ErrorAction SilentlyContinue
            if ($null -ne $fromPath)
            {
                return $fromPath.Source
            }

            $candidates = @(
                (Join-Path $env:LOCALAPPDATA 'NuGet\nuget.exe'),
                (Join-Path $env:ProgramData 'NuGet\nuget.exe'),
                (Join-Path $env:USERPROFILE '.nuget\nuget.exe')
            )

            $commandLinePath = Join-Path $env:USERPROFILE '.nuget\packages\nuget.commandline'
            if (Test-Path $commandLinePath)
            {
                $discovered = Get-ChildItem -Path $commandLinePath -Filter 'nuget.exe' -Recurse -ErrorAction SilentlyContinue |
                    Sort-Object -Property FullName -Descending |
                    Select-Object -First 1
                if ($null -ne $discovered)
                {
                    $candidates += $discovered.FullName
                }
            }

            foreach ($candidate in $candidates)
            {
                if (Test-Path $candidate)
                {
                    return $candidate
                }
            }

            throw 'nuget.exe not found in PATH or common install locations. Add nuget.exe to PATH and retry.'
        }
        end {}
    }

    function ParseNugetVersion
    {
        <#
        .SYNOPSIS
            Parses a NuGet/semver version string into a System.Version. Returns $null on failure.
        #>
        [CmdletBinding()]
        param (
            [Parameter(Mandatory)]
            [string]$VersionString
        )
        begin {}
        process
        {
            $version = $null
            [System.Version]::TryParse($VersionString, [ref]$version) | Out-Null
            return $version
        }
        end {}
    }

    function IsStableVersion
    {
        <#
        .SYNOPSIS
            Returns $true when the version string contains no pre-release suffix.
        #>
        [CmdletBinding()]
        param (
            [Parameter(Mandatory)]
            [string]$VersionString
        )
        begin {}
        process
        {
            return (-not $VersionString.Contains('-'))
        }
        end {}
    }

    function GetNugetVersionAge
    {
        <#
        .SYNOPSIS
            Returns the age in hours of a specific NuGet package version by querying the
            registration leaf. Returns $null if the published date cannot be determined.
        #>
        [CmdletBinding()]
        param (
            [Parameter(Mandatory)]
            [string]$PackageId,

            [Parameter(Mandatory)]
            [string]$Version
        )
        begin {}
        process
        {
            $uri = "https://api.nuget.org/v3/registration5-semver1/$($PackageId.ToLower())/$($Version.ToLower()).json"
            try
            {
                $leaf = Invoke-RestMethod -Uri $uri -ErrorAction Stop -Verbose:$false
                if ($null -ne $leaf.catalogEntry -and $null -ne $leaf.published)
                {
                    $published = [datetime]::Parse(
                        $leaf.published,
                        [System.Globalization.CultureInfo]::InvariantCulture,
                        [System.Globalization.DateTimeStyles]::RoundtripKind
                    )
                    return ([datetime]::UtcNow - $published.ToUniversalTime()).TotalHours
                }
            }
            catch
            {
                Write-Log -Message "Cannot determine published date for $PackageId $Version`: $_" -Stream Warning
            }
            return $null
        }
        end {}
    }

    function GetEligibleNugetVersion
    {
        <#
        .SYNOPSIS
            Returns the newest stable NuGet version of a package that is both newer than
            CurrentVersion and at least MinAgeHours old. Returns $null if none qualifies.
        #>
        [CmdletBinding()]
        param (
            [Parameter(Mandatory)]
            [string]$PackageId,

            [Parameter(Mandatory)]
            [string]$CurrentVersion,

            [Parameter(Mandatory)]
            [int]$MinAgeHours
        )
        begin {}
        process
        {
            $indexUri = "https://api.nuget.org/v3-flatcontainer/$($PackageId.ToLower())/index.json"
            try
            {
                $index = Invoke-RestMethod -Uri $indexUri -ErrorAction Stop -Verbose:$false
            }
            catch
            {
                Write-Log -Message "Failed to query NuGet index for $PackageId`: $_" -Stream Warning
                return $null
            }

            $allVersions = if ($null -ne $index.versions) { @($index.versions) } else { @() }
            $currentParsed = ParseNugetVersion -VersionString $CurrentVersion

            $candidates = $allVersions |
                Where-Object { $null -ne $_ -and (IsStableVersion -VersionString $_) } |
                Where-Object {
                    $v = ParseNugetVersion -VersionString $_
                    ($null -ne $v -and $null -ne $currentParsed -and $v -gt $currentParsed)
                } |
                Sort-Object -Property { ParseNugetVersion -VersionString $_ } -Descending

            foreach ($candidate in $candidates)
            {
                Write-Log -Message "  Checking age of $PackageId $candidate"
                $ageHours = GetNugetVersionAge -PackageId $PackageId -Version $candidate
                if ($null -eq $ageHours)
                {
                    Write-Log -Message "  Age unknown for $PackageId $candidate; skipping"
                    continue
                }
                if ($ageHours -ge $MinAgeHours)
                {
                    return $candidate
                }
                Write-Log -Message "  $PackageId $candidate is $([math]::Round($ageHours, 1))h old (need $MinAgeHours); skipping"
            }

            return $null
        }
        end {}
    }

    function GetHintPathForPackage
    {
        <#
        .SYNOPSIS
            Returns the resolved absolute DLL path from the vbproj HintPath for the given
            NuGet package ID. Returns $null if no matching HintPath entry is found.
        #>
        [CmdletBinding()]
        param (
            [Parameter(Mandatory)]
            [xml]$Vbproj,

            [Parameter(Mandatory)]
            [string]$PackageId,

            [Parameter(Mandatory)]
            [string]$VbprojPath
        )
        begin {}
        process
        {
            $projectDir = Split-Path $VbprojPath -Parent
            $nsMgr = New-Object System.Xml.XmlNamespaceManager($Vbproj.NameTable)
            $nsMgr.AddNamespace('ms', 'http://schemas.microsoft.com/developer/msbuild/2003')

            # Match folder pattern \PackageId.{version}\ to avoid false prefix matches
            $xpath = "//ms:Reference/ms:HintPath[contains(., '\$PackageId.')]"
            $node = $Vbproj.SelectSingleNode($xpath, $nsMgr)
            if ($null -eq $node) { return $null }

            return [System.IO.Path]::GetFullPath([System.IO.Path]::Combine($projectDir, $node.InnerText))
        }
        end {}
    }

    function GetAssemblyVersion
    {
        <#
        .SYNOPSIS
            Returns the Version string read from a .NET assembly file. Returns $null on failure.
        #>
        [CmdletBinding()]
        param (
            [Parameter(Mandatory)]
            [string]$DllPath
        )
        begin {}
        process
        {
            if (-not (Test-Path $DllPath)) { return $null }
            try
            {
                return [System.Reflection.AssemblyName]::GetAssemblyName($DllPath).Version.ToString()
            }
            catch
            {
                Write-Log -Message "Cannot read assembly version from '$DllPath'`: $_" -Stream Warning
                return $null
            }
        }
        end {}
    }

    function UpdateWebConfigAppSettings
    {
        <#
        .SYNOPSIS
            Sets the value of an appSettings key in a loaded Web.config XML document.
        #>
        [CmdletBinding()]
        param (
            [Parameter(Mandatory)]
            [xml]$WebConfig,

            [Parameter(Mandatory)]
            [string]$Key,

            [Parameter(Mandatory)]
            [string]$Value
        )
        begin {}
        process
        {
            $node = @($WebConfig.configuration.appSettings.add) |
                Where-Object { $_.key -eq $Key } |
                Select-Object -First 1
            if ($null -ne $node)
            {
                $node.value = $Value
                Write-Log -Message "  appSettings[$Key] = $Value"
            }
            else
            {
                Write-Log -Message "  appSettings key '$Key' not found in Web.config" -Stream Warning
            }
        }
        end {}
    }

    function UpdateWebConfigBindingRedirect
    {
        <#
        .SYNOPSIS
            Updates an assembly binding redirect's oldVersion ceiling and newVersion to NewVersion
            in the supplied Web.config XML document.
        #>
        [CmdletBinding()]
        param (
            [Parameter(Mandatory)]
            [xml]$WebConfig,

            [Parameter(Mandatory)]
            [string]$AssemblyName,

            [Parameter(Mandatory)]
            [string]$NewVersion
        )
        begin {}
        process
        {
            $nsMgr = New-Object System.Xml.XmlNamespaceManager($WebConfig.NameTable)
            $nsMgr.AddNamespace('asm', 'urn:schemas-microsoft-com:asm.v1')

            $xpath = "//asm:dependentAssembly[asm:assemblyIdentity[@name='$AssemblyName']]"
            $dependentAssembly = $WebConfig.SelectSingleNode($xpath, $nsMgr)
            if ($null -eq $dependentAssembly)
            {
                Write-Log -Message "  Binding redirect for '$AssemblyName' not found in Web.config" -Stream Warning
                return
            }

            $redirect = $dependentAssembly.SelectSingleNode('asm:bindingRedirect', $nsMgr)
            if ($null -ne $redirect)
            {
                $redirect.SetAttribute('oldVersion', "0.0.0.0-$NewVersion")
                $redirect.SetAttribute('newVersion', $NewVersion)
                Write-Log -Message "  bindingRedirect[$AssemblyName] -> $NewVersion"
            }
        }
        end {}
    }

    function RemoveDownloadDomPurifyTarget
    {
        <#
        .SYNOPSIS
            Removes the DownloadDomPurify Target element from the given vbproj XML document.
            Returns $true if the element was found and removed, $false otherwise.
        #>
        [CmdletBinding()]
        param (
            [Parameter(Mandatory)]
            [xml]$Vbproj
        )
        begin {}
        process
        {
            $nsMgr = New-Object System.Xml.XmlNamespaceManager($Vbproj.NameTable)
            $nsMgr.AddNamespace('ms', 'http://schemas.microsoft.com/developer/msbuild/2003')

            $target = $Vbproj.SelectSingleNode("//ms:Target[@Name='DownloadDomPurify']", $nsMgr)
            if ($null -ne $target)
            {
                $target.ParentNode.RemoveChild($target) | Out-Null
                Write-Log -Message 'Removed DownloadDomPurify build target from vbproj'
                return $true
            }

            Write-Log -Message 'DownloadDomPurify build target not found in vbproj'
            return $false
        }
        end {}
    }

    function ReadCurrentDomPurifyVersion
    {
        <#
        .SYNOPSIS
            Reads the DOMPurify version number from the first-line comment of purify.min.js.
            Returns $null if the file does not exist or the version cannot be parsed.
        #>
        [CmdletBinding()]
        param (
            [Parameter(Mandatory)]
            [string]$PurifyJsPath
        )
        begin {}
        process
        {
            if (-not (Test-Path $PurifyJsPath)) { return $null }
            $header = Get-Content -Path $PurifyJsPath -TotalCount 1 -ErrorAction SilentlyContinue
            if ($header -match 'DOMPurify\s+([\d]+\.[\d]+\.[\d]+)')
            {
                return $Matches[1]
            }
            return $null
        }
        end {}
    }

    function GetEligibleDomPurifyVersion
    {
        <#
        .SYNOPSIS
            Queries the NPM registry for dompurify and returns the latest stable version
            that is newer than CurrentVersion (when supplied) and at least MinAgeHours old.
            Returns $null if none qualifies.
        #>
        [CmdletBinding()]
        param (
            [Parameter(Mandatory)]
            [int]$MinAgeHours,

            [Parameter()]
            [AllowNull()]
            [string]$CurrentVersion = $null
        )
        begin {}
        process
        {
            Write-Log -Message 'Querying NPM registry for dompurify'
            try
            {
                $registry = Invoke-RestMethod -Uri 'https://registry.npmjs.org/dompurify' -ErrorAction Stop -Verbose:$false
            }
            catch
            {
                Write-Log -Message "Failed to query NPM registry for dompurify`: $_" -Stream Warning
                return $null
            }

            if ($null -eq $registry.time)
            {
                Write-Log -Message 'NPM registry response for dompurify contains no time data' -Stream Warning
                return $null
            }

            $currentParsed = if (-not [string]::IsNullOrEmpty($CurrentVersion)) { ParseNugetVersion -VersionString $CurrentVersion } else { $null }

            $reservedKeys = @('created', 'modified')
            $stableVersions = $registry.time.PSObject.Properties |
                Where-Object { $reservedKeys -notcontains $_.Name } |
                Where-Object { IsStableVersion -VersionString $_.Name } |
                Where-Object {
                    if ($null -eq $currentParsed) { return $true }
                    $v = ParseNugetVersion -VersionString $_.Name
                    ($null -ne $v -and $v -gt $currentParsed)
                } |
                Sort-Object -Property { ParseNugetVersion -VersionString $_.Name } -Descending

            foreach ($entry in $stableVersions)
            {
                $published = [datetime]::Parse(
                    $entry.Value,
                    [System.Globalization.CultureInfo]::InvariantCulture,
                    [System.Globalization.DateTimeStyles]::RoundtripKind
                )
                $ageHours = ([datetime]::UtcNow - $published.ToUniversalTime()).TotalHours
                if ($ageHours -ge $MinAgeHours)
                {
                    return $entry.Name
                }
                Write-Log -Message "  DOMPurify $($entry.Name) is $([math]::Round($ageHours, 1))h old (need $MinAgeHours); skipping"
            }

            return $null
        }
        end {}
    }

    function SaveXmlDocument
    {
        <#
        .SYNOPSIS
            Saves an XML document to disk with UTF-8 encoding (no BOM), 2-space indentation,
            and Windows line endings -- matching the existing project file conventions.
        #>
        [CmdletBinding()]
        param (
            [Parameter(Mandatory)]
            [xml]$Document,

            [Parameter(Mandatory)]
            [string]$Path
        )
        begin {}
        process
        {
            $settings = New-Object System.Xml.XmlWriterSettings
            $settings.Indent = $true
            $settings.IndentChars = '  '
            $settings.Encoding = New-Object System.Text.UTF8Encoding($false)
            $settings.NewLineChars = "`r`n"
            $settings.NewLineHandling = [System.Xml.NewLineHandling]::Replace

            $writer = [System.Xml.XmlWriter]::Create($Path, $settings)
            try
            {
                $Document.Save($writer)
            }
            finally
            {
                $writer.Dispose()
            }
        }
        end {}
    }
}

process
{
    # --- Resolve and validate paths -------------------------------------------

    $resolvedSolution = [System.IO.Path]::GetFullPath($SolutionPath)
    if (-not (Test-Path $resolvedSolution))
    {
        throw "Solution file not found: $resolvedSolution"
    }

    $projectDir = Split-Path $resolvedSolution -Parent
    $packagesConfig = Join-Path $projectDir 'packages.config'
    $vbprojPath = Join-Path $projectDir 'WebJEA.vbproj'
    $webConfigPath = Join-Path $projectDir 'Web.config'
    $purifyJsPath = Join-Path $projectDir 'resources\purify.min.js'

    foreach ($requiredPath in @($packagesConfig, $vbprojPath, $webConfigPath))
    {
        if (-not (Test-Path $requiredPath))
        {
            throw "Required file not found: $requiredPath"
        }
    }

    # --- Locate nuget.exe -----------------------------------------------------

    Write-Log -Message 'Locating nuget.exe'
    $nugetExe = FindNugetExe
    Write-Log -Message "Using nuget.exe: $nugetExe"

    # --- Phase 1: Scan NuGet packages for eligible updates --------------------

    Write-Log -Message "Reading $packagesConfig"
    [xml]$packagesXml = Get-Content -Path $packagesConfig -Raw
    $packages = @($packagesXml.packages.package)
    $nugetUpdates = New-Object System.Collections.Generic.List[psobject]

    foreach ($pkg in $packages)
    {
        Write-Log -Message "Checking $($pkg.id) (current: $($pkg.version))"
        $targetVersion = GetEligibleNugetVersion -PackageId $pkg.id -CurrentVersion $pkg.version -MinAgeHours $MinAgeHours
        if ($null -ne $targetVersion)
        {
            Write-Log -Message "  Eligible update: $($pkg.version) -> $targetVersion"
            $nugetUpdates.Add([PSCustomObject]@{
                    Id             = $pkg.id
                    CurrentVersion = $pkg.version
                    TargetVersion  = $targetVersion
                })
        }
        else
        {
            Write-Log -Message '  No eligible update found'
        }
    }

    # --- Phase 2: Apply NuGet updates -----------------------------------------

    foreach ($update in $nugetUpdates)
    {
        $shouldProcessTarget = "$($update.Id) $($update.CurrentVersion) -> $($update.TargetVersion)"
        if ($PSCmdlet.ShouldProcess($shouldProcessTarget, 'nuget update'))
        {
            Write-Log -Message "Updating $($update.Id) to $($update.TargetVersion)"
            $nugetParams = @(
                'update', $resolvedSolution,
                '-Id', $update.Id,
                '-Version', $update.TargetVersion,
                '-NonInteractive'
            )
            & $nugetExe @nugetParams
            if ($LASTEXITCODE -ne 0)
            {
                Write-Log -Message "nuget update exited $LASTEXITCODE for $($update.Id)" -Stream Warning
            }
        }
    }

    # --- Phase 3: Update Web.config -------------------------------------------

    if ($nugetUpdates.Count -gt 0)
    {
        [xml]$webConfig = Get-Content -Path $webConfigPath -Raw
        [xml]$vbproj = Get-Content -Path $vbprojPath -Raw

        $jqueryUpdate = $nugetUpdates | Where-Object { $_.Id -eq 'jQuery' } | Select-Object -First 1
        $jqueryUiUpdate = $nugetUpdates | Where-Object { $_.Id -eq 'jQuery.UI.Combined' } | Select-Object -First 1

        if ($null -ne $jqueryUpdate -and $PSCmdlet.ShouldProcess("Web.config jQueryVersion -> $($jqueryUpdate.TargetVersion)", 'update appSetting'))
        {
            UpdateWebConfigAppSettings -WebConfig $webConfig -Key 'jQueryVersion' -Value $jqueryUpdate.TargetVersion
        }

        if ($null -ne $jqueryUiUpdate -and $PSCmdlet.ShouldProcess("Web.config jQueryUIVersion -> $($jqueryUiUpdate.TargetVersion)", 'update appSetting'))
        {
            UpdateWebConfigAppSettings -WebConfig $webConfig -Key 'jQueryUIVersion' -Value $jqueryUiUpdate.TargetVersion
        }

        # NuGet package IDs that have a corresponding assembly binding redirect in Web.config
        $redirectPackageIds = @(
            'System.Runtime.CompilerServices.Unsafe',
            'System.Text.Json',
            'Microsoft.Bcl.AsyncInterfaces',
            'System.Threading.Tasks.Extensions',
            'System.Buffers',
            'System.Memory'
        )

        foreach ($packageId in $redirectPackageIds)
        {
            $pkgUpdate = $nugetUpdates | Where-Object { $_.Id -eq $packageId } | Select-Object -First 1
            if ($null -eq $pkgUpdate) { continue }

            $dllPath = GetHintPathForPackage -Vbproj $vbproj -PackageId $packageId -VbprojPath $vbprojPath
            if ($null -eq $dllPath)
            {
                Write-Log -Message "HintPath not found for $packageId; skipping binding redirect update" -Stream Warning
                continue
            }

            $asmVersion = GetAssemblyVersion -DllPath $dllPath
            if ($null -eq $asmVersion)
            {
                Write-Log -Message "Assembly version not readable for $packageId; skipping binding redirect update" -Stream Warning
                continue
            }

            if ($PSCmdlet.ShouldProcess("Web.config binding redirect $packageId -> $asmVersion", 'update'))
            {
                UpdateWebConfigBindingRedirect -WebConfig $webConfig -AssemblyName $packageId -NewVersion $asmVersion
            }
        }

        if ($PSCmdlet.ShouldProcess($webConfigPath, 'save'))
        {
            SaveXmlDocument -Document $webConfig -Path $webConfigPath
            Write-Log -Message "Saved $webConfigPath"
        }
    }

    # --- Phase 4: DOMPurify ---------------------------------------------------

    $currentDomPurifyVersion = ReadCurrentDomPurifyVersion -PurifyJsPath $purifyJsPath
    $currentDisplayVersion = if ($null -ne $currentDomPurifyVersion) { $currentDomPurifyVersion } else { 'unknown' }
    Write-Log -Message "Current DOMPurify version: $currentDisplayVersion"

    $targetDomPurifyVersion = GetEligibleDomPurifyVersion -MinAgeHours $MinAgeHours -CurrentVersion $currentDomPurifyVersion
    $domPurifyChangeFound = $null -ne $targetDomPurifyVersion

    if ($domPurifyChangeFound)
    {
        if ($PSCmdlet.ShouldProcess("DOMPurify $currentDisplayVersion -> $targetDomPurifyVersion", 'download purify.min.js'))
        {
            Write-Log -Message "Downloading DOMPurify $targetDomPurifyVersion from unpkg"
            $downloadUri = "https://unpkg.com/dompurify@$targetDomPurifyVersion/dist/purify.min.js"
            Invoke-WebRequest -Uri $downloadUri -OutFile $purifyJsPath -ErrorAction Stop -Verbose:$false
            Write-Log -Message "Downloaded DOMPurify $targetDomPurifyVersion to $purifyJsPath"
        }

        if ($PSCmdlet.ShouldProcess($vbprojPath, 'remove DownloadDomPurify build target'))
        {
            [xml]$vbprojForTarget = Get-Content -Path $vbprojPath -Raw
            if (RemoveDownloadDomPurifyTarget -Vbproj $vbprojForTarget)
            {
                SaveXmlDocument -Document $vbprojForTarget -Path $vbprojPath
                Write-Log -Message "Saved $vbprojPath"
            }
        }
    }
    elseif ($null -ne $targetDomPurifyVersion)
    {
        Write-Log -Message "DOMPurify is already at the latest eligible version ($targetDomPurifyVersion)"
    }
    else
    {
        Write-Log -Message 'No eligible DOMPurify version found'
    }

    # --- Phase 5: Commit message ----------------------------------------------

    if ($nugetUpdates.Count -eq 0 -and -not $domPurifyChangeFound)
    {
        Write-Output 'No package updates found.'
        return
    }

    $commitLines = New-Object System.Collections.Generic.List[string]
    $commitLines.Add('chore: update packages')
    $commitLines.Add('')

    if ($nugetUpdates.Count -gt 0)
    {
        $commitLines.Add('NuGet:')
        foreach ($update in $nugetUpdates)
        {
            $commitLines.Add("- $($update.Id): $($update.CurrentVersion) -> $($update.TargetVersion)")
        }
    }

    if ($domPurifyChangeFound)
    {
        if ($nugetUpdates.Count -gt 0) { $commitLines.Add('') }
        $commitLines.Add('Frontend:')
        $commitLines.Add("- DOMPurify: $currentDisplayVersion -> $targetDomPurifyVersion")
    }

    Write-Output ($commitLines -join "`n")
}

end {}
