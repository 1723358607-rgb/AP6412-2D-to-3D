# 第3步：木箱、三关、胜负和星级（成员A）

日期：2026-11-07（Asia/Singapore）。当前任务：“继续完成第三步”。本记录对应member-a / PR #6的玩法交付；B的正式存档与共同页面联调另行验收。

## 收尾与版本核对

上轮因执行环境断线保留草稿。本轮已将本机14份玩法代码/配置/检查文件与远端4a0a805fa2a7486fe401eec4375f21736e190ef6逐文件核对，全部字节一致；随后安全同步草稿文档和证据。原BoardPrototype.tscn的Godot UID保存修改继续留在本机，未覆盖、未夹带进本次提交。新脚本UID已由真实编辑器扫描生成并归档。

本轮新增AppController页面生命周期修复：B的页面会在导航时RemoveChild/QueueFree，BoardHost属于临时UI页面；A的BoardView2D属于应用生命周期，必须先取回A根节点，再允许页面销毁，进游戏时挂入新的BoardHost。补充可销毁页面的测试替身后，旧逻辑真实复现“UI page disposal destroyed A's persistent board presenter”（exit1）；修复后全部检查通过。没有修改B的任何文件或分支，也没有替换B的贴图/坐标校准。

本次代码在member-a保留新增收尾提交，通过PR #6整合，实际SHA/合并状态以GitHub历史为准。没有冻结完整2D或发布3D。

## 实际实现

- 木箱只承受被消除普通棋子上下左右的伤害，同轮多方向命中集合去重后只减一层；对角不伤害，箱子被破坏不传播伤害。
- 双层2→1仍计一个目标；1→0才减少目标。移除后按新连通区段下落，补充和连锁复用同一xorshift32状态。
- 正式三关仍为7×7、15/20/24步、5/10/14箱；第三关第6行第2/4/6列双层。种子641201/641202/641203，未为通过检查改步数、箱位或规则。
- LevelRules用整数判星；GameSession在全部连锁和表现结束后先判目标完成，再判步数耗尽。最后一步获胜为一星，失败零星；每局只发一次结果，终局拒绝新交换。
- 伤害后HUD收到真实目标变化，但phase仍可为resolving；重试/切关/离开取消旧会话，不发旧结果。
- BoardView2D显示剩余层数并播放受击/消失；不在表现层重新判定匹配、伤害或星级。
- AppController先记录结果，再查历史最佳，再显示结果；无ProgressStore时正式UI只允许第一关。公开桥接及snapshot/result字段不变。
- GameplayLoop是A的独立开发入口：1/2/3直测正式关卡，R重试，通关后N下一关；第三关不会进入第4关。它不写存档，不代表生产解锁。

## 本轮检查结果

环境：Godot4.7.2.stable.mono.official.ed1daf0bf；.NET SDK10.0.302；net8.0；macOS arm64。

| 检查 | 实际结果 |
|---|---|
| 本机与远端草稿代码核对 | 14份代码/配置/检查文件字节一致 |
| 最终C#构建 | 通过，0警告、0错误；原nullable警告已消除 |
| 第3步规则/会话/主入口检查 | 通过，exit0，STEP3_SMOKE_PASS |
| 第2步真实消除/场景/接线回归 | 通过，exit0，STEP2_SMOKE_PASS |
| 第1步原型/映射回归 | 通过，exit0，STEP1_SMOKE_PASS |
| 第0步接口/生命周期回归 | 通过，exit0，STEP0_SMOKE_PASS |
| 默认主入口无窗口启动3帧 | 通过，exit0，GAMEPLAY_LOOP_READY |
| 可销毁UI页面的结算/重试/离开/再开局 | 先复现失败，修复后通过，棋盘生命周期保留 |
| 编辑器无窗口导入 | 扫描和载入完成，exit0；仍有editor_settings-4.7.tres写入权限错误，不算无错误导入 |
| 图形窗口、真实鼠标/按键、动画观感 | **未执行本轮手动检查**，待本人按下方操作验收 |
| 队友最新UI与真实存档、重启保留 | **未执行整合检查**；使用的内存探针不是B持久化实现 |

核心覆盖：单/双层木箱、多方向同轮只伤一层、目标1→1→0、无对角/箱子传播伤害、箱子移除后重连下落；三关所有整数余步星级与边界；最后一步通关先于失败；目标归零后等待最终补充；重复操作/旧结果拒绝；正式三关真实会话回放。

实际命令（仓库根；本机路径由队友按其机器替换）：

```sh
dotnet build game/AP6412Match3.sln --no-restore -p:NuGetAudit=false -p:UseSharedCompilation=false --disable-build-servers
/Applications/Godot_mono.app/Contents/MacOS/Godot --headless --path game --fixed-fps 60 --log-file /tmp/ap6412-step3.log -- --level-smoke-test
/Applications/Godot_mono.app/Contents/MacOS/Godot --headless --path game --fixed-fps 60 --log-file /tmp/ap6412-step2-regression.log -- --gameplay-smoke-test
/Applications/Godot_mono.app/Contents/MacOS/Godot --headless --path game --fixed-fps 60 --log-file /tmp/ap6412-step1-regression.log -- --board-smoke-test
/Applications/Godot_mono.app/Contents/MacOS/Godot --headless --path game --fixed-fps 60 --log-file /tmp/ap6412-step0-regression.log -- --smoke-test
```

