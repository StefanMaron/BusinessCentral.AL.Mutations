function Invoke-AppCompile {
    <#
    .SYNOPSIS
        Compiles an AL app inside a BC container.
    .DESCRIPTION
        Wraps Compile-AppInBcContainer from BcContainerHelper.
        Compiles the app and outputs the .app file to the specified output folder.
        Returns the path to the compiled .app file on success, or $null on failure.
    .PARAMETER ContainerName
        Name of the BC container.
    .PARAMETER ProjectPath
        Path to the AL project root containing app.json.
    .PARAMETER OutputFolder
        Path to the folder where the compiled .app file will be written.
        Default: '.output' subfolder within ProjectPath.
    .OUTPUTS
        String - Path to the compiled .app file, or $null if compilation failed.
    #>
    [CmdletBinding()]
    [OutputType([string])]
    param(
        [Parameter(Mandatory)]
        [string]$ContainerName,

        [Parameter(Mandatory)]
        [string]$ProjectPath,

        [string]$OutputFolder = ''
    )

    if (-not (Get-Command 'Compile-AppInBcContainer' -ErrorAction SilentlyContinue)) {
        throw 'BcContainerHelper is not installed.'
    }

    if (-not $OutputFolder) {
        $OutputFolder = Join-Path $ProjectPath '.output'
    }

    if (-not (Test-Path $OutputFolder)) {
        New-Item -ItemType Directory -Path $OutputFolder -Force | Out-Null
    }

    try {
        Compile-AppInBcContainer -containerName $ContainerName `
            -appProjectFolder $ProjectPath `
            -appOutputFolder $OutputFolder `
            -ErrorAction Stop

        $appFile = Get-ChildItem -Path $OutputFolder -Filter '*.app' | Sort-Object LastWriteTime -Descending | Select-Object -First 1
        if ($appFile) {
            return $appFile.FullName
        }
        Write-Verbose "Compilation succeeded but no .app file found in: $OutputFolder"
        return $null
    } catch {
        Write-Verbose "Compilation failed: $_"
        return $null
    }
}
