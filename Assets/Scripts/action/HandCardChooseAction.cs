using System.Collections;
using enums;
using UnityEngine;

namespace action
{
    public class HandCardChooseAction : GameAction
    {
        public override ActionId Id { get; }
        public override EnumActionStatus ActionStatus { get; set; }
        public int CardIndex = -1;

        public void OnPlayerChose(int cardIndex)
        {
            CardIndex = cardIndex;
        }
        
        public virtual GameAction Create(int chosenCardIndex)
        {
            ActionId id = new ActionId(); // Todo: Manage Id generation.
            ActionStatus = EnumActionStatus.WaitingForExecution;
            CardIndex = chosenCardIndex;
            return this;
        }
        
        public override IEnumerator Execute()
        {
            // Todo: ShowChooseCardUI();
            ActionStatus = EnumActionStatus.GatheringPlayerChoice;
            CardIndex = -1;
            yield return new WaitUntil(() => CardIndex >= 0);
            
        }
    }
}