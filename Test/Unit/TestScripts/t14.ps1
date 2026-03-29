param([securestring]$secstr)
$VerbosePreference = "Continue"
$DebugPreference = "Continue"

$BSTR = [System.Runtime.InteropServices.Marshal]::SecureStringToBSTR($secstr)
$clearpw = [System.Runtime.InteropServices.Marshal]::PtrToStringAuto($BSTR)
[Runtime.InteropServices.Marshal]::ZeroFreeBSTR($BSTR) #this is an important step to keep things secure

write-host (get-date)
write-host ("clear string: {0}" -f $clearpw)
