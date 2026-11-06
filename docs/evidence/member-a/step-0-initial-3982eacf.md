# Step 0 evidence — Member A

> 身份更正（2026-11-07）：这里的 Member A 是用户的队友、Gameplay 负责人（`@1723358607-rgb`）；当前用户是 Member B、UI 负责人（`@Sherylshiy`）。保留本文件的原始历史内容。

步骤/负责人：Step 0，共享 Godot 工程初始化；Member A（Gameplay）

代码提交/PR：待本文件随本次变更提交后补充到 PR 记录。

Godot版本、系统和入口：

- macOS Apple Silicon
- Godot `4.7.2.stable.mono.official.ed1daf0bf`
- .NET SDK `10.0.401`，目标框架 `net8.0`
- 工程：`game/project.godot`
- 入口：`res://app/Main.tscn`
- UI 成员的完整 Godot 版本仍待本人确认。

实际完成：

- 创建仓库中唯一的 `game/` Godot .NET 工程。
- 使用 GL Compatibility 渲染方式，兼顾 2D/3D 和较广设备兼容性。
- 建立 AppController、GameSession、BoardModel、MatchResolver、
  BoardGenerator、BoardView2D、BoardView3D 的 Step 0 骨架。
- GameSession 保留 CONTRACT 规定的请求、信号和快照字段边界。
- 建立玩法/UI 文件所有权目录，没有实现三消、关卡或完整 UI。
- 添加忽略规则，排除 `.godot/`、`bin/`、`obj/`。

自动检查（命令、实际输出摘要、通过/失败/未执行）：

- `dotnet build game/AP6412Match3.csproj --configuration Debug`：通过，
  0 warnings、0 errors。
- Godot headless editor import：通过，无资源或脚本解析错误。
- Godot headless project run：通过，输出
  `AP6412 Step 0 shared project ready.`。
- GUI 手动视觉检查：未执行；Step 0 入口不包含正式视觉内容。

手动检查（操作、期望、实际）：

- 当前自动运行已确认基础入口能够启动。
- 合并后两位成员仍需分别用 Godot 4.7.2 .NET 导入并运行，确认本机结果。

复现种子与操作序列：Step 0 尚无棋盘、随机种子或玩家操作。

已知问题/交给对方的依赖：

- 公共文档的接口示例为 GDScript 语法；本工程按相同语义提供 C# API，
  未擅自修改公共约定。
- UI 成员需确认 Godot 完整版本并在 `member-b` 提交 alignment ack。
- UI 页面和 ProgressStore 尚未实现，符合 Step 0 范围。

完整AI日志路径：待成员从当前 Codex 会话完整导出后存入
`ai-logs/member-a/raw/`；本证据文件不冒充完整日志。

有价值对话索引：待完整日志导出后建立。

下一步条件是否满足：A 侧工程初始化和自动验证已满足；进入双方协作的
下一步前，仍需 PR 人工验收、合并，并由 B 同步及验证同一工程和版本。
