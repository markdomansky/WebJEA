<#
.SYNOPSIS
Describe the function here

.DESCRIPTION
Describe the function in more detail

.EXAMPLE
Give an example of how to use it

.EXAMPLE
Give another example of how to use it

.PARAMETER ComputerName
The computer name to query. Just one.

.PARAMETER input
The name of a file to write failed computer names to. Defaults to errors.txt.


#>
#requires -version 3
#r#equires -pssnapin <snapin> -version X.x
#r#equires -modules {<module-name>}
#r#equires -shellid <shellid>
#r#equires -runasadministrator

[CmdletBinding(SupportsShouldProcess=$True,ConfirmImpact='Low')]
#ConfirmImpact='Medium' (Low, Medium (default), High) will prompt for action where $pscmdlet.ShouldProcess("target",["action"]) is run.  Returns true/false, generally only want for high impact actions (i.e. restart computer)
#SupportsShouldProcess=$true - will accept and passthru -whatif and -confirm


param
(

    [Parameter()]
    [VALIDATESET('Input','Output','Both','A',1,2,3,"1/1/2017")] 
	[ValidateCount(1,3)]
    [string[]]$Input2,

    [Parameter(Mandatory)]
    [ValidateSet('Input','Output','Both')] 
    [string]$Input1

)

begin {
    #do pre script checks, etc
    
}

process {
	Write-Host "[[span|psbold|Input1]]"
	Write-Host $Input1
	Write-Host "[[span|psbold|Input2]]"
	$input2 | %{Write-Host $_}
}

end {
    
}
