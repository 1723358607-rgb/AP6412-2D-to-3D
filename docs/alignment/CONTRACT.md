# 接口与文件归属 v1.1 — Godot Mono / C#

用户已确定使用 Godot Mono 与 C# 脚本。本机为 `4.7.2.stable.mono.official.ed1daf0bf`、.NET SDK `10.0.302`、macOS arm64；队友完整版本待其实际确认。工程目标框架为 net8.0，Godot SDK 固定为 4.7.2。所有项目脚本使用 C#，不要按旧版伪代码生成 GDScript。

第 0 步已提供 GameSession 的 idle 骨架、AppController，以及 GameUiBridge / ProgressStoreBridge 两个连接基类；没有可玩三消或正式 UI。以下 C# 声明是双方唯一接口约定。

## 1. 目录与所有权

建议仓库根目录下统一使用 `game/` 作为 Godot 工程目录。

| 路径 | 负责人 | 内容 |
|---|---|---|
| `game/project.godot`、`game/app/` | A | 工程设置、总入口与两侧连接 |
| `game/AP6412Match3.csproj`、`.sln` | A | 共享 .NET 工程和 Godot SDK；B 不另建项目 |
| `game/core/`、`game/gameplay/` | A | 核心棋盘规则与 GameSession 会话、步数、胜负、星级 |
| `game/data/levels/` | A | 三关配置与种子 |
| `game/presentation/` | A | 2D/3D 棋盘显示、选取、交换/消除动画 |
| `game/tests/gameplay/` | A | 规则检查与确定性操作记录 |
| `game/ui/` | B | 开始、选关、HUD、结算、返回确认 |
| `game/progress/` | B | 本地历史最佳成绩、解锁查询 |
| `game/tests/ui/` | B | 模拟数据入口、UI/存档检查 |
| `docs/evidence/member-a/`、`ai-logs/member-a/` | A | A 的验收、说明、完整对话和索引 |
| `docs/evidence/member-b/`、`ai-logs/member-b/` | B | B 的验收、说明、完整对话和索引 |
| `docs/alignment/` | A 整合，双方共同确认 | 公共约定；更改需通知另一侧 |

各自场景保存在各自目录中。A 通过实例化引用 B 的 UI 场景，不直接修改 B 的 UI 文件。B 不自行创建第二个工程或第二套玩法，不修改 `project.godot`；需要项目设置时交由 A 整合。

开发某一步需要跨界时，先写明变更与原因，交给文件负责人处理。禁止覆盖未提交内容、重写队友历史或用 force push 解决冲突。

## 2. 初始类职责

| 类/模块 | 负责人 | 职责 |
|---|---|---|
| BoardModel | A | 格子、普通棋子、木箱状态 |
| MatchResolver | A | 匹配、去重、木箱伤害、连锁结算 |
| BoardGenerator | A | 生成、补充、合法步检查、重排 |
| LevelDefinition | A | 读取关卡配置，提供总步数、木箱等 |
| GameSession | A | 唯一的 UI 玩法入口，管理本局、结果与星级 |
| BoardView2D / BoardView3D | A | 显示和输入映射；不重新计算匹配 |
| AppController | A | 导航连接、会话生命周期、棋盘和 UI 的接线 |
| 页面/HUD 控制器 | B | 接收用户请求并显示快照/结果 |
| ProgressStore | B | 本地进度；最高星数、解锁查询、读写 |

可以在实现中合理合并小类或拆分过大类，但修改类图与约定，并解释真实职责；不为凑 UML 或 SOLID 数量创建空类。

## 3. UI 发出的请求

公共连接基类命名空间为 `AP6412Match3.App.Contracts`；会话位于 `AP6412Match3.Gameplay`，入口位于 `AP6412Match3.App`。B 创建 `game/ui/GameUiRoot.cs`，继承 A 提供的 `GameUiBridge`；根 Control 场景固定为 `res://ui/GameUiRoot.tscn`。继承以下信号，不重新声明同名事件：

```csharp
[Signal] public delegate void StartRequestedEventHandler(int levelId);
[Signal] public delegate void RetryRequestedEventHandler();
[Signal] public delegate void NextRequestedEventHandler();
[Signal] public delegate void LeaveRequestedEventHandler();
[Signal] public delegate void LevelSelectRequestedEventHandler();
[Signal] public delegate void StartScreenRequestedEventHandler();
```

- `StartRequested` 只对已解锁关卡发出；A 也验证关卡编号。
- `NextRequested` 只在非最终关胜利时提供，A 计算下一关编号；B 不复制当前局胜负逻辑。
- B 负责离开确认弹窗。确认后发出 `LeaveRequested`，取消不发出。
- 开始界面进入选关、已结束结算返回选关，发出 `LevelSelectRequested`；选关返回开始发出 `StartScreenRequested`。这两个补充信号明确了原约定未细化的导航连接。
- A 负责结束旧会话、停止动画和清理连接，避免旧结果污染新页面。

C# 用 `EmitSignal(GameUiBridge.SignalName.StartRequested, levelId)` 等方式发出请求；AppController 使用生成的 C# 事件订阅。不要使用普通 Action 事件替代 Godot 生成事件，也不要用旧 snake_case 字符串调用这些 C# 自定义接口。

