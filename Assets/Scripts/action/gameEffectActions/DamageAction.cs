using System.Collections;
using System;
using cards.instance;
using combat;
using enums;
using UnityEngine;

namespace action.gameEffectActions
{
    public readonly struct DamageResult
    {
        public readonly int FinalAmount;
        public readonly int Blocked;
        public readonly int HpLoss;
        public readonly bool WasFatal;
        public readonly bool TargetWasGone;

        public DamageResult(int finalAmount, int blocked, int hpLoss, bool wasFatal, bool targetWasGone)
        {
            FinalAmount = finalAmount;
            Blocked = blocked;
            HpLoss = hpLoss;
            WasFatal = wasFatal;
            TargetWasGone = targetWasGone;
        }
    }

    public sealed class DamageAction : GameAction
    {
        public override ActionId Id { get; }
        public override EnumActionStatus ActionStatus { get; set; }
        public int Value;
        public ActionEntityId Source;
        public ActionEntityId Target;
        public CardInstanceId? SourceCard;
        public DamageResult Result { get; private set; }
        public override string ResultSummary =>
            $"target={Target.Type}#{Target.Id} final={Result.FinalAmount} blocked={Result.Blocked} hpLoss={Result.HpLoss} fatal={Result.WasFatal} gone={Result.TargetWasGone}";

        public DamageAction(int value, ActionEntityId source, ActionEntityId target, CardInstanceId? sourceCard,
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
                Result = new DamageResult(0, 0, 0, false, true);
                Debug.Log($"[DamageAction] target {Target.Type}#{Target.Id} is gone; hit fizzled");
                yield break;
            }

            if (context.StateManager != null)
                hook.HookSystem.Fire(new hook.Hook(EnumHookType.BeforeDamage, Source, Target, Value), context);

            int finalAmount = Value;
            if (EntityApi.TryResolve(Source, context.BattleState, out ICombatActor source))
            {
                if (source.StatusStacks.TryGetValue(EnumStatusType.Power, out int power))
                {
                    finalAmount = checked(finalAmount + power);
                }
                if (source.StatusStacks.TryGetValue(EnumStatusType.Weak, out int weak) && weak > 0)
                {
                    finalAmount = finalAmount * 3 / 4;
                }
            }
            if (target.StatusStacks.TryGetValue(EnumStatusType.Vulnerable, out int vulnerable) && vulnerable > 0)
            {
                finalAmount = finalAmount * 3 / 2;
            }
            if (finalAmount < 0) finalAmount = 0;

            int availableBlock = target.Block < 0 ? 0 : target.Block;
            int blocked = Math.Min(availableBlock, finalAmount);
            target.Block = availableBlock - blocked;
            int hpLoss = finalAmount - blocked;
            target.Hp = Math.Max(0, target.Hp - hpLoss);
            bool fatal = target.Hp == 0;
            Result = new DamageResult(finalAmount, blocked, hpLoss, fatal, false);

            if (fatal && Target.Type == EnumEntityType.Enemy)
            {
                context.BattleState.EnemyList.RemoveAll(enemy => enemy.Id == Target);
            }

            Debug.Log($"[DamageAction] {Source.Type}#{Source.Id} -> {Target.Type}#{Target.Id}: final={finalAmount}, blocked={blocked}, hpLoss={hpLoss}, fatal={fatal}");
            if (context.StateManager != null)
                hook.HookSystem.Fire(new hook.Hook(EnumHookType.AfterDamage, Source, Target, hpLoss), context);
            yield return null;
        }
    }
}
