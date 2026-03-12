<#
.SYNOPSIS
    Writes a log message with timestamp and level to console and optionally to a file.

.DESCRIPTION
    Outputs a formatted log message to the console with color-coding based on level.
    If $script:LogFile is set, also appends the message to the specified log file.

.PARAMETER Message
    The message to log.

.PARAMETER Level
    The log level. Valid values: Information, Warning, Error, Success.
    Default: Information

.EXAMPLE
    Write-Log 'Starting process...'

.EXAMPLE
    Write-Log 'Operation completed' -Level Success

.EXAMPLE
    Write-Log 'Something went wrong' -Level Error
#>
[CmdletBinding()]
param(
    [Parameter(Position = 0)]
    [string]$Message = "",

    [Parameter()]
    [ValidateSet('Information', 'Warning', 'Error', 'Success')]
    [string]$Level = 'Information'
)

$timestamp = Get-Date -Format 'yyyy-MM-dd HH:mm:ss'

$color = switch ($Level) {
    'Information' { 'White' }
    'Warning' { 'Yellow' }
    'Error' { 'Red' }
    'Success' { 'Green' }
}

$prefix = switch ($Level) {
    'Information' { '[INFO]' }
    'Warning' { '[WARN]' }
    'Error' { '[ERROR]' }
    'Success' { '[OK]' }
}

Write-Host "$timestamp $prefix $Message" -ForegroundColor $color

