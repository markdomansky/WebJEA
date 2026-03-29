$VerbosePreference = "Continue"
$DebugPreference = "Continue"

write-host "[[a|?cmdid=t11&p1=abc&p2=123|Test Link]]"
Write-Host @"
You can automatically run a PowerShell script on page load and have it display useful information relevant to the page requested.

This is a very long line to confirm word wrap as desired. This is a very long line to confirm word wrap as desired. This is a very long line to confirm word wrap as desired. This is a very long line to confirm word wrap as desired. This is a very long line to confirm word wrap as desired.

We support some basic formatting like:
Links: [[a |url|display]] -[[a|?cmdid=sample1|go to sample1]]-
CSS tags in a span: [[span |cssclasses|content]] -[[span|psvariable|this span uses a variable width font and italicised]]-.
Img: [[img |cssclasses|url]] [[img||./content/ps.png]]
Nesting: [[a |url|[[img |cssclasses|url]]]] [[a|//powershell.org|[[img||./content/ps.png]]]]
"@
Write-Host "We html encode for safety, <a href='see?'></a>"

write-host "We honor spaces like a <pre>."
write-host (Get-Process | select -First 2 | out-string)

Write-Host "We also format Warning, Error, Verbose, and Debug messages."
Write-Host "WEBJEA:This is an NLOG message"
Write-Warning "This is a warning message"
Write-Host "'natural' ps error:"
12/0
Write-Host "write-error:"
Write-Error "This is an error message with a [[a|?cmdid=overview|link]] in it"
Write-Verbose "This is a verbose message"
Write-Debug "This is a debug message"
write-host ""
Write-Host "All of this is exposed in [[a|psoutput.css|psoutput.css]]."
Write-Host $null
write-host "PSVersion: $($PSVersionTable.psversion.tostring())"
Write-Host (Get-Date).tostring()

#write-error "HI" : HI
#    + CategoryInfo          : NotSpecified: (:) [Write-Error], WriteErrorException
#    + FullyQualifiedErrorId : Microsoft.PowerShell.Commands.WriteErrorException

#Attempted to divide by zero.
#At line:1 char:1
#+ 12/0
#+ ~~~~
#    + CategoryInfo          : NotSpecified: (:) [], RuntimeException
#    + FullyQualifiedErrorId : RuntimeException


