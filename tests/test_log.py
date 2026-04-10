import json
from pathlib import Path

import pytest

from al_mutate.log import MutationLog, MutationResult


class TestMutationResult:
    def test_create_result(self):
        r = MutationResult(
            id="M001",
            operator="rel-gt-to-gte",
            file="src/Test.al",
            line=5,
            original="if Amount > 0 then",
            mutated="if Amount >= 0 then",
            status="KILLED",
            caught_by="TestAmount",
        )
        assert r.status == "KILLED"

    def test_to_dict(self):
        r = MutationResult(
            id="M001",
            operator="rel-gt-to-gte",
            file="src/Test.al",
            line=5,
            original="if Amount > 0 then",
            mutated="if Amount >= 0 then",
            status="SURVIVED",
            caught_by=None,
        )
        d = r.to_dict()
        assert d["id"] == "M001"
        assert d["caught_by"] is None


class TestMutationLog:
    def test_create_empty_log(self, tmp_path):
        log_path = tmp_path / "mutations.json"
        log = MutationLog(log_path, project="./src")
        assert log.runs == []

    def test_load_existing_log(self, tmp_path):
        log_path = tmp_path / "mutations.json"
        data = {
            "schema_version": 1,
            "project": "./src",
            "runs": [
                {
                    "run": 1,
                    "date": "2026-04-10T09:00:00",
                    "mutations": [
                        {
                            "id": "M001",
                            "operator": "rel-gt-to-gte",
                            "file": "src/Test.al",
                            "line": 5,
                            "original": "if Amount > 0 then",
                            "mutated": "if Amount >= 0 then",
                            "status": "KILLED",
                            "caught_by": "TestAmount",
                        }
                    ],
                }
            ],
        }
        log_path.write_text(json.dumps(data))
        log = MutationLog.load(log_path)
        assert len(log.runs) == 1
        assert log.runs[0]["mutations"][0]["status"] == "KILLED"

    def test_append_run(self, tmp_path):
        log_path = tmp_path / "mutations.json"
        log = MutationLog(log_path, project="./src")
        results = [
            MutationResult(
                id="M001",
                operator="rel-gt-to-gte",
                file="src/Test.al",
                line=5,
                original="if Amount > 0 then",
                mutated="if Amount >= 0 then",
                status="KILLED",
                caught_by="TestAmount",
            ),
        ]
        log.append_run(results)
        assert len(log.runs) == 1
        assert log.runs[0]["run"] == 1

    def test_save_and_reload(self, tmp_path):
        log_path = tmp_path / "mutations.json"
        log = MutationLog(log_path, project="./src")
        results = [
            MutationResult(
                id="M001",
                operator="rel-gt-to-gte",
                file="src/Test.al",
                line=5,
                original="if Amount > 0 then",
                mutated="if Amount >= 0 then",
                status="SURVIVED",
                caught_by=None,
            ),
        ]
        log.append_run(results)
        log.save()

        reloaded = MutationLog.load(log_path)
        assert len(reloaded.runs) == 1
        assert reloaded.runs[0]["mutations"][0]["status"] == "SURVIVED"

    def test_get_survived_from_last_run(self, tmp_path):
        log_path = tmp_path / "mutations.json"
        log = MutationLog(log_path, project="./src")
        results = [
            MutationResult(
                id="M001", operator="rel-gt-to-gte", file="src/Test.al",
                line=5, original="x", mutated="y", status="KILLED", caught_by="T",
            ),
            MutationResult(
                id="M002", operator="rel-lt-to-lte", file="src/Test.al",
                line=10, original="a", mutated="b", status="SURVIVED", caught_by=None,
            ),
        ]
        log.append_run(results)
        survived = log.get_survived_from_last_run()
        assert len(survived) == 1
        assert survived[0]["id"] == "M002"

    def test_next_mutation_id(self, tmp_path):
        log_path = tmp_path / "mutations.json"
        log = MutationLog(log_path, project="./src")
        assert log.next_mutation_id() == "M001"

        results = [
            MutationResult(
                id="M001", operator="x", file="f", line=1,
                original="a", mutated="b", status="KILLED", caught_by="T",
            ),
        ]
        log.append_run(results)
        assert log.next_mutation_id() == "M002"

    def test_multiple_runs_increment(self, tmp_path):
        log_path = tmp_path / "mutations.json"
        log = MutationLog(log_path, project="./src")
        log.append_run([
            MutationResult("M001", "x", "f", 1, "a", "b", "KILLED", "T"),
        ])
        log.append_run([
            MutationResult("M002", "x", "f", 2, "c", "d", "SURVIVED", None),
        ])
        assert len(log.runs) == 2
        assert log.runs[1]["run"] == 2
