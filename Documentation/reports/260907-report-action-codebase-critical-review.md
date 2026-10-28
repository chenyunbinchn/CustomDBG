# Action codebase 与 ResolveEffectAction 辩证阅读
日期：2026-09-07
性质：静态代码审阅与讨论记录；未修改运行时代码，未运行 Unity 测试。

结论：支持延迟解析与纯值结果传递，但应先明确调度和结果语义，再逐步扩展效果语言。Action 类型数量不是复杂度或覆盖度的可靠指标。

代码依据与判断：
1. BattleCommandApi.ExecutePlayCard 在命令展开时调用 EffectApi.Translate；后者同时解析目标、消耗随机数、创建 Action 并尾插。拆成工厂与调度入口有价值。
2. GameActionManager.ActionQueue 是公开 List；Add 负责 recording。新增 InsertNext 时应封装队列写入口，并定义批量顺序、连续插入顺序以及生成 ID 与执行序不同的问题。
3. HookSystem.Fire 经 Translate 尾插。Documentation/rules/260718-rule-hook-system.txt 第 7 节明确默认尾插，头插为后续扩展。Resolver continuation 与 Hook reaction 的优先级需要明确；不能把所有反应改成头插就视为解决。
4. DamageAction 的 BeforeDamage 只排队，当前伤害不等待反应；InsertNext 只能抢在下一 Action 前，不能抢在当前 Action 剩余逻辑前。若需要本次伤害前防御，应明确修正阶段或拆分执行阶段。
5. DamageAction 的 hpLoss = finalAmount - blocked，未限制为目标剩余 HP；它表示穿透格挡的伤害，未必是实际 HP 差值。应先定义结果契约。
6. 报告要求仅成功 Action 写结果，但选择取消后又允许读取取消结果，存在契约缺口。应明确取消是正常 ChoiceResult，还是也发布终态结果。
7. 循环体复用同一结果槽与同 Scope 槽唯一规则需要通过 iteration frame、子 scope 或显式结果集合协调。跨回合 scope 寿命也未定义。
8. 原子 DamageAction 仍在执行时读取 Power、Weak、Vulnerable。Resolver 固定基础输入与原子 Action 计算最终结果应区分，不能把冻结整数解释为提前冻结所有战斗修正。
9. ActionExecutor 每步 EvaluateOutcome，BattleState 将终局锁定，但执行器仍继续排空正常队列。胜负判定、反应与收尾的关系应在扩展前确定。
10. BehaviorRecorder 的 ActionRecord 是类型、ID、Message 等诊断记录；DrawCardAction 摘要未记录抽到的具体卡牌 ID。纯值 DTO 和字符串摘要不等于已经实现存档恢复或完整回放。
11. 报告回响斩示例的每击杀一次追加一次效果不能仅实现为 kills > 0 再跑一轮；多击杀的待执行次数需显式定义。

建议：保留 GameAction.Execute 的局部执行职责，不将所有执行逻辑搬到 Executor；把 Executor 保持为统一生命周期和调度设施。第一步验证 Draw -> If -> GainBlock，并覆盖抽牌反应和空结果。第二步验证选择及取消；第三步验证多击杀循环和帧作用域；跨回合调度后置。
