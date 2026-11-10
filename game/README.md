# Shared Godot project

Godot4.7.2 Mono/.NET, C#, net8.0. Import this project.godot from the complete repository, build AP6412Match3.sln and F5.

The integrated candidate uses B's persistent ProgressStore/ResultScreen and settings UI through A's application bridge. Actual highest stars/unlocks survive restart in per-mode crystal_garden_progress_2d.json /3d.json. Corrupt bytes are backed up before safe recovery; user saves are not reset for tests.

A layouts v2 and rules remain unchanged: seeds641224/641454/641326, box counts5/10/14, budgets15/20/24; example wins3/5/8 swaps. Exact moves: ../docs/evidence/member-a/layout-v2.md. Integration/build/test evidence: integration-20261110.md beside it.

Mac build0 warnings/errors and progress/audit/restart/Step0–4/UI/reference checks pass. Human visual/play acceptance remains; no frozen2D or3D yet. Settings toggles are UI state only; audio/export was not validated.

Store/UI tests require a fresh AP6412_PROGRESS_STORE_DIR for each independent test. Use the same isolated directory for restart-write then restart-read. --integration-audit checks source retention/canonical unlocks/visible result UI. Rule-only --rule-reference writes no saves. Never delete real user:// progress for tests.

Read alignment/HANDOFF_UI and NEXT_3D before working. Preserve B original contributions and A integration corrections separately.
