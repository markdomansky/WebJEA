param(
    [string]$ProjectPath="$psscriptroot\..\..\WebJEA",
    [string]$PublishProfile = 'WebDebugServer',
    [string]$RemoteComputer = 'webdebug.domain1.local',
    [string]$RemoteWebRoot = '\\webdebug.domain1.local\iis',
    [string]$RemoteDebuggerPath = 'C:\RemoteDebugger\msvsmon.exe'
)

# 1) Publish the project (uncomment the option that matches your project)
Write-Host "Publishing $ProjectPath..."
# For SDK-style / dotnet projects:
# & dotnet publish $ProjectPath -c Release -o "$env:TEMP\publishOutput"
# For MSBuild publish profile (Web Deploy):
msbuild.exe $ProjectPath /t:Publish /p:PublishProfile=$PublishProfile /p:Configuration=Release

# Set path to the publish output (adjust if using msbuild publish target)
$pubOutput = Join-Path -Path (Split-Path $ProjectPath -Parent) -ChildPath 'bin\Release\netstandard\publish'
# replace above with actual folder if needed

# 2) Copy web.config to the remote server
Write-Host "Copying web.config to $RemoteComputer..."
try
{
    $localWebConfig = Join-Path $pubOutput 'web.Remote.config'
    $remotePath = Join-Path $RemoteWebRoot 'web.config'
    Copy-Item -Path $localWebConfig -Destination $remotePath -Force
    Write-Host 'web.config copied.'
}
finally
{
    # keep session for next step or remove if not needed
}

# # 3) Start remote debugger (runs msvsmon on the remote host)
# Write-Host "Starting remote debugger on $RemoteComputer..."
# Invoke-Command -Session $session -ScriptBlock {
#     param($dbgPath)
#     if (Test-Path $dbgPath)
#     {
#         Start-Process -FilePath $dbgPath -ArgumentList '/noauth' -WindowStyle Hidden
#         Write-Output "Remote debugger started from $dbgPath"
#     }
#     else
#     {
#         Write-Output "Remote debugger not found at $dbgPath"
#     }
# } -ArgumentList $RemoteDebuggerPath

# 4) Cleanup
Remove-PSSession $session
Write-Host 'Publish + remote debugger start complete. You can now attach from Visual Studio (Debug > Attach to Process).'