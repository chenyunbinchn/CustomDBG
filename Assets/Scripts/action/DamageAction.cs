using System.Collections;
using enums;
using UnityEngine;

namespace action
{
    public class DamageAction : GameAction
    {
        public override ActionId Id { get; }
        public override EnumActionStatus ActionStatus { get; set; }
        public int Value;

        public GameAction Create(int dmgValue)
        {
            base.Create();
            Value = dmgValue;
            return this;
        }

        public override IEnumerator Execute()
        {
            Debug.Log($"[DamageAction] Start, Value = {Value}");
            yield return null;
            Debug.Log($"[DamageAction] End, Value = {Value}");
        }
    }
}