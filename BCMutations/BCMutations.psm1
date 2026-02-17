# BCMutations.psm1 - Module loader

# Module root directory (used for locating bundled resources like operators/)
$script:ModuleRoot = $PSScriptRoot
$script:DefaultOperatorFile = Join-Path (Split-Path $PSScriptRoot -Parent) 'operators' 'default.json'

$Public = @(Get-ChildItem -Path "$PSScriptRoot/Public/*.ps1" -ErrorAction SilentlyContinue)
$Private = @(Get-ChildItem -Path "$PSScriptRoot/Private/*.ps1" -ErrorAction SilentlyContinue)

foreach ($import in @($Private + $Public)) {
    try {
        . $import.FullName
    } catch {
        Write-Error "Failed to import function $($import.FullName): $_"
    }
}

Export-ModuleMember -Function $Public.BaseName
