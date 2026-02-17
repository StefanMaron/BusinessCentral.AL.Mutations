function Invoke-AppCompile {
    <#
    .SYNOPSIS
        Compiles an AL app inside a BC container.
    .DESCRIPTION
        Wraps Compile-AppInBcContainer from BcContainerHelper.
        Returns $true if compilation succeeded, $false on error.
    .PARAMETER ContainerName
        Name of the BC container.
    .PARAMETER ProjectPath
        Path to the AL project root containing app.json.
    .OUTPUTS
        Boolean - $true if compiled successfully, $false otherwise.
    #>
    [CmdletBinding()]
    [OutputType([bool])]
    param(
        [Parameter(Mandatory)]
        [string]$ContainerName,

        [Parameter(Mandatory)]
        [string]$ProjectPath
    )

    if (-not (Get-Command 'Compile-AppInBcContainer' -ErrorAction SilentlyContinue)) {
        throw 'BcContainerHelper is not installed.'
    }

    try {
        Compile-AppInBcContainer -containerName $ContainerName -appProjectFolder $ProjectPath -ErrorAction Stop
        return $true
    } catch {
        Write-Verbose "Compilation failed: $_"
        return $false
    }
}
