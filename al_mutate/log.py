"""Read/write the mutations.json log file."""

from __future__ import annotations

import json
from dataclasses import dataclass, asdict
from datetime import datetime
from pathlib import Path


@dataclass
class MutationResult:
    id: str
    operator: str
    file: str
    line: int
    original: str
    mutated: str
    status: str  # KILLED | SURVIVED | COMPILE_ERROR | OBSOLETE
    caught_by: str | None

    def to_dict(self) -> dict:
        return asdict(self)


class MutationLog:
    def __init__(self, path: Path, project: str):
        self.path = path
        self.project = project
        self.runs: list[dict] = []

    @classmethod
    def load(cls, path: Path) -> MutationLog:
        with open(path) as f:
            data = json.load(f)
        log = cls(path, project=data["project"])
        log.runs = data.get("runs", [])
        return log

    def append_run(self, results: list[MutationResult]) -> None:
        run_number = len(self.runs) + 1
        self.runs.append({
            "run": run_number,
            "date": datetime.now().isoformat(timespec="seconds"),
            "mutations": [r.to_dict() for r in results],
        })

    def save(self) -> None:
        data = {
            "schema_version": 1,
            "project": self.project,
            "runs": self.runs,
        }
        self.path.write_text(json.dumps(data, indent=2) + "\n")

    def get_survived_from_last_run(self) -> list[dict]:
        if not self.runs:
            return []
        return [
            m for m in self.runs[-1]["mutations"]
            if m["status"] == "SURVIVED"
        ]

    def next_mutation_id(self) -> str:
        max_num = 0
        for run in self.runs:
            for m in run["mutations"]:
                num = int(m["id"][1:])
                if num > max_num:
                    max_num = num
        return f"M{max_num + 1:03d}"
