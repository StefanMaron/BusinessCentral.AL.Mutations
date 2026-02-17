function Find-MutationTargets {
    <#
    .SYNOPSIS
        Scans an AL source file for mutation candidate locations.
    .DESCRIPTION
        Reads each line of the given AL file and checks for each operator's pattern.
        Filters out matches inside single-line comments, block comments, and string literals
        using Test-LineContext. Returns a list of mutation candidates.
    .PARAMETER FilePath
        Full path to the .al source file to scan.
    .PARAMETER Operators
        Array of operator objects (as returned by Read-OperatorFile).
    .OUTPUTS
        PSCustomObject[] - Mutation candidates with File, Line, Column, Operator, OriginalLine.
    #>
    [CmdletBinding()]
    [OutputType([PSCustomObject[]])]
    param(
        [Parameter(Mandatory)]
        [string]$FilePath,

        [Parameter(Mandatory)]
        [AllowEmptyCollection()]
        [PSCustomObject[]]$Operators
    )

    if (-not (Test-Path -LiteralPath $FilePath -PathType Leaf)) {
        throw "Source file not found: $FilePath"
    }

    if ($Operators.Count -eq 0) {
        return @()
    }

    $lines = Get-Content -LiteralPath $FilePath
    $targets = [System.Collections.Generic.List[PSCustomObject]]::new()
    $precedingText = [System.Text.StringBuilder]::new()

    for ($lineIndex = 0; $lineIndex -lt $lines.Count; $lineIndex++) {
        $line = $lines[$lineIndex]
        $lineNumber = $lineIndex + 1  # 1-based
        $preceding = $precedingText.ToString()

        foreach ($op in $Operators) {
            $searchFrom = 0
            while ($true) {
                $col = $line.IndexOf($op.pattern, $searchFrom, [System.StringComparison]::Ordinal)
                if ($col -lt 0) { break }

                $inContext = Test-LineContext -Line $line -Position $col -PrecedingText $preceding
                if (-not $inContext) {
                    $targets.Add([PSCustomObject]@{
                        File        = $FilePath
                        Line        = $lineNumber
                        Column      = $col
                        Operator    = $op
                        OriginalLine = $line
                    })
                }

                $searchFrom = $col + $op.pattern.Length
            }
        }

        [void]$precedingText.Append($line)
        [void]$precedingText.Append("`n")
    }

    return $targets.ToArray()
}
