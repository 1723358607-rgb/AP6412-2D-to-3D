# Shared Godot project

Godot4.7.2 Mono/.NET, C#, net8.0. Import the complete repository's project.godot, build AP6412Match3.sln, F5 starts real Crystal Garden gameplay.

Current distributed-box layouts v2: upper/middle/lower targets, counts5/10/14, three double-layer boxes, budgets15/20/24 unchanged. Seeds641224/641454/641326; example wins3/5/8 swaps. See ../docs/evidence/member-a/layout-v2.md for exact moves, diagram, diagnostics and real checks. Current rule archive is selected by current-rule-reference.json; older Step4/5 records remain historical.

Build and Step0–4/UI-demo checks pass; 16 swaps/193 presenter steps replay identically and compare to the new archive. Native Mac graphical launch returned-6; new manual visuals/audio/export are unverified. B PR #10 is open, not part of this layout integration; current main progress is still session-only. No frozen2D tag or3D implementation.

Use tools/verify-2d-reference.py after building. --layout-study runs distribution/direct-reward diagnostics; --layout-study --layout-seed-scan is test-only candidate scanning. The independent GameplayLoop scene remains available with F6; it does not save progress.

A owns core/gameplay/data/presentation/app integration/tests/gameplay; B owns ui/progress/tests/ui. Preserve both contributions and actual original history. Read alignment/HANDOFF_UI.md before integration.
