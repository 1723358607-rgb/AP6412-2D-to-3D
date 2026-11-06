# 成员身份与贡献归属更正

更正日期：2026-11-07（新加坡）。本文件只澄清身份映射，不改变既有职责、代码所有权、提交作者或 Git 历史。

## 正确映射

| 角色 | 身份 | 已验证 GitHub 账号 | 个人分支 | 主要职责 |
|---|---|---|---|---|
| Member A | 用户的队友 | `@1723358607-rgb` | `member-a` | Gameplay、Core、关卡、规则、2D/3D 棋盘与玩法测试 |
| Member B | 当前用户 | `@Sherylshiy` | `member-b` | UI、视觉、ProgressStore、页面/存档测试与个人贡献记录 |

姓名、学号、组号等尚未由仓库证据确认的资料继续记为 TBD，不从账号推断。

## 历史表述说明

早期对齐文档曾错误写成“用户=A、队友=B”。这只是身份标签颠倒；A=Gameplay、B=UI 的模块职责从未改变。历史证据中的原始任务引用、测试结果、提交和作者不删除、不改写；阅读其中“用户”“队友”等称呼时，以本文件的角色映射为准。

## Crystal Garden 贡献

PR #5（`codex/crystal-garden-ui-demo`）属于 Member B 的 UI 与视觉整合贡献，包括 Homepage、Level Selection、Gameplay UI、Cinzel Bold 700、PNG 素材、HUD snapshot 绑定、返回确认与设置弹窗。

Member B 还完成了 `BoardView2D` 的视觉整合部分：独立水晶/木箱贴图、Alpha 裁切与尺寸协调、棋盘视觉中心和点击映射校准，以及 `CrystalGardenBoardLayout`。这不转移 `BoardView2D` 的完整所有权：Member A 仍负责核心棋盘表现接口、玩法输入流程、交换/消除动画及 Gameplay/Core 规则。

PR #6（`member-a`）属于 Member A 的 Gameplay Step 3 贡献。其代码、作者和历史不因本次更正而改变。

## 分支与证据边界

- `member-b` 是 Member B 的指定个人分支；当前 Crystal Garden 实现保存在可识别的 `codex/crystal-garden-ui-demo` 分支和 PR #5。
- 本次不迁移分支、不 cherry-pick、不 rebase、不改写作者。后续如需把 UI 成果同步到 `member-b`，应在 PR #5 处理方案确定后另行执行。
- 摘要和贡献索引不能替代课程要求的完整 Prompt 与 AI Response。缺失的原始日志保持明确缺口，不能凭记忆重建。
