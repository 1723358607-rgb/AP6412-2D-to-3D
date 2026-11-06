# 第2步：真实消除闭环（成员A）

日期：2026-11-06。用户本轮原话：“检验没问题，进行下一步步骤2吧”。第1步手动验收由用户报告通过，AI没有代替用户观察屏幕。此记录与历史第0/1步记录分别保留。

## 范围与版本

在 member-a 上从 main 的 9e0350d251ec6076f501cd264a091ef7f540fca5 继续，保留工作区已有4个 Godot 脚本 UID 文件。本次仅改A拥有的 app/core/gameplay/presentation/tests/gameplay、共同文档及A记录；不改B的 ui/progress/tests/ui 或 member-b 分支。提交与PR以本文件的Git历史为准，不虚构队友评审。

本机 Godot 4.7.2.stable.mono.official.ed1daf0bf；.NET SDK10.0.302；macOS26.6 arm64；net8.0。唯一工程 game/project.godot，AP6412Match3 项目身份不变。

## 实际完成

- MatchResolver：水平/垂直3枚及以上匹配、交叉去重；4/5连全部普通消除，不制造特殊棋子。
- 有效交换仅扣1步；相邻但未形成匹配时播放交换及回退，不扣步、不消耗随机序列；越界、非相邻、木箱直接拒绝。
- 清除→按木箱分段下落→区段内补充→重复检查，直至稳定。纯核心返回不可变棋盘和表现步骤，不引用Godot。随机序列沿用 xorshift32，初始生成后的状态传给补充。
- GameSession发布真实 playing/resolving/lost 快照，动画期间拒绝新请求；重试/离开取消旧任务并增长session_id。最多64轮连锁，异常不提交部分核心计算。
- BoardView2D只播放核心结果：交换、回退、缩小消失、下落、补充。不会在表现层重新匹配或扣步。
- 独立 res://presentation/2d/GameplayLoop.tscn 使用真实GameSession、真实步数和目标；F6可单独验收。正式B UI存在时主入口仍优先加载B的UI，否则F5进入本场景。原第1步 BoardPrototype.tscn 保留，仅记录请求。
- 主入口已接真实棋盘、真实HUD事件、重试与离开取消。B可提供可选Control节点 BoardHost（建议560×500或更大）；未提供时棋盘放在CanvasLayer2的中央。开始/选关/结算时棋盘隐藏。公共C#基类及字典字段不变。
- 步数归零在全部连锁结束后只发一次真实失败结果，won=false、stars=0；没有模拟胜利。

## 自动检查：实际结果

| 检查 | 结果 |
|---|---|
| C# build --no-restore | 通过，0警告、0错误 |
| 第2步规则/会话/实际场景/主入口接口检查 | 通过，exit0，STEP2_SMOKE_PASS |
| 第0步接口/生命周期回归 | 通过，exit0；断言随真实玩法更新，不再要求StartLevel为空实现 |
| 第1步独立原型回归 | 通过，exit0；49格映射、固定木箱和选择校验仍成立 |
| 默认主入口无窗口启动3帧 | 通过，exit0，GAMEPLAY_LOOP_READY |
| 编辑器无窗口导入 | 扫描及载入完成、exit0，但有CA/IDE监听端口/编辑器设置权限错误，不能视为无错误导入 |
| 图形显示、真实鼠标、动画观感 | **未运行**，待用户本轮手动验收 |
| 队友真实UI/存档和其电脑运行 | **未运行**，实际B场景尚未交付；测试探针不等于B UI |

运行命令（仓库根目录；本机Godot路径可由队友替换）：

```sh
dotnet build game/AP6412Match3.sln --no-restore
/Applications/Godot_mono.app/Contents/MacOS/Godot --headless --path game --fixed-fps 60 --log-file /tmp/ap6412-step2.log -- --gameplay-smoke-test
/Applications/Godot_mono.app/Contents/MacOS/Godot --headless --path game --fixed-fps 60 --log-file /tmp/ap6412-step0-regression.log -- --smoke-test
/Applications/Godot_mono.app/Contents/MacOS/Godot --headless --path game --fixed-fps 60 --log-file /tmp/ap6412-step1-regression.log -- --board-smoke-test
```

本机离线构建使用已有还原缓存，设置DOTNET_CLI_HOME至工作区、NUGET_PACKAGES至已有缓存，NuGetAudit=false、UseSharedCompilation=false、--disable-build-servers；不把机器绝对路径写入工程依赖。真实构建与运行输出见 logs/step2-*.log。

