function Read-OperatorFile {
    <#
    .SYNOPSIS
        Loads and validates a mutation operator JSON file.
    .DESCRIPTION
        Reads a JSON operator definition file, validates its structure, and returns
        the array of operator objects. Throws a terminating error if the file is
        missing, has invalid JSON, or fails validation.
    .PARAMETER Path
        Full path to the operator JSON file.
    .OUTPUTS
        PSCustomObject[] - Array of operator objects with id, name, category, pattern, replacement.
    #>
    [CmdletBinding()]
    [OutputType([PSCustomObject[]])]
    param(
        [Parameter(Mandatory)]
        [string]$Path
    )

    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        throw "Operator file not found: $Path"
    }

    $json = Get-Content -LiteralPath $Path -Raw -ErrorAction Stop

    try {
        $data = $json | ConvertFrom-Json -ErrorAction Stop
    } catch {
        throw "Operator file contains invalid JSON: $Path. Error: $_"
    }

    if ($null -eq $data.operators) {
        throw "Operator file is missing the 'operators' array: $Path"
    }

    if ($data.operators.Count -eq 0) {
        throw "Operator file 'operators' array is empty: $Path"
    }

    $requiredFields = @('id', 'name', 'category', 'pattern')
    foreach ($op in $data.operators) {
        foreach ($field in $requiredFields) {
            if ([string]::IsNullOrWhiteSpace($op.$field)) {
                throw "Operator '$($op.id)' in file '$Path' is missing required field: $field"
            }
        }
    }

    return $data.operators
}
