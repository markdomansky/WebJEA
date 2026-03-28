# WebJEA psake build definition
Properties {
    #These will all be built during Init (or later)
}

FormatTaskName {
    param($taskName)
    Write-Host ">>>>> Executing $taskName <<<<<" -ForegroundColor cyan
}

Task default -Depends Summary

Task Init {
    #copy from parameters passed by the invoking script to script-scoped variables for easier access in tasks
    $script:repoRoot = $repoRoot
    $script:outputPath = $OutputPath
    $script:buildInfoPath = $buildInfoPath
    $script:buildConfiguration = $buildConfiguration
    $script:createZip = $CreateZip
    $script:skipNuGetRestore = $SkipNuGetRestore
    $script:templatePath = $TemplatePath

    #calculate a couple variables
    $script:solutionsPath = "$script:repoRoot\WebJEA"
    $script:projectFilePath = "$script:solutionsPath\WebJEA.vbproj"
    $script:assemblyInfoPath = "$script:solutionsPath\My Project\AssemblyInfo.vb"
    $script:version = Get-Date -Format 'yyyy.M.d.HHmm'

    $projectXml = [xml](Get-Content -Path $script:projectFilePath)

    # Filter PropertyGroup by the condition matching buildConfiguration
    $buildOutputPathRel = $projectXml.Project.PropertyGroup |
        Where-Object { $_.Condition -match $script:buildConfiguration } |
        Select-Object -First 1 -expand OutputPath
    $script:buildOutputPath = [System.IO.Path]::GetFullPath([System.IO.Path]::Combine($script:solutionsPath, $buildOutputPathRel))

    Write-Host 'Resolved Paths:'
    Write-Host "  repoRoot:            $script:repoRoot"
    Write-Host "  solutionsPath:       $script:solutionsPath"
    Write-Host "  assemblyInfoPath:    $script:assemblyInfoPath"
    Write-Host "  projectFilePath:     $script:projectFilePath"
    Write-Host "  templatePath:        $script:templatePath"
    Write-Host "  buildOutputPath:     $script:buildOutputPath"
    Write-Host "  outputPath:          $script:outputPath"
    Write-Host "  buildInfoPath:       $script:buildInfoPath"
    Write-Host 'Build Properties:'
    Write-Host "  Build Configuration: $script:buildConfiguration"
    Write-Host "  Create Zip:          $script:createZip"
    Write-Host "  Skip NuGet Restore:  $script:skipNuGetRestore"
    Write-Host "  version:             $script:version"
    Write-Host ''

}

Task UpdateAssemblyInfo -Depends Init {
    if (-not $script:assemblyInfoPath)
    {
        throw 'AssemblyInfo path not resolved. Cannot update assembly version info.'
    }
    if (-not (Test-Path $script:assemblyInfoPath))
    {
        throw "AssemblyInfo file not found at path: $script:assemblyInfoPath"
    }
    $content = Get-Content $script:assemblyInfoPath -Raw -Encoding UTF8
    $content = $content -replace '<Assembly: AssemblyVersion\(".*"\)>', '<Assembly: AssemblyVersion("{0}")>' -f $script:version
    $content = $content -replace '<Assembly: AssemblyFileVersion\(".*"\)>', '<Assembly: AssemblyFileVersion("{0}")>' -f $script:version
    $year = $script:version.split('.')[0]
    $content = $content -replace '(<Assembly: AssemblyCopyright.*) \d{4}("\))', ('$1 {0}$2' -f $year)
    $content = $content.trim()
    # write-host -ForegroundColor darkgray $content
    $content | Out-File $script:assemblyInfoPath -Encoding UTF8
}

