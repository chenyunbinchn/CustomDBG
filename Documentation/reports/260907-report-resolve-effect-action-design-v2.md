# ResolveEffectAction 设计重写报告：数据结构、执行顺序与结果传递

日期：2026-09-07  
状态：Proposal / 设计讨论稿  
范围：解释和论证新增效果解析层，不实施运行时代码或 JSON schema 修改。

本文重新组织原报告，作为后续讨论的主要阅读版本；保留原报告供追溯。文中的新增类型、字段和 JSON 均为建议，不代表项目已经实现。本文是分析报告，不替代现行执行规范。

## 1. 目标与核心结论

Action System 把一个合法战斗命令转成有顺序的状态操作，并处理等待选择、触发反应、结果和故障。新增 Resolver 的目标，是让后续操作能够依赖前序操作执行后的状态与结果。

例如“抽一张牌，然后造成等同于手牌数的伤害”，必须在抽牌完成后读取手牌数，不能在出牌命令展开时把后续数值全部算完。

建议采用：

- 效果树保存卡牌逻辑及其嵌套关系。
- 一个共享的 EffectResolver 服务解释节点，不为每层效果创建 Resolver 对象。
- ResolveEffectAction 在队列中保存“何时解释哪个节点”。
- 原有 Action 队列保存后续执行顺序。
- EffectExecutionScope 保存来源、选定目标和结果槽。
- 循环额外使用明确的 LoopFrame；第一版不建立通用解释器调用栈。

Resolver 能生成原子 Action，也能生成后续 ResolveEffectAction。读取当前手牌数等同步查询不需要另建 RequestAction。抽牌、玩家选择等会改变状态或需要等待的操作，必须先真正执行，再由后续 Resolver 继续解析。

## 2. 当前实现与新增内容

| 部分 | 当前实现 | 本文建议 |
|---|---|---|
| 命令入口 | BattleCommandManager / BattleCommandApi；出队时验证 | 保留入口，复杂效果改为排入根 ResolveEffectAction |
| 效果数据 | 单层 Effect[]，固定 Value | 增加 EffectProgram、节点、表达式和条件 |
| 翻译 | EffectApi.Translate 同时解析目标、构造 Action、尾插 | 拆出构造工厂，让解析和入队可以分离 |
| 调度 | GameActionManager.Add 尾插；公开 List | 封装写入口，增加保序批量 InsertNext |
| 执行 | GameAction.Execute(context)，Executor 管理生命周期 | 保留，增加结果发布和作用域生命周期 |
| 结果 | 各 Action 的 Result 属性及文字摘要，结构不统一 | 增加统一纯值结果协议与输出绑定 |
| 选择 | 单选，可取消，通过命令恢复 | 分阶段增加选择结果、候选快照、多选 |
| Hook | 默认尾插普通 Action | 首版保留，明确其与 Resolver 的相对时序 |
| 记录 | Action 类型、ID、Message 等诊断信息 | 增加结构化解析决定、绑定和结果数据 |

当前结果规范见 ../documentations/260904-game-action-execution-standard.txt。Hook 入队规则见 ../rules/260718-rule-hook-system.txt。本文提出的扩展与规则调整必须在实施时同步规范，不能假定已生效。

## 3. 术语与数据结构：先定义，再使用

下面先给出全部主要结构。第 4 节以后使用的接口、循环继续步骤和结果引用均在本节定义。

### 3.1 Command、Effect、Action 与 Result

- BattleCommand：玩家请求，例如“玩家 0 用卡牌 12 指定敌人 3”。合法性尚需验证。
- EffectNode：效果数据，例如“对指定敌人造成手牌数点伤害”。
- GameAction：具体领域操作，例如“玩家 0 对敌人 3 执行基础值为 5 的伤害”。
- ActionResult：操作实际完成后产生的事实，例如是否击杀、实际抽到了哪些牌。

“原子 Action”指一个可独立执行和记录的领域操作，不表示只写一个字段，也不表示具备数据库事务回滚。DamageAction 可以同时处理格挡、生命与死亡。

### 3.2 标识类型

| 标识 | 含义及作用域 |
|---|---|
| ActionId | 现有 Action 身份；由队列管理器分配，不等同于执行序号 |
| RootCommandId | 整条结算链归属的命令 ID |
| ActionEntityId / CardInstanceId | 现有实体与卡牌实例身份，禁止用 UI 下标替代 |
| EffectProgramId | 一份不可变效果程序的标识，结合内容版本解析 |
| EffectNodeId | 程序内唯一节点编号，建议加载期按定义顺序分配 |
| EffectScopeId | 一次效果调用或一轮循环的运行时作用域身份 |
| ResultSlotId | 一个作用域内的结果槽名称或编号 |
| LoopFrameId | 一次循环调用的运行时身份 |

新增运行时 ID 由战斗内确定性分配器产生，不能使用时间戳或随机 GUID 影响执行。NodeId 只在对应 ProgramId 和内容版本下有意义。

