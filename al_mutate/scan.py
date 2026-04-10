"""Scan AL source files for mutation candidates using tree-sitter AST."""

from __future__ import annotations

from dataclasses import dataclass
from pathlib import Path

import tree_sitter_al
from tree_sitter import Language, Parser

from al_mutate.operators import Operator

AL_LANGUAGE = Language(tree_sitter_al.language())


@dataclass(frozen=True)
class MutationCandidate:
    file: Path
    line: int
    operator_id: str
    original: str
    mutated: str


def _make_parser() -> Parser:
    return Parser(AL_LANGUAGE)


def _get_call_identifier(node) -> str | None:
    """Extract the method name from a call_expression node.

    Handles both simple calls (Error(...)) and member calls (Rec.Modify(...)).
    """
    for child in node.children:
        if child.type == "identifier":
            return child.text.decode()
        if child.type == "member_expression":
            # Last identifier in member_expression is the method name
            for mc in reversed(child.children):
                if mc.type == "identifier":
                    return mc.text.decode()
    return None


def _get_argument_text(node) -> list[str]:
    """Get the text of each argument in a call_expression's argument_list."""
    for child in node.children:
        if child.type == "argument_list":
            return [
                c.text.decode()
                for c in child.children
                if c.type not in ("(", ")", ",")
            ]
    return []


def _get_operator_child(node, op: Operator):
    """Find the operator token child node within an expression node."""
    if op.node_type == "comparison_expression":
        for child in node.children:
            if child.type == "comparison_operator":
                if child.text.decode() == op.operator_token:
                    return child
    elif op.node_type in ("additive_expression", "multiplicative_expression", "logical_expression"):
        for child in node.children:
            if child.text.decode() == op.operator_token and child.type not in (
                "identifier", "integer", "decimal", "string", "boolean",
                "comparison_expression", "additive_expression",
                "multiplicative_expression", "logical_expression",
                "unary_expression", "parenthesized_expression",
                "call_expression", "member_expression",
                "argument_list",
            ):
                return child
    return None


def _build_mutated_line(source_lines: list[bytes], node, op: Operator) -> str | None:
    """Build the mutated line text for a given node and operator.

    Returns the full mutated line, or None if the operator doesn't apply.
    """
    line_idx = node.start_point[0]
    original_line = source_lines[line_idx].decode()

    if op.node_type == "call_expression":
        ident = _get_call_identifier(node)
        if ident != op.identifier:
            return None

        if op.argument_match is not None:
            # BC-specific: match a specific argument value and replace it
            args = _get_argument_text(node)
            if op.argument_match not in args:
                return None
            # Find the argument node and replace its text
            for child in node.children:
                if child.type == "argument_list":
                    for arg_node in child.children:
                        if arg_node.text.decode() == op.argument_match:
                            col_start = arg_node.start_point[1]
                            col_end = arg_node.end_point[1]
                            return (
                                original_line[:col_start]
                                + op.replacement
                                + original_line[col_end:]
                            )
            return None

        if op.is_statement_removal:
            return "// " + original_line.lstrip()

        return None

    # Expression operators: replace the operator token
    op_child = _get_operator_child(node, op)
    if op_child is None:
        return None

    col_start = op_child.start_point[1]
    col_end = op_child.end_point[1]
    return (
        original_line[:col_start]
        + op.replacement
        + original_line[col_end:]
    )


def scan_file(path: Path, operators: list[Operator]) -> list[MutationCandidate]:
    """Scan a single AL file for mutation candidates using tree-sitter."""
    source = path.read_bytes()
    parser = _make_parser()
    tree = parser.parse(source)
    source_lines = source.split(b"\n")

    candidates = []
    target_types = {op.node_type for op in operators}

    def visit(node):
        if node.type in target_types:
            line_idx = node.start_point[0]
            original_line = source_lines[line_idx].decode()

            for op in operators:
                if op.node_type != node.type:
                    continue

                mutated = _build_mutated_line(source_lines, node, op)
                if mutated is not None and mutated != original_line:
                    candidates.append(
                        MutationCandidate(
                            file=path,
                            line=line_idx + 1,
                            operator_id=op.id,
                            original=original_line,
                            mutated=mutated,
                        )
                    )

        for child in node.children:
            visit(child)

    visit(tree.root_node)
    return candidates


def scan_directory(directory: Path, operators: list[Operator]) -> list[MutationCandidate]:
    """Scan all .al files in a directory recursively."""
    candidates = []
    for al_file in sorted(directory.rglob("*.al")):
        candidates.extend(scan_file(al_file, operators))
    return candidates
