param ([switch]$fast)
$ErrorActionPreference = "Stop"

$MyData = @{
    AllNodes = @(
        @{
            NodeName = '*'
            WebAppPoolName = 'WebJEA'
            AppPoolUserName = 'domain1\gmsa2$'
            AppPoolPassword = "" #no credential data is actually password because we're using gMSAs
            #if you use a non-msa, use another method to set the apppool identity
            WebJEAIISURI = 'WebJEA'
            WebJEAIISFolder = 'C:\inetpub\wwwroot\webjea'
            WebJEASourceFolder = 'C:\source'
            WebJEAScriptsFolder = 'C:\scripts'
            WebJEAConfigPath = 'C:\scripts\config.json' #must be in webjeascriptsfolder
            WebJEALogPath = 'c:\scripts'
            WebJEA_Nlog_LogFile = "c:\scripts\webjea.log"
            WebJEA_Nlog_UsageFile = "c:\scripts\webjea-usage.log"
            # WebJEARemoteErrors = $false
        },
        @{
            NodeName = 'MGMT1'
            Role = 'WebJEAServer'
            MachineFQDN = 'mgmt1.domain1.local'
            CertThumbprint = '381B9E24C3702BDAD1DA04C68BA97A9C736787E9'
            WebJEASourceFolder = 'C:\dropbox\scripts\vb.net\WebJEA\release'
            # WebJEARemoteErrors = $true
        },
        @{
            NodeName = 'WEB1'
            Role = 'WebJEAServer'
            MachineFQDN = 'web1.domain1.local'
            CertThumbprint = '50495F09B2DC05DB9BB47D834623D38508A50524'
            # WebJEARemoteErrors = $true
        },
        @{
            NodeName = 'WEB2'
            Role = 'WebJEAServer'
            MachineFQDN = 'web2.domain1.local'
            CertThumbprint = '5F37C6626380CD597BD4A36F83F70D0CBE0531D3'
            # WebJEARemoteErrors = $true
        }
    )
}


if (-not $fast) {
    #install necessary powershell modules
    write-host "Configuring Package Provider"
    install-packageprovider -name nuget -minimumversion 2.8.5.201 -force
    write-host "Trusting PSGallery"
    set-psrepository -Name psgallery -InstallationPolicy trusted
    #####install-module WebAdministrationDSC
    write-host "Installing DSC Modules"
    install-module xwebadministration
    install-module xXMLConfigFile 
    install-module cUserRightsAssignment
}

#create the group MSA account
#add-kdsrootkey -effectivetime ((get-date).addhours(-10))
#new-ADServiceAccount -name gmsa1 -dnshostname (get-addomaincontroller).hostname -principalsallowedtoretrievemanagedpassword mgmt1
#install-adserviceaccount gmsa1
#add-adgroupmember -identity "domain1\domain admins" -members (get-adserviceaccount gmsa1).distinguishedname
#at a later time, grant gmsa1 the permissions you want.

#cd wsman::localhost\client 
#Set-Item TrustedHosts * -confirm:$false -force
#restart-service winrm


write-host "Building Configuration"
. .\DSCConfig.inc.ps1
WebJEADeployment -ConfigurationData $MyData -verbose -OutputPath .\WebJEADeployment

write-host "Starting DSC"
Start-DscConfiguration -ComputerName $env:computername -Path .\WebJEADeployment -verbose -Wait -force