### 3.3 EffectProgram：可嵌套的效果树

概念字段：

```text
EffectProgram
    ProgramId
    ContentVersion
    RootNodeId
    Nodes: EffectNodeId -> EffectNode
```

作者可以编写内嵌 JSON 树；加载器将树转换为不可变节点表。运行时 Action 只保存程序和节点标识，不复制整棵树。

EffectNode 是显式 nodeType 的带标签数据结构：每种节点只允许自身所需字段。普通子节点引用禁止成环；循环必须使用显式循环节点。

| nodeType | 主要字段 | 语义 |
|---|---|---|
| Sequence | children | 按顺序安排各子节点，暂不求值子节点 |
| Primitive | effectType、target、value、可选 resultSlot | 当前时刻解析参数，生成一个或多个原子 Action |
| If | condition、then、可选 else | 当前时刻判断，只安排选中的分支 |
| ChooseCards | player、candidates、minCount、maxCount、allowCancel、resultSlot | 当前时刻确定候选快照，生成选择 Action |
| Repeat | count、body、maxIterations | 进入时求一次次数；各轮顺序执行 body |
| RepeatOnKills | body、damageResultsSlot、maxIterations | 先执行一轮；每次直接击杀增加一次待执行轮次 |

Sequence / Primitive / If 是第一阶段核心。ChooseCards 和两种循环分阶段加入。

本版不定义任意 WhileResult、ForEachTarget、Schedule 节点。群体伤害可由 Primitive 展开；通用循环和跨回合效果留到已有具体语义验证后扩展。未知 nodeType 必须拒绝加载，不能接受一个尚无执行定义的名字。

RepeatOnKills 是通用的“击杀增加轮次”控制结构，不按卡牌名称分支；它只统计 body 明确输出的伤害结果，不自动把整个 Hook 链的击杀算进去。

### 3.4 ValueExpression、ConditionExpression 与 TargetSelector

ValueExpression 返回整数。初版支持：

| expressionType | 字段 | 返回 |
|---|---|---|
| Constant | value | 常量 |
| HandCardCount | player=Source | 指定玩家此刻的手牌数 |
| CurrentBlock | actor=Source | 指定实体此刻的格挡 |
| Add / Multiply | left、right | 两个子表达式的整数运算结果 |
| ResultInt | resultSlot、field | 已完成结果的受支持整数属性 |

ConditionExpression 返回布尔值。初版支持：

| conditionType | 字段 | 返回 |
|---|---|---|
| ResultBool | resultSlot、field | 已完成结果的受支持布尔属性 |
| Compare | left、operator、right | 整数比较；operator 为固定枚举 |
| And / Or | children | 从左到右短路求值 |
| Not | child | 子条件取反 |

field 必须是按结果类型校验的固定字段枚举，不通过反射调用任意属性。“没有结果”不同于值为 0 或 false。

TargetSelector 描述待解析目标：Source、SelectedEnemy、AllEnemies；卡牌选择候选使用 SourceHand。ResultCards(resultSlot) 表示选择或牌堆操作结果中的卡牌 ID 列表，供弃牌等 Primitive 使用。新增目标种类逐项定义，不做隐式 fallback。

查询、表达式可以嵌套。求值实现可用普通递归或局部数值栈；它们不会自动创建 Action。表达式深度和计算步数有界；首版不允许表达式内部抽牌、等待选择或消耗随机数。

### 3.5 EffectExecutionScope 与 EffectScopeStore

Scope 是运行时数据容器，不是 Resolver，也不是 BattleState 快照。

```text
EffectExecutionScope
    ScopeId
    RootCommandId
    Source: ActionEntityId
    SourceCard: CardInstanceId?
    SelectedTarget: ActionEntityId?
    ResultSlots: ResultSlotId -> ResultSlotState
```

来源和选定目标是稳定 ID。查询手牌、格挡等状态时仍读取当前世界。X 费用等需要出牌时锁定的值，应在扩展该能力时新增明确的快照字段，不能事后用剩余能量推测支付值。

EffectScopeStore 负责创建、按 ID 查找和释放 Scope。每次卡牌效果调用创建独立 Scope；循环每轮创建独立 Scope，并复制 Source、SourceCard、SelectedTarget 等调用环境。首版结果引用只查当前 Scope，不隐式搜索父 Scope。需要跨轮汇总时由循环帧显式读取，不能意外引用上一轮同名槽。

普通分支节点沿用当前 Scope。一个槽只能由一条确定的生产路径定义一次；第一版加载器保守拒绝同 Scope 中重复声明，包括不同分支复用同名槽。需要复杂分支结果合流时另增明确结构。

### 3.6 ActionResult、ResultSlotState 与 ResultRef

ActionResult 是不可变纯值 DTO 的带标签联合，包含 kind 和对应 payload，不保存实体对象、Action 对象、委托或可被外部修改的 List。

