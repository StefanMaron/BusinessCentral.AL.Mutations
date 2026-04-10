from unittest.mock import patch, MagicMock
from pathlib import Path

import pytest

from al_mutate.cli import build_parser


class TestParser:
    def test_scan_command(self):
        parser = build_parser()
        args = parser.parse_args(["scan", "./src"])
        assert args.command == "scan"
        assert args.source == Path("./src")

    def test_run_command(self):
        parser = build_parser()
        args = parser.parse_args(["run", "./src", "--tests", "test.app"])
        assert args.command == "run"
        assert args.source == Path("./src")
        assert args.tests == Path("test.app")

    def test_run_with_operators(self):
        parser = build_parser()
        args = parser.parse_args([
            "run", "./src", "--tests", "test.app",
            "--operators", "custom.json",
        ])
        assert args.operators == Path("custom.json")

    def test_run_with_max(self):
        parser = build_parser()
        args = parser.parse_args([
            "run", "./src", "--tests", "test.app", "--max", "20",
        ])
        assert args.max == 20

    def test_replay_command(self):
        parser = build_parser()
        args = parser.parse_args([
            "replay", "mutations.json", "--tests", "test.app",
        ])
        assert args.command == "replay"
        assert args.log_file == Path("mutations.json")
