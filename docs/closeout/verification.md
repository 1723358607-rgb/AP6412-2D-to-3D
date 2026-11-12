# Verification at the Submission Snapshots

Both projects were extracted from the recorded Git commits, restored and built with .NET SDK 10.0.302, then imported and run on macOS arm64 with Godot 4.7.2 Mono. All runtime checks used isolated progress files.

| Snapshot | Check | Result |
|---|---|---|
| 2D `a067c5f` | Build and Step 4 smoke check | 0 errors and 0 warnings. Verified 300 openings against an independent move oracle, bounded dead-board recovery, UI flow, all three levels, failure and retry. |
| 3D `250ae4d` | Build and Step 6 smoke check | 0 errors and 0 warnings. Verified 49 ray targets after resize, authored board and six piece scenes, crate damage, 16 swaps and 193 presenter steps against the 2D reference, loss and retry. |
| 3D `250ae4d` | Restart read | Restored 3D stars and unlocks in a second process while preserving separate 2D progress. |
| Combined `250ae4d` | Mode switch probe | Activated both Home mode buttons, entered level 1 with the appropriate presenter, returned to Home and switched again. The level 3 unlock followed the selected mode. |
| Combined `250ae4d` | Graphical mode navigation | Member B reports a pass in [PR #15](https://github.com/1723358607-rgb/AP6412-2D-to-3D/pull/15). The closeout runs were headless. |

The runtime results cover interaction, state and rendering inputs; graphical composition and feel should be described using the members' actual play observations. Settings controls are UI state. Standalone exports and audio playback were outside these checks.

For a repeatable level 1 win, start a fresh level and exchange `(4,3) ↔ (5,3)`, `(6,4) ↔ (7,4)`, then `(6,4) ↔ (6,5)`, waiting for each cascade. Coordinates are one-based (row, column). Expect 12 moves left and 3 stars in both versions. The complete [layout v2 verification](../evidence/member-a/layout-v2.md) includes all three paths.
