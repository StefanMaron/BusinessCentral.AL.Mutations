function Invoke-AppDeploy {
    <#
    .SYNOPSIS
        Publishes and installs a compiled AL app in a BC container.
    .DESCRIPTION
        Wraps Publish-BcContainerApp from BcContainerHelper using the dev endpoint
        for fast iteration during the mutation loop.
    .PARAMETER ContainerName
        Name of the BC container.
    .PARAMETER AppFile
        Path to the compiled .app file.
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$ContainerName,

        [Parameter(Mandatory)]
        [string]$AppFile
    )

    if (-not (Get-Command 'Publish-BcContainerApp' -ErrorAction SilentlyContinue)) {
        throw 'BcContainerHelper is not installed.'
    }

    Publish-BcContainerApp -containerName $ContainerName -appFile $AppFile -useDevEndpoint -skipVerification
}