| kind | 建议 payload |
|---|---|
| Damage | FinalAmount、Blocked、UnblockedDamage、ActualHpLoss、WasFatal、TargetWasGone |
| Draw | Requested、DrawnCards[]、ReshuffleCount、StoppedReason、TargetWasGone |
| Choice | ChoiceId、SelectedCards[]、Canceled |
| Discard | DiscardedCards[]、ActualCount |

当前 DamageResult.HpLoss = FinalAmount - Blocked，可能大于目标原本 HP。新增结果应区分 UnblockedDamage 与 ActualHpLoss；例如目标 3 HP 承受 10 点无格挡伤害，两者分别为 10 和 3。不能把旧字段无说明地当作实际生命差值。

集合在发布时复制并冻结。只存卡牌 ID 表示身份；如果条件要判断“抽到时的类型”而不是“现在的类型”，必须另存抽取时属性快照，不能默认两者相同。

ResultSlotState 有以下形态：

```text
PendingSingle(expectedKind)
ReadySingle(ActionResult)
PendingList(expectedKind, expectedCount, completedItems)
ReadyList(ActionResult[])
```

单目标 Primitive 输出 Single。AllEnemies 输出按稳定目标顺序排列的 List；0 个目标立即产生 ReadyList([])。各伤害结果通过 itemIndex 填入，全部完成才把列表标记 Ready。不能让多个 Action 互相覆盖同一标量槽。

ResultRef 的逻辑地址为 (ScopeId, ResultSlotId)。作者 JSON 只写 resultSlot，运行时由当前 Scope 补齐 ScopeId。读取未就绪、缺失或类型不匹配的槽为故障；合法的空列表不是缺失结果。

### 3.7 IActionResultProvider 与 ActionOutputBinding

IActionResultProvider 是新增接口，让 Executor 用统一方式取得具体 Action 的结果。它不负责往 Scope 写入，也不是事件监听器。

概念接口，非可直接编译的完整实现：

```csharp
public interface IActionResultProvider
{
    ActionResult GetResult();
}
```

调用约束：

- 仅在 Action 正常结束，或明确支持发布结果的用户取消结束后调用。
- Action 尚在执行、等待选择或已经 Faulted 时不能读取。
- 必须返回与预声明 kind 一致的纯值结果；目标已经离场也应产生显式结果。
- 未绑定输出的 Action 可以不实现该接口；绑定输出却无接口属于配置或构造错误。

ActionOutputBinding 指定结果写到哪里：

```text
ActionOutputBinding
    ScopeId
    ResultSlotId
    ItemIndex?       // 列表结果中的位置；Single 时为空
    ExpectedKind
```

绑定作为排队步骤的执行元数据保存，原子 DamageAction 不必理解 Scope 或槽名称。

用户合法取消选择仍保持现行 Canceled 状态，但发布 ChoiceResult(Canceled=true, SelectedCards=[])。Executor 对这种明确的取消路径发布结果后记录 ActionCanceled，再继续后续节点。故障、强制重置和普通 Action 的异常中止不得伪装成玩家取消。

因此“只给成功 Action 落槽”不是本版规则。准确规则是：给具有有效结果的正常终态发布结果，其中包括合法取消选择。

### 3.8 EffectResolver 与受限服务接口

EffectResolver 是共享服务。输入为当前解析工作、Scope 和查询能力；输出为 ExpansionPlan。它不把一次卡牌执行进度保存在自己的实例字段中。

建议按职责组织内部实现：

| 组件 | 职责 |
|---|---|
| EffectResolver | 按节点或继续阶段派发，组合展开计划 |
| ValueExpressionResolver | 计算整数表达式 |
| ConditionResolver | 计算条件，执行短路规则 |
| TargetSelectorResolver | 当前时刻解析目标，固定集合顺序 |
| PrimitiveActionFactory | 根据已解析的参数构造具体 Action；不自行入队 |
| IEffectQueryContext | 只读查询当前战斗信息和已发布结果，不暴露可变集合 |

这些是职责划分，不要求第一天为每一种节点建立类层次。

“只读”针对战斗领域状态；随机目标若后续加入，会通过明确的 RNG 服务消耗随机流，不能宣称随机解析完全无副作用。

伤害执行仍可通过受限的 IDamageContext 读取当前目标、格挡和修正并修改生命。IDamageContext 是可选的后续依赖收窄接口，不是首版 Resolver 的前置条件。Resolver 负责基础参数，伤害领域负责执行当下的最终伤害；不能提前冻结所有状态修正。

### 3.9 ResolveWork 与 ResolveEffectAction

ResolveWork 是可保存的解析工作数据，有两个变体：

```text
EnterNode
    ProgramId
    NodeId
    ScopeId

ContinueLoop
    LoopFrameId
```

