using System.Collections;
using System;
using combat;
using enums;
using gameStates.transient;
using systems;
using UnityEngine;

namespace action.gameEffectActions
{
    public sealed class DrawCardAction : GameAction
    {
        public override ActionId Id { get; }
        public override EnumActionStatus ActionStatus { get; set; }
        public int Value;
        public ActionEntityId Source;
        public ActionEntityId Target;
        public DrawResult Result { get; private set; }
        public bool TargetWasGone { get; private set; }
        public override string ResultSummary => Result == null
            ? $"target={Target.Id} requested={Value} gone={TargetWasGone}"
            : $"target={Target.Id} requested={Value} drawn={Result.DrawnCards.Count} reshuffles={Result.ReshuffleCount} stopped={Result.StoppedReason}";

        public DrawCardAction(int value, ActionEntityId source, ActionEntityId target, ActionId id,
            EnumActionStatus actionStatus)
        {
            if (value < 0) throw new ArgumentOutOfRangeException(nameof(value));
            Value = value;
            Source = source;
            Target = target;
            Id = id;
            ActionStatus = actionStatus;
        }

        public override IEnumerator Execute(BattleActionContext context)
        {
            BattlePlayerState player = EntityApi.Resolve(Target, context.BattleState) as BattlePlayerState
                ?? throw new InvalidOperationException($"Draw target {Target.Type}#{Target.Id} is not a player.");
            if (player.Hp <= 0)
            {
                TargetWasGone = true;
                yield break;
            }
            Result = BattlePileApi.DrawCards(player, Value, context.RandomManager);
            Debug.Log($"[DrawCardAction] player#{Target.Id} drew {Result.DrawnCards.Count}/{Value} card(s)");
            if (context.StateManager != null)
                hook.HookSystem.Fire(new hook.Hook(EnumHookType.AfterCardsDrawn, Source, Target,
                    Result.DrawnCards.Count), context);
            yield return null;
        }
    }
}
