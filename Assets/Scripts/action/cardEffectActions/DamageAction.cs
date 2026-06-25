using System.Collections;
using enemy.instance;
using enums;
using gameStates.transient;
using UnityEngine;

namespace action.cardEffectActions
{
    public sealed class DamageAction : GameAction
    {
        public override ActionId Id { get; }
        public override EnumActionStatus ActionStatus { get; set; }
        public int Value;
        public EnumTargetType TargetType;
        public EnemyInstance Target;

        public DamageAction(int value, EnumTargetType targetType, EnemyInstance target, ActionId id, EnumActionStatus actionStatus)
        {
            Value = value;
            TargetType = targetType;
            Target = target;
            Id = id;
            ActionStatus = actionStatus;
        }

        public override IEnumerator Execute(BattleState battleState, BattlePlayerState playerState)
        {
            Debug.Log($"[DamageAction] Start, Value = {Value}");
            yield return null;
            Debug.Log($"[DamageAction] End, Value = {Value}");
        }
    }
}
