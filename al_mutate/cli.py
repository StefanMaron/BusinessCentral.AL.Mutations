"""CLI entry point for al-mutate."""

from __future__ import annotations

import argparse
import sys
from pathlib import Path

from al_mutate.log import MutationLog, MutationResult
from al_mutate.mutate import apply_mutation, restore_file, MutationError
from al_mutate.operators import load_operators
from al_mutate.report import generate_markdown, mutation_score
from al_mutate.run import check_prerequisites, compile_project, publish_app, run_tests
from al_mutate.scan import scan_directory, MutationCandidate

DEFAULT_OPERATORS = Path(__file__).resolve().parent.parent / "operators" / "default.json"


def build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(
        prog="al-mutate",
        description="Mutation testing tool for Business Central AL code",
    )
    sub = parser.add_subparsers(dest="command", required=True)

    # scan
    scan_p = sub.add_parser("scan", help="List mutation candidates without executing")
    scan_p.add_argument("source", type=Path, help="AL source directory")
    scan_p.add_argument("--operators", type=Path, default=None, help="Custom operator JSON")

    # run
    run_p = sub.add_parser("run", help="Run full mutation testing")
    run_p.add_argument("source", type=Path, help="AL source directory")
    run_p.add_argument("--tests", type=Path, required=True, help="Test app path")
    run_p.add_argument("--operators", type=Path, default=None, help="Custom operator JSON")
    run_p.add_argument("--max", type=int, default=None, help="Max mutations to run")

    # replay
    replay_p = sub.add_parser("replay", help="Replay survived mutations from a log")
    replay_p.add_argument("log_file", type=Path, help="mutations.json path")
    replay_p.add_argument("--tests", type=Path, required=True, help="Test app path")

    return parser


def cmd_scan(args: argparse.Namespace) -> None:
    operators = load_operators(args.operators or DEFAULT_OPERATORS)
    candidates = scan_directory(args.source, operators)
    print(f"Found {len(candidates)} mutation candidates:\n")
    for c in candidates:
        print(f"  [{c.operator_id}] {c.file}:{c.line}")
        print(f"    - {c.original.strip()}")
        print(f"    + {c.mutated.strip()}")
        print()


def cmd_run(args: argparse.Namespace) -> None:
    check_prerequisites()
    operators = load_operators(args.operators or DEFAULT_OPERATORS)
    candidates = scan_directory(args.source, operators)

    if args.max:
        candidates = candidates[: args.max]

    print(f"Found {len(candidates)} mutation candidates")

    # Baseline
    print("Running baseline...")
    if not compile_project():
        print("ERROR: Baseline compile failed", file=sys.stderr)
        sys.exit(1)
    if not publish_app():
        print("ERROR: Baseline publish failed", file=sys.stderr)
        sys.exit(1)
    passed, output = run_tests(args.tests)
    if not passed:
        print("ERROR: Baseline tests failed — fix tests before mutation testing", file=sys.stderr)
        sys.exit(1)
    print("Baseline passed\n")

    # Load or create log
    log_path = Path("mutations.json")
    if log_path.exists():
        log = MutationLog.load(log_path)
    else:
        log = MutationLog(log_path, project=str(args.source))

    results = _run_mutations(candidates, args.tests, log)

    log.append_run(results)
    log.save()

    # Report
    score = mutation_score(results)
    report = generate_markdown(results, str(args.source))
    Path("report.md").write_text(report)

    killed = sum(1 for r in results if r.status == "KILLED")
    survived = sum(1 for r in results if r.status == "SURVIVED")
    print(f"\nMutation Score: {score}% ({killed} killed, {survived} survived)")
    print("Report written to report.md")


def cmd_replay(args: argparse.Namespace) -> None:
    check_prerequisites()
    log = MutationLog.load(args.log_file)
    survived = log.get_survived_from_last_run()
    if not survived:
        print("No survived mutations to replay")
        return

    candidates = []
    for m in survived:
        candidates.append(MutationCandidate(
            file=Path(m["file"]),
            line=m["line"],
            operator_id=m["operator"],
            original=m["original"],
            mutated=m["mutated"],
        ))

    results = _run_mutations(candidates, args.tests, log)
    log.append_run(results)
    log.save()

    score = mutation_score(results)
    killed = sum(1 for r in results if r.status == "KILLED")
    survived_count = sum(1 for r in results if r.status == "SURVIVED")
    print(f"\nReplay Score: {score}% ({killed} killed, {survived_count} survived)")


def _run_mutations(
    candidates: list[MutationCandidate],
    test_app: Path,
    log: MutationLog,
) -> list[MutationResult]:
    results = []
    total = len(candidates)

    for i, candidate in enumerate(candidates, 1):
        mid = log.next_mutation_id()
        print(f"[{i}/{total}] {mid} {candidate.operator_id} @ {candidate.file}:{candidate.line}")

        try:
            apply_mutation(candidate)
        except MutationError:
            results.append(MutationResult(
                id=mid, operator=candidate.operator_id,
                file=str(candidate.file), line=candidate.line,
                original=candidate.original, mutated=candidate.mutated,
                status="OBSOLETE", caught_by=None,
            ))
            log.runs.append({"mutations": [results[-1].to_dict()]})  # for next_mutation_id
            continue

        try:
            if not compile_project():
                results.append(MutationResult(
                    id=mid, operator=candidate.operator_id,
                    file=str(candidate.file), line=candidate.line,
                    original=candidate.original, mutated=candidate.mutated,
                    status="COMPILE_ERROR", caught_by=None,
                ))
                continue

            if not publish_app():
                print(f"  WARNING: publish failed, treating as COMPILE_ERROR")
                results.append(MutationResult(
                    id=mid, operator=candidate.operator_id,
                    file=str(candidate.file), line=candidate.line,
                    original=candidate.original, mutated=candidate.mutated,
                    status="COMPILE_ERROR", caught_by=None,
                ))
                continue

            passed, output = run_tests(test_app)
            if passed:
                print(f"  SURVIVED — tests still pass")
                results.append(MutationResult(
                    id=mid, operator=candidate.operator_id,
                    file=str(candidate.file), line=candidate.line,
                    original=candidate.original, mutated=candidate.mutated,
                    status="SURVIVED", caught_by=None,
                ))
            else:
                results.append(MutationResult(
                    id=mid, operator=candidate.operator_id,
                    file=str(candidate.file), line=candidate.line,
                    original=candidate.original, mutated=candidate.mutated,
                    status="KILLED", caught_by=None,
                ))
        finally:
            try:
                restore_file(candidate.file)
            except MutationError as e:
                print(f"FATAL: Could not restore {candidate.file}: {e}", file=sys.stderr)
                sys.exit(1)

    return results


def main() -> None:
    parser = build_parser()
    args = parser.parse_args()

    commands = {
        "scan": cmd_scan,
        "run": cmd_run,
        "replay": cmd_replay,
    }
    commands[args.command](args)


if __name__ == "__main__":
    main()
