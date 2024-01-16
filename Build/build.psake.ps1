Task Init {
    $script:buildpath = $PSScriptRoot
    write-host "BuildPath: $script:buildpath"
    $script:buildbin = 'C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\amd64\msbuild.exe'
    write-host "BuildBin: $script:buildbin"
    $script:publishtemp = "$script:buildpath\Release"
    write-host "PublishTemp: $script:publishtemp"
    $script:solutionpath = Resolve-Path "$script:buildpath\..\webjea"
    write-host "SolutionPath: $script:solutionpath"
    $script:solutionfile = "$script:solutionpath\webjea.sln"
    write-host "SolutionFile: $script:solutionfile"
    $script:binpath = "$script:solutionpath\bin"
    write-host "binpath: $script:binpath"
    $script:packagepath = "$script:buildpath\template"
    write-host "PackagePath: $script:packagepath"
    $script:assemblyFile = "$script:solutionpath\My Project\AssemblyInfo.vb"
    write-host "AssemblyFile: $script:assemblyFile"
    # $script:projpath = "C:\prj\webjea ce\WebJEA\WebJEA\WebJEA.vbproj"
    $script:dllfile = "$script:binpath\bin\webjea.dll"
    write-host "DllFile: $script:dllfile"
    # $script:projxml = [xml](gc $script:projpath -raw)
    $script:buildDT = Get-Date
    write-host "BuildDT: $script:builddt"
    $script:outpath = Resolve-Path "$script:buildpath\..\Release"
    New-Item $script:outpath -ItemType directory -ea 0
    write-host "Outpath: $script:outpath"
}

Task InitVersion -depends Init {
    $script:Major = 1
    $script:Minor = 1
    $script:build = '{0}{1}' -f $script:builddt.tostring('yy'), $script:builddt.DayOfYear.tostring('000')
    [int]$script:rev = (($script:builddt.hour * 60 + $script:builddt.minute) * 60 + $script:builddt.second) / 2
    $script:ver = '{0}.{1}.{2}.{3}' -f $script:major, $script:minor, $script:build, $script:rev
}

Task UpdateAssemblyVersion -depends InitVersion {
    #<Assembly: AssemblyVersion("0.0.0.0")>
    $script:assemblyinfo = Get-Content $script:assemblyfile | Where-Object { $_ -notlike '*AssemblyVersion*' }
    $script:assemblyinfo += '<Assembly: AssemblyVersion("{0}")>' -f $script:ver
    $script:assemblyinfo | Out-File $script:assemblyfile -Encoding UTF8
}

Task CleanBuildFolders -depends Init {
    #clean folders folder
    Get-ChildItem $script:binpath -Recurse -ea 0 | Remove-Item -Recurse -Confirm:$script:false -Force -ea 0
    Get-ChildItem $script:publishtemp -Recurse -ea 0 | Remove-Item -Recurse -Confirm:$script:false -Force -ea 0
}

Task Compile -depends CleanBuildFolders,UpdateAssemblyVersion {
    #call build process
    & $script:buildbin $script:solutionfile '-m' '/p:DeployOnBuild=true;PublishProfile=ReleaseBuild;Configuration=Release' #"/v:diag" "/t:Restore;Rebuild"
    if ($script:LASTEXITCODE -ne 0) { Write-Warning 'Build Failed'; return }
}

Task CopyBuild -depends Compile {
    Write-Host 'Copy to temp directory'
    & robocopy.exe /mir $script:binpath $script:publishtemp\site
}

Task CopyStarterFiles -depends CopyBuild {
    Write-Host 'Merge starter files'
    Copy-Item $script:packagepath\* -dest $script:publishtemp -Recurse -Force
}

Task Output -depends CopyStarterFiles {

    start $script:publishtemp
}

Task default -depends Output