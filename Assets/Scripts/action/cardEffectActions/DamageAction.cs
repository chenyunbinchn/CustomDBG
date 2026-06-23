using System.Collections;
using enums;
using UnityEngine;

namespace action.cardEffectActions
{
    public sealed class DamageAction : GameAction
    {
        public override ActionId Id { get; }
        public override EnumActionStatus ActionStatus { get; set; }
        public int Value;
        
        public DamageAction(int value, ActionId id, EnumActionStatus actionStatus)
        {
            Value = value;
            Id = id;
            ActionStatus = actionStatus;
        }
        
        public override IEnumerator Execute()
        {
            Debug.Log($"[DamageAction] Start, Value = {Value}");
            yield return null;
            Debug.Log($"[DamageAction] End, Value = {Value}");
        }
    }
}