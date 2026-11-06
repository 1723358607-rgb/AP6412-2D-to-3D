# Shared Godot project

This is the repository's single Godot project for AP6412 Route B. Step 0 only
provides a C# project, application entry scene and the class boundaries agreed
in `docs/alignment/CONTRACT.md`. Match-3 rules and UI pages are not implemented.

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

The entry currently shows A's Step 0 check screen, not the finished UI or a playable board. Actual checks and host-environment restrictions are documented in `docs/evidence/member-a/step-0.md` from the repository root. C# shared bases are in `app/contracts/`; UI uses `AP6412Match3.App.Contracts.GameUiBridge`. Read the updated CONTRACT before implementing.

## Ownership boundaries

- Gameplay member: `app/`, `core/`, `data/`, `gameplay/`, `presentation/`,
  `tests/gameplay/` and project integration.
- UI member: `ui/`, `progress/` and `tests/ui/` after this shared project has
  been merged into `main` and synchronized to `member-b`.
