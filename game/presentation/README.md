# 棋盘表现层（成员A）

2d/GameplayLoop.tscn使用真实GameSession、三关配置、木箱受击/消失、交换/回退/清除/下落/补充与本次三星结果。1/2/3为开发关卡直测，R重试，通关后N下一关；不写存档。历史BoardPrototype.tscn仍只验证选择和邻接请求。

BoardView2D实现IBoardPresenter，仅显示核心计算的BoardStep（含DamagedBoxes），不复制匹配、伤害、星级或胜负规则。AppController拥有它的应用生命周期：UI换页前取回，进入游戏后挂入新的BoardHost，防止临时页面QueueFree误删棋盘。

B负责正式HUD/菜单/结算与存档；其PR #5中的贴图、Alpha和CrystalGardenBoardLayout校准必须在合并时保留，与A受击/层数显示逐项整合。3D在完整2D验收冻结后开发，BoardView3D当前仍为空骨架。
