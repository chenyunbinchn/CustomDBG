# 暂存区 GameAction 改动回顾

日期：2026-10-28。范围：git diff --cached，相对 HEAD 287949a。共 48 个文件，1743 行新增、262 行删除。

目的：落实 2026-09-03 的 GameAction 缺口审计，使现有战斗动作具有明确、可验证、可记录且适合多人串行模拟的执行语义。依据为暂存代码、审计报告、任务实施记录与执行规范。

| 范围 | 改动前 | 暂存后的实现 |
| --- | --- | --- |
| 伤害与状态 | 直接扣 Hp、绕过 Block；部分状态只打印日志；残影写死 2 | 先按 Power/Weak/Vulnerable 计算，再扣 Block/Hp；致死移除敌人、检查胜负；状态按实际 Value 叠层，残影合并监听器 |
| 卡牌生命周期 | 效果期间牌仍在手中；消耗、弃牌为占位 | Hand -> Play -> Effects -> Discard/Exhaust；使用 CardInstanceId；多牌操作先完整校验 |
| 玩家与目标 | 抽牌和回能依赖命令玩家；群体和随机目标退回 picked | 显式玩家与来源；集中解析、验证目标；确定性多目标顺序与独立 combat RNG |
| 抽牌 | 无手牌容量与明确结果 | 上限 10，满手停止，弃牌确定性洗回，记录实际抽牌与停止原因 |
| 选择 | 本地回调和手牌下标，缺少可用恢复入口 | ChoiceId、候选快照、玩家校验、ChooseCard/CancelChoice 命令 |
| 执行安全 | 异常可能令 IsRunning 永久为 true；开发断言不能保护 Release | finally 清理执行状态、Faulted/命令中断、单命令 1024 Action 上限、加载期数据校验与跨战斗清理 |
| Hook 与记录 | AfterCardPlayed 在展开时提前收集；缺少动作结果 | 出牌收尾实际执行时触发；补充结果 Hook；recording schema v2 记录结果、取消、失败和 ChoiceId |

新增 GameActionTests 中 13 个测试方法，覆盖数值、牌堆、状态、选择与参数校验。

验证边界：本次仅静态核对暂存差异，没有重新编译或运行 Unity 测试。2026-09-04 实施记录声称主工程和 Editor 工程编译通过，diff 检查通过；Unity EditMode Test Runner 当时因编辑器占用未完成，因此不能称为测试已通过。

仍待后续：网络/房间层自动提交选择超时命令；多选与随机/全手牌目标的数据模型、弃牌 Reason；状态回合衰减与移除；额外格挡 modifier；完整状态 hash、网络回放集成、逐卡动画。

原始依据：Documentation/reports/260903-report-game-action-gap-audit.txt；Documentation/tasks/260903-task-complete-game-actions.txt；Documentation/documentations/260904-game-action-execution-standard.txt。
