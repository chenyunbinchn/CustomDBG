using combat;
using enums;

namespace hook
{
    // Note: The context of one fired hook — what happened, who caused it, who took it, how much.
    //       Listeners self-filter against Source / Target, so this is the material that makes
    //       "did this concern me?" answerable. All value fields, so it serializes into a network
    //       message / replay line unchanged. See 《260718-rule-hook-system》 §5.
    public readonly struct Hook
    {
        public readonly EnumHookType HookType;
        public readonly ActionEntityId Source; // who caused it (played the card / dealt the damage)
        public readonly ActionEntityId Target; // who took it (default when the event has no target)
        public readonly int Value;             // damage dealt / cards drawn / 0 when meaningless

        public Hook(EnumHookType hookType, ActionEntityId source, ActionEntityId target, int value)
        {
            HookType = hookType;
            Source = source;
            Target = target;
            Value = value;
        }
    }
}
