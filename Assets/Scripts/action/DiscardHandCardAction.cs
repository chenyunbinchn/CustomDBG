using System.Collections;
using enums;

namespace action
{
    public class DiscardHandCardAction : GameAction
    {
        public override ActionId Id { get; }
        public override EnumActionStatus ActionStatus { get; set; }
        public override IEnumerator Execute()
        {
            throw new System.NotImplementedException();
        }
    }
}