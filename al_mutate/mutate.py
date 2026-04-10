"""Apply and restore mutations on AL source files."""

from __future__ import annotations

import subprocess
from pathlib import Path

from al_mutate.scan import MutationCandidate


class MutationError(Exception):
    pass


def apply_mutation(candidate: MutationCandidate) -> None:
    """Apply a mutation by replacing the original line in the file."""
    lines = candidate.file.read_text().splitlines(keepends=True)
    line_idx = candidate.line - 1

    if line_idx >= len(lines):
        raise MutationError(
            f"Line {candidate.line} out of range in {candidate.file}"
        )

    current_line = lines[line_idx].rstrip("\n").rstrip("\r")
    if current_line != candidate.original:
        raise MutationError(
            f"Line {candidate.line} in {candidate.file} does not match expected content.\n"
            f"  Expected: {candidate.original!r}\n"
            f"  Got:      {current_line!r}"
        )

    # Preserve the original line ending
    ending = lines[line_idx][len(current_line):]
    lines[line_idx] = candidate.mutated + ending
    candidate.file.write_text("".join(lines))


def restore_file(path: Path) -> None:
    """Restore a file to its git HEAD state."""
    result = subprocess.run(
        ["git", "checkout", "--", str(path.name)],
        cwd=path.parent,
        capture_output=True,
        text=True,
    )
    if result.returncode != 0:
        raise MutationError(
            f"Failed to restore {path} via git: {result.stderr.strip()}"
        )