## 4. GameSession 提供的入口与信号

以下为公开边界；具体内部实现由 A 完成：

```csharp
public void StartLevel(int levelId);
public void RetryLevel();
public void LeaveLevel();
public Godot.Collections.Dictionary GetSnapshot();
[Signal] public delegate void StateChangedEventHandler(Godot.Collections.Dictionary snapshot);
[Signal] public delegate void LevelFinishedEventHandler(Godot.Collections.Dictionary result);
```

棋盘点击/交换由 A 的表现层处理，不让 B 的 HUD 直接操作 BoardModel。

发布数据为副本，UI 只读，不能反向修改核心状态。订阅后立即调用 `GetSnapshot()`，不依赖启动信号顺序；玩法实现后每个有意义的状态变化发布新快照。C# 使用 Variant 的 `AsInt32()`、`AsString()`、`AsBool()` 等读取字段。

第 0 步：`IsGameplayImplemented` 为 false，快照为真实 idle、level_id=0、计数为0。合法 StartLevel 请求仅报告尚未实现，不伪造 playing/胜负/星级；非法编号抛出 ArgumentOutOfRangeException。不要把这个骨架当作已实现游戏。UI 阶段1仍用隔离测试入口验证完整页面状态。

### snapshot 固定字段

| 字段 | 类型 | 约定 |
|---|---|---|
| `session_id` | int | 本次进程内单调增加；重开/新开局使用新编号 |
| `level_id` | int | 1、2、3；idle 时可为 0 |
| `phase` | String | `idle`、`playing`、`resolving`、`won`、`lost` |
| `initial_moves` | int | 该关总步数 |
| `moves_remaining` | int | 当前步数，不为负 |
| `target_total` | int | 木箱总个数，不是总层数 |
| `target_remaining` | int | 尚未完全破坏的木箱个数 |
| `mode` | String | `2d` 或 `3d`；由应用配置指定 |

### result 固定字段

包含 `session_id`、`level_id`、`initial_moves`、`moves_remaining`、`target_remaining`、`mode`，另加：

| 字段 | 类型 | 约定 |
|---|---|---|
| `won` | bool | 是否完成目标 |
| `stars` | int | A 按 SPEC 计算；胜利 1—3、失败 0 |

每局只发布一次 `level_finished`，在全部连锁结算后发布。AppController 收到结果后先调用 ProgressStore 记录，再显示结算；下次打开选关时刷新历史最佳。

## 5. B 提供的 UI 入口

由 B 的 UI 根控制器提供：

```csharp
public override void ShowStart();
public override void ShowLevelSelect(Godot.Collections.Dictionary progress);
public override void ShowGame(Godot.Collections.Dictionary snapshot);
public override void UpdateHud(Godot.Collections.Dictionary snapshot);
public override void ShowResult(Godot.Collections.Dictionary result, int bestStars);
```

AppController 自动查找 `res://ui/GameUiRoot.tscn` 并挂载，未找到时保留 A 的启动检查页（不是正式 UI）。B 不修改 AppController 或项目设置；页面内部可自由拆分，统一从 GameUiRoot 连接。先实现可替换的页面组件，禁止让模拟数据留在最终生产入口中。

## 6. ProgressStore 边界（B）

```csharp
public override void LoadProgress(string mode);
public override void RecordResult(Godot.Collections.Dictionary result);
public override int GetBestStars(int levelId);
public override bool IsUnlocked(int levelId);
public override Godot.Collections.Dictionary GetProgress();
```

B 的 `game/progress/ProgressStore.cs` 继承 `ProgressStoreBridge`，根 Node 场景固定为 `res://progress/ProgressStore.tscn`。AppController 在场景存在时自动挂载并先调用 LoadProgress("2d")。该场景可以到步骤3再实现；之前只提供初始零星选关数据，不保存、不模拟通关。正式游戏开始前必须接入真实进度模块。

进度快照建议：

```json
{
  "schema_version": 1,
  "mode": "2d",
  "best_stars": {"1": 0, "2": 0, "3": 0}
}
```

- 第一关始终解锁；2/3 关由上一关 best_stars>0 推导，避免两个状态源不一致。
- 按 mode 分别保存到 `user://progress_2d.json` 和 `user://progress_3d.json`。
- 不在存档中重新计算本次星数，只做格式验证、最高值更新和持久化。
- 忽略失败结果对成绩的覆盖；保留最高星数；同一结果重复记录不降低或重复累加成绩。
- 缺失存档使用初始状态；损坏存档不崩溃，保留损坏文件以便诊断，使用安全初始状态继续。测试使用单独测试存档路径，不删除真实玩家成绩。

## 7. 连接前的模拟测试

B 可在 `game/tests/ui/` 中使用与上述字段完全相同的测试快照和结果。模拟入口必须明确标注 TEST/MOCK；不能假装已连接真实玩法，不能写入正式存档。

首次连接使用真实 GameSession。若接口不满足，记录缺失字段/调用/复现步骤，由对应负责人修改；不得两边同时改接口名。
