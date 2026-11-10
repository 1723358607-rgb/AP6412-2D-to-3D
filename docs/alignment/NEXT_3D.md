> 本轮按用户“下一步”已在独立codex/member-a-3d实现可试玩3D；见docs/evidence/member-a/step-6.md。main保留真实2D对照a067c5f。人工2D验收未收到，正式标签未创建，因此当前是隔离试作，不能把下方原计划的冻结前提说成已完成。下一项是实际画面/试玩确认及正式基线收尾。

# 下一阶段：冻结后执行的3D任务

目前只是实现计划，未创建2d-baseline或3D功能。进入条件：联合2D候选通过双方本版三关/重启人工验收，核对main实际SHA并冻结标签。AI日志、姓名/组号并行补，不冒充已归档。

## A负责

1. 从实际2d-baseline继续，在presentation/3d实现真正的棋盘/棋子/木箱Mesh、相机、灯光和动画；不能用2D贴图面片冒充全部3D模型。先用可辨识几何模型完成运行，再替换素材。
2. BoardView3D实现现有IBoardPresenter：SetBoard/SetInteractive/ClearBoard/PresentAsync。使用同一BoardStep和行列数据；不重写匹配、补充、木箱或星级，不让物理模拟决定规则。
3. AppController按应用模式选择表现器；GameSession目前mode固定2d，需要真实配置化时如实记录改动。2D仍可从原标签独立运行。
4. 将3D区域挂载到现有BoardHost，处理相机射线→行列映射；验证每格中心/边界、UI和弹窗遮挡，不让按钮误触棋盘。
5. 相同种子与layout-v2-replays逐项对比稳定格子、箱层、步数、目标、RNG与结果；现有16交换/193步骤是规则参考，仍需真实3D表现/点击检查。记录新增/修改类，不能预报CCR=0。

## B同时负责

复用现有页面/ResultScreen和ProgressStore；由应用传入3d模式，读取独立crystal_garden_progress_3d.json。2D/3D最高成绩不得混用。检查3D区域与HUD的遮挡、选关/结果/重试/返回流程，提供自己实际试玩记录；不重算规则或修改A核心。

## 完成判断

三关真实3D可玩、完整页面和分模式存档工作，同序列规则结果可核对。两版标签、类图和实际变更统计可追踪；未完成前不创建3d-final，不移动旧标签。