构建使用已有还原缓存，以及工作区DOTNET_CLI_HOME/已有NUGET_PACKAGES/关闭遥测等本机设置；不写入项目依赖。最终原始输出为logs/step3-final-build.log、step3-final.log、step3-step0/1/2-regression.log、step3-final-startup.log。编辑器错误保留在step3-editor-import.log，修复前失败在step3-page-lifetime-before.log。此前断线时保存的step3-observed-output.txt只是当时工具返回文本，保留为历史，不替代最终原始输出。

## 正式回放与星级

| 关卡 | 种子 | 有效交换数 | 最终余步 | 结果 |
|---|---:|---:|---:|---|
| 1 | 641201 | 2 | 13/15 | 三星、0箱 |
| 2 | 641202 | 1 | 19/20 | 三星、0箱 |
| 3 | 641203 | 9 | 15/24 | 三星、0箱 |

完整每步行列、连锁数、目标与棋盘为step-3-replays.json；本轮从实际最终输出重新提取。查找操作的有界策略只在检查中使用，不改变正式种子或补充流，不进入生产玩法。

星级边界：第一关余2/3对应1/2星、5/6对应2/3星；第二关3/4与7/8；第三关4/5与9/10。失败始终0。最后一步获胜用明确隔离的1步/1箱夹具（种子991）验证，不是改正式关卡或强制通关按钮。

**难度尚未平衡。** 第二关存在一手5轮连锁通关路径。第4步需要评估初始种子和参数，不能把三星路径当难度合理证明；本轮没有偷偷改配置。死局重排仍待第4步，不保证任意游玩路径都能继续。

## 玩家手动验收

在同一个Godot工程中停止旧运行，先点击锤子构建，再打开presentation/2d/GameplayLoop.tscn并运行当前场景（F6）。以下是**1起始行列**，每次等连锁结束。

第一关按1恢复开局：

| 次序 | 点击交换 | 应看到 |
|---|---|---|
| 1 | 第6行第3列 ↔ 第6行第4列 | 3轮连锁，余14步，剩2箱 |
| 2 | 第5行第4列 ↔ 第5行第5列 | 6轮连锁，余13步，0箱、通关、3星 |

按R应回到15步和5个完整木箱。通关后N应进入第二关，20步和10箱。第二关开局交换第3行第4列↔第3行第5列，当前固定种子应5轮连锁后余19步、0箱、3星。

按3应见24步/14箱，第6行第2/4/6列箱子标2。先交换第5行第4列↔第5行第5列，应余23步、11箱。完整9步路径如下：

1. 第5行第4列 ↔ 第5行第5列：余23步，剩11箱。
2. 第3行第5列 ↔ 第4行第5列：余22步，剩10箱。
3. 第5行第4列 ↔ 第5行第5列：余21步，剩8箱。
4. 第6行第5列 ↔ 第6行第6列：余20步，剩6箱。
5. 第6行第4列 ↔ 第6行第5列：余19步，剩5箱。
6. 第4行第5列 ↔ 第5行第5列：余18步，剩4箱。
7. 第5行第5列 ↔ 第6行第5列：余17步，剩4箱。
8. 第6行第4列 ↔ 第6行第5列：余16步，剩2箱。
9. 第3行第2列 ↔ 第4行第2列：余15步，剩0箱。

第三关完成后N不应进入不存在的第4关。快速点击、动画中R和1/2/3切关应不多扣步、不被旧动画覆盖；Esc退出。窗口内文字/数字辨认、真实点击、完整失败重试仍需要本人记录实际结果。

## UI/存档接入说明

已重新读取B的PR #5最新头d280b88865623368afdc4bd09ad5c9349b5c152d。相较上轮，B已新增GameplayScreen、真实snapshot HUD、BoardHost、贴图和CrystalGardenBoardLayout校准；不再把整个游戏页称为占位页。当前实际GameUiRoot.ShowLevelSelect仍使用DemoLevelProgressProvider，ShowResult直接跳回选关，ProgressStore尚未连接。以上是代码读取结果，不等于本轮已运行B页面。

交给B的下一步：

1. 从最新main同步到B工作分支，保留B已有文件与Git历史；不要覆盖其BoardView2D贴图/Alpha/格心校准。合并这个双方都改过的文件时，应保留B视觉层，再接入A的DamagedBoxes受击及层数显示；不能整文件互相覆盖。
2. 保留真实HUD与BoardHost；接入胜利/失败结算，显示本次星数与历史最佳，第三关没有“下一关”。等待LevelFinished，不能看到target_remaining=0就自行提前判胜。
3. 实现res://progress/ProgressStore.tscn，继承ProgressStoreBridge；最高星数max，失败不覆盖，通关解锁下一关，2D/3D独立文件，重启读取。测试使用隔离存档，不能写假成绩到生产存档。
4. ShowLevelSelect使用传入真实progress替换演示数据；由AppController先记录再刷新页面，B不自己扣步/算星。
5. 实测开始→选关→真实游玩→结算→显示历史成绩，以及三星后低星不降、失败不覆盖、关闭重开保留、离开取消/确认与重试。A已覆盖可销毁页面的棋盘保管，但真实UI合并后仍需两边一起验收。

本轮没有更改或合并B的PR #5、贡献归属PR #7或其分支；贡献角色按账号/模块记录（A=@1723358607-rgb玩法，B=@Sherylshiy UI/视觉/进度），不将另一侧会话里的“当前用户”当本会话角色指令。

## 完成范围与后续

A侧第3步实现、草稿核对、最终构建和自动回归已完成；图形手动验收以及B结果/进度集成仍待完成，因此小组第3步还未全体验收，不打2d-baseline、不开始3D。验收后再进入第4步死局重排和难度跑测。

完整原始AI对话仍待真实导出；ai-logs/member-a/index.md只是价值索引，不能冒充完整Prompt/Response。
