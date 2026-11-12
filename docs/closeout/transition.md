# 2D to 3D Change Map and Core Change Ratio

- 2D baseline `a067c5f50546b9b8a1ca12479e56eb36c0acfdcd`
- Integrated 3D snapshot `250ae4db120e2a3dd2e636a22e99794f39d8a1bb`
- [GitHub comparison](https://github.com/1723358607-rgb/AP6412-2D-to-3D/compare/2d-baseline...250ae4db120e2a3dd2e636a22e99794f39d8a1bb)

| Status | Classes or files | Effect |
|---|---|---|
| Unchanged | `game/core/*.cs`, `game/data/levels/` | Cells, grid, matches, crate damage, refill, legal moves, outcome and stars use the same code and level data. |
| Unchanged | `IBoardPresenter`, `BoardView2D`, `CrystalGardenBoardLayout` | The 2D presenter still renders the shared steps. The presentation contract supports both renderers. |
| Unchanged | `ProgressStore`, `LevelSelectionScreen`, `GameplayScreen`, `ResultScreen` | Existing saves and screen components support the integrated flow. |
| Modified | `GameSession` | Adds mode configuration and sends mode metadata in snapshots and results. |
| Modified | `AppController`, `GameUiBridge`, `IntegratedUi`, `GameUiRoot`, `HomeScreen` | Wires mode selection, the matching presenter and progress file, and the current edition label. |
| Modified | `BoardView3D`, `project.godot` | Completes the 3D presenter and makes the combined project boot in 3D. |
| Added | `Main3D.tscn`, `GemMeshes`, `assets/3d/` | Adds the 3D entry scene, camera-facing presentation and authored board, crystal and crate resources. |
| Added | `StepSixSmokeCheck` | Exercises rays, real piece scenes, frame steps, UI and per-mode persistence. |
| Deleted | None | The original 2D presentation path remains available. |

[file-changes.tsv](file-changes.tsv) lists the exact added and modified paths from `git diff --name-status 2d-baseline 250ae4d`.

## CCR definition and reproducible result

The denominator is the number of baseline C# files in `game/core/`. A changed file is a baseline file that is modified, removed or renamed; any newly added core files are listed separately. This scope measures engine-independent domain implementation. It includes interaction rules on grid coordinates and excludes Godot scene lifecycle and presentation mode metadata.

`BoardCell.cs`, `BoardGenerator.cs`, `BoardInteraction.cs`, `BoardModel.cs`, `BoardMoves.cs`, `BoardResolution.cs`, `LevelDefinition.cs`, `LevelRules.cs`, and `MatchResolver.cs` were all retained with identical bytes. There were no additions to this directory. **CCR = 0 / 9 = 0%.** The three level configurations were also identical.

With the wider definition that includes the rule orchestration file `GameSession.cs`, the ratio is **1 / 10 = 10%**. That file introduces mode configuration and metadata for routing, while the board transaction, scoring and outcome logic remain shared. The ratio measures source changes; the three-level deterministic replay separately verifies gameplay behaviour.

```sh
git ls-tree -r --name-only 2d-baseline -- game/core
git diff --name-status 2d-baseline 250ae4d -- game/core game/gameplay game/data
```
