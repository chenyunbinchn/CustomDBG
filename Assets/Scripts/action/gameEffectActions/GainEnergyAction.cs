using System.Collections;
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

        public GainEnergyAction(int value, ActionId id, EnumActionStatus actionStatus)
        {
            Value = value;
            Id = id;
            ActionStatus = actionStatus;
        }

        public override IEnumerator Execute(BattleState battleState, BattlePlayerState playerState)
        {
            playerState.PlayerEnergy += Value;
            Debug.Log($"[GainEnergyAction] +{Value} -> PlayerEnergy = {playerState.PlayerEnergy}");
            yield return null;
        }
    }
}
