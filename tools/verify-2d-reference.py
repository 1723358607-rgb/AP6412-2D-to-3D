#!/usr/bin/env python3
"""Replay current GameSession and compare the complete archived rule reference.

Build C# first. This command neither updates the reference nor touches player saves.
It checks rule/presenter states, not a future 3D renderer or screen-space input.
"""
import argparse
import json
import os
from pathlib import Path
import shutil
import subprocess
import sys


def main():
    root = Path(__file__).resolve().parents[1]
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--godot", default=shutil.which("godot") or
                        "/Applications/Godot_mono.app/Contents/MacOS/Godot")
    parser.add_argument("--log-file", type=Path, help="Optional raw log outside the reference")
    args = parser.parse_args()
    env = dict(os.environ)
    if "DOTNET_ROOT" not in env and Path("/usr/local/share/dotnet").is_dir():
        env["DOTNET_ROOT"] = "/usr/local/share/dotnet"
    process = subprocess.run([args.godot, "--headless", "--path", str(root / "game"),
                              "--fixed-fps", "60", "--", "--rule-reference"],
                             env=env, capture_output=True, text=True, timeout=100)
    output = process.stdout + process.stderr
    if args.log_file:
        args.log_file.write_text(output)
    if process.returncode != 0:
        print(output, file=sys.stderr)
        return process.returncode if process.returncode > 0 else 1
    lines = [line.removeprefix("[RULE_REFERENCE_JSON] ") for line in output.splitlines()
             if line.startswith("[RULE_REFERENCE_JSON] ")]
    if len(lines) != 1 or "[RULE_REFERENCE_PASS]" not in output:
        raise RuntimeError("Game did not emit one complete successful reference")
    current = json.loads(lines[0])
    saved = json.loads((root / "docs/evidence/member-a/step-5-rule-reference.json").read_text())
    if current != saved:
        raise RuntimeError("Rule reference changed: inspect the real diff; do not overwrite the baseline")
    print("REFERENCE_COMPARE_PASS: all 3 openings, 16 swaps, 217 presenter steps, RNG and results match.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
