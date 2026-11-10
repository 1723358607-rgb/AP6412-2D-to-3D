# Shared Godot project

Godot4.7.2 Mono/.NET, C#, net8.0. Import the complete repository's project.godot, build AP6412Match3.sln, F5 starts real Crystal Garden gameplay.

Current distributed-box layouts v2 use upper/middle/lower targets, counts5/10/14, three double-layer boxes and unchanged budgets15/20/24. Seeds641224/641454/641326 have verified example wins in3/5/8 swaps. B's `ProgressStore` permanently saves per-mode best stars, completions and unlocks; B's `ResultScreen` supplies the formal Crystal Garden result presentation. A's app/integration adapter forwards the shared navigation/gameplay bridge without recalculating results. No frozen 2D tag or standalone export exists.

See ../docs/evidence/member-a/layout-v2.md for exact moves, diagram, diagnostics and real checks. Current rule archive is selected by current-rule-reference.json; older Step4/5 records remain historical. The merged PR #10 candidate must pass the complete automated and manual matrix before freezing; no frozen2D tag or3D implementation exists.

Use tools/verify-2d-reference.py after building. --layout-study runs distribution/direct-reward diagnostics; --layout-study --layout-seed-scan is test-only candidate scanning. The independent GameplayLoop scene remains available with F6; it does not save progress.

Read ../docs/alignment/HANDOFF_UI.md and CONTRACT.md before editing. A owns app/core/gameplay/data/presentation/tests/gameplay and integration; B owns ui/progress/tests/ui. Preserve both contributions in BoardView2D and B's CrystalGardenBoardLayout. Build caches are ignored.

After building, --rule-reference records actual session/presenter states; tools/verify-2d-reference.py from repository root compares with the selected archive. It writes no player saves and does not validate future 3D rendering. PR review, remaining two-person acceptance and final evidence are still pending.
