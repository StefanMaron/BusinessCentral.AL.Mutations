using System.CommandLine;
using AlMutate;
using AlMutate.Models;
using AlMutate.Services;

// ─── guide text ──────────────────────────────────────────────────────────────
// Printed when --guide is passed. Intended for AI agents that need to understand
// how to invoke al-mutate correctly.

const string GuideText = """
# al-mutate — AI Agent Guide

## What It Is
Mutation testing tool for Business Central AL code. Applies automated code mutations
(logic inversions, operator swaps, statement removals) to AL source files, runs the
AL test suite after each mutation via al-runner, and reports which mutations survived
(i.e. tests did not detect the change).

## Prerequisites
- .NET 8.0 SDK installed
- A git repository with tracked AL source files
- Working tree MUST be clean (no tracked changes) — enforced at startup
- AL test source directory compatible with al-runner (codeunits with Subtype=Test)

## Commands

### scan <source>
Dry-run: lists mutation candidates without modifying files or running tests.
Safe to run at any time — no side effects.
Exit 0 = candidates found (or none found). Exit 1 = error.

    al-mutate scan ./src
    al-mutate scan ./src --operators ./my-operators.json

### run <source> --tests <testdir>
Full mutation testing run:
  1. Verify git working tree is clean (abort if dirty)
  2. Run baseline tests (abort if baseline fails)
  3. For each candidate: apply mutation → run tests via al-runner → restore file via git
  4. Append results to mutations.json
  5. Write report.md and print mutation score

Exit 0 = run completed successfully. Exit 1 = error (see stderr).

    al-mutate run ./src --tests ./test/src
    al-mutate run ./src --tests ./test/src --stubs ./stubs
    al-mutate run ./src --tests ./test/src --max 50 --log ./ci/mutations.json

Options:
  --tests <path>      (Required) Path to AL test source directory
  --stubs <path>      Path to stub AL files directory (excluded from mutation scanning;
                      passed to al-runner alongside source and tests)
  --operators <path>  Path to custom operators JSON file (default: built-in 33 operators)
  --max <n>           Limit to first N mutation candidates (useful for sampling or CI)
  --log <path>        Output path for mutations.json (default: mutations.json in CWD)
  --timeout <secs>    Per-mutation test-run time limit in seconds (default: 300).
                      Exceeded mutations are recorded as TIMED_OUT (neutral, excluded from
                      score) and the run continues. Set low (e.g. 15) for projects where
                      tests are fast, to guard against mutations that cause infinite loops.
  --silent            Suppress all progress output; only errors go to stderr

### replay <log> --tests <testdir>
Re-test survived mutations from a previous run. Use after writing new tests to verify
that previously-survived mutations are now killed.
Exit 0 = completed. Exit 1 = error.

    al-mutate replay mutations.json --tests ./test/src

## Output Files
- mutations.json  Append-only log; schema_version 1
                  Statuses: KILLED | SURVIVED | COMPILE_ERROR | OBSOLETE
- report.md       Markdown report with mutation score and per-survived-mutation details

## Mutation Score
  Score = Killed / (Killed + Survived) * 100%
  COMPILE_ERROR and OBSOLETE mutations are excluded from the score denominator.
  A higher score means better test coverage against logic mutations.

## Mutation Statuses
  KILLED        Tests detected the mutation — good, test suite is working
  SURVIVED      Tests missed the mutation — test gap, consider adding a test
  COMPILE_ERROR Mutation broke compilation — excluded from score
  TIMED_OUT     Test run exceeded --timeout — neutral, excluded from score (likely infinite loop)
  OBSOLETE      Original source line no longer exists — stale entry, safe to ignore

## Exit Codes
  0  Success (run completed or scan finished)
  1  Error (dirty working tree, baseline failure, fatal exception — see stderr)

## Common Workflows

Discover test gaps in a project:
    al-mutate run ./src --tests ./test/src

Quick sample run without blocking CI for long:
    al-mutate run ./src --tests ./test/src --max 50

Verify new tests now kill previously-survived mutations:
    al-mutate replay mutations.json --tests ./test/src

Explore what mutations would be generated before committing to a full run:
    al-mutate scan ./src

Use stubs for repos where some AL objects are not supported by al-runner:
    al-mutate run ./src --tests ./test/src --stubs ./test/stubs

## Notes for AI Agents
- The working tree must be clean before running `run` or `replay`. If it is dirty,
  commit or stash changes first.
- mutations.json is append-only — do not delete it between runs; replay depends on it.
- The --silent flag is useful in automated pipelines where only the final exit code
  and stderr matter. Progress output goes to stdout; errors always go to stderr.
- al-runner is installed automatically from NuGet if not already present.
""";

