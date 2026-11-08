# 第5步A交付：2D冻结前资料与规则参考（尚未冻结）

成员A @1723358607-rgb；日期2026-11-09，新加坡。本轮读取GitHub实际分支与开放PR：main仍为072129e，member-a为8209843，B没有提交新UI/存档；仅归属PR #7仍开放。工作前本地无修改，已将main快进合入本地member-a，没有重开未完成合并、没有丢弃先前stash/备份。

## 已完成

- 核对并整理 [实际类图](step-5-class-map.md)、[设计理由与真实边界](step-5-design-notes.md)。纯core为9文件、21公开类型，源码实际清单见source manifest，未把文件数或record数冒充class数。
- 增加测试命令 `--rule-reference`，仅在显式命令下运行；正式F5行为未变。它读取本仓库第4步既有操作档案，用实际GameSession执行，RecordingPresenter记录每个核心步骤，不重新写一套规则，不接触玩家存档。
- 保存3个开局、16次有效交换、217个中间表现步骤和结果的逐格JSON。两个独立运行输出完全一致；比较脚本再与档案逐项比较通过。
- 保存源码SHA256、引擎/.NET版本、实际回归日志与未满足项。为以后3D对比保留明确参考；它不是最终2d-baseline标签。
- 整理 [双方验收单](step-5-acceptance.md)，人工项保持待验，没有代替两位学生或队友声称已通过。

未修改B的ui/、progress/、assets/、tests/ui；未修改纯core、GameSession、关卡、2D显示或3D骨架。没有开始3D、没有创建或移动标签。

## 实际验证

本机macOS arm64，Godot4.7.2.stable.mono.official.ed1daf0bf，.NET SDK10.0.302。C#构建0警告0错误（日志注明来自真实终端输出）；参考回放两次退出0，完整JSON一致；第0/1/2/3/4步与B原独立演示均退出0。原始输出见logs/step5-*.log。

`step-5-check-summary.json`和`step-5-source-manifest.json`分别记录实际检查和源码指纹；后者明确是未冻结候选，包含本次测试入口的改动。Godot编辑器导入、实际图形窗口、音效、独立导出包本轮未重测；此前云端图形截图与环境异常仍保留在第4步记录。本轮无窗口UI测试含实际viewport事件路径，但不等于双方手动试玩。

复现（仓库根目录，GODOT指向匹配版本可执行文件）：

```sh
cd game
dotnet build AP6412Match3.sln --no-restore -p:NuGetAudit=false -p:UseSharedCompilation=false
cd ..
"$GODOT" --headless --path game --fixed-fps 60 -- --rule-reference
python3 tools/verify-2d-reference.py --godot "$GODOT"
```

预期RULE_REFERENCE_PASS及REFERENCE_COMPARE_PASS。测试依赖完整仓库的docs/evidence/member-a/step-4-replays.json，不是独立导出包入口。不要在未来规则变化时为过检查覆盖旧档案，应定位真实差异、保留历史并另存新参考。

## 玩家现在验收什么

继续打开同一个game/project.godot，构建后F5；按第4步逐格路径走三关，检查开始/选关/胜负/返回/快速点击。第一关两次操作后应剩13步三星；第二关5次剩15步三星；第三关9次剩15步三星。人工验收表中的A/B栏只记录各自实际操作。

当前关闭后进度会清空，这是RAM适配器限制。不要把关闭后未保存当成本轮新故障，也不要把它标成最终存档通过。

## 冻结门槛与队友下一步

按共同WORKFLOW第5步，正式UI/存档和两人运行检查完成后才能冻结。当前：

| 条件 | 实际状态 | 下一动作 |
|---|---|---|
| 三关真实玩法与A资料 | 自动验证/整理完成 | 用户按表人工试玩 |
| B页面/素材合并 | 已有并保留原提交 | B保留实际视觉工作 |
| 正式ProgressStore | 没有场景或实现 | B实现分模式存档、缺失/损坏容错、最佳/解锁 |
| B原版选关/结算绑定 | 仍待完成 | B成套提交真实progress/result及导航，不能只加空Store壳 |
| 双人环境与干净副本验收 | B环境待确认，双方本轮人工记录待补 | 各自记录版本/SHA/操作/截图 |
| 完整原始AI对话 | 尚未定位导出 | 双方保存真实完整导出，精选索引不能替代 |
| 组号/姓名 | 待补 | 两位学生补真实信息 |

A可做部分已完成；第5步整体尚未通过，没有2d-baseline标签。B当前最先做的是HANDOFF_UI的第1—5项。存档与正式UI交付后再联调、复测并冻结；不能以当前RAM候选直接宣称完整2D作业完成。

完整AI原始日志仍待实际导出；本页不是逐字记录。价值索引见ai-logs/member-a/index.md。提交/PR以本轮GitHub真实历史为准。
