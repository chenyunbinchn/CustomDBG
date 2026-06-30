using System.Collections;
using enums;
using gameStates.transient;
using UnityEngine;

namespace action.gameEffectActions
{
    public sealed class GainBlockAction : GameAction
    {
        public override ActionId Id { get; }
        public override EnumActionStatus ActionStatus { get; set; }
        public int Value;

        public GainBlockAction(int value, ActionId id, EnumActionStatus actionStatus)
        {
            Value = value;
            Id = id;
            ActionStatus = actionStatus;
        }

        public override IEnumerator Execute(BattleState battleState, BattlePlayerState playerState)
        {
            Debug.Log($"[GainBlockAction] Value = {Value}");
            yield return null;
        }
    }
}
