# Shared Godot project

Single project: Godot4.7.2 Mono/.NET, C#, net8.0. Download/clone the whole repository, import project.godot, build AP6412Match3.sln, F5 opens app/Main.tscn and the actual Crystal Garden UI connected to three-level gameplay.

B's pages/assets are preserved. A's app/integration adapter supplies real session-only results/stars/unlocks while B's ProgressStore is absent; progress resets on closing. B must deliver the formal store together with real original UI progress/result bindings. No frozen 2D tag or standalone export exists.

Final build has0 warnings/errors; Step0–4 and B UI demo checks pass. Real X11 software rendering/input captured all four pages. Editor import has a GodotTools/MSBuild exception, audio uses Dummy; Mac and teammate manual checks are pending. See ../docs/evidence/member-a/step-4.md for logs, screenshots, exact replay and limitations.

A development harness presentation/2d/GameplayLoop.tscn remains independently runnable with F6 (1/2/3, R, N, Esc). It saves nothing. BoardPrototype is historical request-only.

Read ../docs/alignment/HANDOFF_UI.md and CONTRACT.md before editing. A owns app/core/gameplay/data/presentation/tests/gameplay and integration; B owns ui/progress/tests/ui. Preserve both contributions in BoardView2D and B's CrystalGardenBoardLayout. Build caches are ignored.

Step5 A evidence is ready, not frozen: ../docs/evidence/member-a/step-5.md. After building, --rule-reference records actual session/presenter states; tools/verify-2d-reference.py from repository root compares with the saved provisional archive. It needs the complete repository, writes no player saves, and does not validate future 3D rendering. Two-person manual checklist and B persistence remain pending.
