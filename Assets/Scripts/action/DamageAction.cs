using System.Collections;
using enums;

namespace action
{
    public class DamageAction : GameAction
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
            yield return null;
        }
    }
}