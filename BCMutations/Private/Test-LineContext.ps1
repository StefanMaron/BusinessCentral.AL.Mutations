function Test-LineContext {
    <#
    .SYNOPSIS
        Determines if a position in an AL source line is inside a comment or string literal.
    .DESCRIPTION
        Uses a simple state machine to detect whether a given character position is inside:
        - A single-line comment (// to end of line)
        - A block comment (/* ... */)
        - A string literal (' ... ' in AL)
        Returns $true if the position is inside a comment or string (should be skipped).
        Returns $false if the position is in normal code (safe to mutate).
    .PARAMETER Line
        The source line to analyze.
    .PARAMETER Position
        The zero-based character position to check within the line.
    .PARAMETER PrecedingText
        All source text from before this line (used to detect unclosed block comments).
    .OUTPUTS
        Boolean - $true if inside comment or string, $false if in normal code.
    #>
    [CmdletBinding()]
    [OutputType([bool])]
    param(
        [Parameter(Mandatory)]
        [string]$Line,

        [Parameter(Mandatory)]
        [int]$Position,

        [Parameter(Mandatory)]
        [AllowEmptyString()]
        [string]$PrecedingText
    )

    # Determine initial block comment state from preceding text
    $inBlockComment = $false
    if ($PrecedingText.Length -gt 0) {
        $i = 0
        while ($i -lt $PrecedingText.Length) {
            if (-not $inBlockComment) {
                if ($i + 1 -lt $PrecedingText.Length -and
                    $PrecedingText[$i] -eq '/' -and $PrecedingText[$i + 1] -eq '*') {
                    $inBlockComment = $true
                    $i += 2
                    continue
                }
            } else {
                if ($i + 1 -lt $PrecedingText.Length -and
                    $PrecedingText[$i] -eq '*' -and $PrecedingText[$i + 1] -eq '/') {
                    $inBlockComment = $false
                    $i += 2
                    continue
                }
            }
            $i++
        }
    }

    # Now scan through the current line up to and including Position
    $inString = $false
    $i = 0
    while ($i -le $Position -and $i -lt $Line.Length) {
        $ch = $Line[$i]

        if ($inBlockComment) {
            if ($i + 1 -lt $Line.Length -and $ch -eq '*' -and $Line[$i + 1] -eq '/') {
                $inBlockComment = $false
                $i += 2
                continue
            }
            if ($i -eq $Position) { return $true }
            $i++
            continue
        }

        if ($inString) {
            if ($ch -eq "'") {
                $inString = $false
            }
            if ($i -eq $Position) { return $true }
            $i++
            continue
        }

        # Not in block comment or string - check for new contexts
        if ($i + 1 -lt $Line.Length -and $ch -eq '/' -and $Line[$i + 1] -eq '/') {
            # Single-line comment: everything from here to EOL is a comment
            if ($Position -ge $i) { return $true }
            break
        }

        if ($i + 1 -lt $Line.Length -and $ch -eq '/' -and $Line[$i + 1] -eq '*') {
            $inBlockComment = $true
            $i += 2
            continue
        }

        if ($ch -eq "'") {
            $inString = $true
            if ($i -eq $Position) { return $true }
            $i++
            continue
        }

        # Normal code character
        $i++
    }

    return $false
}
