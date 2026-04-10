"""Load and validate mutation operator JSON definitions."""

from __future__ import annotations

import json
from dataclasses import dataclass
from pathlib import Path

import jsonschema

SCHEMA_PATH = Path(__file__).resolve().parent.parent / "operators" / "schema.json"


@dataclass(frozen=True)
class Operator:
    id: str
    name: str
    category: str
    node_type: str
    operator_token: str | None = None
    identifier: str | None = None
    argument_match: str | None = None
    identifier_replacement: str | None = None
    replacement: str | None = None

    @property
    def is_statement_removal(self) -> bool:
        return self.replacement is None


def load_operators(path: Path) -> list[Operator]:
    """Load operators from a JSON file and return a list of Operator objects."""
    with open(path) as f:
        data = json.load(f)

    errors = validate_operators(data)
    if errors:
        raise ValueError(f"Invalid operator file {path}: {errors}")

    return [
        Operator(
            id=op["id"],
            name=op["name"],
            category=op["category"],
            node_type=op["node_type"],
            operator_token=op.get("operator_token"),
            identifier=op.get("identifier"),
            argument_match=op.get("argument_match"),
            identifier_replacement=op.get("identifier_replacement"),
            replacement=op.get("replacement"),
        )
        for op in data["operators"]
    ]


def validate_operators(data: dict) -> list[str]:
    """Validate operator data against the JSON schema. Returns list of error messages."""
    with open(SCHEMA_PATH) as f:
        schema = json.load(f)

    errors = []
    validator = jsonschema.Draft7Validator(schema)
    for error in validator.iter_errors(data):
        errors.append(error.message)
    return errors
