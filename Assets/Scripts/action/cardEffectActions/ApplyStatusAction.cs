using System.Collections;
using enemy.instance;
using enums;
using gameStates.transient;
using UnityEngine;

namespace action.cardEffectActions
{
    public sealed class ApplyStatusAction : GameAction
    {
        public override ActionId Id { get; }
        public override EnumActionStatus ActionStatus { get; set; }
        public EnumCardStatusType StatusType;
        public int Value;
        public EnumTargetType TargetType;
        public EnemyInstance Target;

        public ApplyStatusAction(EnumCardStatusType statusType, int value, EnumTargetType targetType, EnemyInstance target, ActionId id, EnumActionStatus actionStatus)
        {
            StatusType = statusType;
            Value = value;
            TargetType = targetType;
            Target = target;
            Id = id;
            ActionStatus = actionStatus;
        }

        public override IEnumerator Execute(BattleContext context)
        {
            Debug.Log($"[ApplyStatusAction] StatusType = {StatusType}, Value = {Value}");
            yield return null;
        }
    }
}
