"""Compile, publish, and run tests via the Linux BC stack."""

from __future__ import annotations

import os
import subprocess
import sys
from pathlib import Path


def check_prerequisites() -> None:
    """Verify the git working tree is clean. Exits if dirty."""
    result = subprocess.run(
        ["git", "status", "--porcelain"],
        capture_output=True,
        text=True,
    )
    if result.stdout.strip():
        print("ERROR: Git working tree is dirty. Commit or stash changes first.", file=sys.stderr)
        print(result.stdout, file=sys.stderr)
        sys.exit(1)


def compile_project() -> bool:
    """Compile the AL project. Returns True on success."""
    result = subprocess.run(
        ["al-compile", "--analyzers", "none"],
        capture_output=True,
        text=True,
    )
    return result.returncode == 0


def publish_app() -> bool:
    """Publish the compiled app to BC. Returns True on success."""
    bc_host = os.environ.get("BC_SERVER", "localhost")
    result = subprocess.run(
        [
            "bc-publish",
            "--port", "7049",
            "--username", "BCRUNNER",
            "--password", "Admin123!",
        ],
        capture_output=True,
        text=True,
    )
    return result.returncode == 0


def run_tests(test_app: Path) -> tuple[bool, str]:
    """Run the test suite. Returns (passed, output)."""
    bc_host = os.environ.get("BC_SERVER", "localhost")
    env = os.environ.copy()
    env["DOTNET_ROLL_FORWARD"] = "LatestMajor"
    result = subprocess.run(
        [
            "/opt/bc-linux/scripts/run-tests.sh",
            "--base-url", f"http://{bc_host}:7048/BC",
            "--dev-url", f"http://{bc_host}:7049/BC/dev",
            "--auth", "BCRUNNER:Admin123!",
            "--app", str(test_app),
        ],
        capture_output=True,
        text=True,
        env=env,
    )
    output = result.stdout + result.stderr
    return result.returncode == 0, output
