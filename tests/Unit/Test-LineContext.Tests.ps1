BeforeAll {
    . "$PSScriptRoot/../../BCMutations/Private/Test-LineContext.ps1"
}

Describe 'Test-LineContext' {
    Context 'normal code (not in comment or string)' {
        It 'returns False for a plain code line at position 0' {
            $result = Test-LineContext -Line 'Amount := Quantity * Price;' -Position 0 -PrecedingText ''
            $result | Should -Be $false
        }

        It 'returns False for an operator in the middle of a code line' {
            $line = 'if Amount > 0 then'
            $pos = $line.IndexOf('>')
            $result = Test-LineContext -Line $line -Position $pos -PrecedingText ''
            $result | Should -Be $false
        }
    }

    Context 'inside single-line comments' {
        It 'returns True for content after //' {
            $line = '// if Amount > 0 then'
            $pos = $line.IndexOf('>')
            $result = Test-LineContext -Line $line -Position $pos -PrecedingText ''
            $result | Should -Be $true
        }

        It 'returns True for operator after code and comment marker' {
            $line = 'Amount := 0; // Amount > 0'
            $pos = $line.LastIndexOf('>')
            $result = Test-LineContext -Line $line -Position $pos -PrecedingText ''
            $result | Should -Be $true
        }

        It 'returns False for operator before comment marker on same line' {
            $line = 'if Amount > 0 then // check'
            $pos = $line.IndexOf('>')
            $result = Test-LineContext -Line $line -Position $pos -PrecedingText ''
            $result | Should -Be $false
        }
    }

    Context 'inside string literals' {
        It 'returns True for operator inside single-quoted string' {
            $line = "Msg := 'Amount > 0';"
            $pos = $line.IndexOf('>')
            $result = Test-LineContext -Line $line -Position $pos -PrecedingText ''
            $result | Should -Be $true
        }

        It 'returns False for operator outside string literal' {
            $line = "if Amount > StrLen('hello') then"
            $pos = $line.IndexOf('>')
            $result = Test-LineContext -Line $line -Position $pos -PrecedingText ''
            $result | Should -Be $false
        }
    }

    Context 'inside block comments' {
        It 'returns True when PrecedingText contains unclosed block comment' {
            $precedingText = 'code; /* start of block comment'
            $line = 'still in block comment > operator'
            $pos = $line.IndexOf('>')
            $result = Test-LineContext -Line $line -Position $pos -PrecedingText $precedingText
            $result | Should -Be $true
        }

        It 'returns False after block comment is closed in preceding text' {
            $precedingText = '/* block comment */ code;'
            $line = 'if Amount > 0 then'
            $pos = $line.IndexOf('>')
            $result = Test-LineContext -Line $line -Position $pos -PrecedingText $precedingText
            $result | Should -Be $false
        }

        It 'returns False when block comment opens and closes on the same line before position' {
            $line = '/* comment */ if Amount > 0 then'
            $pos = $line.IndexOf('>')
            $result = Test-LineContext -Line $line -Position $pos -PrecedingText ''
            $result | Should -Be $false
        }
    }
}
