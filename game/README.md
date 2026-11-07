# Shared Godot project

The repository's single Godot project uses Godot4.7.2 Mono/.NET, C# and net8.0 (A verified SDK10.0.302). Import this directory's project.godot, build AP6412Match3.sln, then run app/Main.tscn (F5). Build/import the entire repository; do not download project.godot alone.

A's Step3 gameplay includes three formal levels, box damage, target counts, win/loss and integer stars. With no B UI mounted, F5 opens presentation/2d/GameplayLoop.tscn. This scene also runs independently (F6): 1/2/3 test formal levels, R retries, N advances after victory, Esc exits. It does not save progress or simulate production unlocks. The historical BoardPrototype.tscn remains request-only.

B provides ui/GameUiRoot.tscn via the existing GameUiBridge, then progress/ProgressStore.tscn via ProgressStoreBridge. The app discovers them automatically. A's board presenter is reclaimed before UI page replacement and mounted into the next BoardHost. B's result pages and real persistence require integration and joint acceptance.

Final build and step0/1/2/3 automated checks pass. Graphical/manual and real B persistence checks remain pending. Exact sample moves, commands, actual output and limitations: ../docs/evidence/member-a/step-3.md. Read ../docs/alignment/CONTRACT.md before integration.

A owns app/core/gameplay/data/presentation/tests/gameplay and project integration. B owns ui/progress/tests/ui. Existing B visual changes to BoardView2D/CrystalGardenBoardLayout must be preserved when integrating, not overwritten with A's entire version. Build caches (.godot/bin/obj) are ignored; no standalone export or 2D baseline is frozen.
