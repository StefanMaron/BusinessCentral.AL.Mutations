BeforeAll {
    . "$PSScriptRoot/../../BCMutations/Private/Write-MutationProgress.ps1"
}

Describe 'Write-MutationProgress' {
    BeforeEach {
        # Capture Write-Host output by mocking it
        $script:capturedOutput = @()
        Mock Write-Host { $script:capturedOutput += $args[0] }
    }

    Context 'output formatting' {
        It 'formats output with progress percentage' {
            Write-MutationProgress -Current 5 -Total 10 -MutationId 'rel-gt-to-gte' `
                -FilePath 'C:\src\MyFile.al' -LineNumber 42 -Status 'Testing'

            $script:capturedOutput[0] | Should -Match '\[5/10 50%\]'
        }

        It 'includes the mutation ID' {
            Write-MutationProgress -Current 1 -Total 1 -MutationId 'arith-plus-to-minus' `
                -FilePath '/src/test.al' -LineNumber 10 -Status 'Killed'

            $script:capturedOutput[0] | Should -Match 'arith-plus-to-minus'
        }

        It 'includes only the filename without full path' {
            Write-MutationProgress -Current 1 -Total 1 -MutationId 'test-op' `
                -FilePath 'C:\some\deep\path\MyCodeunit.al' -LineNumber 100 -Status 'Survived'

            $script:capturedOutput[0] | Should -Match 'MyCodeunit\.al'
            $script:capturedOutput[0] | Should -Not -Match 'C:\\some\\deep\\path'
        }

        It 'includes the line number' {
            Write-MutationProgress -Current 1 -Total 1 -MutationId 'test-op' `
                -FilePath 'test.al' -LineNumber 42 -Status 'Testing'

            $script:capturedOutput[0] | Should -Match ':42'
        }
    }

    Context 'status symbols' {
        It 'shows [TESTING] for Testing status' {
            Write-MutationProgress -Current 1 -Total 1 -MutationId 'op' `
                -FilePath 'f.al' -LineNumber 1 -Status 'Testing'

            $script:capturedOutput[0] | Should -Match '\[TESTING\]'
        }

        It 'shows [KILLED] for Killed status' {
            Write-MutationProgress -Current 1 -Total 1 -MutationId 'op' `
                -FilePath 'f.al' -LineNumber 1 -Status 'Killed'

            $script:capturedOutput[0] | Should -Match '\[KILLED\]'
        }

        It 'shows [SURVIVED] for Survived status' {
            Write-MutationProgress -Current 1 -Total 1 -MutationId 'op' `
                -FilePath 'f.al' -LineNumber 1 -Status 'Survived'

            $script:capturedOutput[0] | Should -Match '\[SURVIVED\]'
        }

        It 'shows [COMPILE ERROR] for CompileError status' {
            Write-MutationProgress -Current 1 -Total 1 -MutationId 'op' `
                -FilePath 'f.al' -LineNumber 1 -Status 'CompileError'

            $script:capturedOutput[0] | Should -Match '\[COMPILE ERROR\]'
        }
    }

    Context 'percentage calculation' {
        It 'calculates percentage correctly at start' {
            Write-MutationProgress -Current 1 -Total 100 -MutationId 'op' `
                -FilePath 'f.al' -LineNumber 1 -Status 'Testing'

            $script:capturedOutput[0] | Should -Match '\[1/100 1%\]'
        }

        It 'calculates percentage correctly at 100%' {
            Write-MutationProgress -Current 100 -Total 100 -MutationId 'op' `
                -FilePath 'f.al' -LineNumber 1 -Status 'Killed'

            $script:capturedOutput[0] | Should -Match '\[100/100 100%\]'
        }

        It 'rounds percentage to nearest integer' {
            Write-MutationProgress -Current 1 -Total 3 -MutationId 'op' `
                -FilePath 'f.al' -LineNumber 1 -Status 'Testing'

            # 1/3 = 33.33...% should round to 33%
            $script:capturedOutput[0] | Should -Match '\[1/3 33%\]'
        }
    }

    Context 'parameter validation' {
        It 'rejects invalid status values' {
            {
                Write-MutationProgress -Current 1 -Total 1 -MutationId 'op' `
                    -FilePath 'f.al' -LineNumber 1 -Status 'InvalidStatus'
            } | Should -Throw
        }
    }
}
