function New-Mutation {
    <#
    .SYNOPSIS
        Applies a single mutation to an AL source file.
    .DESCRIPTION
        Backs up the original file (.bak), then modifies the specified line:
        - If operator.replacement is non-null: replaces the first occurrence of
          operator.pattern with operator.replacement on that line.
        - If operator.replacement is null: comments out the entire line (prepends '//').
    .PARAMETER Mutation
        Mutation candidate object with File, Line, Column, Operator, OriginalLine properties.
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [PSCustomObject]$Mutation
    )

    $filePath = $Mutation.File
    $lineIndex = $Mutation.Line - 1  # 0-based
    $op = $Mutation.Operator
    $backupPath = "$filePath.bak"

    if (-not (Test-Path -LiteralPath $filePath -PathType Leaf)) {
        throw "Source file not found: $filePath"
    }

    # Backup the original file
    Copy-Item -LiteralPath $filePath -Destination $backupPath -Force

    $lines = [System.IO.File]::ReadAllLines($filePath)

    if ($lineIndex -lt 0 -or $lineIndex -ge $lines.Count) {
        throw "Line number $($Mutation.Line) is out of range for file: $filePath"
    }

    $originalLine = $lines[$lineIndex]

    if ($null -eq $op.replacement) {
        # Statement removal: comment out the entire line
        $lines[$lineIndex] = '//' + $originalLine
    } else {
        # Replace the first occurrence of the pattern at or after the given column
        $col = $originalLine.IndexOf($op.pattern, $Mutation.Column, [System.StringComparison]::Ordinal)
        if ($col -lt 0) {
            # Fallback: replace first occurrence anywhere on the line
            $col = $originalLine.IndexOf($op.pattern, [System.StringComparison]::Ordinal)
        }
        if ($col -lt 0) {
            throw "Pattern '$($op.pattern)' not found on line $($Mutation.Line) of file: $filePath"
        }
        $lines[$lineIndex] = $originalLine.Substring(0, $col) + $op.replacement + $originalLine.Substring($col + $op.pattern.Length)
    }

    [System.IO.File]::WriteAllLines($filePath, $lines)
}
