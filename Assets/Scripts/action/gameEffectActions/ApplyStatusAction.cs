using System.Collections;
using combat;
using enums;
using hook;
using UnityEngine;

namespace action.gameEffectActions
{
    public sealed class ApplyStatusAction : GameAction
    {
        public override ActionId Id { get; }
        public override EnumActionStatus ActionStatus { get; set; }
        public EnumStatusType StatusType;
        public int Value;
        public ActionEntityId Target;

        public ApplyStatusAction(EnumStatusType statusType, int value, ActionEntityId target, ActionId id, EnumActionStatus actionStatus)
        {
            StatusType = statusType;
            Value = value;
            Target = target;
            Id = id;
            ActionStatus = actionStatus;
        }

        public override IEnumerator Execute(BattleActionContext context)
        {
            ICombatActor target = EntityApi.Resolve(Target, context.BattleState);
            if (target != null && StatusRegistry.TryGetTemplate(StatusType, out HookListener template))
            {
                // Hook-type status: copy the shared registry template into a NEW listener (never add the
                //       template itself — it is shared by every actor). Host/Owner are filled in here
                //       because this is the only place that knows who received the status.
                target.HookListeners.Add(new HookListener
                {
                    Owner = target.Id,
                    Hook = template.Hook,
                    Filter = template.Filter,
                    Effect = template.Effect
                });
                Debug.Log($"[ApplyStatusAction] {StatusType} -> hook attached to {Target.Type}#{Target.Id} (Listeners={target.HookListeners.Count})");
            }
            else
            {
                // Todo: modifier-type statuses (Weak/Vulnerable/Power) — not implemented yet, stub logs only.
                Debug.Log($"[ApplyStatusAction] {StatusType} x{Value} -> {Target.Type}#{Target.Id} (no hook template; modifier Todo)");
            }
            yield return null;
        }
    }
}
