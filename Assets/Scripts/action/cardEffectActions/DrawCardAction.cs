using System.Collections;
using enums;
using gameStates.transient;
using UnityEngine;

namespace action.cardEffectActions
{
    public sealed class DrawCardAction : GameAction
    {
        public override ActionId Id { get; }
        public override EnumActionStatus ActionStatus { get; set; }
        public int Value;

        public DrawCardAction(int value, ActionId id, EnumActionStatus actionStatus)
        {
            Value = value;
            Id = id;
            ActionStatus = actionStatus;
        }

        public override IEnumerator Execute(BattleContext context)
        {
            Debug.Log($"[DrawCardAction] Value = {Value}");
            yield return null;
        }
    }
}