Task GenerateAwsSecrets -Depends Init {
    $templatePath = "$script:solutionsPath\AwsSecrets.template.vb"
    $outputPath = "$script:solutionsPath\AwsSecrets.vb"

    if (-not (Test-Path $templatePath))
    {
        throw "AWS secrets template not found: $templatePath"
    }

    $envMap = [ordered]@{
        '{{AWS_KEY}}'       = $env:AWS_KEY
        '{{AWS_KEYSEC}}'    = $env:AWS_KEYSEC
        '{{AWS_QUEUE_URL}}' = $env:AWS_QUEUE_URL
    }

    $providedCount = ($envMap.Values | Where-Object { $_ }).Count

    if ($providedCount -eq $envMap.Count)
    {
        $content = Get-Content $templatePath -Raw -Encoding UTF8
        foreach ($placeholder in $envMap.Keys)
        {
            $content = $content.Replace($placeholder, $envMap[$placeholder])
        }
        $content.Trim() | Out-File $outputPath -Encoding UTF8
        Write-Host "Generated AwsSecrets.vb from template with CI secrets: $outputPath"
    }
    elseif (Test-Path $outputPath)
    {
        Write-Host "Using existing AwsSecrets.vb (local development): $outputPath"
    }
    else
    {
        throw 'AwsSecrets.vb not found and AWS environment variables not set. Copy AwsSecrets.template.vb to AwsSecrets.vb and fill in your values.'
    }
}

Task RestoreNuGet -Depends Init -PreCondition { -not $script:skipNuGetRestore } {
    $nugetExe = Get-Command nuget.exe -ErrorAction SilentlyContinue
    if (-not $nugetExe) { throw 'NuGet.exe not found in PATH. Please install NuGet or add it to your PATH.' }

    # Restore directly from packages.config to avoid MSBuild version compatibility issues.
    # Newer NuGet.exe generates PackageReference-style temp targets that old MSBuild (v4) cannot parse.
    $packagesConfig = "$script:solutionsPath\packages.config"
    $packagesDir = "$script:solutionsPath\packages"

    & nuget restore $packagesConfig -PackagesDirectory $packagesDir -SolutionDirectory $script:solutionsPath -NonInteractive
    if ($LASTEXITCODE -ne 0) { throw "NuGet restore failed with exit code $LASTEXITCODE" }
}

Task CleanBuildPath -Depends Init {
    if (Test-Path $script:buildOutputPath)
    {
        Write-Host "Cleaning build output path: $script:buildOutputPath"
        #We don't want to delete the folder, just the contents
        Get-ChildItem $script:buildOutputPath | Remove-Item -Recurse -Force
    }
}

Task OutputBuildStructure -Depends RestoreNuGet, CleanBuildPath {
    #Needed this temporarily to look for files.  Disable for now but may be useful in the future for debugging build structure issues.
    #Get-ChildItem $script:repoRoot -Recurse | Select-Object -expand fullname
}

Task Compile -Depends UpdateAssemblyInfo, GenerateAwsSecrets, RestoreNuGet, CleanBuildPath, OutputBuildStructure {
    $msbuildExe = $null
    $vsToolsPath = $null

    # Prefer vswhere-discovered MSBuild (VS 2017+) so we get modern targets over the
    # legacy .NET Framework MSBuild that may be first on PATH (v4.0.30319).
    $vswhere = "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe"
    if (Test-Path $vswhere)
    {
        $vsInstallPath = & $vswhere -latest -requires Microsoft.Component.MSBuild -property installationPath 2>$null | Select-Object -First 1
        $msbuildPath = & $vswhere -latest -requires Microsoft.Component.MSBuild -find 'MSBuild\**\Bin\MSBuild.exe' 2>$null | Select-Object -First 1

        if ($msbuildPath -and (Test-Path $msbuildPath))
        {
            $msbuildExe = $msbuildPath
        }

        # Locate the WebApplication targets shipped with this VS install and pass
        # VSToolsPath explicitly so MSBuild does not fall back to the v11.0 path.
        if ($vsInstallPath)
        {
            $targetsRoot = "$vsInstallPath\MSBuild\Microsoft\VisualStudio"
            $versionDir = Get-ChildItem -Path $targetsRoot -Directory -ErrorAction SilentlyContinue |
                Where-Object { Test-Path ("$($_.FullName)\WebApplications\Microsoft.WebApplication.targets") } |
                Sort-Object Name -Descending | Select-Object -First 1
            if ($versionDir) { $vsToolsPath = $versionDir.FullName }
        }
    }

    # Fall back to whatever msbuild.exe is on PATH (last resort).
    if (-not $msbuildExe)
    {
        $msbuildFromPath = Get-Command msbuild.exe -ErrorAction SilentlyContinue
        if ($msbuildFromPath) { $msbuildExe = $msbuildFromPath.Source }
    }

    if (-not $msbuildExe)
    {
        throw 'MSBuild.exe not found. Please ensure Visual Studio Build Tools or Visual Studio is installed.'
    }

    $msbuildArgs = @(
        $script:projectFilePath
        "/p:Configuration=$script:build"
        '/m'
        '/verbosity:minimal'
        '/p:WarningLevel=0'
    )
    if ($vsToolsPath)
    {
        $msbuildArgs += "/p:VSToolsPath=$vsToolsPath"
        Write-Host "Using VSToolsPath: $vsToolsPath"
    }

    #the output folder is defined in the project file
    Write-Host "Using MSBuild: $msbuildExe"
    & $msbuildExe @msbuildArgs

    if ($LASTEXITCODE -ne 0)
    {
        throw "MSBuild failed with exit code $LASTEXITCODE"
    }
}

