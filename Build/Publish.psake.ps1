Task Init {
    $script:releasepath = "$psscriptroot\release"
    $script:outpath = resolve-path ("$psscriptroot\..\release")
}

Task GetVersion -depends Init {
    $dllfile = "$releasepath\site\bin\webjea.dll"
    $script:curver = [System.Diagnostics.FileVersionInfo]::GetVersionInfo($dllfile).FileVersion
    Write-Host "Release Version: $script:curver"
}

Task Compress -depends GetVersion {
    $script:outfile = "$script:outpath\webjea-$script:curver.zip"
    if ((Test-Path $script:outfile)) { Remove-Item $script:outfile }
    Write-Host "Output File; $script:outfile"

    Write-Host 'Archive temp directory'
    Push-Location $script:releasepath
    Compress-Archive -Path .\* -DestinationPath $script:outfile
    pop-location
}

Task Tag -depends Compress {
    push-location $psscriptroot
    #tag
    & git tag -a "$script:curver" -m "$script:curver"

    #push
    & git push origin "$script:curver"

    pop-location
}

Task Publish -depends Tag {
    $token = gc $psscriptroot\publish.json | convertfrom-json | select -expand accesstoken
    .\Publish-GithubRelease.ps1 -AccessToken $token -repositoryowner markdomansky -repositoryname webjea -tagname $script:curver -name $script:curver -artifact $script:outfile
}


Task Default -depends Publish