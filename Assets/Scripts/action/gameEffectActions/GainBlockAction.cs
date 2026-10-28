using System.Collections;
using System;
using cards.instance;
using combat;
using enums;
using UnityEngine;

namespace action.gameEffectActions
{
    public sealed class GainBlockAction : GameAction
    {
        public override ActionId Id { get; }
        public override EnumActionStatus ActionStatus { get; set; }
        public int Value;
        public ActionEntityId Source;
        public ActionEntityId Target;
        public CardInstanceId? SourceCard;
        public int ActualGained { get; private set; }
        public bool TargetWasGone { get; private set; }
        public override string ResultSummary => $"target={Target.Type}#{Target.Id} gained={ActualGained} gone={TargetWasGone}";

        public GainBlockAction(int value, ActionEntityId source, ActionEntityId target, CardInstanceId? sourceCard,
            ActionId id, EnumActionStatus actionStatus)
        {
            if (value < 0) throw new ArgumentOutOfRangeException(nameof(value));
            Value = value;
            Source = source;
            Target = target;
            SourceCard = sourceCard;
            Id = id;
            ActionStatus = actionStatus;
        }

        public override IEnumerator Execute(BattleActionContext context)
        {
            if (!EntityApi.TryResolve(Target, context.BattleState, out ICombatActor target) || target.Hp <= 0)
            {
                TargetWasGone = true;
                Debug.Log($"[GainBlockAction] target {Target.Type}#{Target.Id} is gone; gain fizzled");
                yield break;
            }

            int before = target.Block < 0 ? 0 : target.Block;
            target.Block = checked(before + Value);
            ActualGained = target.Block - before;
            Debug.Log($"[GainBlockAction] {Source.Type}#{Source.Id} gives +{ActualGained} block -> {Target.Type}#{Target.Id}, Block now {target.Block}");
            if (context.StateManager != null)
                hook.HookSystem.Fire(new hook.Hook(EnumHookType.AfterBlockGained, Source, Target,
                    ActualGained), context);
            yield return null;
        }
    }
}
