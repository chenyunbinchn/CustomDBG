using System.Collections;
using enums;
using gameStates.transient;
using UnityEngine;

namespace action.cardEffectActions
{
    public sealed class ExhaustCardAction : GameAction
    {
        public EnumTargetType TargetType;
        public int[] CardIndexArray;
        public override ActionId Id { get; }
        public override EnumActionStatus ActionStatus { get; set; }

        public ExhaustCardAction(EnumTargetType targetType, int[] cardIndexArray, ActionId id, EnumActionStatus actionStatus)
        {
            TargetType = targetType;
            CardIndexArray = cardIndexArray;
            Id = id;
            ActionStatus = actionStatus;
        }

        public override IEnumerator Execute(BattleContext context)
        {
            Debug.Log($"[ExhaustCardAction] TargetType = {TargetType}");
            yield return null;
        }
    }
}
