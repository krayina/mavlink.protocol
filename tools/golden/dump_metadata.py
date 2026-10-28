#!/usr/bin/env python3
"""Dumps wire metadata of every message in MAVLink dialect XMLs, as computed by pymavlink.

The output is the reference ("golden") data for generator tests: message id and name,
CRC_EXTRA, payload lengths, and the fields in wire order with their offsets.

Usage:
    pip install pymavlink
    python dump_metadata.py <xml_dir> <out_dir>

One JSON file per XML file (same stem) is written; files without messages are skipped.
Each XML is parsed on its own, so messages from <include> files are not repeated.
"""
import contextlib
import glob
import io
import json
import os
import sys

from pymavlink import __version__ as pymavlink_version
from pymavlink.generator import mavparse


def describe(message):
    extension_names = {f.name for f in message.fields[message.extensions_start:]} \
        if message.extensions_start is not None else set()
    return {
        "id": message.id,
        "name": message.name,
        "crcExtra": message.crc_extra,
        "baseLength": message.wire_min_length,
        "extendedLength": message.wire_length,
        "fields": [
            {
                "name": f.name,
                "type": f.type,
                "arrayLength": f.array_length,
                "offset": f.wire_offset,
                "extension": f.name in extension_names,
            }
            for f in message.ordered_fields
        ],
    }


def to_json(data):
    """Indented JSON with one line per field, so that diffs stay readable."""
    def compact(value):
        return json.dumps(value, ensure_ascii=False, separators=(", ", ": "))

    lines = ["{"]
    lines.append(f'  "source": {compact(data["source"])},')
    lines.append(f'  "generator": {compact(data["generator"])},')
    lines.append('  "messages": [')
    for i, message in enumerate(data["messages"]):
        head = {k: v for k, v in message.items() if k != "fields"}
        lines.append("    {")
        for key, value in head.items():
            lines.append(f"      {compact(key)}: {compact(value)},")
        lines.append('      "fields": [')
        fields = message["fields"]
        for j, field in enumerate(fields):
            lines.append("        " + compact(field) + ("," if j < len(fields) - 1 else ""))
        lines.append("      ]")
        lines.append("    }" + ("," if i < len(data["messages"]) - 1 else ""))
    lines.append("  ]")
    lines.append("}")
    return "\n".join(lines) + "\n"


def main(xml_dir, out_dir):
    os.makedirs(out_dir, exist_ok=True)
    for path in sorted(glob.glob(os.path.join(xml_dir, "*.xml"))):
        # mavparse prints schema warnings (e.g. non-power-of-two bitmask entries) to stdout.
        with contextlib.redirect_stdout(io.StringIO()):
            xml = mavparse.MAVXML(path, wire_protocol_version=mavparse.PROTOCOL_2_0)
        if not xml.message:
            continue
        stem = os.path.splitext(os.path.basename(path))[0]
        data = {
            "source": os.path.basename(path),
            "generator": "pymavlink " + pymavlink_version,
            "messages": [describe(m) for m in sorted(xml.message, key=lambda m: m.id)],
        }
        out_path = os.path.join(out_dir, stem + ".json")
        with open(out_path, "w", encoding="utf-8", newline="\n") as f:
            f.write(to_json(data))
        print(f"{out_path}: {len(data['messages'])} messages")


if __name__ == "__main__":
    if len(sys.argv) != 3:
        sys.exit(__doc__)
    main(sys.argv[1], sys.argv[2])
