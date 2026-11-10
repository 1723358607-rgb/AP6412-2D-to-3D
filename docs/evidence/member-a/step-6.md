# 第6步：独立3D可试玩版本（正式基线与人工验收待补）

日期2026-11-10，新加坡；成员A @1723358607-rgb。本轮用户再次要求“下一步”，A在保留可用2D主分支的前提下推进独立3D试作，不继续把技术实现停在资料整理。分支codex/member-a-3d，基于真实2D联合提交a067c5f50546b9b8a1ca12479e56eb36c0acfdcd。

没有收到双方本版人工验收记录，因此没有假填表或创建2d-baseline。该提交是明确的2D对照，不冒称正式冻结标签；主分支不被3D试作覆盖。step-6-base.json保存真实基准SHA、9个纯核心文件及规则档案的SHA256。

## 实际实现

- BoardView3D现在是实际Node3D/IBoardPresenter：SubViewport、相机、灯光、环境、实体棋盘、宝石/木箱Mesh。不是把2D贴图立起来；三维模型的X/Y/Z尺寸均有检查。
- GemMeshes生成有厚度的粉色心形、蓝色尖晶、紫色菱晶、金色六边形及带层数/束带的木箱。材质、顶点和模型由代码创建，无外部模型下载或新版权依赖。
- 倾斜正交Camera3D提供真实三维投影；射线与每格三维碰撞代理求最近交点，映射回同一GridPosition。代理用于点选，不是物理下落。测试包含49格和窗口缩放后的选取。
- 播放同一套Swap/SwapBack/Clear/Fall/Refill/Reshuffle步骤；取消/重试/离开会停止旧动画。没有在3D里写第二份消除、伤箱、补充或星级代码。
- SubViewport纹理挂载到B的BoardHost；空白区域允许未处理鼠标事件，按钮/弹窗继续拦截。换页前把表面取回A拥有的CanvasLayer，避免B释放页面时删掉3D呈现器。
- AppController选择2D或3D表现器并向存档传入模式。GameSession实际新增ConfigureMode和Mode，用于snapshot/result；仍是同一个会话实现，不能声称该文件零改动。
- B的UI/ResultScreen/ProgressStore和美术文件完全复用，未修改。两个模式各用既有独立存档文件。根界面角落显示2D/3D EDITION。

## 入口与普通玩家试玩

当前独立3D分支的project.godot主场景为app/Main3D.tscn。重新打开同一工程、构建C#后按F5，开始/选关后看到立体棋盘。操作仍是点击一枚棋子，再点击相邻棋子；按layout-v2.md的同一组行列路径验收。三关示例3/5/8步，分别剩12/15/16步三星。

首次3D成绩独立，2D已有三星不会自动解锁3D第二关。通关后返回选关、重试、下一关和末关完成页仍是B页面；关闭重开检查3D成绩。试试动画中重试/离开、离开取消/确认，按钮不应误触棋盘。

要在该分支运行2D，打开app/Main.tscn并按F6。要获取完全未改的2D源码，用主分支a067c5f或对应提交；不要把3D工作目录直接覆盖到旧副本。原2D正式标签仍待共同验收。

## 实际自动验证

本机macOS arm64、Godot4.7.2.stable.mono.official.ed1daf0bf、.NET SDK10.0.302。最终构建0警告0错误。

| 检查 | 实际结果 |
|---|---|
| 三维模型/射线映射 | 49格投影→射线→三维代理正确，1280×720重设窗口后也正确；模型均有非零厚度 |
| 真实3D视图输入 | viewport事件到GameSession；木箱拒绝、无匹配回退不扣步、busy不重复扣步 |
| 同档案逐步等价 | 3关16次交换、193个实际3D表现步骤；棋盘/木箱伤害/步数/目标/RNG/结果逐项一致 |
| 失败/重试/最佳成绩 | 真实3D失败为0星，最佳三星保留；重试复位；无不存在的第四关 |
| 生命周期与UI | 动画中重试/离开取消；离开弹窗拦截实际鼠标；B页面释放后3D表面仍可再次挂载 |
| 模式存档 | 3D胜利保存，2D文件字节不变；第二个Godot进程恢复3D三星并验证2D仍独立 |
| 旧2D入口回归 | Main.tscn上的第0/1/2/3/4步、B UI演示、存档及联合审计全部通过 |
| 旧2D规则档案 | 2D回放输出与保存的layout-v2档案逐项一致，未覆盖历史档案 |

所有成绩相关检查使用隔离AP6412_PROGRESS_STORE_DIR，没有删除或写入真实玩家成绩。logs/step6-*.log为原始运行输出。新检查标签为STEP6_RAY_PASS / EQUIVALENCE_PASS / FAILURE_PASS / LIFETIME_PASS / SMOKE_PASS / RESTART_PASS。

这些是无窗口实际场景/相机数学/模型数据/动画和UI事件验证，不是实机截图或图像质量验收。本轮未再次尝试此前失败的Mac图形启动，没有声称阴影、构图、色彩和真实手感已经人工通过；声音、导出包、双方人工验收也未执行。当前模型是程序生成的初版美术，可在规则不变的前提下继续优化。

复现命令（仓库根目录）：

```sh
# GODOT指向匹配的Mono可执行文件，隔离目录必须为空。
AP6412_PROGRESS_STORE_DIR=/tmp/ap6412-3d-fresh "$GODOT" --headless --path game --fixed-fps 60 -- --3d-smoke-test
# 第二个进程使用同一隔离目录：
AP6412_PROGRESS_STORE_DIR=/tmp/ap6412-3d-fresh "$GODOT" --headless --path game --fixed-fps 60 -- --3d-smoke-test --3d-restart-read
# 旧2D回归要明确指定旧入口：
AP6412_PROGRESS_STORE_DIR=/tmp/ap6412-2d-fresh "$GODOT" --headless --path game res://app/Main.tscn --fixed-fps 60 -- --step4-smoke-test
```

## 真实变更边界与下一步

纯core9个文件、三份关卡、B UI/存档/美术及A的IntegratedUi桥接与2D基准逐字节一致。修改AppController、GameSession、原3D骨架和project.godot；新增Main3D.tscn、GemMeshes和3D检查。当前不能因为核心文件未改就预报最终CCR=0：最终核心范围、基线标签和真实统计仍须核对。

A下一步：按用户实际截图/试玩调整3D显示、相机和反馈；补齐正式2D冻结与最后3D验收。B下一步：在独立分支复用现有页面检查3D窗口/HUD/结算/重启，不重写匹配；记录真实人工结果。当前仅发布独立3D分支和草稿PR，主分支保留2D，没有创建3d-final。

完整AI原始导出/双方姓名/组号仍可并行整理；本页和价值索引不替代完整对话。
