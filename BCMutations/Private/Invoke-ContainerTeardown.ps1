function Invoke-ContainerTeardown {
    <#
    .SYNOPSIS
        Removes a BC container after mutation testing.
    .DESCRIPTION
        Wraps Remove-BcContainer from BcContainerHelper.
    .PARAMETER ContainerName
        Name of the BC container to remove.
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$ContainerName
    )

    if (-not (Get-Command 'Remove-BcContainer' -ErrorAction SilentlyContinue)) {
        throw 'BcContainerHelper is not installed.'
    }

    Write-Verbose "Removing BC container: $ContainerName"
    Remove-BcContainer -containerName $ContainerName
    Write-Verbose "Container '$ContainerName' removed."
}
