function ConvertTo-MutationReport {
    <#
    .SYNOPSIS
        Generates a mutation testing report from results.
    .DESCRIPTION
        Takes mutation results and generates a report in JSON or Markdown format.
        Mutation score = killed / (killed + survived). CompileErrors are excluded from score.
    .PARAMETER Results
        Array of mutation result objects with properties:
        Mutation (PSCustomObject), Status ('Killed'|'Survived'|'CompileError')
    .PARAMETER Format
        Report format: 'json' or 'markdown'.
    .PARAMETER OutputPath
        Path to write the report file. If not specified, returns the report as a string.
    .OUTPUTS
        String - Report content (or writes to file if OutputPath specified).
    #>
    [CmdletBinding()]
    [OutputType([string])]
    param(
        [Parameter(Mandatory)]
        [PSCustomObject[]]$Results,

        [Parameter(Mandatory)]
        [ValidateSet('json', 'markdown')]
        [string]$Format,

        [string]$OutputPath = ''
    )

    $killed = @($Results | Where-Object { $_.Status -eq 'Killed' })
    $survived = @($Results | Where-Object { $_.Status -eq 'Survived' })
    $compileErrors = @($Results | Where-Object { $_.Status -eq 'CompileError' })

    $totalScored = $killed.Count + $survived.Count
    $score = if ($totalScored -gt 0) {
        [math]::Round(($killed.Count / $totalScored) * 100, 1)
    } else {
        0.0
    }

    if ($Format -eq 'json') {
        $report = [PSCustomObject]@{
            mutationScore  = $score
            killed         = $killed.Count
            survived       = $survived.Count
            compileErrors  = $compileErrors.Count
            total          = $Results.Count
            survivors      = @($survived | ForEach-Object {
                [PSCustomObject]@{
                    file        = $_.Mutation.File
                    line        = $_.Mutation.Line
                    operatorId  = $_.Mutation.Operator.id
                    originalLine = $_.Mutation.OriginalLine
                }
            })
        }
        $content = $report | ConvertTo-Json -Depth 5
    } else {
        $sb = [System.Text.StringBuilder]::new()
        [void]$sb.AppendLine("# Mutation Testing Report")
        [void]$sb.AppendLine()
        [void]$sb.AppendLine("## Summary")
        [void]$sb.AppendLine()
        [void]$sb.AppendLine("| Metric | Value |")
        [void]$sb.AppendLine("|--------|-------|")
        [void]$sb.AppendLine("| Mutation Score | $score% |")
        [void]$sb.AppendLine("| Killed | $($killed.Count) |")
        [void]$sb.AppendLine("| Survived | $($survived.Count) |")
        [void]$sb.AppendLine("| Compile Errors | $($compileErrors.Count) |")
        [void]$sb.AppendLine("| Total Mutants | $($Results.Count) |")
        [void]$sb.AppendLine()

        if ($survived.Count -gt 0) {
            [void]$sb.AppendLine("## Survived Mutants (Test Gaps)")
            [void]$sb.AppendLine()
            [void]$sb.AppendLine("These mutations were NOT caught by your tests:")
            [void]$sb.AppendLine()
            foreach ($r in $survived) {
                $file = Split-Path $r.Mutation.File -Leaf
                [void]$sb.AppendLine("- **$($r.Mutation.Operator.id)** @ ``$file:$($r.Mutation.Line)``")
                [void]$sb.AppendLine("  - Original: ``$($r.Mutation.OriginalLine.Trim())``")
            }
            [void]$sb.AppendLine()
        }

        if ($killed.Count -gt 0) {
            [void]$sb.AppendLine("<details>")
            [void]$sb.AppendLine("<summary>Killed Mutants ($($killed.Count))</summary>")
            [void]$sb.AppendLine()
            foreach ($r in $killed) {
                $file = Split-Path $r.Mutation.File -Leaf
                [void]$sb.AppendLine("- **$($r.Mutation.Operator.id)** @ ``$file:$($r.Mutation.Line)``")
            }
            [void]$sb.AppendLine("</details>")
        }

        $content = $sb.ToString()
    }

    if ($OutputPath) {
        Set-Content -Path $OutputPath -Value $content -Encoding UTF8
        Write-Verbose "Report written to: $OutputPath"
    }

    return $content
}
