function New-BCMutationConfig {
    <#
    .SYNOPSIS
        Generates a default BCMutations configuration file.
    .DESCRIPTION
        Creates a JSON configuration file with default settings for Invoke-BCMutationTest.
        Edit this file to customize mutation testing behavior for your project.
    .PARAMETER OutputPath
        Path to write the configuration file. Default: './bcmutations.config.json'.
    .EXAMPLE
        New-BCMutationConfig
    .EXAMPLE
        New-BCMutationConfig -OutputPath ./config/mutations.json
    #>
    [CmdletBinding(SupportsShouldProcess)]
    param(
        [string]$OutputPath = './bcmutations.config.json'
    )

    $defaultConfig = [PSCustomObject]@{
        sourceFolder         = 'src'
        containerName        = 'bcmutations'
        testSuite            = 'DEFAULT'
        reportFormat         = 'json'
        maxMutants           = 0
        skipContainerCreate  = $false
        skipContainerRemove  = $false
        categories           = @()
        operatorFile         = ''
    }

    $json = $defaultConfig | ConvertTo-Json -Depth 3

    if ($PSCmdlet.ShouldProcess($OutputPath, 'Create configuration file')) {
        Set-Content -Path $OutputPath -Value $json -Encoding UTF8
        Write-Host "Configuration file created: $OutputPath"
        Write-Host "Edit this file to customize mutation testing for your project."
    }
}
