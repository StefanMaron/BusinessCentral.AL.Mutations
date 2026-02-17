BeforeAll {
    . "$PSScriptRoot/../../BCMutations/Private/Read-OperatorFile.ps1"
    . "$PSScriptRoot/../../BCMutations/Public/Get-BCMutationOperators.ps1"
}

Describe 'Get-BCMutationOperators' {
    Context 'default operator file' {
        It 'returns operators from the default file' {
            $result = Get-BCMutationOperators
            $result | Should -Not -BeNullOrEmpty
            $result.Count | Should -BeGreaterThan 5
        }

        It 'returns objects with id, name, category, pattern properties' {
            $result = Get-BCMutationOperators
            $first = $result | Select-Object -First 1
            $first.PSObject.Properties.Name | Should -Contain 'id'
            $first.PSObject.Properties.Name | Should -Contain 'name'
            $first.PSObject.Properties.Name | Should -Contain 'category'
            $first.PSObject.Properties.Name | Should -Contain 'pattern'
        }
    }

    Context 'category filter' {
        It 'filters by relational category' {
            $result = Get-BCMutationOperators -Category relational
            $result | Should -Not -BeNullOrEmpty
            $result | ForEach-Object { $_.category | Should -Be 'relational' }
        }

        It 'filters by bc-specific category' {
            $result = Get-BCMutationOperators -Category 'bc-specific'
            $result | Should -Not -BeNullOrEmpty
            $result | ForEach-Object { $_.category | Should -Be 'bc-specific' }
        }
    }

    Context 'custom operator file' {
        It 'loads operators from a custom file' {
            $fixturesPath = "$PSScriptRoot/../Fixtures"
            $result = Get-BCMutationOperators -OperatorFile "$fixturesPath/valid-operators.json"
            $result.Count | Should -Be 3
        }
    }
}
