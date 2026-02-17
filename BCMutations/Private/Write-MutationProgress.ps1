function Write-MutationProgress {
    <#
    .SYNOPSIS
        Writes mutation testing progress to the console.
    .DESCRIPTION
        Outputs formatted progress information during mutation testing.
    .PARAMETER Current
        Current mutation index (1-based).
    .PARAMETER Total
        Total number of mutations.
    .PARAMETER MutationId
        The operator ID of the current mutation.
    .PARAMETER FilePath
        The file being mutated.
    .PARAMETER LineNumber
        The line number being mutated.
    .PARAMETER Status
        Status of the mutation: 'Testing', 'Killed', 'Survived', 'CompileError'.
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [int]$Current,

        [Parameter(Mandatory)]
        [int]$Total,

        [Parameter(Mandatory)]
        [string]$MutationId,

        [Parameter(Mandatory)]
        [string]$FilePath,

        [Parameter(Mandatory)]
        [int]$LineNumber,

        [Parameter(Mandatory)]
        [ValidateSet('Testing', 'Killed', 'Survived', 'CompileError')]
        [string]$Status
    )

    $fileName = Split-Path $FilePath -Leaf
    $statusSymbol = switch ($Status) {
        'Killed'       { '[KILLED]' }
        'Survived'     { '[SURVIVED]' }
        'CompileError' { '[COMPILE ERROR]' }
        default        { '[TESTING]' }
    }

    $pct = [math]::Round(($Current / $Total) * 100)
    Write-Host "[$Current/$Total $pct%] $statusSymbol $MutationId @ $fileName:$LineNumber"
}
