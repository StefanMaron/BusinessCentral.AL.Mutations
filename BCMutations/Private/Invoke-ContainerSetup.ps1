function Invoke-ContainerSetup {
    <#
    .SYNOPSIS
        Creates a BC container for mutation testing.
    .DESCRIPTION
        Wraps New-BcContainer from BcContainerHelper. Creates a container configured
        for compilation and test execution. Requires BcContainerHelper to be installed.
    .PARAMETER ContainerName
        Name for the BC container.
    .PARAMETER ProjectPath
        Path to the user's AL project (will be volume-mounted).
    .PARAMETER ArtifactUrl
        BC artifact URL to use. If not specified, uses latest sandbox.
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$ContainerName,

        [Parameter(Mandatory)]
        [string]$ProjectPath,

        [string]$ArtifactUrl = ''
    )

    if (-not (Get-Command 'New-BcContainer' -ErrorAction SilentlyContinue)) {
        throw 'BcContainerHelper is not installed. Install it with: Install-Module BcContainerHelper'
    }

    Write-Verbose "Creating BC container: $ContainerName"

    $params = @{
        accept_eula             = $true
        containerName           = $ContainerName
        includeTestToolkit      = $true
        includeTestLibrariesOnly = $true
        isolation               = 'process'
    }

    if ($ArtifactUrl) {
        $params['artifactUrl'] = $ArtifactUrl
    } else {
        $params['artifactUrl'] = Get-BcArtifactUrl -type Sandbox -country base -select Latest
    }

    New-BcContainer @params
    Write-Verbose "Container '$ContainerName' created successfully."
}