所有无窗口运行仍打印宿主 get_system_ca_certificates 错误（os_macos.mm:1035）。编辑器另有IDE socket Permission denied及无法写 editor_settings-4.7.tres。它们与上述通过标记同时存在；没有隐藏或宣称引擎日志零错误。本轮未再次尝试图形窗口。

自动覆盖3/4/5连、十字交点只计一次、木箱不参与匹配、单/双层箱阻隔下落、区段补充、有界连锁失败不改源棋盘、100种子稳定终盘（36个样本产生多轮连锁）、输入锁定、重试/离开取消、15步耗尽仅一次失败。实际.tscn通过映射后的鼠标处理方法发信号并等待逐帧动画；主入口使用明确标注的测试UI探针验证继承信号、BoardHost接线及真实快照。以上不等于人工点击或像素截图验收。

## 可复现数据

正式种子641201；下表使用玩家看到的**1起始行列**，每次等动画完成再操作。

| 操作（从新开局开始） | 真实结果 |
|---|---|
| 第1行第1列 ↔ 第1行第2列 | 无匹配，回退，15→15，棋盘与随机状态不变 |
| 第1行第2列 ↔ 第2行第2列 | 15→14，1轮同时消除6枚（两组匹配），补齐至稳定 |
| 再交换第1行第2列 ↔ 第1行第3列 | 14→13，1轮 |
| 再交换第1行第2列 ↔ 第2行第2列 | 13→12，2轮连锁，仍只扣1步 |

首个有效交换前后，按行由上至下，R/B/G/Y为四色，#为木箱：

```text
before: RBRGBBG/GRBRBRB/GBYBRRY/RBGYYBR/YGGRYYB/GRBGBRG/Y#####R
after:  RRGGBBG/GGBRBRB/GGYBRRY/RYGYYBR/YGGRYYB/GRBGBRG/Y#####R
```

完整15步有效操作序列与每步连锁数、余步在 logs/step2-smoke.log 的 STEP2_PLAYTHROUGH 行。这是步数与失败验证，不是通关证据。

## 用户手动验收

1. GitHub Desktop当前仓库应是这一个本地副本；本轮AI已处理提交与同步。Godot中停止旧运行，等待外部文件刷新，点击右上锤子构建。
2. 文件系统打开 presentation/2d/GameplayLoop.tscn，点“运行当前场景”（F6；若Mac功能键受系统控制则用按钮）。无需重新导入或另下载project.godot。
3. 初始应见7×7、5木箱、15步、目标5/5；先按上表的无效操作检查回退不扣步。
4. 按上表三个有效操作依次检查14、13、12步，第三次有2轮连锁。快速连续点击不应多扣步。
5. 按R恢复15步与原始布局；再在消除动画中按R，仍应回到同样15步初始局，没有旧动画覆盖。正常游玩应可多次交换和自动补充，无缺格。

本轮手动实际结果：**待用户反馈**。木箱本轮不会受伤，目标不会递减，这是第3步待接；不要把开发场景当最终UI。

## 队友下一步

从最新main合并到member-b，保留其已有UI修改。按CONTRACT继承GameUiBridge并提供res://ui/GameUiRoot.tscn，ShowGame立即用传入快照刷新，UpdateHud显示真实moves_remaining/target_remaining；不使用假计数、不自己扣步。提供BoardHost时其覆盖棋盘的空白Control及祖先应允许鼠标事件通过（MouseFilter Ignore），按钮/弹窗仍拦截；取消离开不发请求，确认才发LeaveRequested。A负责入口接线，B不改AppController。

队友需实际验收开始→选关→第一关，确认15→14、连锁不多扣、重试恢复15、离开后无旧结算。第2/3关、木箱伤害、胜利/星级留第3步；ProgressStore未交付前只允许第一关，不写假进度。当前未取得队友确认。

## 下一步和证据缺口

A第2步代码与自动检查完成；本轮图形验收及B真实HUD联调仍待完成。用户验收后，A可进入第3步木箱伤害/三关/星级/胜负，与B存档开发并行。死局重排在第4步，本轮若遇无合法步可R重试，尚未承诺任意游玩轨迹可继续。

原始完整AI日志尚待实际导出。ai-logs/member-a/index.md仅为有价值决策索引；不能替代完整对话。本轮没有冻结2D、没有3D或导出成品，也没有为三关难度做平衡证明。
