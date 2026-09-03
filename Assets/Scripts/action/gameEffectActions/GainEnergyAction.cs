using System.Collections;
using enums;
using UnityEngine;

namespace action.gameEffectActions
{
    public sealed class GainEnergyAction : GameAction
    {
        public override ActionId Id { get; }
        public override EnumActionStatus ActionStatus { get; set; }
        public int Value;

        public GainEnergyAction(int value, ActionId id, EnumActionStatus actionStatus)
        {
            Value = value;
            Id = id;
            ActionStatus = actionStatus;
        }

        public override IEnumerator Execute(BattleActionContext context)
        {
            context.PlayerState.PlayerEnergy += Value;
            Debug.Log($"[GainEnergyAction] +{Value} -> PlayerEnergy = {context.PlayerState.PlayerEnergy}");
            yield return null;
        }
    }
}
