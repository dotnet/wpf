import json
from pathlib import Path
import sys
import tempfile
import unittest


sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
from triage import TriageError, load_area_map, validate_area_map


class AreaMapTests(unittest.TestCase):
    def test_exact_existing_labels_preserve_case_and_guidance(self):
        mapping = {"area-DataGrid": "DataGrid editing.", "TextBox": "Text entry."}
        validate_area_map(mapping, {"area-DataGrid", "TextBox", "Bug"})
        self.assertEqual(list(mapping), ["area-DataGrid", "TextBox"])

    def test_unknown_or_wrong_case_label_is_rejected(self):
        for name in ("area-NewControl", "area-datagrid", "textbox"):
            with self.subTest(name=name), self.assertRaises(TriageError):
                validate_area_map({name: "Control behavior."}, {"area-DataGrid", "TextBox"})

    def test_invalid_map_shapes_are_rejected(self):
        for mapping in (None, [], {}, {"TextBox": None}, {"TextBox": "  "}, {"TextBox": []}):
            with self.subTest(mapping=mapping), self.assertRaises(TriageError):
                validate_area_map(mapping, {"TextBox"})

    def test_invalid_label_names_are_rejected(self):
        for name in ("", " ", 42):
            with self.subTest(name=name), self.assertRaises(TriageError):
                validate_area_map({name: "Text entry."}, {name})

    def test_loading_validates_against_the_supplied_catalog(self):
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / "area-map.json"
            path.write_text(json.dumps({"TextBox": "Text entry."}), encoding="utf-8")
            self.assertEqual(load_area_map({"TextBox"}, path), {"TextBox": "Text entry."})
            with self.assertRaises(TriageError):
                load_area_map({"area-DataGrid"}, path)

    def test_duplicate_keys_are_rejected(self):
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / "area-map.json"
            path.write_text('{"TextBox": "First.", "TextBox": "Second."}', encoding="utf-8")
            with self.assertRaisesRegex(TriageError, "Duplicate area label"):
                load_area_map({"TextBox"}, path)

    def test_unreadable_or_invalid_json_is_rejected(self):
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / "area-map.json"
            with self.assertRaises(TriageError):
                load_area_map({"TextBox"}, path)
            for content in (b"{", b"\xff"):
                with self.subTest(content=content):
                    path.write_bytes(content)
                    with self.assertRaises(TriageError):
                        load_area_map({"TextBox"}, path)


if __name__ == "__main__":
    unittest.main()
