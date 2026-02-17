BeforeAll {
    . "$PSScriptRoot/../../BCMutations/Private/Read-OperatorFile.ps1"
}

Describe 'Read-OperatorFile' {
    BeforeAll {
        $fixturesPath = "$PSScriptRoot/../Fixtures"
    }

    Context 'when given a valid operator file' {
        It 'returns an array of operator objects' {
            $result = Read-OperatorFile -Path "$fixturesPath/valid-operators.json"
            $result | Should -Not -BeNullOrEmpty
            $result | Should -BeOfType [PSCustomObject]
        }

        It 'each operator has required fields' {
            $result = Read-OperatorFile -Path "$fixturesPath/valid-operators.json"
            foreach ($op in $result) {
                $op.id | Should -Not -BeNullOrEmpty
                $op.name | Should -Not -BeNullOrEmpty
                $op.category | Should -Not -BeNullOrEmpty
                $op.pattern | Should -Not -BeNullOrEmpty
            }
        }

        It 'replacement can be null for statement-removal operators' {
            $result = Read-OperatorFile -Path "$fixturesPath/valid-operators.json"
            $nullReplacement = $result | Where-Object { $null -eq $_.replacement }
            $nullReplacement | Should -Not -BeNullOrEmpty
        }

        It 'loads the default operator file without error' {
            $defaultPath = "$PSScriptRoot/../../operators/default.json"
            { Read-OperatorFile -Path $defaultPath } | Should -Not -Throw
        }

        It 'returns all operators from the default file' {
            $defaultPath = "$PSScriptRoot/../../operators/default.json"
            $result = Read-OperatorFile -Path $defaultPath
            $result.Count | Should -BeGreaterThan 5
        }
    }

    Context 'when the file does not exist' {
        It 'throws a terminating error' {
            { Read-OperatorFile -Path 'C:\nonexistent\file.json' } | Should -Throw
        }
    }

    Context 'when the file has invalid JSON' {
        It 'throws a terminating error' {
            { Read-OperatorFile -Path "$fixturesPath/invalid-json.json" } | Should -Throw
        }
    }

    Context 'when the file is missing the operators array' {
        It 'throws a terminating error' {
            { Read-OperatorFile -Path "$fixturesPath/missing-operators-array.json" } | Should -Throw
        }
    }

    Context 'when the file has operators with missing required fields' {
        It 'throws a terminating error' {
            { Read-OperatorFile -Path "$fixturesPath/invalid-operator-missing-fields.json" } | Should -Throw
        }
    }

    Context 'when the operators array is empty' {
        It 'throws a terminating error' {
            { Read-OperatorFile -Path "$fixturesPath/empty-operators.json" } | Should -Throw
        }
    }
}