Task CopyBuildFiles -Depends Compile {
    function Copy-SiteFile
    {
        param(
            [string]$Source,
            [string]$Destination
        )
        $destDir = Split-Path $Destination -Parent
        if (-not (Test-Path $destDir)) { New-Item -Path $destDir -ItemType Directory -Force | Out-Null }
        if (-not (Test-Path $Source)) { Write-Warning "Missing: $Source"; return }
        Copy-Item -Path $Source -Destination $Destination -Force
    }

    $src = $script:solutionsPath
    $binPath = $script:buildOutputPath
    $outSite = "$script:outputPath\site"

    Write-Host 'Package Paths:'
    Write-Host "  Source:       $src"
    Write-Host "  Build Bin:   $binPath"
    Write-Host "  Output:      $script:outputPath"
    Write-Host "  Site:        $outSite"

    if (Test-Path $script:outputPath)
    {
        Write-Host "Removing output directory contents: $script:outputPath"
        Get-ChildItem $script:outputPath | ForEach-Object { Remove-Item -Path $_.FullName -Recurse -Force }
    }

    @($script:outputPath, $outSite) | ForEach-Object {
        if (-not (Test-Path $_)) { New-Item -Path $_ -ItemType Directory -Force | Out-Null }
    }

    # -- bin\ : compiled assemblies and XML doc only --
    Write-Host 'Copying bin\ assemblies...'
    New-Item -Path "$outSite\bin" -ItemType Directory -Force | Out-Null
    Get-ChildItem -Path $binPath -File | Where-Object {
        $_.Extension -eq '.dll' -or $_.Name -eq 'WebJEA.xml'
    } | ForEach-Object {
        Copy-Item -Path $_.FullName -Destination "$outSite\bin\$($_.Name)" -Force
        Write-Host "  bin\$($_.Name)"
    }

    # ── Site files from source tree ──
    Write-Host 'Copying site files...'
    $siteFiles = @(
        'default.aspx'
        'error.aspx'
        'Global.asax'
        'main.css'
        'NLog.config'
        'psoutput.css'
        'sidebar.css'
        'startup.js'
        'validation.js'
        'Web.config'
        'images\*'
    )

    foreach ($relPath in $siteFiles)
    {
        if ($relPath -match '\\\*$')
        {
            $sourceDir = Join-Path -Path $src -ChildPath ($relPath -replace '\\\*$', '')
            Get-ChildItem -Path $sourceDir -File -ErrorAction SilentlyContinue | ForEach-Object {
                $fileRel = $_.FullName.Substring($src.Length).TrimStart('\')
                Copy-SiteFile -Source $_.FullName -Destination "$outSite\$fileRel"
                Write-Host "  $fileRel"
            }
        }
        else
        {
            Copy-SiteFile -Source "$src\$relPath" -Destination "$outSite\$relPath"
            Write-Host "  $relPath"
        }
    }

    $script:buildResult = @{
        Version       = $script:version
        OutputPath    = $script:outputPath
        buildInfoPath = $script:buildInfoPath
    }
}

Task CopyPackageFiles -Depends CopyBuildFiles {
    $packagesDir = "$script:solutionsPath\packages"
    $outSite = "$script:outputPath\site"

    # Package name → relative paths from the package content root.
    # Wildcard patterns are resolved via Get-ChildItem; slim builds are excluded.
    $packageFiles = [ordered]@{
        'bootstrap'               = @(
            'Content\bootstrap-grid.min.css'
            'Content\bootstrap-reboot.min.css'
            'Content\bootstrap.min.css'
            'Scripts\bootstrap.bundle.min.js'
            'Scripts\bootstrap.min.js'
        )
        'jQuery'                  = @(
            'Scripts\jquery-*.min.js'
        )
        'jQuery.UI.Combined'      = @(
            'Scripts\jquery-ui-*.min.js'
            'Content\themes\base\*.css'
            'Content\themes\base\images\*.png'
        )
        'jQuery-Timepicker-Addon' = @(
            'Content\jquery-ui-timepicker-addon.min.css'
            'Scripts\jquery-ui-sliderAccess.js'
            'Scripts\jquery-ui-timepicker-addon.min.js'
        )
        'popper.js'               = @(
            'Scripts\popper-utils.min.js'
            'Scripts\popper.min.js'
        )
    }

    foreach ($packageName in $packageFiles.Keys)
    {
        # Resolve the versioned package folder (e.g. jQuery.3.7.1)
        $packageDir = Get-ChildItem -Path $packagesDir -Directory |
            Where-Object { $_.Name -match "^$([regex]::Escape($packageName))\.\d" } |
            Sort-Object Name -Descending | Select-Object -First 1

        if (-not $packageDir)
        {
            throw "Package folder not found for '$packageName' in $packagesDir"
        }

        # NuGet content root (content/ or Content/) sits directly inside the package folder
        $contentRoot = Get-ChildItem -Path $packageDir.FullName -Directory |
            Where-Object { $_.Name -eq 'content' -or $_.Name -eq 'Content' } |
            Select-Object -First 1

        if (-not $contentRoot)
        {
            throw "Content root not found in package: $($packageDir.Name)"
        }

        Write-Host "Package: $packageName ($($packageDir.Name))"

        foreach ($relPath in $packageFiles[$packageName])
        {
            $sourcePath = Join-Path -Path $contentRoot.FullName -ChildPath $relPath

            if ($relPath -match '\*')
            {
                $parentDir = Split-Path -Path $sourcePath -Parent
                $filter = Split-Path -Path $sourcePath -Leaf
                Get-ChildItem -Path $parentDir -Filter $filter -File -ErrorAction SilentlyContinue |
                    Where-Object { $_.Name -notmatch 'slim' } |
                    ForEach-Object {
                        $fileRelPath = $_.FullName.Substring($contentRoot.FullName.Length).TrimStart('\')
                        $destFile = Join-Path -Path $outSite -ChildPath $fileRelPath
                        $destDir = Split-Path -Path $destFile -Parent
                        if (-not (Test-Path $destDir)) { New-Item -Path $destDir -ItemType Directory -Force | Out-Null }
                        Copy-Item -Path $_.FullName -Destination $destFile -Force
                        Write-Host "  $fileRelPath"
                    }
            }
            else
            {
                if (-not (Test-Path $sourcePath))
                {
                    Write-Warning "Missing: $sourcePath"
                    continue
                }
                $destFile = Join-Path -Path $outSite -ChildPath $relPath
                $destDir = Split-Path -Path $destFile -Parent
                if (-not (Test-Path $destDir)) { New-Item -Path $destDir -ItemType Directory -Force | Out-Null }
                Copy-Item -Path $sourcePath -Destination $destFile -Force
                Write-Host "  $relPath"
            }
        }
    }

    $fileCount = (Get-ChildItem -Path $outSite -File -Recurse).Count
    Write-Host "Package complete: $fileCount files assembled at $outSite" -ForegroundColor Green
}

Task UpdateWebConfig -Depends CopyBuildFiles {
    $packagesDir = "$script:solutionsPath\packages"
    $webConfigPath = "$script:outputPath\site\Web.config"

    if (-not (Test-Path $webConfigPath))
    {
        throw "Web.config not found in output at: $webConfigPath"
    }

    $jQueryDir = Get-ChildItem -Path $packagesDir -Directory |
        Where-Object { $_.Name -match '^jQuery\.\d' } |
        Sort-Object Name -Descending | Select-Object -First 1
    $jQueryUIDir = Get-ChildItem -Path $packagesDir -Directory |
        Where-Object { $_.Name -match '^jQuery\.UI\.Combined\.\d' } |
        Sort-Object Name -Descending | Select-Object -First 1

    if (-not $jQueryDir) { throw "jQuery package folder not found in $packagesDir" }
    if (-not $jQueryUIDir) { throw "jQuery.UI.Combined package folder not found in $packagesDir" }

    $jQueryVersion = $jQueryDir.Name -replace '^jQuery\.', ''
    $jQueryUIVersion = $jQueryUIDir.Name -replace '^jQuery\.UI\.Combined\.', ''

    $xml = [xml](Get-Content $webConfigPath -Raw -Encoding UTF8)
    $appSettings = $xml.configuration.appSettings.add

    ($appSettings | Where-Object { $_.key -eq 'jQueryVersion' }).value = $jQueryVersion
    ($appSettings | Where-Object { $_.key -eq 'jQueryUIVersion' }).value = $jQueryUIVersion

    $xml.Save($webConfigPath)
    Write-Host "Updated Web.config: jQueryVersion=$jQueryVersion, jQueryUIVersion=$jQueryUIVersion"
}

Task CopyReleaseFiles -Depends CopyBuildFiles {

    if (-not (Test-Path -Path $script:templatePath -PathType Container))
    {
        throw "Template source path does not exist: $script:templatePath"
    }

    Get-ChildItem -Path $script:templatePath -File -Recurse | ForEach-Object {
        $relativePath = $_.FullName.Substring($script:templatePath.Length).TrimStart('\\')
        $destFile = Join-Path -Path $script:outputPath -ChildPath $relativePath
        $destDir = Split-Path -Path $destFile -Parent
        if (-not (Test-Path -Path $destDir)) { New-Item -Path $destDir -ItemType Directory -Force | Out-Null }
        Copy-Item -Path $_.FullName -Destination $destFile -Force
        Write-Host "  $relativePath"
    }
}

Task PackageComplete -Depends CopyBuildFiles, CopyPackageFiles, CopyReleaseFiles, UpdateWebConfig { }

Task CreateZip -Depends PackageComplete -PreCondition { $createZip } {
    $zipName = "webjea-$script:version.zip"
    $zipPath = "$script:outputPath\$zipName"
    Write-Host "Compressing $script:outputPath"
    Write-Host "to $zipPath..."
    if (Test-Path $zipPath) { Remove-Item -Path $zipPath -Force }

    Compress-Archive -Path "$script:outputPath\*" -DestinationPath $zipPath -Force

    $zipInfo = Get-Item $zipPath
    Write-Host "Created release archive: $zipName ($([Math]::Round($zipInfo.Length / 1MB, 2)) MB)" -ForegroundColor Green

    $script:buildResult.ZipPath = $zipPath
}

Task RevertAssemblyInfo -Depends PackageComplete {
    if (-not (Test-Path $script:assemblyInfoPath))
    {
        Write-Warning "AssemblyInfo not found at $script:assemblyInfoPath - skipping revert."
        return
    }
    $content = Get-Content $script:assemblyInfoPath -Raw -Encoding UTF8
    $content = $content -replace '<Assembly: AssemblyVersion\(".*"\)>', '<Assembly: AssemblyVersion("1.0.0.0")>'
    $content = $content -replace '<Assembly: AssemblyFileVersion\(".*"\)>', '<Assembly: AssemblyFileVersion("1.0.0.0")>'
    $content = $content.Trim()
    $content | Out-File $script:assemblyInfoPath -Encoding UTF8
    Write-Host 'Reverted AssemblyVersion and AssemblyFileVersion to 1.0.0.0'
}

Task SaveBuildInfo -Depends RevertAssemblyInfo, CreateZip {
    $script:buildInfoPath = "$($script:buildResult.OutputPath)\build-info.json"
    $script:buildResult | ConvertTo-Json | Out-File -FilePath $script:buildInfoPath -Encoding utf8 -Force
    Write-Host "buildResult:$script:buildResult"
    Write-Host "Build info saved: $script:buildInfoPath" -ForegroundColor Green

    if ($script:buildResult.ZipPath)
    {
        Write-Host ("Archive: $($script:buildResult.ZipPath)")
    }
}

Task Summary -Depends SaveBuildInfo {
    #empty, just here to to be used by the default task
}
