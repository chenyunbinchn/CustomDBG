using System.Collections;
using enums;

namespace action.cardEffectActions
{
    public sealed class ExhaustCardAction : GameAction
    {
        public int[] CardIndexArray;
        public override ActionId Id { get; }
        public override EnumActionStatus ActionStatus { get; set; }
     
        public ExhaustCardAction(int[] cardIndexArray, ActionId id, EnumActionStatus actionStatus)
        {
            CardIndexArray = cardIndexArray;
            Id = id;
            ActionStatus = actionStatus;
        }
        
        public override IEnumerator Execute()
        {
            throw new System.NotImplementedException();
        }
    }
}