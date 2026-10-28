using System.Collections;
using enums;
using systems;
using UnityEngine;

namespace action.gameEffectActions
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

        public override IEnumerator Execute(BattleActionContext context)
        {
            BattlePileApi.DrawCards(context.PlayerState, Value, context.RandomManager);
            Debug.Log($"[DrawCardAction] drew up to {Value} card(s)");
            yield return null;
        }
    }
}
