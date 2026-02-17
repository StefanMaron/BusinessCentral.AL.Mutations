BeforeAll {
    . "$PSScriptRoot/../../BCMutations/Private/New-Mutation.ps1"
    . "$PSScriptRoot/../../BCMutations/Private/Restore-Mutation.ps1"
}

Describe 'New-Mutation' {
    BeforeAll {
        $tempDir = [System.IO.Path]::GetTempPath()
        $testFile = Join-Path $tempDir "TestMutation_$(New-Guid).al"
        $originalContent = @"
codeunit 50100 "Test"
{
    procedure Check(Amount: Decimal): Boolean
    begin
        if Amount > 0 then
            exit(true);
        exit(false);
    end;
}
"@
        Set-Content -Path $testFile -Value $originalContent -NoNewline
    }

    AfterAll {
        if (Test-Path $testFile) { Remove-Item $testFile }
        $backupFile = "$testFile.bak"
        if (Test-Path $backupFile) { Remove-Item $backupFile }
    }

    Context 'applying a replacement mutation' {
        It 'modifies the line with the replacement string' {
            $op = [PSCustomObject]@{
                id          = 'rel-gt-to-gte'
                pattern     = ' > '
                replacement = ' >= '
            }
            $mutation = [PSCustomObject]@{
                File        = $testFile
                Line        = 5
                Column      = 19
                Operator    = $op
                OriginalLine = '        if Amount > 0 then'
            }

            New-Mutation -Mutation $mutation

            $modified = Get-Content -Path $testFile
            $modified[4] | Should -Match '>='
            $modified[4] | Should -Not -Match ' > '
        }

        It 'creates a backup file' {
            $backupFile = "$testFile.bak"
            Test-Path $backupFile | Should -Be $true
        }
    }

    Context 'applying a null-replacement (statement removal) mutation' {
        BeforeEach {
            # Restore original content for each test
            Set-Content -Path $testFile -Value $originalContent -NoNewline
            $backupFile = "$testFile.bak"
            if (Test-Path $backupFile) { Remove-Item $backupFile }
        }

        It 'comments out the line when replacement is null' {
            $op = [PSCustomObject]@{
                id          = 'stmt-remove-modify'
                pattern     = '.Modify('
                replacement = $null
            }
            $targetLine = '        Rec.Modify(true);'
            # Add a Modify line to test content
            $contentWithModify = $originalContent -replace 'exit\(false\);', "exit(false);`n        Rec.Modify(true);"
            Set-Content -Path $testFile -Value $contentWithModify -NoNewline

            $lines = Get-Content -Path $testFile
            $lineNum = ($lines | Select-String -Pattern '\.Modify\(').LineNumber | Select-Object -First 1

            $mutation = [PSCustomObject]@{
                File        = $testFile
                Line        = $lineNum
                Column      = ($targetLine.IndexOf('.Modify('))
                Operator    = $op
                OriginalLine = $targetLine
            }

            New-Mutation -Mutation $mutation

            $modified = Get-Content -Path $testFile
            $modified[$lineNum - 1] | Should -Match '^//'
        }
    }
}

Describe 'Restore-Mutation' {
    BeforeAll {
        $tempDir = [System.IO.Path]::GetTempPath()
        $testFile2 = Join-Path $tempDir "TestRestore_$(New-Guid).al"
        $originalContent = "codeunit 50100 `"Test`"`n{`n    procedure Check(): Boolean`n    begin`n        if Amount > 0 then`n    end;`n}"
        Set-Content -Path $testFile2 -Value $originalContent -NoNewline
    }

    AfterAll {
        if (Test-Path $testFile2) { Remove-Item $testFile2 }
        $backupFile2 = "$testFile2.bak"
        if (Test-Path $backupFile2) { Remove-Item $backupFile2 }
    }

    Context 'restoring from backup' {
        It 'restores the original file content' {
            # First create a backup
            $backupFile2 = "$testFile2.bak"
            Copy-Item -Path $testFile2 -Destination $backupFile2

            # Save the original content before modifying
            $expectedContent = Get-Content -Path $testFile2 -Raw

            # Modify the file
            Set-Content -Path $testFile2 -Value 'MODIFIED CONTENT'

            # Restore
            Restore-Mutation -FilePath $testFile2

            $restored = Get-Content -Path $testFile2 -Raw
            $restored | Should -Be $expectedContent
        }

        It 'removes the backup file after restore' {
            $backupFile2 = "$testFile2.bak"
            Test-Path $backupFile2 | Should -Be $false
        }

        It 'throws if no backup file exists' {
            { Restore-Mutation -FilePath $testFile2 } | Should -Throw
        }
    }
}
