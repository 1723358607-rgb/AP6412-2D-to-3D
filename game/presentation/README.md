# 棋盘表现层（成员 A）

第1步提供2d/BoardPrototype.tscn，可F6独立运行。BoardView2D显示7×7棋盘并映射点击；BoardPrototypeRunner是A的隔离开发入口，含范围说明和R/Esc测试操作。当前只有选择/请求高亮，没有实际交换/消除动画。2D冻结后再开发真正3D表现，已有BoardView3D仍为空骨架。

不要在表现层复制匹配、星级或胜负规则。正式 HUD、菜单和结算属于成员 B 的 `ui/`，本目录只负责棋盘本身。
