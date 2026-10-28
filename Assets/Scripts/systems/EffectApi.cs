using System;
using System.Collections.Generic;
using action;
using action.gameEffectActions;
using cards.instance;
using combat;
using enums;
using gameEffects;
using gameStates.transient;

namespace systems
{
    // Note: Translate one Effect into a GameAction and enqueue it. Shared by card play and hook
    //       reactions — both call Translate, only the (source, picked) context differs.
    //       Target types resolve to a concrete EntityId here (Self/User -> source, SelectedEnemy -> picked).
    public static class EffectApi
    {
        public static void Translate(Effect effect, ActionEntityId source, ActionEntityId picked,
            CardInstanceId? sourceCard, BattleActionContext context)
        {
            if (effect.Value < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(effect), "Effect value cannot be negative.");
            }

            if (effect.EffectType == EnumEffectType.Exhaust)
            {
                if (effect.TargetType != EnumTargetType.Self || !sourceCard.HasValue)
                {
                    throw new InvalidOperationException("Exhaust currently requires Self and a source card.");
                }
                context.ActionManager.Add(
                    new ExhaustCardAction(source, new[] { sourceCard.Value }, context.ActionManager.NextId(),
                        EnumActionStatus.WaitingForExecution),
                    context.RootCommandId);
                return;
            }

            List<ActionEntityId> targets = ResolveActorTargets(
                effect.TargetType, source, picked, context.BattleState, context.RandomManager.CombatNextInt);
            for (int i = 0; i < targets.Count; i++)
            {
                ActionEntityId target = targets[i];
                GameAction action;
                switch (effect.EffectType)
                {
                    case EnumEffectType.GainEnergy:
                        RequirePlayerTarget(target, effect.EffectType);
                        action = new GainEnergyAction(effect.Value, source, target, context.ActionManager.NextId(),
                            EnumActionStatus.WaitingForExecution);
                        break;
                    case EnumEffectType.DealDamage:
                        action = new DamageAction(effect.Value, source, target, sourceCard,
                            context.ActionManager.NextId(), EnumActionStatus.WaitingForExecution);
                        break;
                    case EnumEffectType.DrawCards:
                        RequirePlayerTarget(target, effect.EffectType);
                        action = new DrawCardAction(effect.Value, source, target, context.ActionManager.NextId(),
                            EnumActionStatus.WaitingForExecution);
                        break;
                    case EnumEffectType.GainBlock:
                        action = new GainBlockAction(effect.Value, source, target, sourceCard,
                            context.ActionManager.NextId(), EnumActionStatus.WaitingForExecution);
                        break;
                    case EnumEffectType.ApplyStatus:
                        action = new ApplyStatusAction(effect.StatusType, effect.Value, source, target, sourceCard,
                            context.ActionManager.NextId(), EnumActionStatus.WaitingForExecution);
                        break;
                    default:
                        throw new InvalidOperationException($"Unsupported authored EffectType: {effect.EffectType}.");
                }
                context.ActionManager.Add(action, context.RootCommandId);
            }
        }

        public static bool IsTargetValid(Effect effect, ActionEntityId source, ActionEntityId picked,
            CardInstanceId? sourceCard, BattleState battleState)
        {
            if (effect.EffectType == EnumEffectType.None || effect.EffectType == EnumEffectType.CostEnergy ||
                effect.Value < 0)
            {
                return false;
            }
            if (effect.EffectType == EnumEffectType.ApplyStatus &&
                (effect.StatusType == EnumStatusType.None || effect.Value <= 0))
            {
                return false;
            }
            if (effect.EffectType == EnumEffectType.Exhaust)
            {
                return effect.TargetType == EnumTargetType.Self && sourceCard.HasValue;
            }

            List<ActionEntityId> targets;
            try
            {
                targets = ResolveActorTargets(effect.TargetType, source, picked, battleState, null);
            }
            catch
            {
                return false;
            }
            if (targets.Count == 0)
            {
                return false;
            }
            if (effect.EffectType == EnumEffectType.GainEnergy || effect.EffectType == EnumEffectType.DrawCards)
            {
                for (int i = 0; i < targets.Count; i++)
                {
                    if (targets[i].Type != EnumEntityType.Player) return false;
                }
            }
            return true;
        }

        private static List<ActionEntityId> ResolveActorTargets(EnumTargetType type, ActionEntityId source,
            ActionEntityId picked, BattleState battleState, Func<int, int> randomNext)
        {
            List<ActionEntityId> result = new List<ActionEntityId>();
            switch (type)
            {
                case EnumTargetType.Self:
                case EnumTargetType.User:
                    if (!EntityApi.TryResolve(source, battleState, out ICombatActor sourceActor) || sourceActor.Hp <= 0)
                        throw new InvalidOperationException("Effect source is not a living actor.");
                    result.Add(source);
                    break;
                case EnumTargetType.SelectedEnemy:
                    AddSelected(result, picked, EnumEntityType.Enemy, battleState);
                    break;
                case EnumTargetType.SelectedAlly:
                    AddSelected(result, picked, EnumEntityType.Player, battleState);
                    break;
                case EnumTargetType.AllEnemy:
                    for (int i = 0; i < battleState.EnemyList.Count; i++)
                        if (battleState.EnemyList[i].Hp > 0) result.Add(battleState.EnemyList[i].Id);
                    result.Sort((left, right) => left.Id.CompareTo(right.Id));
                    break;
                case EnumTargetType.AllAllies:
                    if (battleState.Players != null)
                        for (int i = 0; i < battleState.Players.Length; i++)
                            if (battleState.Players[i] != null && battleState.Players[i].Hp > 0)
                                result.Add(battleState.Players[i].Id);
                    break;
                case EnumTargetType.RandomEnemy:
                    List<ActionEntityId> enemies = ResolveActorTargets(
                        EnumTargetType.AllEnemy, source, picked, battleState, null);
                    if (enemies.Count == 0) break;
                    result.Add(randomNext == null ? enemies[0] : enemies[randomNext(enemies.Count)]);
                    break;
                default:
                    throw new InvalidOperationException($"Unsupported TargetType: {type}.");
            }
            return result;
        }

        private static void AddSelected(List<ActionEntityId> result, ActionEntityId picked,
            EnumEntityType expectedType, BattleState battleState)
        {
            if (picked.Type != expectedType || !EntityApi.TryResolve(picked, battleState, out ICombatActor actor) ||
                actor.Hp <= 0)
            {
                throw new InvalidOperationException($"Selected target {picked.Type}#{picked.Id} is invalid.");
            }
            result.Add(picked);
        }

        private static void RequirePlayerTarget(ActionEntityId target, EnumEffectType effectType)
        {
            if (target.Type != EnumEntityType.Player)
                throw new InvalidOperationException($"{effectType} requires a player target.");
        }
    }
}
