#Requires -RunAsAdministrator
[CmdletBinding()]
param (
    [Parameter(Mandatory)]
    [ValidateScript({ Test-Path -Path $_ -PathType Leaf })]
    [string]$SettingsFile,

    [Parameter()]
    [switch]$TestOnly,

    [Parameter()]
    [switch]$returnSteps,

    [Parameter()]
    [ValidateSet('PowerShell', 'Server', 'WebServer', 'WebJEA', 'Finalize','All')]
    [string[]]$OnlySections = 'All'
)
begin
{
    $ErrorActionPreference = 'Stop'

    #region Config
    function GetSteps_PowerShell
    {

        @{Description = '***** Configuring PowerShell for DSC *****' }
        #Package Management and NuGet provider are required to install the other DSC resource modules from the PowerShell Gallery, so ensure they're installed before trying to run any DSC resources.
        @{
            Description = 'NuGet Package Provider >=2.8.5.201 is installed'
            TestScript  = {
                $verboseMemory = $VerbosePreference
                $VerbosePreference = 'SilentlyContinue'
                $provider = Get-PackageProvider -Name NuGet -ErrorAction SilentlyContinue
                $VerbosePreference = $verboseMemory
                return $provider -and ($provider.Version -ge [Version]'2.8.5.201')
            }
            SetScript   = {
                $verboseMemory = $VerbosePreference
                $VerbosePreference = 'SilentlyContinue'
                Install-PackageProvider -Name NuGet -Force -MinimumVersion '2.8.5.201'
                #reload packagemanagement to ensure the new provider is available in the current session
                # Remove-Module PackageManagement -Force
                # Import-Module PackageManagement -Force
                $VerbosePreference = $verboseMemory
            }
        }

        #WinRM is required for DSC to work, so ensure it's configured before trying to run any DSC resources.
        # WinRM service startup type is Automatic so it survives reboots
        @{ Description = 'WinRM service startup type is Automatic'
            TestScript = { (Get-Service -Name WinRM).StartType -eq 'Automatic' }
            SetScript  = { Set-Service -Name WinRM -StartupType Automatic }
        }

        # WinRM service is running
        @{ Description = 'WinRM service is running'
            TestScript = { (Get-Service -Name WinRM).Status -eq 'Running' }
            SetScript  = { Start-Service -Name WinRM }
        }

        # At least one WinRM listener is configured
        @{ Description = 'WinRM has at least one listener configured'
            TestScript = { (Get-ChildItem WSMan:\localhost\Listener | Measure-Object).Count -gt 0 }
            SetScript  = { winrm quickconfig -quiet }
        }

        # # WinRM is listening on IPv6 — only checked when IPv6 is enabled on any adapter
        # [bool]$ipv6Enabled = Get-NetAdapterBinding -ComponentID ms_tcpip6 -ErrorAction SilentlyContinue | Where-Object Enabled
        # if ($ipv6Enabled)
        # {
        #     @{ Description = 'WinRM is listening on IPv6'
        #         TestScript       = {
        #             $null -ne (Get-NetTCPConnection -LocalPort 5985 -State Listen -ErrorAction SilentlyContinue |
        #                     Where-Object { $_.LocalAddress -match ':' })
        #         }
        #         SetScript        = { Restart-Service -Name WinRM }
        #     }
        # } else {
        # }
        #on an IPv4 address (0.0.0.0 means all IPv4 interfaces)

        # WinRM is listening
        @{ Description = 'WinRM is listening on IPv4'
            TestScript = { $null -ne (Get-NetTCPConnection -LocalPort 5985 -State Listen -ErrorAction SilentlyContinue) }
            SetScript  = { Restart-Service -Name WinRM }
        }

        @{
            Description = 'PowerShellGet v2.2.5 installed'
            TestScript  = {
                $module = Get-Module -Name PowerShellGet -ListAvailable | Sort-Object Version -Descending | Select-Object -First 1
                return $module -and ($module.Version -ge [Version]'2.2.5')
            }
            SetScript   = {
                Install-Module -Name PowerShellGet -Force -RequiredVersion '2.2.5'
                #reload PowerShellGet to ensure the new version is available in the current session
                Remove-Module PowerShellGet -Force
                Import-Module PowerShellGet -Force
            }
        }
        @{
            Description = 'PSGallery Installation Policy is Trusted'
            TestScript  = { (Get-PSRepository -Name PSGallery -ErrorAction SilentlyContinue).InstallationPolicy -eq 'Trusted' }
            SetScript   = { Set-PSRepository -Name PSGallery -InstallationPolicy Trusted }
        }

        #Install required modules
        $modules = @(
            'xWebAdministration'
            'WebAdministrationDsc'
            'xXMLConfigFile'
            'cUserRightsAssignment'
            'WebJEAConfig'
            'DSCR_FileContent'
        )
        foreach ($module in $modules)
        {
            @{
                Description = "PowerShell Module Installed: $module"
                TestScript  = { Get-Module -Name $module -ListAvailable -ErrorAction SilentlyContinue }.GetNewClosure()
                SetScript   = { Install-Module -Name $module -Force }.GetNewClosure()
            }
        }

    }
    function GetSteps_Server
    {
        @{Description = '***** Configuring the Server *****' }

        #Install Necessary Windows Features
        @(
            'Web-WebServer'
            'Web-Default-Doc'
            'Web-Http-Errors'
            'Web-Static-Content'
            'Web-IP-Security'
            'Web-Security'
            'Web-Windows-Auth'
            'Web-Net-Ext45'
            'Web-Asp-Net45'
            'NET-Framework-45-Core'
            'NET-Framework-45-ASPNET'
            'Web-Stat-Compression'
            'Web-Dyn-Compression'
            'Web-HTTP-Redirect'
        ) | ForEach-Object {
            @{
                Description = "Windows Feature Installed: $_"
                Module      = 'PSDesiredStateConfiguration'
                Resource    = 'WindowsFeature'
                Property    = @{
                    Ensure = 'Present'
                    Name   = $_
                }
            }
        }

        #IIS URL Rewrite module is required for HTTP-to-HTTPS and backward-compatibility rules
        @{
            Description = 'IIS URL Rewrite Module 2.1 is installed'
            TestScript  = {
                Import-Module WebAdministration -Verbose:$false
                $null -ne (Get-WebGlobalModule -Name 'RewriteModule' -ErrorAction SilentlyContinue)
            }
            SetScript   = {
                $msiUrl = 'https://download.microsoft.com/download/1/2/8/128E2E22-C1B9-44A4-BE2A-5859ED1D4592/rewrite_amd64_en-US.msi'
                $msiPath = Join-Path $env:TEMP 'urlrewrite2.msi'
                Write-Verbose "Downloading IIS URL Rewrite 2.1 from Microsoft..."
                Invoke-WebRequest -Uri $msiUrl -OutFile $msiPath -UseBasicParsing
                Start-Process -FilePath msiexec.exe -ArgumentList "/i `"$msiPath`" /quiet /norestart" -Wait
                Remove-Item $msiPath -Force -ErrorAction SilentlyContinue
            }
        }

        #add site contents
        @{
            Description = "Site contents copied to $($settings.SitePath)"
            TestScript  = { $false } # Always overwrite — no checksum comparison
            SetScript   = {
                $src = "$($settings.SourcePath)\site"
                $dst = $settings.SitePath
                # /E  - copy subdirectories including empty ones
                # /IS - include same files (force-overwrites even when content matches)
                # /IT - include tweaked files (same timestamp, different size)
                $params = @($src,$dst,'/E', '/IS', '/IT', '/NJH', '/NJS', '/NFL', '/NDL')
                write-verbose "robocopy.exe $($params -join ' ')"
                if (-not (Test-Path $dst)) { New-Item -Path $dst -ItemType Directory -Force | Out-Null }
                & robocopy.exe $params
                if ($LASTEXITCODE -ge 8) { throw "robocopy failed with exit code $LASTEXITCODE" }
            }
        }

        #Copy the scripts to the server
        if ((Test-Path -Path $settings.ScriptsPath -PathType Container) -and
            (Get-ChildItem -Path $settings.ScriptsPath -Recurse | Measure-Object).Count -gt 1)
        {
            @{
                Description = 'Starter scripts copied (skipped, files already exist)'
                TestScript  = { $true }
                SetScript   = { }
            }
        }
        else
        {
            #add starter scripts
            @{
                Description = 'Starter scripts copied'
                Module      = 'PSDesiredStateConfiguration'
                Resource    = 'file'
                Property    = @{
                    Ensure          = 'Present'
                    SourcePath      = $settings.SourcePath + '\Scripts'
                    DestinationPath = $settings.ScriptsPath
                    Recurse         = $true
                    type            = 'Directory'
                    MatchSource     = $true #always copy files to ensure accurate
                    Checksum        = 'SHA-256'
                }
            }
        }

    }
    function GetSteps_WebServer($Settings)
    {
        @{ Description = '***** Configuring the Web Server *****' }
        if ($settings.DisableDefaultWebsite)
        {
            #Disable the default website and app pool to free up port 80 and 443 and avoid confusion.  This is optional because some users may want to use the default website instead of creating a new one
            @{
                Description = 'Default Web Site is stopped'
                Module      = 'WebAdministrationDsc'
                Resource    = 'Website'
                Property    = @{
                    Ensure = 'Present'
                    Name   = 'Default Web Site'
                    State  = 'Stopped'
                }
            }

            @{
                Description = 'DefaultAppPool is stopped'
                Module      = 'xWebAdministration'
                Resource    = 'xWebAppPool'
                Property    = @{
                    Name   = 'DefaultAppPool'
                    Ensure = 'Present'
                    State  = 'Stopped'
                }
            }
        }

        # AppPool base config, will adjust identity settings next
        $appPoolRes = @{
            Description = 'App Pool created'
            Module      = 'xWebAdministration'
            Resource    = 'xWebAppPool'
            Property    = @{
                Name                  = $settings.AppPoolName
                Ensure                = 'Present'
                State                 = 'Started'
                autoStart             = $true
                managedPipelineMode   = 'Integrated'
                managedRuntimeVersion = 'v4.0'
                identityType          = 'SpecificUser'
                # userName              = $null
                # password              = $null
                loadUserProfile       = $Settings.AppPoolLoadUserProfile
            }
        }

        #build app pool and set identity
        if ($settings.apppoolpassword)
        {
            #FIXME Need to work on this
            #User Account with known password
            $appPoolRes.Description = $appPoolRes.Description + ' (with cred)'
            $appPoolRes.userName = $settings.AppPoolUserName
            $appPoolRes.password = $settings.AppPoolPassword

            Write-Output $appPoolRes
        }
        else
        {
            #no additional changes required
            $appPoolRes.Description = $appPoolRes.Description + ' (no cred)'
            Write-Output $appPoolRes
            #GMSA - set username with trailing $ and blank password, and set identity type to SpecificUser
            #set the app pool to use GMSA.  This is a separate resource because xWebAppPool doesn't have built in support for gMSA accounts where you don't specify the password.
            @{
                Description = 'App Pool GMSA Identity configured'
                TestScript  = {
                    Import-Module webadministration -Verbose:$false
                    $pool = Get-Item("IIS:\AppPools\$($settings.AppPoolName)")
                    # Write-Verbose ('{0} -eq {1} = {2}' -f $pool.processModel.userName, "$($settings.AppPoolUserName)", ($pool.processModel.userName -eq $settings.AppPoolUserName))
                    return ($pool.processModel.userName -eq $settings.AppPoolUserName)
                }
                SetScript   = {
                    Import-Module webadministration -Verbose:$false
                    $pool = Get-Item("IIS:\AppPools\$($settings.AppPoolName)")

                    $pool.processModel.identityType = [String]('SpecificUser')
                    $pool.processModel.userName = [String]($($settings.AppPoolUserName))
                    $pool.processModel.password = '' #[String]($settings.AppPoolPassword)
                    # write-host ($pool | convertto-json -depth 2)
                    $pool | Set-Item
                }
            }
        }

        #build IIS site
        @{
            Description = 'Web Site created'
            Module      = 'WebAdministrationDsc'
            Resource    = 'Website'
            Property    = @{
                Ensure                  = 'Present'
                Name                    = $settings.SiteName
                ApplicationPool         = $settings.AppPoolName
                PhysicalPath            = $settings.SitePath
                State                   = 'Started'
                PreloadEnabled          = $true
                ServiceAutoStartEnabled = $true
            }
        }

        #Configure Authentication - one step per auth type
        $AuthSettings = @{
            Anonymous = $false
            Basic     = $false
            Digest    = $false
            Windows   = $true
        }
        foreach ($key in $AuthSettings.Keys)
        {
            $authType = $key
            $shouldBeEnabled = $AuthSettings[$key]
            $Filter = "system.webServer/security/authentication/${authType}Authentication"
            @{
                Description = "IIS $authType authentication allows overrides at the site level"
                TestScript  = {
                    Import-Module WebAdministration -Verbose:$false
                    # $section = Get-WebConfiguration -Filter $filter -PSPath "MACHINE/WEBROOT/APPHOST"
                    $section = Get-WebConfiguration -Filter $filter -PSPath 'MACHINE/WEBROOT/APPHOST'
                    return ($section.OverrideModeEffective -eq 'Allow')
                }.GetNewClosure()
                SetScript   = {
                    & "$env:windir\system32\inetsrv\appcmd.exe" unlock config -section:"$filter"
                }.GetNewClosure()
            }
            @{
                Description = "IIS $authType authentication is $(if ($shouldBeEnabled) { 'enabled' } else { 'disabled' }) on $($settings.SiteName)"
                TestScript  = "
                    Import-Module WebAdministration -Verbose:`$false
                    `$section = Get-WebConfiguration -Filter $filter -PSPath 'IIS:\Sites\$($settings.SiteName)'
                    return (`$section.Enabled -eq `$$shouldBeEnabled)
                "
                SetScript   = "
                    Import-Module WebAdministration -Verbose:`$false
                    Set-WebConfigurationproperty -Filter $filter -PSPath 'IIS:\Sites\$($settings.SiteName)' -name enabled -value `$$shouldBeEnabled
                "
            }
        }

        #configure SSL binding
        if ($settings.CertThumbprint)
        {
            @{
                Description = 'SSL binding configured'
                TestScript  = {
                    Import-Module WebAdministration -Verbose:$false
                    $binding = Get-WebBinding -Name $settings.siteName -Protocol 'https' -Port 443 -ErrorAction SilentlyContinue
                    return ($null -ne $binding) -and ($binding.certificateHash -eq $settings.certThumbprint)
                }
                SetScript   = {
                    Import-Module WebAdministration -Verbose:$false
                    # Remove any existing https binding on port 443 before re-adding
                    Remove-WebBinding -Name $settings.siteName -Protocol 'https' -Port 443 -ErrorAction SilentlyContinue
                    New-WebBinding -Name $settings.siteName -Protocol 'https' -Port 443 -HostHeader $settings.siteFQDN -IPAddress '*' -SslFlags 1
                    # Assign the certificate to the new binding
                    $binding = Get-WebBinding -Name $settings.siteName -Protocol 'https' -Port 443
                    $binding.AddSslCertificate($settings.certThumbprint, 'MY')
                }
            }
        }

        #URL Rewrite rule: force HTTP traffic to HTTPS
        if ($settings.RedirectPort80To443)
        {
            @{
                Description = 'URL Rewrite rule node exists: HTTP to HTTPS Redirect'
                TestScript  = {
                    Import-Module WebAdministration -Verbose:$false
                    $rule = Get-WebConfigurationProperty -pspath "IIS:\Sites\$($settings.SiteName)" `
                        -filter "system.webServer/rewrite/rules/rule[@name='HTTP to HTTPS Redirect']" `
                        -name '.' -ErrorAction SilentlyContinue
                    ($null -ne $rule) -and ($rule.stopProcessing -eq $true)
                }
                SetScript   = {
                    Import-Module WebAdministration -Verbose:$false
                    $sitePath = "IIS:\Sites\$($settings.SiteName)"
                    $ruleName = 'HTTP to HTTPS Redirect'
                    Remove-WebConfigurationProperty -pspath $sitePath `
                        -filter 'system.webServer/rewrite/rules' -name '.' `
                        -AtElement @{ name = $ruleName } -ErrorAction SilentlyContinue
                    Add-WebConfigurationProperty -pspath $sitePath `
                        -filter 'system.webServer/rewrite/rules' -name '.' `
                        -value @{ name = $ruleName; stopProcessing = $true }
                }.GetNewClosure()
            }
            @{
                Description = 'URL Rewrite rule match configured: HTTP to HTTPS Redirect'
                TestScript  = {
                    Import-Module WebAdministration -Verbose:$false
                    $match = Get-WebConfigurationProperty -pspath "IIS:\Sites\$($settings.SiteName)" `
                        -filter "system.webServer/rewrite/rules/rule[@name='HTTP to HTTPS Redirect']/match" `
                        -name '.' -ErrorAction SilentlyContinue
                    $match.url -eq '(.*)'
                }
                SetScript   = {
                    Import-Module WebAdministration -Verbose:$false
                    Set-WebConfigurationProperty -pspath "IIS:\Sites\$($settings.SiteName)" `
                        -filter "system.webServer/rewrite/rules/rule[@name='HTTP to HTTPS Redirect']/match" `
                        -name 'url' -value '(.*)'
                }
            }
            @{
                Description = 'URL Rewrite rule HTTPS condition configured: HTTP to HTTPS Redirect'
                TestScript  = {
                    Import-Module WebAdministration -Verbose:$false
                    # Avoid using {@input='{HTTPS}'} as an XPath predicate — the IIS config API
                    # treats curly braces as .NET format specifiers and throws a FormatException.
                    # Enumerate all conditions and match in PowerShell instead.
                    $conds = @(Get-WebConfiguration -pspath "IIS:\Sites\$($settings.SiteName)" `
                        -filter "system.webServer/rewrite/rules/rule[@name='HTTP to HTTPS Redirect']/conditions/add" `
                        -ErrorAction SilentlyContinue)
                    $cond = $conds | Where-Object { $_.input -eq '{HTTPS}' }
                    ($null -ne $cond) -and ($cond.pattern -eq 'off') -and ($cond.ignoreCase -eq $true)
                }
                SetScript   = {
                    Import-Module WebAdministration -Verbose:$false
                    $sitePath = "IIS:\Sites\$($settings.SiteName)"
                    $ruleName = 'HTTP to HTTPS Redirect'
                    # Clear all conditions then re-add — avoids the FormatException from {HTTPS} in XPath
                    Clear-WebConfiguration -pspath $sitePath `
                        -filter "system.webServer/rewrite/rules/rule[@name='$ruleName']/conditions" `
                        -ErrorAction SilentlyContinue
                    Add-WebConfigurationProperty -pspath $sitePath `
                        -filter "system.webServer/rewrite/rules/rule[@name='$ruleName']/conditions" `
                        -name '.' -value @{ input = '{HTTPS}'; pattern = 'off'; ignoreCase = $true }
                }
            }
            @{
                Description = 'URL Rewrite rule action configured: HTTP to HTTPS Redirect'
                TestScript  = {
                    Import-Module WebAdministration -Verbose:$false
                    $action = Get-WebConfigurationProperty -pspath "IIS:\Sites\$($settings.SiteName)" `
                        -filter "system.webServer/rewrite/rules/rule[@name='HTTP to HTTPS Redirect']/action" `
                        -name '.' -ErrorAction SilentlyContinue
                    ($action.type            -eq 'Redirect') -and
                    ($action.url             -eq 'https://{HTTP_HOST}/{R:1}') -and
                    ($action.redirectType    -eq 'Permanent') -and
                    ($action.appendQueryString -eq $true)
                }
                SetScript   = {
                    Import-Module WebAdministration -Verbose:$false
                    $sitePath = "IIS:\Sites\$($settings.SiteName)"
                    $filter   = "system.webServer/rewrite/rules/rule[@name='HTTP to HTTPS Redirect']/action"
                    Set-WebConfigurationProperty -pspath $sitePath -filter $filter -name 'type'              -value 'Redirect'
                    Set-WebConfigurationProperty -pspath $sitePath -filter $filter -name 'url'               -value 'https://{HTTP_HOST}/{R:1}'
                    Set-WebConfigurationProperty -pspath $sitePath -filter $filter -name 'redirectType'      -value 'Permanent'
                    Set-WebConfigurationProperty -pspath $sitePath -filter $filter -name 'appendQueryString' -value $true
                }
            }
        }

        #URL Rewrite rule: transparently rewrite /webjea/* to /* (server-side rewrite preserves POST body and query string)
        if ($settings.EnableBackwardCompatibility)
        {
            @{
                Description = 'URL Rewrite rule node exists: WebJEA Backward Compatibility'
                TestScript  = {
                    Import-Module WebAdministration -Verbose:$false
                    $rule = Get-WebConfigurationProperty -pspath "IIS:\Sites\$($settings.SiteName)" `
                        -filter "system.webServer/rewrite/rules/rule[@name='WebJEA Backward Compatibility']" `
                        -name '.' -ErrorAction SilentlyContinue
                    ($null -ne $rule) -and ($rule.stopProcessing -eq $true)
                }
                SetScript   = {
                    Import-Module WebAdministration -Verbose:$false
                    $sitePath = "IIS:\Sites\$($settings.SiteName)"
                    $ruleName = 'WebJEA Backward Compatibility'
                    Remove-WebConfigurationProperty -pspath $sitePath `
                        -filter 'system.webServer/rewrite/rules' -name '.' `
                        -AtElement @{ name = $ruleName } -ErrorAction SilentlyContinue
                    Add-WebConfigurationProperty -pspath $sitePath `
                        -filter 'system.webServer/rewrite/rules' -name '.' `
                        -value @{ name = $ruleName; stopProcessing = $true }
                }
            }
            @{
                Description = 'URL Rewrite rule match configured: WebJEA Backward Compatibility'
                TestScript  = {
                    Import-Module WebAdministration -Verbose:$false
                    $match = Get-WebConfigurationProperty -pspath "IIS:\Sites\$($settings.SiteName)" `
                        -filter "system.webServer/rewrite/rules/rule[@name='WebJEA Backward Compatibility']/match" `
                        -name '.' -ErrorAction SilentlyContinue
                    ($match.url -eq '^webjea/(.*)') -and ($match.ignoreCase -eq $true)
                }
                SetScript   = {
                    Import-Module WebAdministration -Verbose:$false
                    $sitePath = "IIS:\Sites\$($settings.SiteName)"
                    $filter   = "system.webServer/rewrite/rules/rule[@name='WebJEA Backward Compatibility']/match"
                    Set-WebConfigurationProperty -pspath $sitePath -filter $filter -name 'url'        -value '^webjea/(.*)'
                    Set-WebConfigurationProperty -pspath $sitePath -filter $filter -name 'ignoreCase' -value $true
                }
            }
            @{
                Description = 'URL Rewrite rule action configured: WebJEA Backward Compatibility'
                TestScript  = {
                    Import-Module WebAdministration -Verbose:$false
                    $action = Get-WebConfigurationProperty -pspath "IIS:\Sites\$($settings.SiteName)" `
                        -filter "system.webServer/rewrite/rules/rule[@name='WebJEA Backward Compatibility']/action" `
                        -name '.' -ErrorAction SilentlyContinue
                    ($action.type             -eq 'Redirect') -and
                    ($action.url              -eq '/{R:1}') -and
                    ($action.redirectType     -eq 'Permanent') -and
                    ($action.appendQueryString -eq $true)
                }
                SetScript   = {
                    Import-Module WebAdministration -Verbose:$false
                    $sitePath = "IIS:\Sites\$($settings.SiteName)"
                    $filter   = "system.webServer/rewrite/rules/rule[@name='WebJEA Backward Compatibility']/action"
                    Set-WebConfigurationProperty -pspath $sitePath -filter $filter -name 'type'              -value 'Redirect'
                    Set-WebConfigurationProperty -pspath $sitePath -filter $filter -name 'url'               -value '/{R:1}'
                    Set-WebConfigurationProperty -pspath $sitePath -filter $filter -name 'redirectType'      -value 'Permanent'
                    Set-WebConfigurationProperty -pspath $sitePath -filter $filter -name 'appendQueryString' -value $true
                }
            }
        }

        #add to logon as service
        @{
            Description = "$($settings.AppPoolUserName) has Logon as a Service right"
            Module      = 'cUserRightsAssignment'
            Resource    = 'cUserRight'
            Property    = @{
                Ensure    = 'Present'
                Constant  = 'SeServiceLogonRight'
                Principal = 'IIS APPPOOL\' + $settings.AppPoolName
            }
        }

        #add account to iusrs
        @{
            Description = 'App Pool User is in IIS_IUSRS group'
            Module      = 'PSDesiredStateConfiguration'
            Resource    = 'Group'
            Property    = @{
                GroupName        = 'IIS_IUSRS'
                MembersToInclude = @($settings.AppPoolUserName)
                Ensure           = 'Present'
            }
        }

        ##################################################
        #Update Config Files
        ##################################################
        #set json config location in web.config
        @{
            Description = 'Setting WebJEA config location in web.config'
            Module      = 'xXMLConfigFile'
            Resource    = 'XMLConfigFile'
            Property    = @{
                Ensure             = 'Present'
                ConfigPath         = "$($settings.SitePath)\web.config"
                XPath              = "/configuration/applicationSettings/WebJEA.My.MySettings/setting[@name='configfile']"
                isElementTextValue = $true
                Name               = 'value'
                Value              = "$($settings.ScriptsPath)\config.json"
            }
        }

        #set nlog log location in nlog.config in iis site
        @{
            Description = 'Setting log file location in nlog.config'
            Module      = 'xXMLConfigFile'
            Resource    = 'XMLConfigFile'
            Property    = @{
                Ensure      = 'Present'
                ConfigPath  = "$($settings.SitePath)\nlog.config"
                XPath       = "/nlog/targets/target[@name='file']/target"
                isAttribute = $true
                Name        = 'fileName'
                Value       = "$($settings.LogPath)\$($settings.LogFile)"
            }
        }

        #set nlog usage file location in nlog.config in iis site
        @{
            Description = 'Setting usage log file location in nlog.config'
            Module      = 'xXMLConfigFile'
            Resource    = 'XMLConfigFile'
            Property    = @{
                Ensure      = 'Present'
                ConfigPath  = "$($settings.SitePath)\nlog.config"
                XPath       = "/nlog/targets/target[@name='fileSummary']/target"
                isAttribute = $true
                Name        = 'fileName'
                Value       = "$($settings.LogPath)\$($settings.LogUsageFile)"
            }
        }
        #TODO assign permissions to scripts folder?

    }
    function GetSteps_WebJEA($Settings)
    {
        @{ Description = '***** Configuring WebJEA Specific Settings *****' }
        #Update config.json basePath property
        @{
            Description = 'basePath in config.json is set to the scripts folder'
            Module      = 'DSCR_FileContent'
            Resource    = 'JSONFile'
            Property    = @{
                Ensure   = 'Present'
                Path     = "$($settings.ScriptsPath)\config.json"
                Key      = 'basePath'
                Value    = $settings.ScriptsPath
                Encoding = 'ascii'
            }
        }

    }
    function GetSteps_Finalize
    {
        @{ Description = '***** Finalizing Deployment *****' }
        #Restart IIS to ensure all changes are applied
        @{
            Description = 'IIS Stopped to force reload'
            Module      = 'PSDesiredStateConfiguration'
            Resource    = 'Service'
            Property    = @{
                Name   = 'W3SVC'
                Ensure = 'Present'
                State  = 'Stopped'
            }
        }
        @{
            Description = 'IIS Started'
            Module      = 'PSDesiredStateConfiguration'
            Resource    = 'Service'
            Property    = @{
                Name   = 'W3SVC'
                Ensure = 'Present'
                State  = 'Running'
            }
        }
    }

    function GetSteps($Settings, $OnlySections)
    {
        if ($OnlySections -contains 'All' -or $OnlySections -contains 'PowerShell') { GetSteps_PowerShell }
        if ($OnlySections -contains 'All' -or $OnlySections -contains 'Server') { GetSteps_Server }
        if ($OnlySections -contains 'All' -or $OnlySections -contains 'WebServer') { GetSteps_WebServer $Settings }
        if ($OnlySections -contains 'All' -or $OnlySections -contains 'WebJEA') { GetSteps_WebJEA $Settings }
        if ($OnlySections -contains 'All' -or $OnlySections -contains 'Finalize') { GetSteps_Finalize }
    }
    #endregion Config
    #region Functions
    function ConvertToExpression($Obj)
    {
        if ($null -eq $Obj)
        {
            return '$null'
        }
        else
        {
            $strB = [System.Text.StringBuilder]::new()
            switch ($Obj.GetType().Name)
            {
                'String' { return "'$($Obj)'" }
                'Boolean' { if ($Obj) { return '$true' } else { return '$false' } }
                'Hashtable'
                {
                    $strB.append('@{') | Out-Null
                    foreach ($property in $Obj.keys)
                    {
                        $strB.append("$property = $(ConvertToExpression $Obj.$property); ") | Out-Null
                    }
                    $strB.append('}') | Out-Null
                    return $strB.ToString()
                }
                'Object[]'
                {
                    $strB.append('@(') | Out-Null
                    $ArrayObj = $Obj | ForEach-Object {
                        "$(ConvertToExpression $_)"
                    }
                    $strB.append(($ArrayObj -join ', ')) | Out-Null
                    $strB.append(')') | Out-Null
                    return $strB.ToString()
                }
                default { return $Obj.tostring() }
            }
        }

    }

    function ValidateSettings( [psobject]$Settings )
    {
        #quick helper function
        function Assert($Condition, $Message) { if (-not $Condition) { throw $Message } }

        #Remove trailing paths from SitePath, ScriptsPath, and LogPath to avoid problems later
        $settings.SitePath = $settings.SitePath.TrimEnd('\')
        $settings.ScriptsPath = $settings.ScriptsPath.TrimEnd('\')
        $settings.LogPath = $settings.LogPath.TrimEnd('\')

        #verify the user account
        Assert ($settings.AppPoolUserName -match '^[^\\]+\\[^\\]+$') "AppPoolUserName '$($settings.AppPoolUserName)' is not in the correct format. It should be in the format 'domain\\username' or 'machinename\\username'."
        if ($settings.AppPoolPassword)
        {
            #regular user account with password
            Assert ($settings.AppPoolUserName -notmatch '\$$') "AppPoolUserName '$($settings.AppPoolUserName)' appears to be a gMSA account (ends with $) but a password is provided. Please remove the password for gMSA accounts or provide a valid password for regular user accounts."
        }
        else
        {
            #gmsa
            Assert ($settings.AppPoolUserName -match '\$$') "AppPoolUserName '$($settings.AppPoolUserName)' appears to be a regular user account but no password is provided. Please provide a password for regular user accounts or use a gMSA account by adding a trailing $ to the username and leaving the password blank."
            try
            {
                $userobj = get-adserviceaccount -Identity $settings.AppPoolUserName.split('\')[1]
            }
            catch {}
            Assert ($userobj -ne $null) "AppPoolUserName '$($settings.AppPoolUserName)' does not appear to be a valid gMSA account. Please provide a valid gMSA account or specify a regular user account with a password."
        }

        #SitePath <> ScriptsPath
        Assert ($settings.SitePath -ne $settings.ScriptsPath) 'SitePath and ScriptsPath cannot be the same. Please update the settings file to specify different paths.'
        #SitePath <> LogPath
        Assert ($settings.SitePath -ne $settings.LogPath) 'SitePath and LogPath cannot be the same. Please update the settings file to specify different paths.'
        #LogFile <> LogUsageFile
        Assert ($settings.LogFile -ne $settings.LogUsageFile) 'LogFile and LogUsageFile cannot be the same. Please update the settings file to specify different paths.'
        #RedirectPort80to443=true, CertThumbprint is required
        Assert (-not ($settings.RedirectPort80To443 -and -not $settings.CertThumbprint)) 'RedirectPort80To443 is set to true, but CertThumbprint is not provided. Please provide a valid CertThumbprint in the settings file.'
        #If CertThumbprint, CertThumbprint is valid
        if ($settings.CertThumbprint)
        {
            $cert = Get-ChildItem -Path cert:\LocalMachine\My\$($settings.CertThumbprint) -ErrorAction SilentlyContinue
            Assert ($cert) "CertThumbprint '$($settings.CertThumbprint)' not found in LocalMachine\My store. Please provide a valid CertThumbprint in the settings file."
            #Cert exists
            #If CertThumbprint, SiteFQDN match CertThumbprint FQDN
            if ($settings.SiteFQDN)
            {
                $certFQDN = $cert.GetNameInfo([System.Security.Cryptography.X509Certificates.X509NameType]::DnsName, $false)
                Assert ($certFQDN -eq $settings.SiteFQDN) "SiteFQDN '$($settings.SiteFQDN)' does not match the FQDN '$certFQDN' in the certificate with thumbprint '$($settings.CertThumbprint)'. Please ensure the SiteFQDN in the settings file matches the certificate's FQDN."
            }
        }
    }

    function InvokeStep([psobject]$Step, [switch]$TestOnly)
    {
        if (-not $step.description)
        {
            Write-Host "Error with configuration: $($Step | ConvertTo-Json -Depth 2)" -ForegroundColor Yellow
            return
        }
        if ($step.description -and -not ($step.testscript -or $step.module))
        {
            Write-Host $($Step.Description) -ForegroundColor Cyan
        }
        else
        {
            Write-Host "> $($Step.Description)" -ForegroundColor Cyan
        }
        if ($Step.Module -and $Step.Resource)
        {
            ##### DSC Resource
            #Write-Host -ForegroundColor black -BackgroundColor yellow
            Write-Verbose "Invoke-DscResource -Module $($Step.Module) -Name $($Step.Resource) -Property $(ConvertToExpression $Step.Property) -Method Test"
            # Write-Host (Invoke-DscResource -Module $Step.Module -Name $Step.Resource -Property $Step.Property -Method Get -Verbose:$false | ConvertTo-Json -Depth 1)
            $verboseMemory = $VerbosePreference
            $VerbosePreference = 'SilentlyContinue'
            $test = Invoke-DscResource -Module $Step.Module -Name $Step.Resource -Property $Step.Property -Method Test
            if (-not $test.InDesiredState -and -not $TestOnly)
            {
                Write-Host '  [SET] not in desired state. Updating...'
                $set = Invoke-DscResource -Module $Step.Module -Name $Step.Resource -Property $Step.Property -Method Set

                #After setting, test again to confirm it reached the desired state
                $test = Invoke-DscResource -Module $Step.Module -Name $Step.Resource -Property $Step.Property -Method Test
            }
            $VerbosePreference = $verboseMemory
        }
        elseif ($Step.TestScript -and $Step.SetScript)
        {
            ##### Custom Script Resource
            Write-Verbose "  Test: $($Step.TestScript.ToString())"
            if ($Step.testscript -is [string]) { $Step.TestScript = [scriptblock]::Create($Step.TestScript) }
            if ($Step.setscript -is [string]) { $Step.SetScript = [scriptblock]::Create($Step.SetScript) }

            $test = @{InDesiredState = & $Step.TestScript }
            if (-not $test.InDesiredState -and -not $TestOnly -and $Step.SetScript)
            {
                Write-Host '  [SET] not in desired state. Updating...'
                Write-Verbose "  Set: $($Step.SetScript.ToString())"
                $set = & $Step.SetScript
                $test = @{InDesiredState = & $Step.TestScript }
            }
        }
        elseif (-not $Step.TestScript -and -not $Step.SetScript -and -not $Step.Module -and -not $Step.Resource)
        {
            #Empty step, do nothing
            Write-Verbose 'No TestScript/SetScript or Module/Resource specified for this step. Skipping.'
        }
        else
        {
            Write-Verbose ($step | ConvertTo-Json -Depth 2)
            throw "Invalid resource step: $($Step | ConvertTo-Json -Depth 2)"
        }

        if ($test.InDesiredState -and $test.description)
        {
            Write-Host '  [OK] in desired state.' -ForegroundColor Green
        }
        elseif (-not $test.testscript)
        {
            #nothing to output
        }
        else
        {
            Write-Host '  [FAILED] still not in desired state after Set.' -ForegroundColor Red
            if (-not $TestOnly) { throw "step '$Description' failed to reach desired state." }
        }
    }
    #endregion Functions
}
process
{
    #region Main
    [string]$SettingsFilePath = Resolve-Path -Path $SettingsFile
    Write-Verbose "Reading settings from file: $($SettingsFilePath)"
    $Settings = Get-Content -Path $SettingsFilePath | Where-Object { $_ -notmatch '^\s*//' } | ConvertFrom-Json
    $settings | Add-Member -NotePropertyName 'SourcePath' -NotePropertyValue $PSScriptRoot
    Write-Verbose "Settings read from file (and calculated settings): $($Settings | ConvertTo-Json -Depth 1)"

    # ValidateSettings -Settings $Settings

    [hashtable[]]$Steps = GetSteps -Settings $Settings -OnlySections $OnlySections
    write-host "Found $($Steps.Count) configuration steps to apply based on the settings and selected sections."
    if ($returnSteps) { Write-Output $Steps }
    #Use a combination of DSC and custom scripts to configure the server.
    foreach ($Step in $Steps)
    {
        InvokeStep -Step $Step -TestOnly:$TestOnly
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

    #endregion Main
}
end
{

}