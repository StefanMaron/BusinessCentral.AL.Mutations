BeforeAll {
    . "$PSScriptRoot/../../BCMutations/Private/Test-LineContext.ps1"
    . "$PSScriptRoot/../../BCMutations/Private/Read-OperatorFile.ps1"
    . "$PSScriptRoot/../../BCMutations/Private/Find-MutationTargets.ps1"
}

Describe 'Find-MutationTargets' {
    BeforeAll {
        $fixturesPath = "$PSScriptRoot/../Fixtures"
        $operators = Read-OperatorFile -Path "$fixturesPath/valid-operators.json"
        $sampleFile = "$fixturesPath/SampleCode.al"
    }

    Context 'basic target discovery' {
        It 'finds mutation targets in an AL source file' {
            $result = Find-MutationTargets -FilePath $sampleFile -Operators $operators
            $result | Should -Not -BeNullOrEmpty
        }

        It 'returns objects with required properties' {
            $result = Find-MutationTargets -FilePath $sampleFile -Operators $operators
            $first = $result | Select-Object -First 1
            $first.PSObject.Properties.Name | Should -Contain 'File'
            $first.PSObject.Properties.Name | Should -Contain 'Line'
            $first.PSObject.Properties.Name | Should -Contain 'Column'
            $first.PSObject.Properties.Name | Should -Contain 'Operator'
            $first.PSObject.Properties.Name | Should -Contain 'OriginalLine'
        }

        It 'finds the greater-than operator in code' {
            $result = Find-MutationTargets -FilePath $sampleFile -Operators $operators
            $gtTargets = $result | Where-Object { $_.Operator.id -eq 'rel-gt-to-gte' }
            $gtTargets | Should -Not -BeNullOrEmpty
        }

        It 'finds the Modify( pattern in code' {
            $result = Find-MutationTargets -FilePath $sampleFile -Operators $operators
            $modifyTargets = $result | Where-Object { $_.Operator.id -eq 'stmt-remove-modify' }
            $modifyTargets | Should -Not -BeNullOrEmpty
        }
    }

    Context 'context filtering' {
        It 'does not find targets inside single-line comments' {
            $result = Find-MutationTargets -FilePath $sampleFile -Operators $operators
            $commentLineTargets = $result | Where-Object { $_.OriginalLine -match '// if Amount > 0' }
            $commentLineTargets | Should -BeNullOrEmpty
        }

        It 'does not find targets inside string literals' {
            $result = Find-MutationTargets -FilePath $sampleFile -Operators $operators
            # The > in 'if Amount > 0 then' string should not be found
            $stringLineTargets = $result | Where-Object { $_.OriginalLine -match "Msg := 'if Amount" }
            $stringLineTargets | Should -BeNullOrEmpty
        }
    }

    Context 'edge cases' {
        It 'returns empty array for a file with no matches' {
            $result = Find-MutationTargets -FilePath "$fixturesPath/NoMatches.al" -Operators $operators
            $result | Should -BeNullOrEmpty
        }

        It 'throws when file does not exist' {
            { Find-MutationTargets -FilePath 'C:\nonexistent.al' -Operators $operators } | Should -Throw
        }

        It 'returns empty array when operators list is empty' {
            $result = Find-MutationTargets -FilePath $sampleFile -Operators @()
            $result | Should -BeNullOrEmpty
        }
    }
}
