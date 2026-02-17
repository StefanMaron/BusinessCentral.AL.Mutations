function Invoke-BCMutationTest {
    <#
    .SYNOPSIS
        Runs mutation testing on a Business Central AL project.
    .DESCRIPTION
        Scans AL source files for mutation candidates, creates a BC container,
        compiles and tests each mutation, and reports which mutations survived
        (indicating test gaps) vs were killed (indicating tests are effective).
    .PARAMETER ProjectPath
        Path to the user's AL project root (must contain app.json).
    .PARAMETER SourceFolder
        Subfolder within ProjectPath containing .al source files. Default: 'src'.
    .PARAMETER OperatorFile
        Path to custom operator JSON file. Defaults to the bundled operators/default.json.
    .PARAMETER ContainerName
        Name for the BC container. Default: 'bcmutations'.
    .PARAMETER TestSuite
        Name of the test suite to run. Default: 'DEFAULT'.
    .PARAMETER ReportFormat
        Report output format: 'json' or 'markdown'. Default: 'json'.
    .PARAMETER ReportPath
        Path to write the report. Default: 'mutation-report/report.{json|md}'.
    .PARAMETER DryRun
        If specified, lists mutations without executing them.
    .PARAMETER SkipContainerCreate
        If specified, uses an existing container (must already be running).
    .PARAMETER SkipContainerRemove
        If specified, leaves the container running after completion.
    .PARAMETER MaxMutants
        Maximum number of mutants to test. 0 means unlimited.
    .PARAMETER ArtifactUrl
        BC artifact URL to use for container creation.
    .EXAMPLE
        Invoke-BCMutationTest -ProjectPath ./MyBCExtension
    .EXAMPLE
        Invoke-BCMutationTest -ProjectPath ./MyBCExtension -DryRun
    #>
    [CmdletBinding(SupportsShouldProcess)]
    param(
        [Parameter(Mandatory)]
        [ValidateScript({ Test-Path $_ -PathType Container })]
        [string]$ProjectPath,

        [string]$SourceFolder = 'src',

        [string]$OperatorFile = '',

        [string]$ContainerName = 'bcmutations',

        [string]$TestSuite = 'DEFAULT',

        [ValidateSet('json', 'markdown')]
        [string]$ReportFormat = 'json',

        [string]$ReportPath = '',

        [switch]$DryRun,

        [switch]$SkipContainerCreate,

        [switch]$SkipContainerRemove,

        [int]$MaxMutants = 0,

        [string]$ArtifactUrl = ''
    )

    $ErrorActionPreference = 'Stop'

    # Phase 1: Initialization
    Write-Host "=== BCMutations: Mutation Testing Tool ==="

    $projectPath = Resolve-Path $ProjectPath | Select-Object -ExpandProperty Path

    # Load operator file
    if (-not $OperatorFile) {
        $OperatorFile = $script:DefaultOperatorFile
        if (-not $OperatorFile -or -not (Test-Path $OperatorFile)) {
            # Fallback: resolve relative to script location
            $OperatorFile = Join-Path $PSScriptRoot '../../operators/default.json'
        }
        if (Test-Path $OperatorFile) {
            $OperatorFile = (Resolve-Path $OperatorFile).Path
        }
    }

    Write-Host "Loading operators from: $OperatorFile"
    $operators = Read-OperatorFile -Path $OperatorFile

    # Phase 2: Mutation Discovery
    $sourceDir = Join-Path $projectPath $SourceFolder
    if (-not (Test-Path $sourceDir -PathType Container)) {
        throw "Source folder not found: $sourceDir"
    }

    Write-Host "Scanning AL files in: $sourceDir"
    $alFiles = Get-ChildItem -Path $sourceDir -Filter '*.al' -Recurse

    if ($alFiles.Count -eq 0) {
        Write-Warning "No .al files found in: $sourceDir"
        return
    }

    $allTargets = [System.Collections.Generic.List[PSCustomObject]]::new()
    foreach ($file in $alFiles) {
        $targets = Find-MutationTargets -FilePath $file.FullName -Operators $operators
        foreach ($t in $targets) { $allTargets.Add($t) }
    }

    Write-Host "Found $($allTargets.Count) mutation candidates across $($alFiles.Count) file(s)."

    if ($MaxMutants -gt 0 -and $allTargets.Count -gt $MaxMutants) {
        Write-Host "Limiting to $MaxMutants mutants (MaxMutants parameter)."
        $limited = [System.Collections.Generic.List[PSCustomObject]]::new()
        $allTargets | Select-Object -First $MaxMutants | ForEach-Object { $limited.Add($_) }
        $allTargets = $limited
    }

    if ($DryRun) {
        Write-Host "`n=== DRY RUN - Mutations that would be tested: ==="
        $allTargets | ForEach-Object {
            $f = Split-Path $_.File -Leaf
            Write-Host "  $($_.Operator.id) @ $f:$($_.Line) col $($_.Column)"
        }
        return
    }

    if ($allTargets.Count -eq 0) {
        Write-Host "No mutation targets found. Nothing to test."
        return
    }

    # Phase 3: Container setup
    if (-not $SkipContainerCreate) {
        Write-Host "Creating BC container: $ContainerName"
        Invoke-ContainerSetup -ContainerName $ContainerName -ProjectPath $projectPath -ArtifactUrl $ArtifactUrl
    } else {
        Write-Host "Using existing container: $ContainerName"
    }

    $results = [System.Collections.Generic.List[PSCustomObject]]::new()

    try {
        # Phase 4: Baseline
        Write-Host "Compiling baseline..."
        $baselineAppFile = Invoke-AppCompile -ContainerName $ContainerName -ProjectPath $projectPath
        if (-not $baselineAppFile) {
            throw "Baseline compilation failed. Fix compilation errors before running mutation testing."
        }

        Write-Host "Deploying baseline..."
        Invoke-AppDeploy -ContainerName $ContainerName -AppFile $baselineAppFile

        Write-Host "Running baseline tests..."
        $baselinePassed = Invoke-TestRun -ContainerName $ContainerName -TestSuite $TestSuite
        if (-not $baselinePassed) {
            throw "Baseline tests failed. Fix failing tests before running mutation testing."
        }
        Write-Host "Baseline: OK"

        # Phase 5: Mutation loop
        Write-Host "`nStarting mutation loop ($($allTargets.Count) mutants)..."
        $total = $allTargets.Count

        for ($i = 0; $i -lt $total; $i++) {
            $mutation = $allTargets[$i]
            $current = $i + 1

            Write-MutationProgress -Current $current -Total $total `
                -MutationId $mutation.Operator.id `
                -FilePath $mutation.File `
                -LineNumber $mutation.Line `
                -Status 'Testing'

            $status = 'Survived'

            try {
                New-Mutation -Mutation $mutation

                $appFile = Invoke-AppCompile -ContainerName $ContainerName -ProjectPath $projectPath
                if (-not $appFile) {
                    $status = 'CompileError'
                } else {
                    Invoke-AppDeploy -ContainerName $ContainerName -AppFile $appFile
                    $passed = Invoke-TestRun -ContainerName $ContainerName -TestSuite $TestSuite
                    $status = if (-not $passed) { 'Killed' } else { 'Survived' }
                }
            } catch {
                Write-Verbose "Error testing mutation: $_"
                $status = 'CompileError'
            } finally {
                Restore-Mutation -FilePath $mutation.File
            }

            Write-MutationProgress -Current $current -Total $total `
                -MutationId $mutation.Operator.id `
                -FilePath $mutation.File `
                -LineNumber $mutation.Line `
                -Status $status

            $results.Add([PSCustomObject]@{
                Mutation = $mutation
                Status   = $status
            })
        }

    } finally {
        # Phase 6: Teardown
        if (-not $SkipContainerRemove) {
            Write-Host "`nRemoving container: $ContainerName"
            Invoke-ContainerTeardown -ContainerName $ContainerName
        }
    }

    # Phase 7: Report
    $ext = if ($ReportFormat -eq 'markdown') { 'md' } else { 'json' }
    if (-not $ReportPath) {
        $ReportPath = Join-Path $projectPath "mutation-report/report.$ext"
    }

    $reportDir = Split-Path $ReportPath -Parent
    if (-not (Test-Path $reportDir)) {
        New-Item -ItemType Directory -Path $reportDir -Force | Out-Null
    }

    $report = ConvertTo-MutationReport -Results $results.ToArray() -Format $ReportFormat -OutputPath $ReportPath

    $killed = @($results | Where-Object { $_.Status -eq 'Killed' }).Count
    $survived = @($results | Where-Object { $_.Status -eq 'Survived' }).Count
    $score = if (($killed + $survived) -gt 0) {
        [math]::Round(($killed / ($killed + $survived)) * 100, 1)
    } else { 0.0 }

    Write-Host "`n=== Mutation Testing Complete ==="
    Write-Host "Mutation Score: $score% ($killed killed, $survived survived)"
    Write-Host "Report written to: $ReportPath"

    return [PSCustomObject]@{
        Score         = $score
        Killed        = $killed
        Survived      = $survived
        CompileErrors = @($results | Where-Object { $_.Status -eq 'CompileError' }).Count
        Total         = $results.Count
        ReportPath    = $ReportPath
        Results       = $results.ToArray()
    }
}
