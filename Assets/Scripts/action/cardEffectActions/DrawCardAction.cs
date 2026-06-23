using System.Collections;
using enums;

namespace action.cardEffectActions
{
    public sealed class DrawCardAction : GameAction
    {
        public override ActionId Id { get; }
        public override EnumActionStatus ActionStatus { get; set; }
        public int Value;
        
        public DrawCardAction(int value, ActionId id, EnumActionStatus actionStatus)
        {
            Value = value;
            Id = id;
            ActionStatus = actionStatus;
        }

        public override IEnumerator Execute()
        {
            throw new System.NotImplementedException();
        }
    }
}