# Shared Godot project: isolated3D preview branch

On codex/member-a-3d, reopen project.godot with Godot4.7.2 Mono, build C#, F5 starts app/Main3D.tscn. The menu/selection/HUD/result remain shared. Actual meshes/camera/ray picking/animations use the same core and configs;2D/3D best scores persist separately. First3D entry does not inherit2D unlocks.

To play2D here, open app/Main.tscn and F6. Main branch retains the original2D candidate at a067c5f. Human2D acceptance remains unreported; no fake2d-baseline or3d-final tags.

See ../docs/evidence/member-a/step-6.md for current model descriptions, exact3/5/8-step paths, actual verification and unverified graphical quality. Mesh/input/equivalence/lifetime/loss/restart tests and old2D regressions pass; C# build0 warnings/errors. No new real screenshot/audio/export/human-play claim.

--3d-smoke-test and --3d-smoke-test --3d-restart-read need the same isolated directory for the two processes. Old2D checks must explicitly select res://app/Main.tscn before --. Never reset real player saves for diagnostics.
