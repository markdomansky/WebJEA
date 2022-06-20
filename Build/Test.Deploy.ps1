param ([string]$Computer = $env:computername, [switch]$fast)
$ErrorActionPreference = 'Stop'

#region DSC Data
$MyData = @{
    AllNodes = @(
        @{
            NodeName              = '*'
            WebAppPoolName        = 'WebJEA'
            AppPoolUserName       = 'domain1\gmsa2$'
            AppPoolPassword       = '' #no credential data is actually password because we're using gMSAs
            #if you use a non-msa, use another method to set the apppool identity
            WebJEAIISURI          = 'WebJEA'
            WebJEAIISFolder       = 'C:\inetpub\wwwroot\webjea'
            WebJEASourceFolder    = "c:\source"
            WebJEAScriptsFolder   = 'C:\scripts'
            WebJEAConfigPath      = 'C:\scripts\config.json' #must be in webjeascriptsfolder
            WebJEALogPath         = 'c:\scripts'
            WebJEA_Nlog_LogFile   = 'c:\scripts\webjea.log'
            WebJEA_Nlog_UsageFile = 'c:\scripts\webjea-usage.log'
            # WebJEARemoteErrors = $false
        },
        # @{
        #     NodeName           = 'web22dbg'
        #     Role               = 'WebJEAServer'
        #     MachineFQDN        = 'web22dbg.domain1.local'
        #     CertThumbprint     = 'B91AFAAE825D5C2CAA5B09534B136D957747E819'
        #     WebJEARemoteErrors = $true
        # },
        @{
            NodeName        = 'web22c'
            Role            = 'WebJEAServer'
            MachineFQDN     = 'web22c.domain1.local'
            CertThumbprint  = 'B21262B1758C7838657A801A497B6C46F0B64EEB'
            # WebJEARemoteErrors = $true
            AppPoolUserName = 'domain1\msa_web22c$'
        },
        @{
            NodeName        = 'web22d'
            Role            = 'WebJEAServer'
            MachineFQDN     = 'web22d.domain1.local'
            CertThumbprint  = 'BDC89DAF1C4048CFB6C7E4808D8748E449E9DAA1'
            # WebJEARemoteErrors = $true
            AppPoolUserName = 'domain1\msa_web22d$'
        }
    )
}
#endregion DSC Data

