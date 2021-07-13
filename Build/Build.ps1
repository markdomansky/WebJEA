$ErrorActionPreference = "Stop"
$buildpath = $PSScriptRoot

$buildbin = "C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin\amd64\msbuild.exe"
$publishtemp = "$buildpath\temp"
$solutionpath = resolve-path "$buildpath\..\webjea"
$solutionfile = "$solutionpath\webjea.sln"
$publishpath = "$solutionpath\bin"
$packagepath = "$buildpath\Package"
$assemblyFile = "$solutionpath\My Project\AssemblyInfo.vb"
# $projpath = "C:\prj\webjea ce\WebJEA\WebJEA\WebJEA.vbproj"
$dllfile = "$publishpath\webjea.dll"
# $projxml = [xml](gc $projpath -raw)
$outpath = "C:\prj\webjea ce\Release"

$buildDT = get-date
$Major = 1
$Minor = 1
$build = "{0}{1}" -f $builddt.tostring('yy'),$builddt.DayOfYear.tostring('000')
[int]$rev = (($builddt.hour*60 + $builddt.minute)*60 + $builddt.second) /2
$ver = "{0}.{1}.{2}.{3}" -f $major, $minor, $build, $rev

#<Assembly: AssemblyVersion("0.0.0.0")>
$assemblyinfo = gc $assemblyfile | ?{$_ -notlike '*AssemblyVersion*'}
$assemblyinfo += '<Assembly: AssemblyVersion("{0}")>' -f $ver
$assemblyinfo | out-file $assemblyfile -Encoding UTF8

#call build process
& $buildbin $solutionfile "/t:Restore;Rebuild" "/property:Configuration=Release" #"/v:diag"
if ($LASTEXITCODE -ne 0) { Write-Warning "Build Failed"; return}

$curver = [System.Diagnostics.FileVersionInfo]::GetVersionInfo($dllfile).FileVersion
Write-Host "Release Version: $curver"

$outfile = "$outpath\webjea-$curver.zip"
if ((Test-Path $outfile)) { Remove-Item $outfile }
Write-Host "Target File; $outfile"

& robocopy.exe /mir $publishpath $publishtemp\site
Push-Location $publishtemp
& "$buildpath\zip.exe" -D -r -o $outfile .
Pop-Location

Push-Location $packagepath
& "$buildpath\zip.exe" -D -r -o $outfile .
Pop-Location


write-host -foreground cyan "Output file: $outfile"
#####zip structure
#dscConfig.inc.ps1
#dscDeploy.ps1
#Files\
#   config.json
#   validate.ps1
#   overview.ps1
#Site\
#   All files from Release build
