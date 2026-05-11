## A simple DSC declaration

```
$MyData = @{
    AllNodes = @(
        @{
            NodeName = '*'
            WebAppPoolName = 'WebJEA'
            AppPoolUserName = 'domain1\gmsa2$' #Change to your service account
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
        },
        @{
            NodeName = 'WEB1' #Change me to the DNS name of your host
            Role = 'WebJEAServer'
            MachineFQDN = 'web1.domain1.local' #Change me
            CertThumbprint = '50495F09B2DC05DB9BB47D834623D38508A50524' #Change me
        }
    )
}
```