#region DSC Configuration
Configuration WebJEADeployment {

    Import-DscResource -ModuleName PSDesiredStateConfiguration
    #Import-DSCResource -ModuleName WebAdministrationDSC
    Import-DSCResource -ModuleName xWebAdministration -ModuleVersion 3.2.0
    Import-DSCResource -ModuleName xXMLConfigFile -ModuleVersion 2.0.0.3
    Import-DscResource -ModuleName cUserRightsAssignment -ModuleVersion 1.0.2

    Node $AllNodes.where{ $_.role -eq 'WebJEAServer' }.nodename {

        #Install Necessary Windows Features
        $WFs = @('Web-WebServer', 'Web-Default-Doc', 'Web-Http-Errors', 'Web-Static-Content', 'Web-IP-Security', 'Web-Security', 'Web-Windows-Auth', 'Web-Net-Ext45', 'Web-Asp-Net45', 'NET-Framework-45-Core', 'NET-Framework-45-ASPNET', 'Web-Stat-Compression', 'Web-Dyn-Compression', 'Web-HTTP-Redirect')
        foreach ($WF in $WFs)
        {
            WindowsFeature "WF_$WF"
            {
                Ensure = 'Present'
                Name   = $WF
            }
        }

        #build app pool
        xWebAppPool 'WebJEA_IISAppPool'
        {
            Name                  = $node.WebAppPoolName
            Ensure                = 'Present'
            State                 = 'Started'
            autoStart             = $true
            managedPipelineMode   = 'Integrated'
            managedRuntimeVersion = 'v4.0'
            identityType          = 'SpecificUser'
            loadUserProfile       = $true #this is necessary to be able to create remote pssessions and import them
        }

        ####2/3
        #this is how we use the GMSA without specifying a PW we don't know.  If using a regular user account, disable this and use the built-in credential support in xWebAppPool
        Script ChangeAppPoolIdentity
        {
            GetScript  = { return @{ AppPoolName = "$($using:Node.WebAppPoolName)" } }
            TestScript = {
                Import-Module webadministration -Verbose:$false
                $pool = Get-Item("IIS:\AppPools\$($using:Node.WebAppPoolName)")
                return $pool.processModel.userName -eq $using:Node.AppPoolUserName
            }
            SetScript  = {
                Import-Module webadministration -Verbose:$false

                $pool = Get-Item("IIS:\AppPools\$($using:Node.WebAppPoolName)");

                $pool.processModel.identityType = [String]('SpecificUser');
                $pool.processModel.userName = [String]($using:Node.AppPoolUserName)
                $pool.processModel.password = [String]($using:Node.AppPoolPassword)

                $pool | Set-Item
            }
            DependsOn  = '[xWebAppPool]WebJEA_IISAppPool'
        }

        #add webjea content
        File WebJEA_WebContent
        {
            Ensure          = 'Present'
            SourcePath      = $node.WebJEASourceFolder + '\site'
            DestinationPath = $node.WebJEAIISFolder
            Recurse         = $true
            Type            = 'Directory'
            MatchSource     = $true #always copy files to ensure accurate
            Checksum        = 'SHA-256'
        }

        #build webjea web app subdirectory
        xWebApplication 'WebJEA_IISWebApp'
        {
            Website                 = 'Default Web Site'
            Name                    = $node.WebJEAIISURI
            WebAppPool              = $node.WebAppPoolName
            PhysicalPath            = $node.WebJEAIISFolder
            AuthenticationInfo      = MSFT_xWebApplicationAuthenticationInformation
            {
                Anonymous = $false
                Basic     = $false
                Digest    = $false
                Windows   = $true
            }
            PreloadEnabled          = $true
            ServiceAutoStartEnabled = $true
            SslFlags                = @('ssl')

            DependsOn               = '[WindowsFeature]WF_Web-WebServer'
        }


        #add redirect
        #Package UrlRewrite {
        #    #Install URL Rewrite module for IIS
        #   DependsOn = "[WindowsFeature]WF_Web-WebServer"
        #    Ensure = "Present"
        #    Name = "IIS URL Rewrite Module 2"
        #    Path = "https://download.microsoft.com/download/C/9/E/C9E8180D-4E51-40A6-A9BF-776990D8BCA9/rewrite_amd64.msi"
        #    Arguments = "/quiet"
        #    ProductId = "08F0318A-D113-4CF0-993E-50F191D397AD"
        #}

        #Script ReWriteRule {
        #    #Adds rewrite allowedServerVariables to applicationHost.config
        #    DependsOn = "[Package]UrlRewrite"
        #    SetScript = {
        #        $current = Get-WebConfiguration /system.webServer/rewrite/allowedServerVariables | select -ExpandProperty collection | ?{$_.ElementTagName -eq "add"} | select -ExpandProperty name
        #        $expected = @("HTTPS", "HTTP_X_FORWARDED_FOR", "HTTP_X_FORWARDED_PROTO", "REMOTE_ADDR")
        #        $missing = $expected | where {$current -notcontains $_}
        #        try {
        #            Start-WebCommitDelay
        #            $missing | %{ Add-WebConfiguration /system.webServer/rewrite/allowedServerVariables -atIndex 0 -value @{name="$_"} -Verbose }
        #            Stop-WebCommitDelay -Commit $true
        #        } catch [System.Exception] {
        #            $_ | Out-String
        #        }
        #    }
        #    TestScript = {
        #        $current = Get-WebConfiguration /system.webServer/rewrite/allowedServerVariables | select -ExpandProperty collection | select -ExpandProperty name
        #        $expected = @("HTTPS", "HTTP_X_FORWARDED_FOR", "HTTP_X_FORWARDED_PROTO", "REMOTE_ADDR")
        #        $result = -not @($expected| where {$current -notcontains $_}| select -first 1).Count
        #        return $result
        #    }
        #    GetScript = {
        #        $allowedServerVariables = Get-WebConfiguration /system.webServer/rewrite/allowedServerVariables | select -ExpandProperty collection
        #        return $allowedServerVariables
        #    }
        #}




        #configure SSL
        xWebsite 'DefaultWeb'
        {
            Ensure      = 'Present'
            Name        = 'Default Web Site'
            State       = 'Started'
            BindingInfo = @(
                MSFT_xWebBindingInformation
                {
                    Protocol              = 'https'
                    Port                  = '443'
                    CertificateStoreName  = 'MY'
                    CertificateThumbprint = $node.CertThumbprint
                    HostName              = $node.machinefqdn
                    IPAddress             = '*'
                    SSLFlags              = '1'
                }#;
                # MSFT_xWebBindingInformation {
                #     Protocol = 'https'
                #     Port = '443'
                #     CertificateStoreName = 'MY'
                #     CertificateThumbprint = $node.CertThumbprint
                #     HostName = $node.nodename
                #     IPAddress = '*'
                #     SSLFlags = '1'
                # };
                #MSFT_xWebBindingInformation {
                #     Protocol = 'http'
                #     Port = '80'
                #     HostName = $null
                #     IPAddress = '*'
                # }
            )
            DependsOn   = @('[WindowsFeature]WF_Web-WebServer', '[File]WebJEA_WebContent')
        }


        #set json config location in web.config
        XMLConfigFile 'WebJEAConfig'
        {
            Ensure             = 'Present'
            ConfigPath         = "$($node.WebJEAIISFolder)\web.config"
            XPath              = "/configuration/applicationSettings/WebJEA.My.MySettings/setting[@name='configfile']"
            isElementTextValue = $true
            Name               = 'value'
            Value              = $node.WebJEAConfigPath
            DependsOn          = '[File]WebJEA_WebContent', '[xWebsite]DefaultWeb'
        }

        #set nlog log location in nlog.config in iis site
        XMLConfigFile 'WebJEA_NLOGFile'
        {
            Ensure      = 'Present'
            ConfigPath  = "$($node.WebJEAIISFolder)\nlog.config"
            XPath       = "/nlog/targets/target[@name='file']/target"
            isAttribute = $true
            Name        = 'fileName'
            Value       = $node.WebJEA_Nlog_LogFile
            DependsOn   = '[File]WebJEA_WebContent'
        }
        XMLConfigFile 'WebJEA_NLOGUsageFile'
        {
            Ensure      = 'Present'
            ConfigPath  = "$($node.WebJEAIISFolder)\nlog.config"
            XPath       = "/nlog/targets/target[@name='fileSummary']/target"
            isAttribute = $true
            Name        = 'fileName'
            Value       = $node.WebJEA_Nlog_UsageFile
            DependsOn   = '[File]WebJEA_WebContent'
        }


        #assign permissions to scripts folder?

        #Configure Default Web Site to support SSL

        ####3/3
        #add to logon as service
        cUserRight WebJEA_Batch
        {
            ensure    = 'Present'
            constant  = 'SeServiceLogonRight'
            principal = 'IIS APPPOOL\' + $node.AppPoolPoolName
            dependson = '[xWebAppPool]WebJEA_IISAppPool'
        }

        #add gmsa to iusrs
        Group WebJEA_IISIUSRS
        {
            GroupName        = 'IIS_IUSRS'
            MembersToInclude = $node.AppPoolUserName
            Ensure           = 'Present'
        }


        #apppool timeout in webconfig

        #add starter scripts
        File WebJEA_ScriptsContent
        {
            Ensure          = 'Present'
            SourcePath      = $node.WebJEASourceFolder + '\StarterFiles'
            DestinationPath = $node.WebJEAScriptsFolder
            Recurse         = $true
            Type            = 'Directory'
            MatchSource     = $false #always copy files to ensure accurate
            Checksum        = 'SHA-256'
        }

    } #/WebJEAServer

}
#endregion DSC Configuration

