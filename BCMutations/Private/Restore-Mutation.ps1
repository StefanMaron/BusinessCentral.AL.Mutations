function Restore-Mutation {
    <#
    .SYNOPSIS
        Restores an AL source file from its backup after a mutation test.
    .DESCRIPTION
        Moves the .bak file back to the original path, overwriting the mutated version.
        Throws if no backup file exists.
    .PARAMETER FilePath
        Full path to the mutated .al source file to restore.
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$FilePath
    )

    $backupPath = "$FilePath.bak"

    if (-not (Test-Path -LiteralPath $backupPath -PathType Leaf)) {
        throw "Backup file not found: $backupPath. Cannot restore: $FilePath"
    }

    Move-Item -LiteralPath $backupPath -Destination $FilePath -Force
}
