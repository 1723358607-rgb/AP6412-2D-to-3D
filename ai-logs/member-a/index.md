# 成员 A：AI 使用价值索引

本索引是导航，不是完整原始AI对话。当前只有实施记录；本轮完整原始聊天尚待从会话实际导出/保存，不伪造缺失记录。

| 日期/任务 | 设计或修正 | 本人需要检查 | 代码/证据 |
|---|---|---|---|
| 2026-11-06，第0步 | 用户指定Godot Mono和C#，替换旧GDScript接口，保留数据字段 | 双方均使用相同C#基类与实际Mono版本 | docs/alignment/CONTRACT.md |
| 同上 | 不提前实现三消或队友UI，GameSession保持真实idle且不发假胜负 | 本人运行启动页，检查范围标注 | game/gameplay/GameSession.cs、docs/evidence/member-a/step-0.md |
| 同上 | UI/进度模块可选挂载，B未交付时工程仍启动 | 队友后来提供两个固定路径的场景并验证连接 | game/app/AppController.cs、game/app/contracts/ |
| 同上 | headless测试发现独立测试Node释放警告，改为finally显式Free并复查 | 确认最终日志没有该警告；不把宿主权限错误隐藏 | game/tests/gameplay/StepZeroSmokeCheck.cs、docs/evidence/member-a/logs/ |

原始日志缺口明确保留。最终提交前需补齐从真实会话取得的逐轮用户提问和AI回复；本文件不能替代它。

## 第1步实施索引（非原始对话）

用户原文任务：“队友正在做ui，我们这边能先往下走吗？”；前置授权原文：“那每次你自动帮我进行提交、下载拉取等一系列操作”。

- 采用独立棋盘原型并保持B接口不变，避免等待UI或重复实现正式菜单。证据：docs/evidence/member-a/step-1.md。
- 选择/邻接请求与绘制分离，接受请求不伪造消除/扣步，核心数据不引用Godot。
- 实施中发现测试辅助函数括号错误及FileAccess名称歧义，修正后C#构建0警告0错误，再执行实际无窗口检查。
- 正式种子及100个样本种子开局无现成三连、请求/数据隔离和实际场景点击映射检查通过；图形与真实鼠标验收未执行。
- 原始完整会话仍待实际导出；上面引用的用户原文和实施索引不能替代完整聊天。
