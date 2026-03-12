# WebJEA psake build definition
Properties {
    #These will all be built during Init (or later)
}

Task default -depends Summary

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
    $script:version = Get-Date -Format 'yyyy.M.d.Hmm'

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

Task UpdateAssemblyInfo -depends Init {
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

Task RestoreNuGet -depends Init -precondition { -not $script:skipNuGetRestore } {
    $nugetExe = Get-Command nuget.exe -ErrorAction SilentlyContinue
    if (-not $nugetExe) { throw 'NuGet.exe not found in PATH. Please install NuGet or add it to your PATH.' }

    # Restore directly from packages.config to avoid MSBuild version compatibility issues.
    # Newer NuGet.exe generates PackageReference-style temp targets that old MSBuild (v4) cannot parse.
    $packagesConfig = "$script:solutionsPath\packages.config"
    $packagesDir = "$script:solutionsPath\packages"

    & nuget restore $packagesConfig -PackagesDirectory $packagesDir -SolutionDirectory $script:solutionsPath -NonInteractive
    if ($LASTEXITCODE -ne 0) { throw "NuGet restore failed with exit code $LASTEXITCODE" }
}

Task CleanBuildPath -depends Init {
    if (Test-Path $script:buildOutputPath)
    {
        Write-Host "Cleaning build output path: $script:buildOutputPath"
        #We don't want to delete the folder, just the contents
        Get-ChildItem $script:buildOutputPath | Remove-Item -Recurse -Force
    }
}

Task Compile -depends UpdateAssemblyInfo, RestoreNuGet, CleanBuildPath {
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

Task Package -depends Compile {
    function Copy-FileTree
    {
        param(
            [Parameter(Mandatory = $true)]
            [string]$SourcePath,

            [Parameter(Mandatory = $true)]
            [string]$DestinationPath
        )
        Write-Host "Copying File Tree: $SourcePath -> $DestinationPath"

        # Validate source exists
        if (-not (Test-Path -Path $SourcePath -PathType Container))
        {
            throw "Source path does not exist: $SourcePath"
        }

        # Create destination if it doesn't exist
        if (-not (Test-Path -Path $DestinationPath))
        {
            New-Item -Path $DestinationPath -ItemType Directory -Force | Out-Null
        }

        # Get all files recursively
        Get-ChildItem -Path $SourcePath -File -Recurse | ForEach-Object {
            # Calculate relative path from source
            $relativePath = $_.FullName.Substring($SourcePath.Length).TrimStart('\')

            # Build destination file path
            $destFile = Join-Path -Path $DestinationPath -ChildPath $relativePath

            # Create destination folder if needed
            $destFolder = Split-Path -Path $destFile -Parent
            if (-not (Test-Path -Path $destFolder))
            {
                New-Item -Path $destFolder -ItemType Directory -Force | Out-Null
            }

            # Copy file (overwrites if exists)
            # write-host "  Copying file: $($_.FullName) -> $destFile"
            Copy-Item -Path $_.FullName -Destination $destFile -Force
        }
    }

    $srcPathBin = $script:buildOutputPath
    $templatePath = $script:templatePath
    $outPathSite = "$script:outputPath\site"

    Write-Host 'Paths'
    Write-Host "  srcPathBin:     $srcPathBin"
    Write-Host "  templatePath:   $templatePath"
    Write-Host "  outputPath:     $script:outputPath"
    Write-Host "  outPathSite:    $outPathSite"

    if (Test-Path $script:outputPath)
    {
        Write-Host "Removing output directory contents: $script:outputPath"
        Get-ChildItem $script:outputPath | ForEach-Object { Remove-Item -Path $_.fullname -Recurse -Force }
    }

    @($script:outputPath, $outPathSite) | ForEach-Object {
        if (-not (Test-Path $_))
        {
            Write-Host "Creating directory: $_"
            New-Item -Path $_ -ItemType Directory -Force | Out-Null
        }
    }

    Write-Host 'Copying files to output directory...'
    Copy-FileTree -SourcePath $srcPathBin -DestinationPath $outPathSite
    Copy-FileTree -SourcePath $templatePath -DestinationPath $script:outputPath

    $script:buildResult = @{
        Version       = $script:version
        OutputPath    = $script:outputPath
        buildInfoPath = $script:buildInfoPath
    }
}

Task CreateZip -depends Package -precondition { $createZip } {
    $zipName = "webjea-$script:version.zip"
    $zipPath = Join-Path $script:outputPath $zipName

    if (Test-Path $zipPath)
    {
        Remove-Item -Path $zipPath -Force
    }

    Compress-Archive -Path "$script:outputPath\*" -DestinationPath $zipPath -Force

    $zipInfo = Get-Item $zipPath
    Write-Host "Created release archive: $zipName ($([Math]::Round($zipInfo.Length / 1MB, 2)) MB)" -ForegroundColor Green

    $script:buildResult.ZipPath = $zipPath
}

Task SaveBuildInfo -depends CreateZip, Package {
    $script:buildInfoPath = "$($buildResult.OutputPath)\build-info.json"
    $buildResult | ConvertTo-Json | Out-File -FilePath $script:buildInfoPath -Encoding utf8 -Force
    Write-Host "Build info saved: $script:buildInfoPath" -ForegroundColor Green

    if ($buildResult.ZipPath)
    {
        Write-Host "Archive: $($buildResult.ZipPath)"
    }
}

Task Summary -depends SaveBuildInfo {
    #empty, just here to to be used by the default task
}
