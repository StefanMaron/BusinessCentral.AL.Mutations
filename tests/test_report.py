from pathlib import Path

import pytest

from al_mutate.log import MutationResult
from al_mutate.report import generate_markdown, mutation_score


@pytest.fixture
def sample_results():
    return [
        MutationResult("M001", "rel-gt-to-gte", "src/Test.al", 5,
                        "if x > 0", "if x >= 0", "KILLED", "TestFoo"),
        MutationResult("M002", "rel-lt-to-lte", "src/Test.al", 10,
                        "if x < 0", "if x <= 0", "SURVIVED", None),
        MutationResult("M003", "arith-add-to-sub", "src/Test.al", 15,
                        "x + 1", "x - 1", "KILLED", "TestBar"),
        MutationResult("M004", "stmt-remove-modify", "src/Test.al", 20,
                        "Rec.Modify(true);", "// Rec.Modify(true);", "COMPILE_ERROR", None),
    ]


class TestMutationScore:
    def test_basic_score(self, sample_results):
        score = mutation_score(sample_results)
        # 2 killed out of 3 testable (COMPILE_ERROR excluded)
        assert score == pytest.approx(66.67, abs=0.01)

    def test_all_killed(self):
        results = [
            MutationResult("M001", "x", "f", 1, "a", "b", "KILLED", "T"),
            MutationResult("M002", "x", "f", 2, "a", "b", "KILLED", "T"),
        ]
        assert mutation_score(results) == 100.0

    def test_none_killed(self):
        results = [
            MutationResult("M001", "x", "f", 1, "a", "b", "SURVIVED", None),
        ]
        assert mutation_score(results) == 0.0

    def test_empty_results(self):
        assert mutation_score([]) == 100.0

    def test_only_compile_errors(self):
        results = [
            MutationResult("M001", "x", "f", 1, "a", "b", "COMPILE_ERROR", None),
        ]
        assert mutation_score(results) == 100.0


class TestGenerateMarkdown:
    def test_contains_score(self, sample_results):
        md = generate_markdown(sample_results, "./src")
        assert "66.67%" in md

    def test_contains_survived_section(self, sample_results):
        md = generate_markdown(sample_results, "./src")
        assert "Survived" in md
        assert "M002" in md

    def test_contains_summary(self, sample_results):
        md = generate_markdown(sample_results, "./src")
        assert "Killed" in md
        assert "Survived" in md