ResolveEffectAction 是现有 GameAction 的新增子类，保存一个 ResolveWork。它轮到执行时调用共享 EffectResolver，提交展开计划，然后结束；不在自身协程内等待所有子 Action 完成。

ResolveEffectAction 完成仅表示“这次展开已经完成”，不表示整棵子效果树完成。

```text
ResolveEffectAction.Execute:
    plan = EffectResolver.Resolve(Work, 查询及执行数据视图)
    校验并提交 plan
    返回
```

本文中的 R(Node, Scope) 是 ResolveEffectAction(EnterNode(...)) 的简写；C(Frame) 是 ResolveEffectAction(ContinueLoop(...)) 的简写。它们不是额外的 Action 类型。

### 3.10 ExpansionPlan 与 ScheduledAction

ExpansionPlan 是一次解析提出的执行计划：

```text
ExpansionPlan
    Steps: ScheduledAction[]            // 顺序不可变
    ExecutionDataChanges                // 创建/更新 Scope、槽或 LoopFrame
    Decision                            // 结构化解析记录

ScheduledAction
    Action: GameAction
    RootCommandId
    ParentActionId?
    OutputBinding?: ActionOutputBinding
```

ExecutionDataChanges 是受限制的执行元数据操作列表，例如预留槽、创建本轮 Scope、更新剩余轮次、释放不再使用的本轮数据；不包含生命、格挡、牌堆等战斗修改。

执行基础设施先完整校验计划的槽、类型、数量和预算，再提交元数据并保序插入步骤。示例为便于阅读会说“Resolver 创建槽”，实际通过这个提交边界完成。

ActionId 应在提交时按 Steps 顺序分配。若保留现有构造器必须传入 ID 的形式，工厂可先产生构造描述，再在提交阶段实例化。不能拿 ActionId 大小判断执行先后：后生成的子 Action 会在早生成的收尾 Action 前执行。

### 3.11 LoopFrame 与 LoopContinuation 的确切含义

LoopFrame 保存一次循环的进度：

```text
LoopFrame
    FrameId
    ProgramId
    BodyNodeId
    OwnerScopeId
    Mode: FixedCount | PerKill
    MaxIterations
    StartedIterations
    PendingIterations
    ActiveIterationScopeId?
    DamageResultsSlot?                  // PerKill 使用
```

- FixedCount 对应 Repeat，进入时求出总次数并写入 PendingIterations。
- PerKill 对应 RepeatOnKills，初始 PendingIterations=1。
- 每开始一轮，PendingIterations 减 1，StartedIterations 加 1；创建新 Scope 并记录为 ActiveIterationScopeId。
- 该轮结束后的 ContinueLoop 读取本轮结果，决定是否再安排一轮。

旧报告的 LoopContinuation 在本文中明确指 ResolveWork.ContinueLoop。它既不是作者可随意填写的 EffectNode，也不是新 Resolver 实例，更不是未说明的回调函数。

循环运行时在每轮 body 后安排 C(Frame)。由于子节点展开全部插在它前面，C(Frame) 会等本轮 body 的所有普通子步骤结束后执行。它不自动等待尾插到整条队列末端的 Hook 反应，具体限制见第 7 节。

## 4. 为什么不需要一个“存放 Resolver 的栈”

嵌套的是 EffectNode 数据；排队的是 ResolveEffectAction；解释服务只有共享实例。

```text
EffectProgram 保存：
Sequence
    DrawCards
    If
        Sequence
            Damage
            GainBlock
```

每次 R(Sequence) 只安排直接子节点，不递归求出所有子节点参数。子节点轮到时再解析，并把其步骤插到后续节点之前。这样用现有队列即可得到局部深度优先的展开顺序。

显式解释器栈是另一种可行实现。栈里应放 NodeId、子节点位置、ScopeId、循环进度等执行帧，不放 Resolver 服务对象。第一版同时维护“队列后续步骤”和一套“通用恢复栈”会增加状态同步负担，因此本文选队列方案。

表达式内部的局部求值栈与战斗调度无关：它可以计算“手牌数 × 2 + 格挡”，但不能靠压栈代替真正抽牌或等待玩家输入。

## 5. 调度协议与各节点的执行算法

### 5.1 InsertNext

当前 Action 已被 Pop 移出队列。若剩余队列为 [X, Y]：

```text
InsertNext([A, B]) -> [A, B, X, Y]
```

一次展开提交只进行一次批量插入。连续两次 InsertNext([A])、InsertNext([B]) 会得到 [B, A, X, Y]，不能把它误当成按调用顺序追加。

每个插入步骤记录 ActionQueued，所有步骤通过同一管理器入队。封装公开 ActionQueue 写权限，避免漏记、绕过预算和作用域绑定。

### 5.2 Sequence

```text
R(Sequence[A, B, C], S)
    -> InsertNext([R(A,S), R(B,S), R(C,S)])
```

这一阶段不计算 A/B/C 的动态参数。本文所有完整示例使用这个统一包装规则，不混用固定节点提前展开优化，因此 Resolver 次数可直接核对。

