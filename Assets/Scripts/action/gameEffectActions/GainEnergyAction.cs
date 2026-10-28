using System.Collections;
using System;
using combat;
using enums;
using gameStates.transient;
using UnityEngine;

namespace action.gameEffectActions
{
    public sealed class GainEnergyAction : GameAction
    {
        public override ActionId Id { get; }
        public override EnumActionStatus ActionStatus { get; set; }
        public int Value;
        public ActionEntityId Source;
        public ActionEntityId Target;
        public int EnergyAfter { get; private set; }
        public bool TargetWasGone { get; private set; }
        public override string ResultSummary => $"target={Target.Id} gained={Value} energyAfter={EnergyAfter} gone={TargetWasGone}";

        public GainEnergyAction(int value, ActionEntityId source, ActionEntityId target, ActionId id,
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
                ?? throw new InvalidOperationException($"Energy target {Target.Type}#{Target.Id} is not a player.");
            if (player.Hp <= 0)
            {
                TargetWasGone = true;
                EnergyAfter = player.PlayerEnergy;
                yield break;
            }
            player.PlayerEnergy = checked(player.PlayerEnergy + Value);
            EnergyAfter = player.PlayerEnergy;
            Debug.Log($"[GainEnergyAction] player#{Target.Id} +{Value} -> PlayerEnergy = {player.PlayerEnergy}");
            if (context.StateManager != null)
                hook.HookSystem.Fire(new hook.Hook(EnumHookType.AfterEnergyGained, Source, Target, Value), context);
            yield return null;
        }
    }
}
