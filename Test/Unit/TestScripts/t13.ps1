param([pscredential]$TestCred)
$VerbosePreference = "Continue"
$DebugPreference = "Continue"

write-host (get-date)
write-host ("UN: {0}" -f $testcred.getnetworkcredential().username)
write-host ("PW: {0}" -f $testcred.getnetworkcredential().password)