### 5.3 Primitive

轮到当前 Primitive 时：

1. 解析目标集合及其固定顺序。
2. 求值 value 一次；同一群体 Primitive 的所有目标共享该基础值。
3. 如有 resultSlot，预留 Single 或 List 槽及输出绑定。
4. 工厂创建每个目标的 Action，保序插入。

“对所有敌人造成手牌数伤害”是在该 Primitive 解析时读取一次手牌数；随后每个伤害 Action 自行处理执行时的格挡和伤害修正。若设计想逐个目标重新读取手牌数，必须增加明确的逐目标执行结构，不能悄悄改变本节点语义。

SelectedEnemy 使用 Scope 中选定的 ID。之前合法、后来死亡的目标可交由伤害 Action 产生 TargetWasGone 的正常结果。非法身份或配置错误仍 fault。AllEnemies 在解析时为空则正常展开零个 Action，不消费无意义随机数。

### 5.4 If

只在执行到 R(If) 时计算条件。真则安排 R(then)，假则安排 R(else)；else 缺省表示不插入任何步骤。And/Or 按从左到右短路，未选中的分支不查询状态、不读取槽、不消耗随机数。

### 5.5 ChooseCards

解析到节点时才读取候选牌堆并冻结候选 ID 列表。选择 Action 等待期间不执行新的普通根命令。收到输入后验证 ChoiceId、玩家、唯一性、数量和候选成员资格。

本版建议 Mandatory 选择采用“可选数量不足时选择全部可用牌”的数量规则：effectiveMin=min(minCount, candidateCount)，effectiveMax=min(maxCount, candidateCount)。空候选直接发布未取消的空选择结果，不进入永久等待。该规则必须记录并校验 minCount <= maxCount；当前构造器拒绝空候选，需要扩展后才能使用。

合法取消产生 ChoiceResult；后续 If 显式决定跳过哪个分支。首版取消不会退款，也不会自动清空整张牌的剩余队列。

### 5.6 Repeat

进入节点时求 count 一次，要求 0 <= count <= maxIterations。创建 FixedCount 帧。

count=0：直接结束。否则安排一轮：

```text
创建本轮 Scope S_i
更新帧计数
InsertNext([R(body,S_i), C(frame)])
```

C(frame) 检查本轮正常结束；清理本轮 Scope。如果 PendingIterations > 0，安排下一轮；否则释放帧。各轮同名结果槽位于不同 Scope，互不覆盖。首版循环体外不能直接读取本轮局部槽，循环汇总输出需要另行定义。

### 5.7 RepeatOnKills

进入节点时创建 PerKill 帧，待执行轮次为 1。每轮 body 必须在本轮 Scope 写出 damageResultsSlot 指定的 Damage 列表；加载器验证类型和所有正常路径均产出该结果。

C(frame) 执行以下算法：

```text
results = 读取本轮已完成 Damage 列表
kills = results 中 WasFatal=true 的项数
PendingIterations = checked(PendingIterations + kills)
清理本轮 Scope
若 PendingIterations == 0：释放帧，结束
否则：检查上限，再安排一轮 body 和 C(frame)
```

每开始一轮都会先扣除一个待执行轮次。因此一次击杀 2 个敌人会增加 2 轮，不能仅实现为“kills > 0 就再跑一轮”。

StartedIterations + PendingIterations 不得超过 maxIterations；超限为明确故障，不能静默截断并宣称卡牌成功。空目标本轮输出空列表，不新增轮次；已有待执行轮次仍按上限内规则耗尽。

## 6. 结果发布与 Scope 生命周期

具有输出绑定的 Action 正常结束后，Executor 按以下协议处理：

1. 取得并校验 IActionResultProvider.GetResult()。
2. 按既有执行规则检查战斗结果；若此时发生故障，不发布正常结果。
3. 把纯值结果写入绑定槽；List 槽只有全部项完成后才 Ready。
4. 确定 Finished 或合法选择 Canceled 的终态，记录结果与终态。
5. 才开始执行下一个 Action，包括下一个 Resolver。

合法选择取消沿用当前不做普通成功操作检查的路径，但仍校验并发布 Choice 结果。发布失败是战斗故障，不能先记录 ActionCompleted 再假装结果存在。

故障不提供默认结果继续执行，也不自动回滚已经发生的战斗修改。队列不得继续处理依赖故障结果的 Resolver。

Scope 在以下时机释放：

- 循环本轮 Scope：C(frame) 已读取所需结果，且没有其他排队步骤引用它后。
- 根 Scope：整条根命令包括尾插反应全部结束后。
- 故障或重置：取消剩余步骤并清理其 Scope、帧和待发布绑定。

首版 Hook 仍生成原有固定效果 Action，不允许暗中持有循环局部 Scope；后续动态 Hook 应创建独立作用域或明确保留引用。不能仅在 PlayedCardToDiscardAction 执行时释放根 Scope，因为队尾可能还有反应。

