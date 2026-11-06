# 第 0 步交付与验收记录

日期：2026-11-06（新加坡） · 负责人：A / @1723358607-rgb · 分支：member-a

## 1. 实际环境与初始状态

- Godot Mono：`4.7.2.stable.mono.official.ed1daf0bf`。
- 路径：`/Applications/Godot_mono.app/Contents/MacOS/Godot`；不要打开不支持C#的普通 Godot.app。
- .NET SDK：10.0.302；运行时10.0.10；macOS26.6、arm64。
- 本机初始工作目录不是Git仓库，没有发现已有共同工程。远端main/member-a/member-b均为 `e39d14cbace41cbb7ad0ed663b550280fe98d81c`。
- 读取全部对齐文档后，建立新的独立本地副本，分支member-a。初始git状态干净，没有覆盖任何已有工作。
- 终端DNS无法解析github.com，因此通过已授权GitHub连接取得文件与真实Git对象，在本地建立相同SHA/树的浅克隆；这不是伪造的初始化提交。Git对象完整性检查通过。后续远端写入也使用该连接。
- 提交前发现远端member-a并行新增 `3982eacf0a28d1559a4e023169b9d5bf59742f7e`（同为第0步初始化）。已读取其全部变更，以它为后续父提交整合：沿用AP6412Match3项目名、game/gameplay会话路径、既有UID和空类，不覆盖提交历史，不保留第二套工程。
- 原提交的环境与检查记录原样保存在 [并行初始化原始记录](step-0-initial-3982eacf.md)。该记录中的SDK10.0.401与本轮实测10.0.302来自不同执行记录；没有把其检查结果冒充本轮验证。最终以本轮整合后的实际日志为准。
- 队友环境未知，没有把“双方版本一致”标为完成。

## 2. 实际交付

- 唯一工程：`game/project.godot`；入口：`res://app/Main.tscn`。
- C#工程/解决方案：`game/AP6412Match3.csproj`、`game/AP6412Match3.sln`。
- Godot.NET.Sdk固定4.7.2；目标net8.0；允许在本机.NET10运行。没有写入本机绝对路径依赖或强制升级系统SDK。
- AppController连接骨架、GameSession idle骨架、GameUiBridge和ProgressStoreBridge抽象接口。
- A目录中的启动检查页，明确标“尚无可玩三消”；没有正式开始/选关/结算界面，也没有存档。
- 预留玩法表现和关卡数据目录；未实现棋盘、匹配、关卡、星级算法或3D。
- 更新公共接口为C#，保留Godot Dictionary字段；补齐开始/选关的导航请求。
- 只修改A负责的工程/玩法/接线文件、公共约定及A自己的证据目录；B的ui/progress/tests/ui中已有.gitkeep原样保留。

## 3. 已执行的检查

| 检查 | 实际结果 |
|---|---|
| Godot版本和dotnet --info | 已执行，结果如上 |
| 本地初始git状态与远端分支 | 已执行，初始干净，基于同一真实提交 |
| .NET恢复依赖 | 已完成；网络受限，使用已安装Godot附带包和本机已有NuGet缓存；初次SDK解析失败在指定现有缓存后解决 |
| C#解决方案编译 | 通过，0警告、0错误 |
| Godot编辑器无窗口导入 | 导入扫描完成，退出0；但存在下述沙箱宿主错误，不能称“编辑器无错误验收通过” |
| --smoke-test | 通过，退出0，输出STEP0_SMOKE_PASS；快照字段、隔离、idle生命周期、无假结算、非法关卡编号、继承UI信号检查通过 |
| 普通入口无窗口运行5帧 | 退出0，输出BOOTSTRAP_OK和预期UI_PENDING |
| 图形窗口/帧图尝试 | 当前环境启动退出134，无可检查画面；失败/受阻，不标通过 |
| 资源释放 | 首次测试发现独立测试节点未显式Free；已修正并重新编译、复查，最终运行没有该泄漏警告 |

### 宿主权限限制

工具运行时不能创建 macOS 用户应用数据目录、读取系统CA证书；编辑器也不能绑定IDE通信端口或保存全局编辑器设置。日志出现对应ERROR，属于本次受限运行环境，未改动项目来掩盖它们。headless检查仍完成并返回0，但这不等于图形界面或存档已验证。不要通过公开仓库、关闭系统安全或修改全局权限来绕过。

实际日志见 logs/headless-smoke.log、logs/headless-startup.log 与 logs/editor-import.log。图形尝试没有有效截图，只有退出码记录。

## 4. 本人手动验收（待用户执行）

1. 打开 **Godot_mono.app / Godot .NET 4.7.2** 项目管理器，导入本仓库的 `game/project.godot`。不要创建新工程。
2. 在编辑器构建C#工程；首次联网电脑可先在game目录执行 `dotnet restore`、`dotnet build AP6412Match3.sln`。
3. 按F5（运行项目）。应出现“彩块工坊 · 共同工程已启动”，并标明Godot4.7.2Mono、C#、尚无可玩三消、双方分工。
4. 拉伸窗口，确认文字仍可读取；这是待验收项，AI未看到实际窗口。
5. 当前启动检查页中按Esc，应关闭运行窗口并回到编辑器；这也是待验收项。
6. 再次F5能重新进入相同检查页。输出应有BOOTSTRAP_OK、UI_PENDING，不应出现C#解析/脚本缺失错误。
7. UI_PENDING表示B尚未交付UI，当前属于正常状态；不要把它当作功能损坏。
8. 记录实际成功/失败与复现；若出现红色错误，保留第一条错误和完整相关输出交给A修复。

没有棋盘、开始按钮或选关卡片是第0步的范围，不是三消已完成。不能验收消除、星级、存档或3D，这些尚未实现。

## 5. 队友下一步需要接入

先确认自己Godot Mono和.NET版本；从合并后的main同步到member-b，打开同一个game/project.godot并完成上述启动验收。

然后执行UI第1步：

- 用C#在 `game/ui/` 实现 GameUiRoot，继承 `AP6412Match3.App.Contracts.GameUiBridge`。
- 场景路径固定 `res://ui/GameUiRoot.tscn`；AppController会自动识别，无需B编辑A入口或项目设置。
- 实现ShowStart、ShowLevelSelect、ShowGame、UpdateHud、ShowResult，发出基类已有请求信号。
- 第1步在 `game/tests/ui/` 使用隔离模拟快照预览各页面；当前GameSession只idle，不能假装已接入完整玩法。
- ProgressStore到第3步再在 `game/progress/` 继承ProgressStoreBridge实现，场景为 `res://progress/ProgressStore.tscn`。
- 不创建第二个project.godot、csproj/sln，不添加GDScript，不重新定义同名信号。
- 把实际版本、运行结果、接口理解和缺项写入B自己的alignment-ack.md。

## 6. 下一步条件

本机自动检查满足开始第1步实现的基础；用户图形手动验收、队友环境与接入确认仍待完成。没有创建2d-baseline或3d-final标签，没有宣称双人第0步全部验收通过。

当前提交和PR链接以GitHub历史为准。实施记录/索引不是完整原始AI聊天；完整聊天仍需真实导出或逐轮保存，不能回忆补写。
