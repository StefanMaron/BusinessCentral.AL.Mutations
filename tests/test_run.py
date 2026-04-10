from unittest.mock import patch, MagicMock
from pathlib import Path

import pytest

from al_mutate.run import compile_project, publish_app, run_tests, check_prerequisites


class TestCheckPrerequisites:
    @patch("al_mutate.run.subprocess.run")
    def test_clean_working_tree(self, mock_run):
        mock_run.return_value = MagicMock(returncode=0, stdout="", stderr="")
        check_prerequisites()  # Should not raise

    @patch("al_mutate.run.subprocess.run")
    def test_dirty_working_tree_raises(self, mock_run):
        mock_run.return_value = MagicMock(
            returncode=0, stdout=" M src/Test.al\n", stderr=""
        )
        with pytest.raises(SystemExit):
            check_prerequisites()


class TestCompile:
    @patch("al_mutate.run.subprocess.run")
    def test_compile_success(self, mock_run):
        mock_run.return_value = MagicMock(returncode=0, stdout="", stderr="")
        assert compile_project() is True

    @patch("al_mutate.run.subprocess.run")
    def test_compile_failure(self, mock_run):
        mock_run.return_value = MagicMock(returncode=1, stdout="", stderr="error")
        assert compile_project() is False


class TestPublish:
    @patch("al_mutate.run.subprocess.run")
    def test_publish_success(self, mock_run):
        mock_run.return_value = MagicMock(returncode=0, stdout="", stderr="")
        assert publish_app() is True

    @patch("al_mutate.run.subprocess.run")
    def test_publish_failure(self, mock_run):
        mock_run.return_value = MagicMock(returncode=1, stdout="", stderr="error")
        assert publish_app() is False


class TestRunTests:
    @patch("al_mutate.run.subprocess.run")
    def test_run_tests_pass(self, mock_run):
        mock_run.return_value = MagicMock(returncode=0, stdout="All tests passed", stderr="")
        passed, output = run_tests(Path("test.app"))
        assert passed is True

    @patch("al_mutate.run.subprocess.run")
    def test_run_tests_fail(self, mock_run):
        mock_run.return_value = MagicMock(
            returncode=1, stdout="TestFoo FAILED", stderr=""
        )
        passed, output = run_tests(Path("test.app"))
        assert passed is False
        assert "FAILED" in output
