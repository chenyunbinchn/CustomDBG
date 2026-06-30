using System.Collections;
using enums;
using gameStates.transient;
using UnityEngine;

namespace action.gameEffectActions
{
    public sealed class HandCardChooseAction : GameAction
    {
        public HandCardChooseAction(int cardIndex, ActionId id, EnumActionStatus actionStatus)
        {
            CardIndex = cardIndex;
            Id = id;
            ActionStatus = actionStatus;
        }

        public override ActionId Id { get; }
        public override EnumActionStatus ActionStatus { get; set; }
        public int CardIndex = -1;

        public void OnPlayerChose(int cardIndex)
        {
            CardIndex = cardIndex;
        }

        public override IEnumerator Execute(BattleState battleState, BattlePlayerState playerState)
        {
            // Todo: ShowChooseCardUI();
            ActionStatus = EnumActionStatus.GatheringPlayerChoice;
            CardIndex = -1;
            yield return new WaitUntil(() => CardIndex >= 0);
        }
    }
}
