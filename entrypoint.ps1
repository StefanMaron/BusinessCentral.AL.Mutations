#!/usr/bin/env pwsh
<#
.SYNOPSIS
    GitHub Action entrypoint for BCMutations.
.DESCRIPTION
    Loads the BCMutations module and runs Invoke-BCMutationTest.
    Writes outputs to GITHUB_OUTPUT for use in subsequent action steps.
#>
[CmdletBinding()]
param(
    [string]$ProjectPath = '.',
    [string]$SourceFolder = 'src',
    [string]$OperatorFile = '',
    [string]$ContainerName = 'bcmutations',
    [string]$TestSuite = 'DEFAULT',
    [string]$ReportFormat = 'json',
    [int]$MaxMutants = 0,
    [string]$ArtifactUrl = ''
)

$ErrorActionPreference = 'Stop'

# Import the BCMutations module from the action directory
$modulePath = Join-Path $PSScriptRoot 'BCMutations/BCMutations.psd1'
Import-Module $modulePath -Force

$params = @{
    ProjectPath    = $ProjectPath
    SourceFolder   = $SourceFolder
    ContainerName  = $ContainerName
    TestSuite      = $TestSuite
    ReportFormat   = $ReportFormat
    MaxMutants     = $MaxMutants
}

if ($OperatorFile) { $params['OperatorFile'] = $OperatorFile }
if ($ArtifactUrl) { $params['ArtifactUrl'] = $ArtifactUrl }

$result = Invoke-BCMutationTest @params

# Write GitHub Action outputs
if ($env:GITHUB_OUTPUT) {
    Add-Content -Path $env:GITHUB_OUTPUT -Value "mutation-score=$($result.Score)"
    Add-Content -Path $env:GITHUB_OUTPUT -Value "report-path=$($result.ReportPath)"
    Add-Content -Path $env:GITHUB_OUTPUT -Value "killed=$($result.Killed)"
    Add-Content -Path $env:GITHUB_OUTPUT -Value "survived=$($result.Survived)"
}

# Exit with failure if mutation score is 0 and there were survivors
if ($result.Survived -gt 0 -and $result.Score -eq 0) {
    Write-Warning "All mutants survived - tests may not be asserting correctly."
}
