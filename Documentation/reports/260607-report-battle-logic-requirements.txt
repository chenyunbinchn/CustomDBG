# 战斗逻辑需求分析与可能实现报告

日期：2026-06-07

## 目标

本报告描述当前项目中战斗逻辑应满足的基础需求，并给出一套适合现有代码结构的实现方案。目标不是一次性实现完整商业级卡牌战斗系统，而是先完成一个可运行、可测试、可扩展的第一版战斗闭环。

## 战斗逻辑的核心需求

### 1. 战斗开始

系统应能从玩家牌组和敌人配置创建一场战斗。

需求：

- 清空上一场战斗的临时牌堆。
- 将玩家牌组实例复制或移动到本场战斗抽牌堆。
- 洗牌。
- 初始化玩家生命、能量、格挡。
- 初始化敌人生命、格挡和第一回合意图。
- 抽起始手牌。
- 进入玩家回合。

建议默认值：

- 初始能量：3。
- 每回合抽牌：5。
- 手牌上限：10。

### 2. 玩家回合

玩家回合应允许玩家重复出牌，直到主动结束回合或战斗结束。

需求：

- 回合开始时清除玩家格挡，除非未来有保留格挡效果。
- 恢复能量。
- 抽牌。
- 允许拖拽卡牌并选择目标。
- 出牌时校验费用和目标。
- 出牌后执行卡牌效果。
- 出牌后进入弃牌堆或消耗堆。
- 所有敌人死亡时立即胜利。

### 3. 出牌流程

出牌是战斗系统最重要的流程，应由 `CardPlaySystem` 管理。

推荐流程：

1. 判断当前是否为玩家回合。
2. 判断卡牌是否在手牌。
3. 通过 `CardDefinitionManager` 取卡牌定义。
4. 判断玩家能量是否足够。
5. 判断目标是否符合 `EnumTargetType`。
6. 从手牌移动到打出堆。
7. 扣除能量。
8. 逐条执行 `CardEffect`。
9. 根据卡牌类型、关键词或效果决定进入弃牌堆还是消耗堆。
10. 检查战斗是否胜利或失败。

伪代码：

```csharp
public bool TryPlayCard(CardInstanceId cardId, CombatActorId? selectedTargetId)
{
    if (!CanPlayCard(cardId, selectedTargetId))
    {
        return false;
    }

    CardInstance instance = deckManager.Get(cardId);
    CardDefinition definition = definitionManager.Get(instance.DefinitionId);

    pileSystem.Move(cardId, BattlePile.Hand, BattlePile.Play);
    playerCombatState.Energy -= definition.Cost;

    foreach (CardEffect effect in definition.Effects)
    {
        effectResolver.Resolve(effect, new CardEffectContext
        {
            SourceCardId = cardId,
            SourceActorId = playerActorId,
            SelectedTargetId = selectedTargetId
        });
    }

    pileSystem.Move(cardId, BattlePile.Play, ResolveResultPile(definition));
    battleRuleSystem.CheckBattleEnd();
    return true;
}
```

### 4. 卡牌效果

当前项目已经定义了 `EnumCardEffectType`，第一版可以直接围绕这个枚举实现。

需求：

- `DealDamage`：对目标造成伤害。
- `DrawCards`：抽指定数量牌。
- `GainBlock`：给目标或玩家获得格挡。
- `ApplyStatus`：施加状态。
- `GainEnergy`：获得能量。
- `ExhaustSelf`：本牌打出后进入消耗堆。

建议不要把每张卡写成一个 C# 类。你当前的数据结构更适合第一版数据驱动：一张卡由多个 `CardEffect` 组合。

未来如果出现复杂卡牌，可以再加：

```csharp
public interface ICustomCardBehavior
{
    bool CanPlay(CardPlayContext context);
    void OnPlay(CardPlayContext context);
}
```

### 5. 目标选择

`EnumTargetType` 已经提供了足够第一版使用的目标类型。

需求：

- `Self` / `User`：目标为玩家自己。
- `SelectedEnemy`：必须传入一个存活敌人。
- `AllEnemy`：所有存活敌人。
- `RandomEnemy`：随机一个存活敌人。
- `SelectedAlly`：第一版可以暂不支持，或只支持玩家。
- `AllAllies`：第一版可以只包含玩家。

目标解析应集中在 `TargetResolver`，不要散落在卡牌效果里。

### 6. 伤害和格挡

伤害系统应满足：

- 伤害先扣格挡。
- 溢出伤害扣生命。
- 生命小于等于 0 时死亡。
- 死亡敌人不能再作为目标。
- 多目标和多段伤害都复用同一套逻辑。

伪代码：

```csharp
public DamageResult DealDamage(CombatActorId source, CombatActorId target, int amount)
{
    CombatActorState actor = battle.GetActor(target);
    int blocked = Math.Min(actor.Block, amount);
    actor.Block -= blocked;

    int hpLoss = amount - blocked;
    actor.Hp -= hpLoss;

    if (actor.Hp <= 0)
    {
        actor.Hp = 0;
        actor.IsDead = true;
    }

    return new DamageResult(blocked, hpLoss, actor.IsDead);
}
```

### 7. 敌人回合

敌人回合应执行所有存活敌人的当前意图。

需求：

- 玩家结束回合后，弃掉不保留的手牌。
- 进入敌人回合。
- 每个存活敌人依次执行当前意图。
- 执行过程中玩家死亡则战斗失败。
- 敌人回合结束后，敌人重新生成下一回合意图。
- 回到玩家回合。

第一版敌人行动可以使用简单数据结构：

```csharp
public class EnemyMoveDefinition
{
    public string Id;
    public EnemyIntentType IntentType;
    public int Damage;
    public int Block;
    public EnumCardStatusType StatusType;
    public int StatusValue;
}
```