// ─── --guide early exit ───────────────────────────────────────────────────────
// Check for --guide before System.CommandLine parses args so it always works
// regardless of which subcommand is specified. Also registered as a root option
// below so it appears in --help output.
if (args.Contains("--guide"))
{
    Console.Write(GuideText);
    return 0;
}

// ─── global options ───────────────────────────────────────────────────────────

var silentOption = new Option<bool>(
    "--silent",
    "Suppress progress output; only errors go to stderr");

var guideOption = new Option<bool>(
    "--guide",
    "Print AI agent usage guide and exit");

// ─── scan command ─────────────────────────────────────────────────────────────

var scanSourceArg = new Argument<string>(
    "source",
    "Path to AL source directory to scan for mutation candidates");

var scanOperatorsOption = new Option<string?>(
    "--operators",
    "Path to custom operators JSON file (default: built-in 33 operators across 8 categories)");

var scanCommand = new Command(
    "scan",
    "Scan AL source for mutation candidates without running tests (dry-run, no side effects)")
{
    scanSourceArg,
    scanOperatorsOption,
    silentOption,
};

scanCommand.SetHandler((string source, string? operators, bool silent) =>
{
    var pipeline = new MutationPipeline(new AlRunnerTestRunner(), operators);
    var result = pipeline.Scan(new PipelineOptions
    {
        Command = "scan",
        SourcePath = source,
        OperatorsPath = operators,
        Silent = silent,
    });

    if (!silent)
    {
        if (result.Results.Count == 0)
        {
            Console.WriteLine("No mutation candidates found.");
        }
        else
        {
            Console.WriteLine($"Found {result.Results.Count} mutation candidate(s):");
            foreach (var m in result.Results)
            {
                Console.WriteLine($"  {m.Id}  [{m.Operator}]  {m.File}:{m.Line}");
                Console.WriteLine($"        - {m.Original.Trim()}");
                Console.WriteLine($"        + {m.Mutated.Trim()}");
            }
        }
    }
    Environment.Exit(result.ExitCode);
}, scanSourceArg, scanOperatorsOption, silentOption);

// ─── run command ──────────────────────────────────────────────────────────────

var runSourceArg = new Argument<string>(
    "source",
    "Path to AL source directory to mutate");

var runTestsOption = new Option<string>(
    "--tests",
    "Path to AL test source directory (must be compatible with al-runner)")
{ IsRequired = true };

var runStubsOption = new Option<string?>(
    "--stubs",
    "Path to stub AL files directory — excluded from mutation scanning, passed to al-runner alongside source and tests");

var runOperatorsOption = new Option<string?>(
    "--operators",
    "Path to custom operators JSON file (default: built-in 33 operators across 8 categories)");

var runMaxOption = new Option<int?>(
    "--max",
    "Limit to the first N mutation candidates (useful for sampling or faster CI runs)");

var runLogOption = new Option<string?>(
    "--log",
    "Output path for the mutations.json log file (default: mutations.json in the current directory)");

