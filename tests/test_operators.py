from pathlib import Path

import pytest

from al_mutate.operators import load_operators, validate_operators, Operator

OPERATORS_DIR = Path(__file__).resolve().parent.parent / "operators"


class TestOperatorDataclass:
    def test_operator_fields(self):
        op = Operator(
            id="rel-gt-to-gte",
            name="Greater-than to greater-or-equal",
            category="relational",
            node_type="comparison_expression",
            operator_token=">",
            replacement=">=",
        )
        assert op.id == "rel-gt-to-gte"
        assert op.node_type == "comparison_expression"
        assert op.operator_token == ">"
        assert op.replacement == ">="

    def test_operator_with_null_replacement(self):
        op = Operator(
            id="stmt-remove-modify",
            name="Remove Modify call",
            category="statement-removal",
            node_type="call_expression",
            identifier="Modify",
            replacement=None,
        )
        assert op.replacement is None

    def test_is_statement_removal(self):
        op = Operator(
            id="stmt-remove-modify",
            name="Remove Modify call",
            category="statement-removal",
            node_type="call_expression",
            identifier="Modify",
            replacement=None,
        )
        assert op.is_statement_removal

        op2 = Operator(
            id="rel-gt-to-gte",
            name="Greater-than",
            category="relational",
            node_type="comparison_expression",
            operator_token=">",
            replacement=">=",
        )
        assert not op2.is_statement_removal

    def test_bc_specific_operator(self):
        op = Operator(
            id="bc-modify-trigger-true",
            name="Modify(true) to Modify(false)",
            category="bc-specific",
            node_type="call_expression",
            identifier="Modify",
            argument_match="true",
            replacement="false",
        )
        assert op.identifier == "Modify"
        assert op.argument_match == "true"


class TestLoadOperators:
    def test_load_default_operators(self):
        ops = load_operators(OPERATORS_DIR / "default.json")
        assert len(ops) > 0
        assert all(isinstance(op, Operator) for op in ops)

    def test_load_nonexistent_file_raises(self):
        with pytest.raises(FileNotFoundError):
            load_operators(Path("/nonexistent/operators.json"))

    def test_load_has_expected_categories(self):
        ops = load_operators(OPERATORS_DIR / "default.json")
        categories = {op.category for op in ops}
        assert "relational" in categories
        assert "arithmetic" in categories
        assert "logical" in categories
        assert "statement-removal" in categories
        assert "bc-specific" in categories

    def test_all_ids_unique(self):
        ops = load_operators(OPERATORS_DIR / "default.json")
        ids = [op.id for op in ops]
        assert len(ids) == len(set(ids))

    def test_all_operators_have_node_type(self):
        ops = load_operators(OPERATORS_DIR / "default.json")
        for op in ops:
            assert op.node_type != ""


class TestValidateOperators:
    def test_validate_valid_operators(self):
        data = {
            "operators": [
                {
                    "id": "test-op",
                    "name": "Test",
                    "category": "relational",
                    "node_type": "comparison_expression",
                    "operator_token": ">",
                    "replacement": ">=",
                }
            ]
        }
        errors = validate_operators(data)
        assert errors == []

    def test_validate_missing_operators_key(self):
        errors = validate_operators({})
        assert len(errors) > 0

    def test_validate_empty_operators_array(self):
        errors = validate_operators({"operators": []})
        assert len(errors) > 0

    def test_validate_missing_required_field(self):
        data = {
            "operators": [
                {
                    "id": "test-op",
                    "name": "Test",
                    "category": "relational",
                    # missing node_type
                }
            ]
        }
        errors = validate_operators(data)
        assert len(errors) > 0

    def test_validate_invalid_category(self):
        data = {
            "operators": [
                {
                    "id": "test-op",
                    "name": "Test",
                    "category": "invalid-category",
                    "node_type": "comparison_expression",
                    "operator_token": ">",
                    "replacement": ">=",
                }
            ]
        }
        errors = validate_operators(data)
        assert len(errors) > 0

    def test_validate_default_json(self):
        import json

        with open(OPERATORS_DIR / "default.json") as f:
            data = json.load(f)
        errors = validate_operators(data)
        assert errors == [], f"default.json validation errors: {errors}"
