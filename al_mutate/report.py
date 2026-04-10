"""Generate mutation testing reports."""

from __future__ import annotations

from al_mutate.log import MutationResult


def mutation_score(results: list[MutationResult]) -> float:
    """Calculate mutation score as percentage of killed vs testable mutations.

    COMPILE_ERROR and OBSOLETE mutations are excluded from the calculation.
    """
    testable = [r for r in results if r.status in ("KILLED", "SURVIVED")]
    if not testable:
        return 100.0
    killed = sum(1 for r in testable if r.status == "KILLED")
    return round(killed / len(testable) * 100, 2)


def generate_markdown(results: list[MutationResult], project: str) -> str:
    """Generate a Markdown mutation testing report."""
    score = mutation_score(results)
    killed = [r for r in results if r.status == "KILLED"]
    survived = [r for r in results if r.status == "SURVIVED"]
    compile_errors = [r for r in results if r.status == "COMPILE_ERROR"]
    obsolete = [r for r in results if r.status == "OBSOLETE"]

    lines = [
        f"# Mutation Testing Report",
        f"",
        f"**Project:** {project}",
        f"**Mutation Score:** {score}%",
        f"",
        f"## Summary",
        f"",
        f"| Status | Count |",
        f"|--------|-------|",
        f"| Killed | {len(killed)} |",
        f"| Survived | {len(survived)} |",
        f"| Compile Error | {len(compile_errors)} |",
        f"| Obsolete | {len(obsolete)} |",
        f"| **Total** | **{len(results)}** |",
        f"",
    ]

    if survived:
        lines.extend([
            "## Survived Mutations",
            "",
            "These mutations were not caught by your tests:",
            "",
        ])
        for r in survived:
            lines.extend([
                f"### {r.id} — {r.operator}",
                f"",
                f"**File:** {r.file}:{r.line}",
                f"```",
                f"- {r.original}",
                f"+ {r.mutated}",
                f"```",
                f"",
            ])

    return "\n".join(lines)
