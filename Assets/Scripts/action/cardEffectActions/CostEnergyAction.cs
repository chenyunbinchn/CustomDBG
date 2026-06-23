using System.Collections;
using enums;

namespace action.cardEffectActions
{
    public sealed class CostEnergyAction : GameAction
    {
        public override ActionId Id { get; }
        public override EnumActionStatus ActionStatus { get; set; }
        public int Value;

        public override IEnumerator Execute()
        {
            throw new System.NotImplementedException();
        }
    }
}