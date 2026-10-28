using System.Collections;
using System;
using cards.instance;
using combat;
using enums;
using gameEffects;
using hook;
using UnityEngine;

namespace action.gameEffectActions
{
    public sealed class ApplyStatusAction : GameAction
    {
        public override ActionId Id { get; }
        public override EnumActionStatus ActionStatus { get; set; }
        public EnumStatusType StatusType;
        public int Value;
        public ActionEntityId Source;
        public ActionEntityId Target;
        public CardInstanceId? SourceCard;
        public int TotalAmount { get; private set; }
        public bool TargetWasGone { get; private set; }
        public override string ResultSummary => $"target={Target.Type}#{Target.Id} status={StatusType} added={Value} total={TotalAmount} gone={TargetWasGone}";

        public ApplyStatusAction(EnumStatusType statusType, int value, ActionEntityId source, ActionEntityId target,
            CardInstanceId? sourceCard, ActionId id, EnumActionStatus actionStatus)
        {
            if (statusType == EnumStatusType.None || !Enum.IsDefined(typeof(EnumStatusType), statusType))
                throw new ArgumentOutOfRangeException(nameof(statusType));
            if (value <= 0) throw new ArgumentOutOfRangeException(nameof(value), "Status amount must be positive.");
            StatusType = statusType;
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
                Debug.Log($"[ApplyStatusAction] target {Target.Type}#{Target.Id} is gone; apply fizzled");
                yield break;
            }

            target.StatusStacks.TryGetValue(StatusType, out int oldAmount);
            int newAmount = checked(oldAmount + Value);
            target.StatusStacks[StatusType] = newAmount;
            TotalAmount = newAmount;

            if (StatusRegistry.TryGetTemplate(StatusType, out HookListener template))
            {
                HookListener statusListener = null;
                for (int i = 0; i < target.HookListeners.Count; i++)
                {
                    if (target.HookListeners[i].StatusType == StatusType)
                    {
                        statusListener = target.HookListeners[i];
                        break;
                    }
                }

                Effect templateEffect = template.Effect;
                Effect stackedEffect = new Effect(
                    templateEffect.EffectType,
                    templateEffect.TargetType,
                    templateEffect.StatusType,
                    newAmount);
                if (statusListener == null)
                {
                    target.HookListeners.Add(new HookListener
                    {
                        Owner = target.Id,
                        Hook = template.Hook,
                        Filter = template.Filter,
                        StatusType = StatusType,
                        Effect = stackedEffect
                    });
                }
                else
                {
                    statusListener.Owner = target.Id;
                    statusListener.Hook = template.Hook;
                    statusListener.Filter = template.Filter;
                    statusListener.Effect = stackedEffect;
                }
            }
            Debug.Log($"[ApplyStatusAction] {Source.Type}#{Source.Id} applies {StatusType} +{Value} -> {Target.Type}#{Target.Id}, amount={newAmount}");
            if (context.StateManager != null)
                HookSystem.Fire(new Hook(EnumHookType.AfterStatusApplied, Source, Target, Value), context);
            yield return null;
        }
    }
}
