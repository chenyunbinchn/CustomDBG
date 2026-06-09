# 当前项目欠缺模块报告

日期：2026-06-07

## 范围

本报告基于当前 `Assets/Scripts` 下的代码结构，重点评估卡牌、战斗、状态、UI 与系统层之间已经存在的能力，以及继续推进类 Slay the Spire 战斗玩法时仍缺失的核心模块。

## 当前已有基础

项目已经具备以下基础：

- 卡牌定义：`CardDefinition`、`CardDefinitionAuthoring`、`CardDefinitionLibrary`、`CardDefinitionManager`。
- 卡牌实例：`CardInstance`、`CardInstanceId`、`DeckManager`。
- 卡牌效果数据：`CardEffect`、`CardEffectAuthoring`、`EnumCardEffectType`、`EnumTargetType`。
- 战斗牌堆状态：`BattleCardState`，包含抽牌堆、手牌、弃牌堆、消耗堆、打出堆。
- 全局状态雏形：`GameState`，持有玩家状态、战斗牌堆和卡牌定义管理器。
- 卡牌 UI 雏形：`CardView`，已经支持绑定、悬停、拖拽和释放，并预留了 `CardPlaySystem.TryPlayCard` 调用点。

这些内容足以支撑“开始写第一版战斗闭环”，但还不足以支撑完整战斗。

## P0：必须优先补齐的模块

### 1. BattleState

当前只有 `BattleCardState`，没有“一场战斗”的整体状态。

建议新增：

- 当前战斗阶段：战斗开始、玩家回合、敌人回合、胜利、失败。
- 回合数。
- 玩家战斗数据。
- 敌人列表。
- 当前选中目标。
- 战斗结束原因。

没有这个模块时，所有系统都会被迫依赖 `GameState` 或 UI 状态，后续会很难维护。

### 2. CombatActorState

当前 `PlayerState` 只有数组形式的 `Hps`、`Energies`、`Golds`，缺少战斗单位模型。敌人也没有状态模型。

建议抽象统一的战斗单位：

- `CombatActorId`
- 阵营：玩家、敌人。
- `Hp`、`MaxHp`、`Block`
- 存活状态。
- 状态效果列表。
- 敌人意图。

玩家和敌人都可以基于这个模型参与伤害、格挡、状态、目标选择。

### 3. BattleCardPileSystem

`BattleCardPileSystem` 当前为空，但牌堆规则是卡牌战斗的基础。

至少需要：

- 初始化战斗牌堆。
- 洗牌。
- 抽牌。
- 弃牌。
- 消耗。
- 手牌进入打出区。
- 打出区进入弃牌堆或消耗堆。
- 回合结束时弃掉手牌。
- 手牌上限。

外部系统不应直接改 `BattleCardState` 的列表，应全部通过该系统移动卡牌。

### 4. CardPlaySystem

`CardView` 已经预留了出牌调用点，但没有实际系统。

该系统负责：

- 校验是否在玩家回合。
- 校验卡牌是否在手牌。
- 校验费用。
- 校验目标。
- 扣除能量。
- 移动牌堆。
- 调用效果结算。
- 判断打出后进入弃牌堆、消耗堆或其他区域。

这部分不应写在 UI 里。

### 5. CardEffectResolver

当前 `CardEffect` 只是数据，缺少解释执行者。

建议第一版支持：

- `DealDamage`
- `GainBlock`
- `DrawCards`
- `ApplyStatus`
- `GainEnergy`
- `ExhaustSelf`

每个效果应通过目标解析器找到目标，再调用伤害、格挡、状态等系统。

### 6. TargetResolver

`EnumTargetType` 已经有 `User`、`AllEnemy`、`SelectedEnemy`、`RandomEnemy`、`SelectedAlly`、`AllAllies`、`Self`，但缺少解析逻辑。

该模块负责：

- 判断目标是否合法。
- 将 `SelectedEnemy` 转换为一个敌人。
- 将 `AllEnemy` 转换为敌人列表。
- 将 `RandomEnemy` 转换为随机敌人。
- 过滤死亡、不可选或不可命中的单位。

没有目标解析器时，卡牌效果和 UI 会混在一起。

## P1：战斗闭环稳定后补齐

### 1. TurnSystem

需要明确回合生命周期：

