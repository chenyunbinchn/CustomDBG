using System.Collections;
using combat;
using enums;
using gameStates.transient;
using UnityEngine;

namespace action.gameEffectActions
{
    public sealed class DamageAction : GameAction
    {
        public override ActionId Id { get; }
        public override EnumActionStatus ActionStatus { get; set; }
        public int Value;
        public ActionEntityId Target;

        public DamageAction(int value, ActionEntityId target, ActionId id, EnumActionStatus actionStatus)
        {
            Value = value;
            Target = target;
            Id = id;
            ActionStatus = actionStatus;
        }

        public override IEnumerator Execute(BattleState battleState, BattlePlayerState playerState)
        {
            ICombatActor target = EntityApi.Resolve(Target, battleState, playerState);
            if (target != null)
            {
                target.Hp -= Value;   // Todo: Block absorbs first, then Hp
                Debug.Log($"[DamageAction] {Value} dmg -> {Target.Type}#{Target.Id}, Hp now {target.Hp}");
            }
            yield return null;
        }
    }
}
