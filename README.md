# AP6412 — 2D to 3D

AP6412 Group Assignment · Route B · Two-person collaboration.

## Current status

Step 0 C# bootstrap and shared connection contracts are implemented. There is no playable match-3 level or frozen baseline yet. Local compilation and headless checks completed; graphical/manual and teammate checks remain pending.

- Game concept: 彩块工坊 — three match-3 levels, limited moves, box targets and 1–3 stars.
- Engine: Godot 4.7.2 Mono/.NET, C# only. A's exact build: `4.7.2.stable.mono.official.ed1daf0bf`; teammate version pending.
- .NET: A tested SDK 10.0.302 on macOS arm64; project targets net8.0 with Godot.NET.Sdk 4.7.2 and major runtime roll-forward. Teammate environment must be checked separately.
- Group identifier and member names: TBD
- Member A: @1723358607-rgb — gameplay; personal branch `member-a`.
- Member B: GitHub account TBD — UI and local progress; personal branch `member-b`.

## 项目对齐与下一步

双方 AI 开始工作前，先读取 [共同对齐入口](docs/alignment/README.md)。

- [游戏设定：三关、木箱、三星与存档](docs/alignment/SPEC.md)
- [接口与文件归属](docs/alignment/CONTRACT.md)
- [每一步双方任务、可复制指令与验收](docs/alignment/WORKFLOW.md)
- [给 UI 负责人 AI 的启动说明](docs/alignment/HANDOFF_UI.md)
- [真实同步状态与待办](docs/alignment/STATUS.md)

第 0 步共同工程已建立，见 [实际验收记录](docs/evidence/member-a/step-0.md) 和 [当前类关系](docs/evidence/member-a/step-0-class-map.md)。用户先做图形手动验收，队友确认版本并同步工程。后续先完成并冻结完整 2D，再开发 3D。对齐文档不是完整原始 AI 日志；对方 AI 是否已阅读，需要实际确认。

## Development sequence

1. Agree on a small complete game, controls, scoring and win/lose rules. Record initial assumptions and plausible future needs.
2. Draw the initial Class Map; distinguish core game rules from input, camera, rendering and engine-specific components.
3. Build and verify a complete playable 2D game. Document setup and run instructions, reproducible checks and known limitations.
4. Freeze the verified 2D version with tag `2d-baseline`. Do not move or overwrite this tag.
5. Convert the same game to 3D, preserving essential objectives, scoring and win/lose rules. Record reused and changed classes.
6. Verify both versions; update Class Maps and change map; define and calculate the Core Change Ratio (CCR).
7. Freeze the working 3D version with tag `3d-final`. Add compare links, the PDF group report, individual reflections and declarations.
8. Prepare the approximately 15-minute live demonstration and final ZIP for NTULearn.

Tags are created only when the corresponding playable versions have been completed and checked.

## Collaboration

- `main`: integrated, checked work.
- `member-a` and `member-b`: identifiable personal development branches; each member commits using their own GitHub account.
- Before starting, sync the latest main into the personal branch and agree on file ownership. Avoid concurrent edits to the same scene or large script.
- Submit small meaningful commits and a pull request to main. The other member reviews and runs relevant checks.
- Preserve pull-request and merge history; use merge commits rather than squashing away all development evidence.
- Initial repository scaffolding is setup work, not evidence of either member's game implementation.

## Evidence locations

- `docs/`: game specification, assumptions, Class Maps, change map, verification, report and declarations.
- `ai-logs/member-a/`: Member A's complete chronological AI conversations and value index.
- `ai-logs/member-b/`: Member B's complete chronological AI conversations and value index.

Full prompts AND responses, including corrections and relevant code blocks, must be retained. Selected excerpts or summaries alone are insufficient. See `ai-logs/README.md`.

## How to run

Use **Godot .NET / Mono 4.7.2**, not the standard build without C# support. The only project is `game/project.godot`; the main scene is `res://app/Main.tscn`.

1. Install/confirm the .NET SDK on each computer; the current machine has 10.0.302. Confirm the teammate's exact Godot Mono version before parallel implementation.
2. In the repository's `game` directory, restore and build:

```sh
dotnet restore AP6412Match3.csproj
dotnet build AP6412Match3.sln --no-restore
```

3. Import `game/project.godot` in the Godot .NET project manager. Build the C# project, then press **F5**.
4. Expect the **Step 0 shared-project check screen**, explicitly stating that gameplay is not implemented. Before B supplies the UI, **Esc** closes this bootstrap.
5. The bootstrap does not contain a playable board, win/lose, stars or persistence. Those belong to later steps. No Windows/macOS standalone export has been made.

Headless check on this Mac:

```sh
/Applications/Godot_mono.app/Contents/MacOS/Godot --headless --path game --log-file /tmp/ap6412-step0-smoke.log -- --smoke-test
```

Run the command from the repository root. Expect `[STEP0_SMOKE_PASS]` and exit 0. It checks the actual C# runtime and interfaces, not unfinished match-3 functionality. In the restricted tool environment, macOS user-data/CA and editor IDE-port/settings operations report host permission errors; these are recorded, not hidden. The graphical window attempt exited 134, so visual layout and Esc remain manual checks.

For offline work, the local validation restored from the installed Godot .NET bundle and an existing NuGet cache. These machine-specific paths are not project dependencies; a teammate with network access can use normal `dotnet restore`.

### UI teammate handoff

B implements C# `GameUiRoot : AP6412Match3.App.Contracts.GameUiBridge` at `res://ui/GameUiRoot.tscn`. Later, `ProgressStore : ProgressStoreBridge` lives at `res://progress/ProgressStore.tscn`. The app discovers these scenes automatically; do not create another project or edit A's entry scene/settings. See the actual base classes in `game/app/contracts/` and the updated CONTRACT, including both navigation signals. Until gameplay exists, preview UI states in B's isolated test scene with clearly labelled mock data.

## Submission checklist

- [ ] Playable 2D and 3D versions with consistent essential rules
- [ ] Immutable baseline/final tags and GitHub comparison links
- [ ] Initial and final Class Maps matching the code
- [ ] Change map and explicitly defined CCR
- [ ] Personal branches, meaningful commits and merge/PR evidence
- [ ] Complete AI logs for each member, or explicit non-use declarations
- [ ] One PDF group report with a named reflection subsection per member
- [ ] Signed group Original Work declaration and individual AI-use declarations
- [ ] Approximately 15-minute live presentation with both members
- [ ] `AP6412_GroupXX.zip`; exact deadline checked on NTULearn