- `StartBattle`
- `StartPlayerTurn`
- `EndPlayerTurn`
- `StartEnemyTurn`
- `ExecuteEnemyTurn`
- `EndEnemyTurn`
- `EndBattle`

回合系统应负责能量恢复、抽牌、清格挡、弃手牌、敌人行动等流程。

### 2. DamageSystem

不要把伤害逻辑直接写在卡牌效果里。

建议包含：

- 基础伤害。
- 格挡抵消。
- 扣血。
- 死亡判定。
- 多段伤害。
- 对全体目标伤害。
- 后续扩展：易伤、力量、虚弱、伤害来源。

### 3. StatusSystem

当前有 `EnumCardStatusType`，但没有状态的实际数据结构和生命周期。

建议：

- 状态类型。
- 层数或持续回合。
- 回合开始触发。
- 回合结束衰减。
- 伤害/格挡/费用修改。

第一版可以先只支持 `Weak`、`Vulnerable`、`Power`。

### 4. EnemyIntentSystem

敌人需要在玩家回合展示意图，并在敌人回合执行。

建议拆成：

- `EnemyDefinition`：敌人静态数据。
- `EnemyMoveDefinition`：行动数据。
- `EnemyIntent`：当前展示给玩家的意图。
- `EnemyAiSystem`：选择下一步行动。

第一版可以先做固定循环或随机选择，不必立刻做完整状态机。

### 5. BattleRuleSystem

集中判断战斗规则：

- 所有敌人死亡则胜利。
- 玩家死亡则失败。
- 出牌后是否立即结束战斗。
- 敌人死亡是否从可选目标中移除。

### 6. BattleEvent / Hook System

如果后续要做遗物、能力、关键词、状态联动，需要事件系统。

可先定义简单事件：

- `BeforeCardPlayed`
- `AfterCardPlayed`
- `BeforeDamage`
- `AfterDamage`
- `AfterCardDrawn`
- `TurnStarted`
- `TurnEnded`

不要第一版就做复杂 Hook，但要给未来留接口。

## P2：内容生产和长期维护相关

### 1. Authoring Validation

当前 `CardDefinitionManager` 会校验效果数组非空，但还需要更多编辑器期校验：

- `idName` 不能为空。
- 卡牌 ID 不重复。
- 费用不能小于 0。
- 目标类型和效果类型是否匹配。
- 需要目标的卡牌是否配置了可选目标。
- 图片是否缺失。

### 2. Runtime Lookup

`DeckManager` 当前只有 `List<CardInstance>`，缺少按 `CardInstanceId` 查找的方法。

建议增加：

- `Get(CardInstanceId id)`
- `TryGet(CardInstanceId id, out CardInstance instance)`
- 防止重复实例 ID。

### 3. Seeded Random

`tools.Random` 目前为空。战斗洗牌、随机目标、敌人 AI 都需要可复现随机。

建议拆分随机流：

- 洗牌随机。
- 敌人意图随机。
- 随机目标。
- 地图/奖励随机。

### 4. UI Binding Layer

`CardView` 目前能显示卡牌和拖拽，但缺少和战斗状态的同步层。

建议新增：

- `HandView`
- `PileCounterView`
- `ActorView`
- `EnemyIntentView`
- `BattleHudView`

这些视图只订阅状态变化，不直接执行业务规则。

### 5. Test Coverage

战斗系统必须有纯 C# 测试，否则后续会很难验证。

优先测试：

- 抽牌和洗牌。
- 出牌扣费。
- 目标非法不能出牌。
- 伤害先扣格挡再扣血。
- 回合结束弃手牌。
- 敌人死亡后无法被选中。

## 推荐实现顺序

1. `BattleState`
2. `CombatActorState`
3. `DeckManager.Get/TryGet`
4. `BattleCardPileSystem`
5. `TargetResolver`
6. `DamageSystem`
7. `CardEffectResolver`
8. `CardPlaySystem`
9. `TurnSystem`
10. 敌人意图和敌人行动
11. UI 绑定
12. 测试

## 结论

当前项目最缺的不是卡牌数据，而是战斗运行时模型和系统边界。下一步应优先完成纯 C# 的战斗闭环，让 UI 只作为输入和展示层。只要 `BattleState`、`BattleCardPileSystem`、`CardPlaySystem`、`CardEffectResolver`、`DamageSystem` 这几块成型，项目就能进入可迭代状态。
