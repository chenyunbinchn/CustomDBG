using System.Collections;
using combat;
using enums;
using UnityEngine;

namespace action.gameEffectActions
{
    public sealed class GainBlockAction : GameAction
    {
        public override ActionId Id { get; }
        public override EnumActionStatus ActionStatus { get; set; }
        public int Value;
        public ActionEntityId Target;

        public GainBlockAction(int value, ActionEntityId target, ActionId id, EnumActionStatus actionStatus)
        {
            Value = value;
            Target = target;
            Id = id;
            ActionStatus = actionStatus;
        }

        public override IEnumerator Execute(BattleActionContext context)
        {
            ICombatActor target = EntityApi.Resolve(Target, context.BattleState);
            if (target != null)
            {
                target.Block += Value;
                Debug.Log($"[GainBlockAction] +{Value} block -> {Target.Type}#{Target.Id}, Block now {target.Block}");
            }
            yield return null;
        }
    }
}