#this bootstraps DSC
if (-not $fast) #use fast if you're trying to run it again.
{
    Write-Host 'Setting up local computer'
    #install necessary powershell modules
    Write-Host 'Configuring Package Provider'
    Install-PackageProvider -Name nuget -MinimumVersion 2.8.5.201 -Force
    Write-Host 'Trusting PSGallery'
    Set-PSRepository -Name psgallery -InstallationPolicy trusted
    #####install-module WebAdministrationDSC
    Write-Host 'Installing DSC Modules'
    install-module powershellget -required 2.2.5
    Install-Module xwebadministration -required 3.2.0
    Install-Module xXMLConfigFile -required 2.0.0.3
    Install-Module cUserRightsAssignment -require 1.0.2
}

#create the group MSA account
#if (-not (get-kdsrootkey)) {add-kdsrootkey -effectivetime ((get-date).addhours(-10))} #should only be run once.
#$msaname = ("msa_{0}" -f $env:computername)
#new-ADServiceAccount -name $msaname -dnshostname (get-addomaincontroller).hostname -principalsallowedtoretrievemanagedpassword $env:computername
#install-adserviceaccount $msaname
#at a later time, grant gmsa1 the permissions you want.

Write-Host 'Building Configuration'
WebJEADeployment -ConfigurationData $MyData -verbose -OutputPath .\WebJEADeployment

Write-Host 'Starting DSC'
Start-DscConfiguration -ComputerName $Computer -Path .\WebJEADeployment -Verbose -Wait -Force
