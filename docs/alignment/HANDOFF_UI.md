# 给 UI 负责人 AI 的启动说明

更新 v1.2：Godot Mono / C# 已确认，旧版 GDScript 接口示例作废，以 CONTRACT 和 C# 基类为准。

身份更正：本说明面向 Member B（当前用户，UI/视觉/进度，`@Sherylshiy`）。Member A 是用户的队友（Gameplay，`@1723358607-rgb`）。早期版本把“用户/队友”对应关系写反，但职责和接口不变。

把下面整段发给队友使用的 AI，并让它能够读取真实仓库或本资料包。只发送仓库链接不会自动授权访问私有仓库。

---

我们两个人正在做 NTU AP6412 小组作业，路线 B：先完整完成并冻结 2D，再转换同一款游戏为 3D。使用 Godot 4.7.2 Mono/.NET 与 C#。A 侧已记录的完整版本为 4.7.2.stable.mono.official.ed1daf0bf，.NET SDK 10.0.302；工程目标net8.0。请实际检查并回报当前电脑的版本，不能假设已匹配。游戏是《彩块工坊》三关三消：我的队友是玩法负责人 A；我是 UI/视觉/进度负责人 B。

仓库：https://github.com/1723358607-rgb/AP6412-2D-to-3D

请先读取真实仓库中的：

1. docs/alignment/README.md
2. docs/alignment/SPEC.md
3. docs/alignment/CONTRACT.md
4. docs/alignment/WORKFLOW.md
5. docs/alignment/STATUS.md

如果这些文件尚未上传，先读取我提供的同路径资料包，明确当前仍未同步 GitHub；不要假装已读取仓库最新版。

当前公共工程入口为 game/project.godot，C#解决方案为 game/AP6412Match3.sln。请额外读取 docs/evidence/member-a/step-0.md 和 game/app/contracts 下的实际 C# 基类。A已提供第2步真实GameplayLoop及入口接线，没有替我制作正式UI或存档。请同时读取docs/evidence/member-a/step-2.md。

我（Member B）负责开始、选关、HUD、胜利/失败结算、返回确认、视觉整合，以及最高星数/解锁的本地进度模块，个人分支为 `member-b`。当前 Crystal Garden UI 证据还可通过 `codex/crystal-garden-ui-demo` 和 PR #5 追踪。玩法侧 A 使用 `member-a`，负责消除、棋盘、三关配置、木箱、胜负和星级计算，以及2D/3D棋盘表现。B 对 BoardView2D 的贴图与坐标校准只记为视觉整合，不接管完整玩法类。不要替 A 重写玩法或修改其 Core 文件。公共工程入口和 project.godot 由 A 整合。

只在同一个 game/ 工程中工作。先检查当前分支、未提交改动、工程是否已初始化。没有共同工程时不要自行另建一套；先整理 UI 页面和测试方案。已有工程时从 WORKFLOW 对应步骤开发，不一次性跳到所有阶段。

UI 根类 GameUiRoot 继承 AP6412Match3.App.Contracts.GameUiBridge，场景固定为 res://ui/GameUiRoot.tscn；在后续步骤3实现的 ProgressStore 继承 ProgressStoreBridge，场景为 res://progress/ProgressStore.tscn。AppController 会自动发现并挂载。使用继承的 C# Godot 信号和 PascalCase 方法；不要重新定义同名信号或编写 GDScript。开始/选关的导航也按 CONTRACT 的 LevelSelectRequested 和 StartScreenRequested 连接。

GameSession当前可开始第一关并发布真实步数/phase/木箱计数。第1步页面仍可在game/tests/ui/隔离预览，第2步正式HUD必须使用ShowGame/UpdateHud传入的真实snapshot。请在游戏页预留可选BoardHost（Control，建议560×500，空白区域及祖先允许鼠标通过）；详见CONTRACT。木箱伤害/胜利/三星及第2/3关留第3步，当前目标保持5/5，不造假进度。

本项目从开始到选关，有三张关卡卡片，显示历史最高0—3星。通关即可解锁下一关。玩法计算本次星数，UI展示并保存历史最大值；低星和失败不覆盖高星。2D/3D独立存档。字段、方法和信号按 CONTRACT，不在聊天中自行改名。

先回报你理解的范围、接口依赖和当前就绪状态，再执行第0步中B的任务。每次交付说明真实检查结果、学生可操作的验收步骤、提交/PR和完整AI日志位置。模拟数据仅用于隔离测试，必须明确标注；最终接入真实玩法。不得编造已运行、测试通过、队友评审或历史日志。

对齐后，请把你的确认与发现的缺项写入你自己的 `docs/evidence/member-b/alignment-ack.md` 并通过个人分支提交。若无法写仓库，先把确认文本交给我转发，不宣称另一侧已经收到。

---

## A 如何核实已对齐

- 读取 B 实际提交的 alignment-ack 或实际发来的确认文本。
- 核对 B 使用的文档版本/提交、文件归属、接口名、当前步骤和缺项。
- 如果对方提出不同规则，先统一 SPEC/CONTRACT，再写实现。
- 本资料包本身不等于对方 AI 已阅读或确认。