## 7. Hook、读取时点与战斗终局

首版保留 Hook 默认尾插。必须接受并测试下面的具体顺序：

```text
队列：Damage -> R(If) -> Finish
Damage 触发一个尾插 Reaction
结果：R(If) -> Finish -> Reaction
```

R(If) 看到的是 Damage 完成后、Reaction 执行前的状态。LoopContinuation 同样只等待 body 的局部步骤，不等尾插反应。若玩法要求“反应全结算后再继续”，需要额外定义反应优先级和完成边界，不能声称 InsertNext 已解决。

将所有 Hook 直接头插也不是充分方案：同次多个监听者的顺序、混合尾插和头插，以及反应再次触发反应均需要成批保序规则。

BeforeDamage 当前只排队反应。即使把反应插在队头，也不能抢在当前 DamageAction 后半段前执行。若要抵挡本次伤害，必须定义同步纯修正阶段或拆分伤害阶段，这是独立的执行规则扩展。

当前每个 Action 后 EvaluateOutcome，一旦终局锁定便不重新评估，但正常队列仍继续排空。Resolver 不自动解决“最后一个敌人死亡后的反应、抽牌、选择是否还执行”。本版示例避免触发全局终局；正式支持这类内容前必须补充终局规则及测试，不将该问题掩盖在 Resolver 内。

## 8. 完整 CardDefinition 与出牌全过程

以下是假想卡牌“临机追击”。这是建议 schema 示例，不是当前可直接导入的资源。节点表及 NodeId 由加载器从内嵌树生成。

效果：消耗 1 能量；抽 1 张牌；对选定敌人造成等同于当前手牌数的基础伤害；若本次伤害击杀，获得 3 点格挡。

```json
{
  "idName": "improvised_pursuit",
  "cardType": "Attack",
  "energyCost": 1,
  "effectProgram": {
    "nodeType": "Sequence",
    "children": [
      {
        "nodeType": "Primitive",
        "effectType": "DrawCards",
        "target": "Source",
        "value": { "expressionType": "Constant", "value": 1 }
      },
      {
        "nodeType": "Primitive",
        "effectType": "DealDamage",
        "target": "SelectedEnemy",
        "value": { "expressionType": "HandCardCount", "player": "Source" },
        "resultSlot": "hit"
      },
      {
        "nodeType": "If",
        "condition": {
          "conditionType": "ResultBool",
          "resultSlot": "hit",
          "field": "WasFatal"
        },
        "then": {
          "nodeType": "Primitive",
          "effectType": "GainBlock",
          "target": "Source",
          "value": { "expressionType": "Constant", "value": 3 }
        }
      }
    ]
  }
}
```

前提：手牌 5 张包含此牌；能量 2；格挡 0；敌人 A 有 5 HP、无格挡，另有敌人 B；无伤害修正、Hook 反应或满手问题，抽牌堆至少 1 张。

定义加载后的节点别名：N0=Sequence，N1=Draw，N2=Damage，N3=If，N4=GainBlock。S 为本次根 Scope。Finish 为现有 PlayedCardToDiscardAction。

| 步骤 | 执行内容 | 执行后的队列或状态 |
|---|---|---|
| 1 | PlayCard 出队并通过验证，创建 S | HandToPlay、Cost、R(N0,S)、Finish |
| 2 | HandCardToPlayAction | 手中 5 -> 4，牌进入 PlayPile |
| 3 | CostEnergyAction(1) | 能量 2 -> 1 |
| 4 | Resolver ①：R(N0,S) | 插入 R(N1,S)、R(N2,S)、R(N3,S) |
| 5 | Resolver ②：R(N1,S) | 插入 DrawCardAction(1) |
| 6 | DrawCardAction | 手牌 4 -> 5 |
| 7 | Resolver ③：R(N2,S) | 查询手牌数得 5，预留 hit，插入 Damage(A,5) 并绑定输出 |
| 8 | DamageAction | A 死亡；发布 hit.WasFatal=true；B 存活，不触发全局胜利 |
| 9 | Resolver ④：R(N3,S) | 读取 hit，选择 then，插入 R(N4,S) |
| 10 | Resolver ⑤：R(N4,S) | 插入 GainBlockAction(3) |
| 11 | GainBlockAction | 格挡 0 -> 3 |
| 12 | Finish | 牌进入弃牌堆，触发 AfterCardPlayed；本例无匹配反应 |
| 13 | 队列排空 | 根命令完成，释放 S |

本例统一包装每个节点，因此有 5 次 Resolver，而非把固定节点提前折叠后的 3 次。两者可产生相同战斗结果，但 Action 日志和预算计数不同；首版选择可解释性更直接的 5 次，不在同一报告里混用算法。

若 A 未被击杀，步骤 9 不生成分支；若 A 已离场，伤害应发布 WasFatal=false、TargetWasGone=true。两者均不会凭空获得格挡。

