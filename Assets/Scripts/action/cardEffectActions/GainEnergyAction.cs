using System.Collections;
using enums;

namespace action.cardEffectActions
{
    public sealed class GainEnergyAction : GameAction
    {
        public GainEnergyAction(ActionId id, EnumActionStatus actionStatus)
        {
            Id = id;
            ActionStatus = actionStatus;
        }

        public override ActionId Id { get; }
        public override EnumActionStatus ActionStatus { get; set; }
        public override IEnumerator Execute()
        {
            throw new System.NotImplementedException();
        }
    }
}