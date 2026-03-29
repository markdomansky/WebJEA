param ($name, $webjeahostname, $webjeausername)
sleep 2
Write-Host "webjeahostname: $webjeahostname"
Write-Host "webjeausername: $webjeausername"
Write-Host "name: $name"
Write-Verbose "Verbose Data"
if ($name -eq "chrome")
{
    write-verbose "Checking for Chrome Processes"
    Write-Warning "Memory limit exceeded"
    Write-Information "Info Test"
    Write-Host "name=chrome"
    $procs = Get-Process $name
    Write-Host "Processes: $($procs.count)"
    Write-Host "CPU time: $($procs | measure -sum cpu | select -ExpandProperty sum)"
    Write-Host "Total Memory: all"
    Get-Process $name | out-string
    Write-Error "Out of Memory due to Chrome"
}
elseif ($name -ne $null -and $name -ne "")
{
    Write-Host "name=$name"
    Get-Process $name -ea 0 | out-string
}
else
{
    Write-Host "name=null"
    get-process "System" | out-string
}