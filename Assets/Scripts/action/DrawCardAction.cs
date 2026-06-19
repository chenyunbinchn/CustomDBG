using System.Collections;
using enums;

namespace action
{
    public class DrawCardAction : GameAction
    {
        public override ActionId Id { get; }
        public override EnumActionStatus ActionStatus { get; set; }
        public int Value;

        public GameAction Create(int dmgValue)
        {
            base.Create();
            Value = dmgValue;
            return this;
        }

        public override IEnumerator Execute()
        {
            throw new System.NotImplementedException();
        }
    }
}