# AP6412 — 2D to 3D

AP6412 Group Assignment · Route B · Two-person collaboration.

## Current status

**Distributed box layouts v2 are playable.** Targets now occupy upper, middle and lower regions; every starting region has a directly box-damaging swap. Counts5/10/14, three double-layer boxes, budgets15/20/24 and star rules are unchanged. Seeds641224/641454/641326 avoid one-move wins; verified example wins use3/5/8 swaps.

See [current layout, exact click replays and checks](docs/evidence/member-a/layout-v2.md). Build0 warnings/errors, Step0–4/UI-demo regressions and current rule-reference comparison pass. The updated reference contains16 swaps and193 actual presenter steps. Old Step4/5 references are retained as history; current-rule-reference.json selects the active archive.

B's progress/result PR #10 is open and not integrated in this layout change. Main currently still uses real session-only progress, reset on close. Mac graphical launch returned-6 this turn; new visual/manual acceptance is pending. No 2D baseline or 3D implementation yet.

- Godot4.7.2 Mono/.NET, exact4.7.2.stable.mono.official.ed1daf0bf, C#, net8.0. Mac verified SDK10.0.302.
- A @1723358607-rgb: gameplay. B @Sherylshiy: UI/visuals/progress. Group number and names pending.

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

For a deterministic level1 win, start/retry, then swap **row4/col3↔row5/col3**, **row6/col4↔row7/col4**, **row6/col4↔row6/col5** (1-based). Wait after each move. Expect12 moves remaining and3 stars. See [layout v2](docs/evidence/member-a/layout-v2.md) for all replays.

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
