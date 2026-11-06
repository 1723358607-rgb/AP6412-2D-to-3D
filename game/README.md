# Shared Godot project

This is the repository's single Godot project for AP6412 Route B. It provides
the C# shared entry/contracts and Step 1 independent board prototype. Actual
match resolution and B's formal UI pages are not implemented by A here.

## Required environment

- Godot `4.7.2.stable.mono` (.NET build)
- A .NET SDK capable of targeting .NET 8 (initial parallel record: 10.0.401; this reconciliation verified with 10.0.302)

Both members must confirm the exact Godot version before beginning dependent
work. The gameplay machine has been verified; the UI member's version remains
to be confirmed.

## Open and run

1. Open Godot 4.7.2 .NET.
2. Import `game/project.godot`.
3. Build the C# solution if Godot requests it.
4. Run the project. The entry scene is `res://app/Main.tscn`.

Command-line verification from the repository root:

```sh
dotnet build game/AP6412Match3.csproj
/Applications/Godot_mono.app/Contents/MacOS/Godot --path game
```

Generated `.godot/`, `bin/` and `obj/` directories are intentionally ignored.

Before B's UI exists, F5 shows A's Step 1 board prototype; after UI is supplied, F5 prioritizes that UI. A's `res://presentation/2d/BoardPrototype.tscn` always runs independently with Run Current Scene. It records adjacent-swap requests only: no actual exchange, elimination, moves or results yet. Actual checks and host restrictions are recorded in repository-root `docs/evidence/member-a/step-1.md`. C# shared bases remain in `app/contracts/`; UI uses `AP6412Match3.App.Contracts.GameUiBridge`. Read CONTRACT before implementing.

## Ownership boundaries

- Gameplay member: `app/`, `core/`, `data/`, `gameplay/`, `presentation/`,
  `tests/gameplay/` and project integration.
- UI member: `ui/`, `progress/` and `tests/ui/` after this shared project has
  been merged into `main` and synchronized to `member-b`.
