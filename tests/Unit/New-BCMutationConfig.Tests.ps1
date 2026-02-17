BeforeAll {
    . "$PSScriptRoot/../../BCMutations/Public/New-BCMutationConfig.ps1"
}

Describe 'New-BCMutationConfig' {
    Context 'creates a config file' {
        It 'creates a JSON file at the specified path' {
            $tmpFile = [System.IO.Path]::GetTempFileName() -replace '\.tmp$', '.json'
            try {
                New-BCMutationConfig -OutputPath $tmpFile
                Test-Path $tmpFile | Should -Be $true
            } finally {
                if (Test-Path $tmpFile) { Remove-Item $tmpFile }
            }
        }

        It 'creates valid JSON content' {
            $tmpFile = [System.IO.Path]::GetTempFileName() -replace '\.tmp$', '.json'
            try {
                New-BCMutationConfig -OutputPath $tmpFile
                $content = Get-Content $tmpFile -Raw
                { $content | ConvertFrom-Json } | Should -Not -Throw
            } finally {
                if (Test-Path $tmpFile) { Remove-Item $tmpFile }
            }
        }

        It 'includes expected default settings' {
            $tmpFile = [System.IO.Path]::GetTempFileName() -replace '\.tmp$', '.json'
            try {
                New-BCMutationConfig -OutputPath $tmpFile
                $config = Get-Content $tmpFile -Raw | ConvertFrom-Json
                $config.sourceFolder | Should -Be 'src'
                $config.containerName | Should -Be 'bcmutations'
                $config.testSuite | Should -Be 'DEFAULT'
                $config.reportFormat | Should -Be 'json'
            } finally {
                if (Test-Path $tmpFile) { Remove-Item $tmpFile }
            }
        }
    }

    Context 'WhatIf support' {
        It 'does not create file with -WhatIf' {
            $tmpFile = Join-Path ([System.IO.Path]::GetTempPath()) "bcmutations-test-whatif-$(New-Guid).json"
            try {
                New-BCMutationConfig -OutputPath $tmpFile -WhatIf
                Test-Path $tmpFile | Should -Be $false
            } finally {
                if (Test-Path $tmpFile) { Remove-Item $tmpFile }
            }
        }
    }
}
