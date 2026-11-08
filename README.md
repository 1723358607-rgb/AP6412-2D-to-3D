# AP6412 — 2D to 3D

AP6412 Group Assignment · Route B · Two-person collaboration.

## Current status

**Step5 A preparation is ready; 2D is not frozen.** Actual class maps, design notes, source fingerprints and 3-level rule references are saved. The reference records 16 accepted swaps and 217 actual presenter steps; two runs match exactly. Mac build and Step0–4/UI-demo regression pass. See [Step5 evidence](docs/evidence/member-a/step-5.md) and [two-person acceptance checklist](docs/evidence/member-a/step-5-acceptance.md). B persistence, original UI result/progress binding, human acceptance and full raw AI exports remain pending.


Step4 gameplay and actual UI integration are implemented. F5 opens B's Crystal Garden home/level/game pages and assets, connected to actual swaps, boxes, wins/losses, earned stars and current-session unlocks. Dead boards recover through bounded deterministic piece-preserving shuffle. Level2 seed is now641209; move budgets and star rules are unchanged.

**Progress is currently session-only and resets on close.** B's persistent store and original UI result/progress binding remain pending. No 2D baseline tag has been created. See [Step4 evidence, screenshots and exact manual replay](docs/evidence/member-a/step-4.md) and [B handoff](docs/alignment/HANDOFF_UI.md).

Build:0 warnings/errors. Step0–4 and B original UI demo checks passed, including real X11/software OpenGL rendering and input. The cloud editor import logged a GodotTools/MSBuild exception, and audio fell back to Dummy; Mac/teammate manual acceptance is pending. The earlier local Mac unfinished merge has since been backed up/resolved; its member-a copy is synced and current Mac checks are recorded in Step5.

- Engine: Godot4.7.2 Mono/.NET, C#; exact build `4.7.2.stable.mono.official.ed1daf0bf`.
- Project: net8.0 / Godot.NET.Sdk4.7.2. Tested SDK8.0.427 on Linux and previously10.0.302 on macOS arm64; teammate version pending.
- A @1723358607-rgb: gameplay / member-a. B @Sherylshiy: UI, visuals and progress / member-b; original Crystal Garden work preserved through its source branch and PR #5.
- Group number and human names: TBD.

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

Get the **entire repository**. Import `game/project.godot` with **Godot Mono/.NET4.7.2**, build C#, press **F5**. The main scene is `res://app/Main.tscn`. Do not import only a downloaded project.godot file.

```sh
cd game
dotnet restore AP6412Match3.csproj
dotnet build AP6412Match3.sln --no-restore
```

Home→Levels: initially only level1 is unlocked, no earned stars. Home→Play starts level1 (15 moves/5 boxes). Click two adjacent gems; invalid swaps do not spend moves. Win to unlock next level and see actual current/best stars. Retry/Levels/Next use real gameplay. Closing the app resets the current temporary progress.

For a quick deterministic win, start/retry level1, then swap **row6/col3↔row6/col4**, wait, then **row5/col4↔row5/col5** (1-based). Expect victory,13 moves,3 stars. See [Step4](docs/evidence/member-a/step-4.md) for all three replays and edge cases.

Independent A development harness: open `res://presentation/2d/GameplayLoop.tscn`, F6; 1/2/3 select test levels, R retries, N advances after victory, Esc exits. It never persists progress. Historical BoardPrototype remains request-only.

From repository root, with GODOT pointing to your executable:

```sh
"$GODOT" --headless --path game --fixed-fps 60 -- --step4-smoke-test
```

Expected `[STEP4_SMOKE_PASS]` and exit0. Regression flags: `--level-smoke-test`, `--gameplay-smoke-test`, `--board-smoke-test`, `--smoke-test`, `--ui-smoke-test` (isolated B demo). Diagnostic `--balance-scan`. `--rule-reference` records actual rule/session/presenter states; after building, `python3 tools/verify-2d-reference.py --godot "$GODOT"` compares them with the provisional archive without updating it. Actual commands, logs and environment warnings are in Step4 evidence. No standalone export exists.

The known Mac unfinished merge was resolved after verified backup and stash preservation. Its existing checkout can be used directly. If a new unfinished merge appears, inspect and preserve it before pulling; ZIP copies are only an alternative for a separate play copy.

### UI teammate handoff

B retains `ui/`, `progress/`, `tests/ui/`. Implement `ProgressStoreBridge` at `res://progress/ProgressStore.tscn` **together with** original UI snapshot-backed level selection and real result pages. Adding the store scene switches off A's temporary app adapter, so do not deliver an empty store shell alone. Keep input propagation, measured BoardHost alignment and A box-damage rendering. See [CONTRACT](docs/alignment/CONTRACT.md) and [current checklist](docs/alignment/HANDOFF_UI.md).

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
