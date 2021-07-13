
$outpath = 'C:\Dropbox\Scripts\VB.NET\WebJEA\Release\Modules'
$srcpath = "C:\Dropbox\Scripts\PowerShell\WebJEAConfig\Module"
$srcdata = Import-LocalizedData -basedirectory $srcpath -FileName "webjeaconfig.psd1"
$ver = $srcdata.ModuleVersion
$name = 'WebJEAConfig'
$apikey = "811ad534-09c7-445f-8174-b940b516f9ea" 
if ($env:psmodulepath -notlike "*$outpath*" ) {$env:PSModulePath = $env:PSModulePath + ";$outpath"}
$targetpath = "$outpath\$name\$ver"

if ((test-path $targetpath)) {remove-item $targetpath -recurse -force}

new-item $targetpath -ItemType Directory
robocopy /mir $srcpath $targetpath

#publish-module -name $name -nugetapikey $apikey -repository PSGallery
#-path "C:\Dropbox\Scripts\VB.NET\WebJEA\WebJEAConfig\Module" 
#
publish-module -path $targetpath -NuGetApiKey $apikey -Verbose -Repository PSGallery
