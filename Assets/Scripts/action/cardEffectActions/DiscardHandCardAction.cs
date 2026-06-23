using System.Collections;
using enums;
using gameStates.transient;
using UnityEngine;

namespace action
{
    public class DiscardHandCardAction : GameAction
    {
        public override ActionId Id { get; }
        public override EnumActionStatus ActionStatus { get; set; }

        public override IEnumerator Execute(BattleContext context)
        {
            Debug.Log($"[DiscardHandCardAction] Execute");
            yield return null;
        }
    }
}