选择行动可以先固定循环：

```csharp
enemy.NextMoveIndex = (enemy.NextMoveIndex + 1) % enemy.Moves.Length;
```

等战斗闭环稳定后，再改成权重随机或状态机。

### 8. 回合结束

玩家回合结束时：

- 禁止继续出牌。
- 手牌进入弃牌堆，保留牌除外。
- 清理临时费用修改。
- 进入敌人回合。

敌人回合结束时：

- 清理敌人临时格挡或状态。
- 生成下一回合意图。
- 进入玩家回合。

### 9. 胜负判断

需求：

- 所有敌方主要单位死亡：胜利。
- 玩家生命为 0：失败。
- 战斗结束后不再允许出牌、抽牌或敌人行动。

建议由 `BattleRuleSystem` 集中处理，不要在每个系统里重复判断。

## 可能实现架构

### 建议新增目录

```text
Assets/Scripts/battles
Assets/Scripts/battles/state
Assets/Scripts/battles/systems
Assets/Scripts/battles/enemies
Assets/Scripts/battles/results
```

### 建议新增核心类

```text
BattleState
BattlePhase
CombatActorState
CombatActorId
StatusStack
EnemyDefinition
EnemyMoveDefinition
EnemyIntent
```

```text
BattleSystem
TurnSystem
CardPlaySystem
BattleCardPileSystem
CardEffectResolver
TargetResolver
DamageSystem
StatusSystem
EnemyIntentSystem
BattleRuleSystem
```

### 系统之间的调用关系

```text
CardView
  -> CardPlaySystem.TryPlayCard
      -> TargetResolver
      -> BattleCardPileSystem
      -> CardEffectResolver
          -> DamageSystem
          -> StatusSystem
          -> BattleCardPileSystem.Draw
      -> BattleRuleSystem

EndTurnButton
  -> TurnSystem.EndPlayerTurn
      -> BattleCardPileSystem.DiscardHand
      -> EnemyIntentSystem.ExecuteEnemyTurn
      -> TurnSystem.StartPlayerTurn
```

## 与当前代码的对接方式

### CardDefinition

继续作为卡牌静态定义使用：

- `Id`
- `Type`
- `Cost`
- `Effects`
- `Description`

第一版不需要大改。

### CardEffect

继续作为数据效果。

建议后续补充：

- 命中次数。
- 是否消耗。
- 是否需要选择卡牌。
- 是否基于状态动态计算数值。

但第一版不必提前加复杂字段。

### BattleCardState

继续持有五个牌堆，但改动必须通过 `BattleCardPileSystem`。

### PlayerState

建议不要继续用裸数组表达战斗数据。可以保留它作为长期玩家存档状态，但战斗中使用 `CombatActorState`。

### CardView

`CardView` 只做 UI：

- 展示卡牌。
- 拖拽。
- 释放时请求出牌。
- 根据 `CanPlay` 展示可用或不可用状态。

不要在 `CardView` 中扣能量、扣血、移动牌堆。

## 分阶段实现建议

### 阶段一：纯逻辑闭环

目标：不依赖完整 UI，也能在代码里模拟一场战斗。

完成：

- `BattleState`
- `CombatActorState`
- `BattleCardPileSystem`
- `DamageSystem`
- `TargetResolver`
- `CardEffectResolver`
- `CardPlaySystem`

验收：

- 创建玩家和一个敌人。
- 玩家抽 5 张。
- 玩家打出攻击牌。
- 敌人扣血。
- 卡牌进入弃牌堆。

### 阶段二：回合闭环

完成：

- `TurnSystem`
- `EnemyIntentSystem`
- `BattleRuleSystem`

验收：

- 玩家可结束回合。
- 敌人执行攻击。
- 玩家下一回合恢复能量并抽牌。
- 敌人死亡后胜利。
- 玩家死亡后失败。

### 阶段三：UI 接入

完成：

- `HandView`
- `EnemyView`
- `BattleHudView`
- 出牌拖拽目标选择。

验收：

- UI 能正确显示手牌、能量、生命、格挡、敌人意图。
- 拖拽攻击牌到敌人可以出牌。
- 不满足费用时自动回手。

### 阶段四：扩展玩法

完成：

- 状态系统。
- Power 卡。
- 遗物或被动效果。
- 关键词：消耗、保留、虚无、固有等。
- 战斗事件或 Hook。

## 风险和注意事项

### 1. 不要让 UI 成为规则来源

UI 可以问系统“能不能出”，但不能自己决定扣费和结算。否则后续 AI、测试和存档都会复制逻辑。

### 2. 不要让卡牌效果直接遍历全局状态

卡牌效果应通过 `CardEffectContext` 和系统方法访问目标。这样未来加入多人、召唤物或特殊目标时更容易扩展。

### 3. 随机必须可复现

洗牌、随机目标、敌人意图都应使用同一个可保存 seed 的随机系统，避免测试和存档回放困难。

### 4. 第一版不要过度抽象

当前最重要的是让“抽牌、出牌、扣血、弃牌、敌人行动”跑起来。Hook、复杂状态、动画队列可以后置。

## 结论

战斗逻辑的第一目标是完成纯数据驱动的闭环。以当前项目结构，最稳妥的实现方式是：保留现有卡牌定义和卡牌实例模型，新增 `BattleState` 作为战斗根状态，以 `CardPlaySystem` 串联出牌，以 `CardEffectResolver` 解释 `CardEffect`，再由 `DamageSystem`、`TargetResolver`、`BattleCardPileSystem` 执行具体规则。这样能最快从“卡牌 UI 与数据雏形”进入“可运行战斗原型”。
