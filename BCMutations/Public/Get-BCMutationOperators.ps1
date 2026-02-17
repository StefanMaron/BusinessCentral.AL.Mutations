function Get-BCMutationOperators {
    <#
    .SYNOPSIS
        Lists available mutation operators.
    .DESCRIPTION
        Returns the operators from the default operator file, or from a custom file
        if specified. Operators can be filtered by category.
    .PARAMETER OperatorFile
        Path to a custom operator JSON file. Defaults to the bundled operators/default.json.
    .PARAMETER Category
        Filter operators by category (relational, arithmetic, logical, boolean,
        statement-removal, boundary, control-flow, bc-specific).
    .EXAMPLE
        Get-BCMutationOperators
    .EXAMPLE
        Get-BCMutationOperators -Category relational
    #>
    [CmdletBinding()]
    [OutputType([PSCustomObject[]])]
    param(
        [string]$OperatorFile = '',

        [ValidateSet('relational', 'arithmetic', 'logical', 'boolean',
                     'statement-removal', 'boundary', 'control-flow', 'bc-specific')]
        [string]$Category = ''
    )

    if (-not $OperatorFile) {
        $OperatorFile = Join-Path $PSScriptRoot '../../operators/default.json'
        $OperatorFile = (Resolve-Path $OperatorFile).Path
    }

    $operators = Read-OperatorFile -Path $OperatorFile

    if ($Category) {
        $operators = $operators | Where-Object { $_.category -eq $Category }
    }

    return $operators
}