## 9. 两个补充例子

### 9.1 抽牌后选择，再弃牌

```text
Sequence
    Primitive DrawCards(3, Source)
    ChooseCards(Source, SourceHand, min=1, max=1,
                allowCancel=false, resultSlot=selection)
    Primitive DiscardCards(target=ResultCards(selection))
```

DiscardCards 在这里是建议 Primitive 类型，由工厂生成现有 DiscardHandCardAction。它不需要整数 value，加载器按 effectType 验证字段。

流程：R(Sequence) -> R(Draw) -> DrawAction -> R(ChooseCards) -> HandCardChooseAction 等待 -> 收到合法选择命令 -> 发布 selection -> R(Discard) -> DiscardHandCardAction -> Finish。

候选快照是在抽牌完成后创建。实际移动前仍由弃牌 Action 验证卡牌所在牌堆；选中身份不等于永久保证可移动。空列表是明确无操作，工厂可不生成弃牌 Action。

若 allowCancel=true，应将弃牌节点放入 If(Not(ResultBool(selection,Canceled))) 的 then 分支。取消只跳过该分支，后面的正常节点和出牌收尾仍执行。

### 9.2 每击杀一个敌人追加一轮群体伤害

```text
RepeatOnKills(maxIterations=8, damageResultsSlot=roundHits)
    body:
        Primitive DealDamage(AllEnemies, Constant(10), resultSlot=roundHits)
```

假设三个敌人分别为 A=10 HP、B=10 HP、C=100 HP，没有其他修正或反应：

| 时点 | StartedIterations | PendingIterations | 行为 |
|---|---:|---:|---|
| 进入循环 | 0 | 1 | 创建帧 |
| 开始第 1 轮 | 1 | 0 | 新 Scope S1；解析目标 A/B/C，生成三次伤害 |
| C(frame) 读取 S1 | 1 | 2 | A/B 被击杀，增加 2 轮；C=90 HP |
| 开始第 2 轮 | 2 | 1 | 新 Scope S2；仅对 C 造成 10 点伤害 |
| C(frame) 读取 S2 | 2 | 1 | 无击杀；C=80 HP；待执行轮次仍为 1 |
| 开始第 3 轮 | 3 | 0 | 新 Scope S3；仅对 C 造成 10 点伤害 |
| C(frame) 读取 S3 | 3 | 0 | 无击杀；C=70 HP；释放帧，结束 |

roundHits 在 S1、S2、S3 中是三个不同地址，不冲突。C(frame) 是前文已定义的 ResolveEffectAction(ContinueLoop)，不是临时发明的新函数。

## 10. 确定性、预算与加载校验

确定性约束：

- 相同初始状态、内容版本、命令和 RNG 状态应得到相同解析决定及执行顺序。
- AllEnemies 按敌人 ID 排序；玩家集合沿用既有玩家数组顺序。卡牌候选保留规则规定的牌堆顺序，不随意重新排序牌堆。
- 随机敌人选择若加入，使用 combat RNG；既有洗牌继续使用 shuffle RNG。不能将所有随机行为统一迁移到 combat 域。
- Resolver 不读取帧时间、墙钟或 UI 状态。日志时间戳可用于观测，但不能参与模拟决定。
- 算术使用 checked，并明确整数范围；不接受脚本、方法名执行或任意反射。

至少需要三层预算：

1. 现有每根命令最多执行 1024 个 Action，包括所有 Resolver、选择与反应。
2. 循环的 maxIterations，限制正常展开轮数。
3. 解析阶段的树深度、表达式步数、单次展开数量及待入队总量，避免还没执行第一个子 Action 就分配巨大列表。

新增待入队预算与现有“执行数上限”是不同概念。具体数值应集中配置并测试；不能把执行 1024 步的检查描述为已经限制了所有解析内存消耗。

加载期能够检查：节点及字段合法性、普通引用无环、表达式类型、目标组合、结果生产者类型、同 Scope 唯一性、先产出后读取的结构关系、循环体必须输出指定列表、常量范围和静态深度。

加载期无法证明动态手牌数、运行期目标存活、每次运算不溢出或所有状态相关路径均安全。动态 count 超限、运行期类型/槽错误和不变量失败仍须确定性 fault。不要宣称所有非法条件与循环都能在加载期完全排除。

## 11. Recording 与恢复能力边界

Resolve 的 Decision 建议记录：ProgramId、ContentVersion、NodeId 或 ContinueLoop、ScopeId/LoopFrameId、条件结果、已解析数值、具体目标 ID、选中分支和展开数量。

排队记录增加 ParentActionId、RootCommandId 和输出绑定，才能定位哪个解析步骤产生了哪个子 Action。运行期结果采用结构化字段保存；ResultSummary 只负责易读显示，不能靠解析字符串供后续效果使用。

