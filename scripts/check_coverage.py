#!/usr/bin/env python3
"""Per-file coverage gate for Cobertura reports.

Parses one or more `coverage.cobertura.xml` files, aggregates line coverage per
source file (merging partial classes), and fails if total coverage OR any
single included file falls below the threshold.

Generated code (EF migrations, source-generator output in obj/, and files
flagged generated) is excluded so the gate only measures hand-written code.

Usage:
    python scripts/check_coverage.py <report-or-dir> [--threshold 80]
"""
from __future__ import annotations

import argparse
import glob
import os
import sys
import xml.etree.ElementTree as ET

# Substrings (matched against the normalized filename) that mark generated code.
EXCLUDED_FRAGMENTS = (
    "/migrations/",
    "/obj/",
    ".designer.cs",
    "appdbcontextmodelsnapshot",
    ".generated.cs",
    "/generated/",
)


def is_generated(filename: str) -> bool:
    f = filename.replace("\\", "/").lower()
    return any(frag in f for frag in EXCLUDED_FRAGMENTS)


def find_reports(target: str) -> list[str]:
    if os.path.isdir(target):
        return glob.glob(os.path.join(target, "**", "coverage.cobertura.xml"), recursive=True)
    if os.path.isfile(target):
        return [target]
    return []


def collect(reports: list[str]) -> dict[str, list[int]]:
    """filename -> [covered_lines, total_lines] merged across reports/classes."""
    files: dict[str, list[int]] = {}
    for report in reports:
        root = ET.parse(report).getroot()
        for cls in root.iter("class"):
            filename = cls.get("filename", "")
            if not filename or is_generated(filename):
                continue
            entry = files.setdefault(filename, [0, 0])
            lines = cls.find("lines")
            if lines is None:
                continue
            for line in lines.findall("line"):
                entry[1] += 1
                if int(line.get("hits", "0")) > 0:
                    entry[0] += 1
    return files


def main() -> int:
    parser = argparse.ArgumentParser(description="Per-file Cobertura coverage gate.")
    parser.add_argument("target", help="Cobertura xml file or directory to search.")
    parser.add_argument("--threshold", type=float, default=80.0, help="Minimum %% per file and overall.")
    args = parser.parse_args()

    reports = find_reports(args.target)
    if not reports:
        print(f"ERROR: no coverage.cobertura.xml found under '{args.target}'", file=sys.stderr)
        return 2

    files = collect(reports)
    if not files:
        print("ERROR: no covered source files found in report(s).", file=sys.stderr)
        return 2

    total_c = sum(c for c, _ in files.values())
    total_t = sum(t for _, t in files.values())
    total_pct = (total_c / total_t * 100) if total_t else 100.0

    below = []
    for filename, (c, t) in sorted(files.items()):
        pct = (c / t * 100) if t else 100.0
        if pct < args.threshold:
            below.append((pct, c, t, filename))

    print(f"Coverage: {total_c}/{total_t} lines = {total_pct:.1f}% across {len(files)} files "
          f"(threshold {args.threshold:.0f}%)")

    ok = True
    if below:
        ok = False
        print(f"\n{len(below)} file(s) below {args.threshold:.0f}%:")
        for pct, c, t, filename in sorted(below):
            print(f"  {pct:5.1f}%  {c:4d}/{t:<4d}  {filename}")
    if total_pct < args.threshold:
        ok = False
        print(f"\nTotal coverage {total_pct:.1f}% is below {args.threshold:.0f}%.")

    if ok:
        print("\nPASS: all files meet the threshold.")
        return 0
    print("\nFAIL: coverage gate not met.")
    return 1


if __name__ == "__main__":
    raise SystemExit(main())
