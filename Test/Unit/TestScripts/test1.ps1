<#
(just a test{with another test[[x]]})
.SYNOPSIS
Describe the function here

.DESCRIPTION
Describe the function in more detail. Describe the function in more detail. Describe the function in more detail. Describe the function in more detail. Describe the function in more detail. Describe the function in more detail. 
Describe the function in more detail. Describe the function in more detail. Describe the function in more detail. Describe the function in more detail. Describe the function in more detail. Describe the function in more detail. Describe the function in more detail. Describe the function in more detail. 
Describe the function in more detail. Describe the function in more detail. Describe the function in more detail. Describe the function in more detail. BLAH

.EXAMPLE
Give an example of how to use it

.EXAMPLE
Give another example of how to use it

.PARAMETER WebJEAUsername
The computer name Input1 to query. Just one.
#WEBJEA-Multiline

.PARAMETER Input02MandatoryMinLen
The computer name Input2 to query. Just one.

.PARAMETER Input03Str
The name of a file to write failed computer names to. Defaults to errors.txt.

.PARAMETER Input11Switch
I accept the terms of the agreement blah blah blah.

#>
#requires -version 3
#r#equires -pssnapin <snapin> -version X.x
#r#equires -modules {<module-name>}
#r#equires -shellid <shellid>
#r#equires -runasadministrator

[CmdletBinding(SupportsShouldProcess=$True,ConfirmImpact='Low')]
#ConfirmImpact='Medium' (Low, Medium (default), High) will prompt for action where $pscmdlet.ShouldProcess("target",["action"]) is run.  Returns true/false, generally only want for high impact actions (i.e. restart computer)
#SupportsShouldProcess=$true - will accept and passthru -whatif and -confirm
#    [Parameter(Position=0, Mandatory, ParameterSetName="Group1", ValueFromPipeline, ValueFromPipelineByPropertyName, HelpMessage='What computer name would you like to target?')]
#if you use parametersetname and want a parameter eligible for some but not all params, you have to specify the same param multiple times
#if you want a parameter available always, don't specify parametersetname
#    [SupportsWildcards()]
#    [Alias('MachineName')] #can have multiple on difference lines
#    [ValidateLength(3,30)]
#    [ValidateRange(1,100)]
#    [ValidateScript({$_ -gt (get-date)})] #must return $true/$false
#    [ValidateNotNullOrEmpty()]
#    [ValidateNotNull] #treated the same as above
#    [ValidatePattern("regexpattern")]
#    [ValidateCount(1,5)] #number of items in collection
#    [ValidateSet('Input','Output','Both')] #any set of values you want here
#    [AllowNull] #recommend set default and presume empty
#    [AllowEmptyString] #recommend set default and presume empty
#    [AllowEmptyCollection] #recommend set default and presume empty
#    [string[]]$ComputerName, #accepts multiple, runs process multiple times
#    [string]$ComputerName, #only one
#    [switch][int][psobject] many others possible and most support [] within for array

param
(
   

    [Parameter()]
    [Alias('Username')]

    [string]$WebJEAUsername, #this won't be shown to the user

    [Parameter()]
    [string]$WebJEAHostname #, #this won't be shown to the user

    #[Parameter()]
    #[string]$WebJEAUPN #this won't be shown to the user
)

begin {
    #do pre script checks, etc
    
}

process {
	Write-Verbose "Starting Process"
	Write-Host (Get-Date).tostring()
	Write-Host "Running as: $($env:username)"
	Write-Host "PSBoundParameters"
	$PSBoundParameters.keys | %{
		Write-Host "[[span|psbold|$_]]"
		$PSBoundParameters[$_] | write-host
		
	}

	#Write-Host "Get-Variable -scope script"
	#$vars = get-variable -Scope 0
	#$vars | %{
	#	Write-Host "[[span|psbold|$($_.name)]]"
	#	$_.value | write-host
	#}

	#Import-Module activedirectory
	#disable-adaccount user
	#Write-Host "Disabled AD User user1"

}

end {
    
}
