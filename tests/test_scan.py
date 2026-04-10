from pathlib import Path

import pytest

from al_mutate.operators import load_operators
from al_mutate.scan import scan_file, scan_directory, MutationCandidate

FIXTURES = Path(__file__).resolve().parent / "fixtures"
OPERATORS_DIR = Path(__file__).resolve().parent.parent / "operators"


@pytest.fixture
def operators():
    return load_operators(OPERATORS_DIR / "default.json")


class TestScanFile:
    def test_finds_mutations_in_sample(self, operators):
        candidates = scan_file(FIXTURES / "sample.al", operators)
        assert len(candidates) > 0
        assert all(isinstance(c, MutationCandidate) for c in candidates)

    def test_candidate_has_required_fields(self, operators):
        candidates = scan_file(FIXTURES / "sample.al", operators)
        c = candidates[0]
        assert c.file is not None
        assert c.line > 0
        assert c.operator_id != ""
        assert c.original != ""
        assert c.mutated != ""

    def test_finds_relational_operators(self, operators):
        candidates = scan_file(FIXTURES / "sample.al", operators)
        rel_ops = [c for c in candidates if c.operator_id.startswith("rel-")]
        assert len(rel_ops) > 0

    def test_skips_comments(self, operators):
        """Line comments are not parsed as expression nodes — no mutations."""
        candidates = scan_file(FIXTURES / "sample.al", operators)
        # Line 10 has "// Amount < 0" — tree-sitter parses this as a comment node
        comment_mutations = [c for c in candidates if c.line == 10]
        assert len(comment_mutations) == 0

    def test_skips_block_comments(self, operators):
        """Block comments are not parsed as expression nodes — no mutations."""
        candidates = scan_file(FIXTURES / "sample.al", operators)
        # Lines 26-27 are a block comment
        block_comment_mutations = [c for c in candidates if c.line in (26, 27)]
        assert len(block_comment_mutations) == 0

    def test_skips_string_literals(self, operators):
        """Operators inside strings are not matched — only the call itself is targeted."""
        candidates = scan_file(FIXTURES / "sample.al", operators)
        # No relational/arithmetic mutation should come from inside a string
        expr_mutations = [
            c for c in candidates
            if c.operator_id.startswith(("rel-", "arith-", "logic-"))
        ]
        for c in expr_mutations:
            assert "Credit limit" not in c.original

    def test_finds_comparison_gt(self, operators):
        """Should find > on line 5: if Amount > 0 then."""
        candidates = scan_file(FIXTURES / "sample.al", operators)
        gt = [c for c in candidates if c.operator_id == "rel-gt-to-gte" and c.line == 5]
        assert len(gt) == 1
        assert ">=" in gt[0].mutated

    def test_finds_comparison_gte(self, operators):
        """Should find >= on line 6: if Amount >= CreditLimit then."""
        candidates = scan_file(FIXTURES / "sample.al", operators)
        gte = [c for c in candidates if c.operator_id == "rel-gte-to-gt" and c.line == 6]
        assert len(gte) == 1

    def test_finds_comparison_neq(self, operators):
        """Should find <> on line 31: if Balance <> 0 then."""
        candidates = scan_file(FIXTURES / "sample.al", operators)
        neq = [c for c in candidates if c.operator_id == "rel-neq-to-eq" and c.line == 31]
        assert len(neq) == 1

    def test_finds_additive(self, operators):
        """Should find + on line 14: TotalAmount + Amount."""
        candidates = scan_file(FIXTURES / "sample.al", operators)
        add = [c for c in candidates if c.operator_id == "arith-add-to-sub" and c.line == 14]
        assert len(add) == 1
        assert "-" in add[0].mutated

    def test_finds_multiplicative(self, operators):
        """Should find * operators in CalculateDiscount."""
        candidates = scan_file(FIXTURES / "sample.al", operators)
        mul = [c for c in candidates if c.operator_id == "arith-mul-to-div"]
        assert len(mul) > 0

    def test_finds_logical_and(self, operators):
        """Should find 'and' on line 20."""
        candidates = scan_file(FIXTURES / "sample.al", operators)
        logic = [c for c in candidates if c.operator_id == "logic-and-to-or"]
        assert len(logic) == 1
        assert "or" in logic[0].mutated

    def test_finds_statement_removal(self, operators):
        """Should find Modify and Insert calls for removal."""
        candidates = scan_file(FIXTURES / "sample.al", operators)
        removals = [c for c in candidates if c.operator_id.startswith("stmt-remove-")]
        assert len(removals) > 0
        for r in removals:
            assert r.mutated.strip().startswith("//")

    def test_finds_modify_true(self, operators):
        """Should find Modify(true) for bc-specific mutation."""
        candidates = scan_file(FIXTURES / "sample.al", operators)
        modify_true = [c for c in candidates if c.operator_id == "bc-modify-trigger-true"]
        assert len(modify_true) == 1
        assert "false" in modify_true[0].mutated

    def test_no_mutations_in_properties(self, operators):
        """Should not mutate object properties, permission sets, or attributes."""
        # Create a file with only non-executable AL constructs
        no_exec = FIXTURES / "NoMatches.al"
        candidates = scan_file(no_exec, operators)
        assert len(candidates) == 0


class TestScanDirectory:
    def test_scan_fixtures_directory(self, operators):
        candidates = scan_directory(FIXTURES, operators)
        assert len(candidates) > 0

    def test_only_scans_al_files(self, operators):
        candidates = scan_directory(FIXTURES.parent, operators)
        for c in candidates:
            assert str(c.file).endswith(".al")