当前 ActionRecord 主要是 ActionType、ActionId、Message 等信息；它不是完整可恢复的执行快照。若将来支持中途存档或断线恢复，还需要同时保存世界状态、RNG 域状态、待执行队列、Scope、结果槽、LoopFrame、待选择数据和程序版本。

本报告只建立可保存数据的边界，不宣称已经实现网络同步、完整回放或存档恢复。Recorder 写入失败仍遵循现有规则，不影响战斗模拟；结果落槽失败则影响模拟，必须故障处理。这两个失败域必须分开。

## 12. 实施顺序与验收

### 阶段一：最小解析链

实现 Program/Node、Scope、EnterNode 工作、保序 InsertNext、Sequence/固定 Primitive；将 EffectApi 的构造与入队拆开。固定 Effect[] 可加载成 Sequence + Primitive 的兼容程序，但需测试目标解析时机变化，尤其是随机目标和前序击杀。

验收：原出牌生命周期不变；嵌套 Sequence 保序；所有子步骤都在 Finish 前；每步有 ActionQueued；公共集合不能绕过管理器写入。

### 阶段二：状态查询与结果依赖

加入 HandCardCount、If、结果联合、绑定、IActionResultProvider、Executor 发布协议。执行第 8 节完整例子。

验收：手牌读取发生在抽牌后；五次 Resolver 顺序准确；击杀/未击杀/目标离场分支正确；实际 HP 损失区分过量伤害；缺失和错误结果必须故障；不依赖 Action 对象引用。

### 阶段三：选择及群体结果

增加选择结果、空候选规则、多选校验和 Damage 列表槽。

验收：空候选不死等；取消结果可供分支读取；等待时普通根命令不启动；群体零目标发布空列表；所有列表项完成前禁止读取；重复或非法选择被拒绝。

### 阶段四：有界循环

实现 LoopFrame、ContinueLoop、Repeat 和 RepeatOnKills。

验收：每轮 Scope 独立；多击杀增加对应次数；本轮 body 完成后才继续；0 次重复不执行 body；循环与解析预算故障可重复；Scope 和帧正常结束、取消、重置后无残留。

### 阶段五：扩展前确认规则

针对顺序敏感 Hook、终局后效果、跨回合捕获与调度，另行形成具体提案并补充现有规范。ScheduleEffectAction 等名称在此仅代表未来可能的独立调度能力，不属于本报告已定义的节点或首版交付范围。

本次报告通过静态阅读形成，未运行 Unity 测试。上面的验收项是实施后应执行的测试要求，不是已经通过的测试结果。

## 13. 主要代码依据与旧报告修正索引

代码依据（项目相对路径）：

- Assets/Scripts/action/GameAction.cs：共享 Context 与 Execute 入口。
- Assets/Scripts/action/GameActionManager.cs：当前尾插与 Action ID 分配。
- Assets/Scripts/action/ActionExecutor.cs：状态、选择等待、结果检查、1024 上限与故障。
- Assets/Scripts/systems/BattleCommandApi.cs：命令验证、出牌展开和选择命令恢复。
- Assets/Scripts/systems/EffectApi.cs：目标解析、工厂逻辑与尾插耦合。
- Assets/Scripts/action/gameEffectActions/DamageAction.cs：伤害结果、修正、死亡及 Hook。
- Assets/Scripts/action/gameEffectActions/HandCardChooseAction.cs：现有单选与取消状态。
- Assets/Scripts/systems/BattlePileApi.cs：抽牌结果、牌堆顺序和 shuffle RNG。
- Assets/Scripts/hook/HookSystem.cs：反应翻译入队。
- Assets/Scripts/recording/ActionRecord.cs：当前诊断记录字段。

| 旧报告不清晰之处 | 本文对应说明 |
|---|---|
| IActionResultProvider 未定义就使用 | 3.6–3.7 定义结果、接口、绑定；第 6 节定义调用时机 |
| LoopContinuation 突然出现在例子里 | 3.9–3.11 定义工作变体与循环帧；5.7 和 9.2 给完整算法 |
| Resolver、节点、Action 混称 | 3.3、3.8、3.9 及第 4 节分别说明 |
| 没有解释嵌套与栈 | 第 4 节明确效果树与队列方案 |
| Scope 内结果槽唯一却又循环复用 | 3.5–3.6 与 3.11 定义逐轮 Scope 和列表结果 |
| 只成功落槽却希望读取取消 | 3.7 和第 6 节定义合法取消结果发布 |
| 一次多击杀的重复次数含糊 | 5.7、9.2 使用待执行轮次计数 |
| 固定整数可能被误解为冻结最终伤害 | 3.8 与 5.3 区分基础参数和执行时修正 |
| 将日志 DTO 等同于完整回放 | 第 11 节明确所缺执行快照 |
| 新增几个 Action 就等同覆盖全部卡牌 | 本文不作未经逐卡验证的覆盖数量承诺 |
