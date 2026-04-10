import subprocess
from pathlib import Path
from unittest.mock import patch

import pytest

from al_mutate.mutate import apply_mutation, restore_file, MutationError
from al_mutate.scan import MutationCandidate


@pytest.fixture
def tmp_al_file(tmp_path):
    """Create a temporary AL file for mutation testing."""
    content = """\
codeunit 50100 "Test"
{
    procedure Check(Amount: Decimal)
    begin
        if Amount > 0 then
            Message('Positive');
    end;
}
"""
    path = tmp_path / "Test.al"
    path.write_text(content)
    return path


@pytest.fixture
def candidate(tmp_al_file):
    return MutationCandidate(
        file=tmp_al_file,
        line=5,
        operator_id="rel-gt-to-gte",
        original="        if Amount > 0 then",
        mutated="        if Amount >= 0 then",
    )


class TestApplyMutation:
    def test_apply_replaces_line(self, tmp_al_file, candidate):
        apply_mutation(candidate)
        content = tmp_al_file.read_text()
        assert "Amount >= 0" in content
        assert "Amount > 0" not in content

    def test_apply_only_changes_target_line(self, tmp_al_file, candidate):
        original_lines = tmp_al_file.read_text().splitlines()
        apply_mutation(candidate)
        mutated_lines = tmp_al_file.read_text().splitlines()

        for i, (orig, mut) in enumerate(zip(original_lines, mutated_lines)):
            if i == 4:  # line 5 (0-indexed)
                assert orig != mut
            else:
                assert orig == mut

    def test_apply_statement_removal(self, tmp_al_file):
        candidate = MutationCandidate(
            file=tmp_al_file,
            line=6,
            operator_id="stmt-remove-message",
            original="            Message('Positive');",
            mutated="// Message('Positive');",
        )
        apply_mutation(candidate)
        lines = tmp_al_file.read_text().splitlines()
        assert lines[5].strip().startswith("//")

    def test_apply_raises_if_original_not_found(self, tmp_al_file):
        candidate = MutationCandidate(
            file=tmp_al_file,
            line=5,
            operator_id="rel-gt-to-gte",
            original="        if Amount > 999 then",
            mutated="        if Amount >= 999 then",
        )
        with pytest.raises(MutationError):
            apply_mutation(candidate)


class TestRestoreFile:
    def test_restore_via_git(self, tmp_al_file, candidate):
        original = tmp_al_file.read_text()
        # Initialize a git repo so restore works
        subprocess.run(["git", "init"], cwd=tmp_al_file.parent, capture_output=True)
        subprocess.run(["git", "add", "."], cwd=tmp_al_file.parent, capture_output=True)
        subprocess.run(
            ["git", "commit", "-m", "init"],
            cwd=tmp_al_file.parent,
            capture_output=True,
            env={"GIT_AUTHOR_NAME": "test", "GIT_AUTHOR_EMAIL": "t@t",
                 "GIT_COMMITTER_NAME": "test", "GIT_COMMITTER_EMAIL": "t@t",
                 "PATH": "/usr/bin:/bin"},
        )

        apply_mutation(candidate)
        assert "Amount >= 0" in tmp_al_file.read_text()

        restore_file(tmp_al_file)
        assert tmp_al_file.read_text() == original

    def test_restore_raises_on_failure(self, tmp_al_file):
        # No git repo — restore should fail
        with pytest.raises(MutationError):
            restore_file(tmp_al_file)
