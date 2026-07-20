using System.Collections;
using enums;
using gameStates.transient;
using tools.assert;
using UnityEngine;

namespace action.gameEffectActions
{
    public sealed class CostEnergyAction : GameAction
    {
        public override ActionId Id { get; }
        public override EnumActionStatus ActionStatus { get; set; }
        public int Value;

        public CostEnergyAction(int value, ActionId id, EnumActionStatus actionStatus)
        {
            Value = value;
            Id = id;
            ActionStatus = actionStatus;
        }

        public override IEnumerator Execute(BattleState battleState, BattlePlayerState playerState)
        {
            playerState.PlayerEnergy -= Value;
            MyAssert.Assert(playerState.PlayerEnergy >= 0, "PlayerEnergy < 0 after CostEnergyAction — validation let an unaffordable card through!");
            Debug.Log($"[CostEnergyAction] -{Value} -> PlayerEnergy = {playerState.PlayerEnergy}");
            yield return null;
        }
    }
}