var runTimeoutOption = new Option<int?>(
    "--timeout",
    "Maximum seconds allowed for a single mutation's test run (default: 300). " +
    "When exceeded the mutation is recorded as TIMED_OUT (neutral, excluded from score) and the run continues. " +
    "Use a low value (e.g. 15) to guard against mutations that cause infinite loops.");

var runCommand = new Command(
    "run",
    "Run full mutation testing: baseline → mutate → test → restore → report")
{
    runSourceArg,
    runTestsOption,
    runStubsOption,
    runOperatorsOption,
    runMaxOption,
    runLogOption,
    runTimeoutOption,
    silentOption,
};

runCommand.SetHandler(
    async (string source, string tests, string? stubs, string? operators, int? max, string? log, int? timeout, bool silent) =>
    {
        var pipeline = new MutationPipeline(new AlRunnerTestRunner(), operators);
        var result = await pipeline.RunAsync(new PipelineOptions
        {
            Command = "run",
            SourcePath = source,
            TestPath = tests,
            StubsPath = stubs,
            OperatorsPath = operators,
            MaxMutations = max,
            LogFilePath = log,
            MutationTimeout = TimeSpan.FromSeconds(timeout ?? 300),
            Silent = silent,
        });

        if (result.ErrorMessage != null)
        {
            Console.Error.WriteLine($"Error: {result.ErrorMessage}");
        }
        else if (!silent)
        {
            Console.WriteLine();
            Console.WriteLine($"Mutation score: {result.Score:F1}%");
            Console.WriteLine($"  Killed:         {result.Results.Count(r => r.Status == MutationStatus.Killed)}");
            Console.WriteLine($"  Survived:       {result.Results.Count(r => r.Status == MutationStatus.Survived)}");
            Console.WriteLine($"  Compile errors: {result.Results.Count(r => r.Status == MutationStatus.CompileError)}");
            var timedOut = result.Results.Count(r => r.Status == MutationStatus.TimedOut);
            if (timedOut > 0)
                Console.WriteLine($"  Timed out:      {timedOut}");

            if (result.ReportMarkdown != null)
                Console.WriteLine("  Report written: report.md");
        }

        if (result.ReportMarkdown != null)
            File.WriteAllText("report.md", result.ReportMarkdown);

        Environment.Exit(result.ExitCode);
    },
    runSourceArg, runTestsOption, runStubsOption, runOperatorsOption, runMaxOption, runLogOption, runTimeoutOption, silentOption);

// ─── replay command ───────────────────────────────────────────────────────────

var replayLogArg = new Argument<string>(
    "log",
    "Path to an existing mutations.json log file produced by a previous run");

var replayTestsOption = new Option<string>(
    "--tests",
    "Path to AL test source directory (must be compatible with al-runner)")
{ IsRequired = true };

var replayStubsOption = new Option<string?>(
    "--stubs",
    "Path to stub AL files directory — excluded from mutation scanning, passed to al-runner alongside source and tests");

var replayCommand = new Command(
    "replay",
    "Re-test survived mutations from a previous run to verify new tests kill them")
{
    replayLogArg,
    replayTestsOption,
    replayStubsOption,
    silentOption,
};

replayCommand.SetHandler((string log, string tests, string? stubs, bool silent) =>
{
    var pipeline = new MutationPipeline(new AlRunnerTestRunner());
    var result = pipeline.Replay(new PipelineOptions
    {
        Command = "replay",
        LogFilePath = log,
        TestPath = tests,
        StubsPath = stubs,
        Silent = silent,
    });

    Environment.Exit(result.ExitCode);
}, replayLogArg, replayTestsOption, replayStubsOption, silentOption);

// ─── root command ──────────────────────────────────────────────────────────────

var rootCommand = new RootCommand("al-mutate — Mutation testing for Business Central AL code")
{
    scanCommand,
    runCommand,
    replayCommand,
};

rootCommand.AddOption(guideOption);

return await rootCommand.InvokeAsync(args);
