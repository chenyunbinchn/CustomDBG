using System.Collections.Generic;
using enums;
using gameEffects;

namespace hook
{
    // Note: Data table — EnumStatusType -> the hook listener that status attaches. This is what keeps
    //       EnumEffectType a small fixed set: ONE ApplyStatus primitive applies ANY status; WHICH status
    //       is data (EnumStatusType, which is allowed to grow with content — it's a key, not a switch
    //       branch). Adding a new hook-type status = one entry here + one EnumStatusType value, and
    //       EnumEffectType is never touched.
    //       Todo: move to a ScriptableObject; support amount/layer-driven Effect values;
    //             modifier-type statuses (Weak/Vulnerable/Power) belong to the modifier chain, not here.
    public static class StatusRegistry
    {
        private static readonly Dictionary<EnumStatusType, HookListener> _templates = new Dictionary<EnumStatusType, HookListener>
        {
            {
                EnumStatusType.Afterimage,
                new HookListener
                {
                    Hook = EnumHookType.AfterCardPlayed,
                    Effect = new Effect(EnumEffectType.GainBlock, EnumTargetType.Self, EnumStatusType.None, 2)
                }
            }
        };

        public static bool TryGetTemplate(EnumStatusType type, out HookListener template)
        {
            return _templates.TryGetValue(type, out template);
        }
    }
}
