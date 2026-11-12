# AP6412 — 2D to 3D

AP6412 Group Assignment · Route B · Two-person collaboration.

## Play the 2D baseline and integrated 3D version

| Version | Source | How to launch |
|---|---|---|
| Frozen 2D baseline | [`2d-baseline`](https://github.com/1723358607-rgb/AP6412-2D-to-3D/tree/2d-baseline) · `a067c5f` | Import `game/project.godot` from the tag and press F5. |
| Integrated 3D snapshot | [`250ae4d`](https://github.com/1723358607-rgb/AP6412-2D-to-3D/tree/250ae4db120e2a3dd2e636a22e99794f39d8a1bb) | Import `game/project.godot`, press F5 and use **3D EDITION** or **2D CLASSIC** on Home before Play. |

Godot **4.7.2 Mono/.NET**, C# targeting **net8.0** and .NET SDK **10.0.302** were used to verify both snapshots. Use a separate extracted folder for the frozen baseline and the integrated snapshot.

- [2D to 3D GitHub comparison](https://github.com/1723358607-rgb/AP6412-2D-to-3D/compare/2d-baseline...250ae4db120e2a3dd2e636a22e99794f39d8a1bb)
- [2D and 3D Class Maps](docs/closeout/class-maps.md)
- [Change map and CCR](docs/closeout/transition.md)
- [Verification at the submission snapshots](docs/closeout/verification.md)

Member A (`1723358607-rgb`) implemented core gameplay, level rules and layouts, and 3D gameplay integration. Member B (`Sherylshiy`) implemented the Crystal Garden UI and progress store, supplied the 3D assets, and added mode selection. Key integration history is recorded in [2D integration #12](https://github.com/1723358607-rgb/AP6412-2D-to-3D/pull/12), [3D gameplay #13](https://github.com/1723358607-rgb/AP6412-2D-to-3D/pull/13), [3D assets #14](https://github.com/1723358607-rgb/AP6412-2D-to-3D/pull/14) and [mode selection #15](https://github.com/1723358607-rgb/AP6412-2D-to-3D/pull/15).

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

Import `game/project.godot` from the complete version folder in Godot Mono/.NET 4.7.2. The baseline boots into 2D. The integrated snapshot boots into the mode-select Home screen with 3D selected.

```sh
cd game
dotnet restore AP6412Match3.csproj
dotnet build AP6412Match3.sln --no-restore
```

On a fresh save, only level 1 is unlocked. From Home, choose Play and select level 1 (15 moves, 5 boxes). Click two adjacent gems; invalid swaps do not spend moves. Win to unlock the next level and save actual best stars. Retry/Levels/Next use real gameplay. Closing and reopening the app restores the saved stars and unlocks.

For a deterministic level1 win, start/retry, then swap **row4/col3↔row5/col3**, **row6/col4↔row7/col4**, **row6/col4↔row6/col5** (1-based). Wait after each move. Expect12 moves remaining and3 stars. See [layout v2](docs/evidence/member-a/layout-v2.md) for all replays.

Independent A development harness: open `res://presentation/2d/GameplayLoop.tscn`, F6; 1/2/3 select test levels, R retries, N advances after victory, Esc exits. It never persists progress. Historical BoardPrototype remains request-only.

From repository root, with GODOT pointing to your executable:

```sh
AP6412_PROGRESS_STORE_DIR="$(mktemp -d)" "$GODOT" --headless --path game res://app/Main.tscn --fixed-fps 60 -- --step4-smoke-test
```

Expected `[STEP4_SMOKE_PASS]` and exit0 for that explicit2D scene. To check3D, use --3d-smoke-test on Main3D; see Step6 for isolated-save/restart commands. Regression flags: `--level-smoke-test`, `--gameplay-smoke-test`, `--board-smoke-test`, `--smoke-test`, `--ui-smoke-test` (isolated B demo). Diagnostic `--balance-scan`. `--rule-reference` records actual rule/session/presenter states; after building, `python3 tools/verify-2d-reference.py --godot "$GODOT"` compares them with the provisional archive without updating it. Actual commands, logs and environment warnings are in Step4 evidence. No standalone export exists.

The known Mac unfinished merge was resolved after verified backup and stash preservation. Its existing checkout can be used directly. If a new unfinished merge appears, inspect and preserve it before pulling; ZIP copies are only an alternative for a separate play copy.

### UI teammate handoff

Formal B UI/store are now connected. Sync the integrated main into your own branch and complete the [manual acceptance checklist](docs/evidence/member-a/step-5-acceptance.md). Keep your actual results distinct from A's automated review. B owns ui/progress/tests/ui; A's small integration corrections are credited in the integration evidence. Read [next3D tasks](docs/alignment/NEXT_3D.md) after the2D freeze.

## Submission checklist

- [x] Playable 2D and 3D versions with consistent essential rules
- [ ] Immutable baseline/final tags and GitHub comparison links
- [x] 2D and 3D Class Maps matching the recorded snapshots
- [x] Change map and explicitly defined CCR
- [x] Personal branches, meaningful commits and merge/PR evidence
- [ ] Complete AI logs for each member, or explicit non-use declarations
- [ ] One PDF group report with a named reflection subsection per member
- [ ] Signed group Original Work declaration and individual AI-use declarations
- [ ] Approximately 15-minute live presentation with both members
- [ ] `AP6412_GroupXX.zip`; exact deadline checked on NTULearn
