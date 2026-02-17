BeforeAll {
    . "$PSScriptRoot/../../BCMutations/Private/ConvertTo-MutationReport.ps1"
}

Describe 'ConvertTo-MutationReport' {
    BeforeAll {
        $mockMutation = [PSCustomObject]@{
            File        = '/project/src/MyCodeunit.al'
            Line        = 10
            Column      = 15
            OriginalLine = '        if Amount > 0 then'
            Operator    = [PSCustomObject]@{
                id          = 'rel-gt-to-gte'
                name        = 'Greater-than to greater-or-equal'
                category    = 'relational'
                pattern     = ' > '
                replacement = ' >= '
            }
        }

        $killedResult = [PSCustomObject]@{ Mutation = $mockMutation; Status = 'Killed' }
        $survivedResult = [PSCustomObject]@{ Mutation = $mockMutation; Status = 'Survived' }
        $compileErrorResult = [PSCustomObject]@{ Mutation = $mockMutation; Status = 'CompileError' }
    }

    Context 'JSON format' {
        It 'generates valid JSON output' {
            $result = ConvertTo-MutationReport -Results @($killedResult) -Format 'json'
            { $result | ConvertFrom-Json } | Should -Not -Throw
        }

        It 'calculates 100% mutation score when all killed' {
            $result = ConvertTo-MutationReport -Results @($killedResult, $killedResult) -Format 'json'
            $data = $result | ConvertFrom-Json
            $data.mutationScore | Should -Be 100.0
        }

        It 'calculates 0% mutation score when all survived' {
            $result = ConvertTo-MutationReport -Results @($survivedResult) -Format 'json'
            $data = $result | ConvertFrom-Json
            $data.mutationScore | Should -Be 0.0
        }

        It 'calculates 50% mutation score with equal killed and survived' {
            $result = ConvertTo-MutationReport -Results @($killedResult, $survivedResult) -Format 'json'
            $data = $result | ConvertFrom-Json
            $data.mutationScore | Should -Be 50.0
        }

        It 'excludes compile errors from score calculation' {
            $result = ConvertTo-MutationReport -Results @($killedResult, $compileErrorResult) -Format 'json'
            $data = $result | ConvertFrom-Json
            $data.mutationScore | Should -Be 100.0
            $data.compileErrors | Should -Be 1
        }

        It 'includes survivor details in output' {
            $result = ConvertTo-MutationReport -Results @($survivedResult) -Format 'json'
            $data = $result | ConvertFrom-Json
            $data.survivors.Count | Should -Be 1
            $data.survivors[0].operatorId | Should -Be 'rel-gt-to-gte'
        }

        It 'handles empty results without error' {
            { ConvertTo-MutationReport -Results @() -Format 'json' } | Should -Not -Throw
        }
    }

    Context 'Markdown format' {
        It 'generates markdown output with a header' {
            $result = ConvertTo-MutationReport -Results @($killedResult, $survivedResult) -Format 'markdown'
            $result | Should -Match '# Mutation Testing Report'
        }

        It 'includes mutation score in markdown output' {
            $result = ConvertTo-MutationReport -Results @($killedResult, $survivedResult) -Format 'markdown'
            $result | Should -Match 'Mutation Score'
            $result | Should -Match '50'
        }

        It 'lists survived mutants in markdown output' {
            $result = ConvertTo-MutationReport -Results @($survivedResult) -Format 'markdown'
            $result | Should -Match 'Survived Mutants'
            $result | Should -Match 'rel-gt-to-gte'
        }
    }

    Context 'file output' {
        It 'writes report to specified file' {
            $tmpFile = [System.IO.Path]::GetTempFileName()
            try {
                ConvertTo-MutationReport -Results @($killedResult) -Format 'json' -OutputPath $tmpFile
                Test-Path $tmpFile | Should -Be $true
                (Get-Content $tmpFile -Raw) | Should -Not -BeNullOrEmpty
            } finally {
                if (Test-Path $tmpFile) { Remove-Item $tmpFile }
            }
        }
    }
}
