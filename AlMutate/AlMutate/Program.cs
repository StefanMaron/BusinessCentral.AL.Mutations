using System.CommandLine;
using AlMutate;
using AlMutate.Models;
using AlMutate.Services;

// ─── scan command ────────────────────────────────────────────────────────────

var scanSourceArg = new Argument<string>("source", "Path to AL source directory to scan");
var scanOperatorsOption = new Option<string?>("--operators", "Path to custom operators JSON file");

var scanCommand = new Command("scan", "Scan AL source for mutation candidates without running tests")
{
    scanSourceArg,
    scanOperatorsOption
};

scanCommand.SetHandler((string source, string? operators) =>
{
    var pipeline = new MutationPipeline(new AlRunnerTestRunner(), operators);
    var result = pipeline.Scan(new PipelineOptions
    {
        Command = "scan",
        SourcePath = source,
        OperatorsPath = operators
    });

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
    Environment.Exit(result.ExitCode);
}, scanSourceArg, scanOperatorsOption);

// ─── run command ─────────────────────────────────────────────────────────────

var runSourceArg = new Argument<string>("source", "Path to AL source directory");
var runTestsOption = new Option<string>("--tests", "Path to compiled test .app file") { IsRequired = true };
var runStubsOption = new Option<string?>("--stubs", "Directory with stub AL files (passed to AL Runner alongside source and tests)");
var runOperatorsOption = new Option<string?>("--operators", "Path to custom operators JSON file");
var runMaxOption = new Option<int?>("--max", "Maximum number of mutations to apply");
var runLogOption = new Option<string?>("--log", "Path to mutation log file (default: mutations.json)");

var runCommand = new Command("run", "Run full mutation testing on AL source")
{
    runSourceArg,
    runTestsOption,
    runStubsOption,
    runOperatorsOption,
    runMaxOption,
    runLogOption
};

runCommand.SetHandler((string source, string tests, string? stubs, string? operators, int? max, string? log) =>
{
    var pipeline = new MutationPipeline(new AlRunnerTestRunner(), operators);
    var result = pipeline.Run(new PipelineOptions
    {
        Command = "run",
        SourcePath = source,
        TestPath = tests,
        StubsPath = stubs,
        OperatorsPath = operators,
        MaxMutations = max,
        LogFilePath = log
    });

    if (result.ErrorMessage != null)
    {
        Console.Error.WriteLine($"Error: {result.ErrorMessage}");
    }
    else
    {
        Console.WriteLine($"Mutation score: {result.Score:F2}%");
        Console.WriteLine($"  Killed:   {result.Results.Count(r => r.Status == MutationStatus.Killed)}");
        Console.WriteLine($"  Survived: {result.Results.Count(r => r.Status == MutationStatus.Survived)}");
        Console.WriteLine($"  Compile errors: {result.Results.Count(r => r.Status == MutationStatus.CompileError)}");

        if (result.ReportMarkdown != null)
            File.WriteAllText("mutation-report.md", result.ReportMarkdown);
    }
    Environment.Exit(result.ExitCode);
}, runSourceArg, runTestsOption, runStubsOption, runOperatorsOption, runMaxOption, runLogOption);

// ─── replay command ───────────────────────────────────────────────────────────

var replayLogArg = new Argument<string>("log", "Path to existing mutations.json log file");
var replayTestsOption = new Option<string>("--tests", "Path to compiled test .app file") { IsRequired = true };
var replayStubsOption = new Option<string?>("--stubs", "Directory with stub AL files (passed to AL Runner alongside source and tests)");

var replayCommand = new Command("replay", "Re-run survived mutations from a previous log")
{
    replayLogArg,
    replayTestsOption,
    replayStubsOption
};

replayCommand.SetHandler((string log, string tests, string? stubs) =>
{
    var pipeline = new MutationPipeline(new AlRunnerTestRunner());
    var result = pipeline.Replay(new PipelineOptions
    {
        Command = "replay",
        LogFilePath = log,
        TestPath = tests,
        StubsPath = stubs
    });

    Environment.Exit(result.ExitCode);
}, replayLogArg, replayTestsOption, replayStubsOption);

// ─── root command ─────────────────────────────────────────────────────────────

var rootCommand = new RootCommand("al-mutate — Mutation testing for Business Central AL code")
{
    scanCommand,
    runCommand,
    replayCommand
};

return await rootCommand.InvokeAsync(args);
