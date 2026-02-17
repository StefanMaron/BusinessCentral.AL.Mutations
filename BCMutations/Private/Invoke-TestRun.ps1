function Invoke-TestRun {
    <#
    .SYNOPSIS
        Runs AL tests in a BC container and returns pass/fail.
    .DESCRIPTION
        Wraps Run-TestsInBcContainer from BcContainerHelper.
        Returns $true if all tests passed, $false if any test failed.
    .PARAMETER ContainerName
        Name of the BC container.
    .PARAMETER TestSuite
        Name of the test suite to run.
    .OUTPUTS
        Boolean - $true if all tests passed, $false if any failed.
    #>
    [CmdletBinding()]
    [OutputType([bool])]
    param(
        [Parameter(Mandatory)]
        [string]$ContainerName,

        [Parameter(Mandatory)]
        [string]$TestSuite
    )

    if (-not (Get-Command 'Run-TestsInBcContainer' -ErrorAction SilentlyContinue)) {
        throw 'BcContainerHelper is not installed.'
    }

    try {
        $passed = Run-TestsInBcContainer -containerName $ContainerName -testSuite $TestSuite -returnTrueIfAllPassed
        return [bool]$passed
    } catch {
        Write-Verbose "Test run error: $_"
        return $false
    }
}
