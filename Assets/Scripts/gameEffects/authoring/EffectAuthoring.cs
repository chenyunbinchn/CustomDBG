using System;
using enums;

namespace gameEffects.authoring
{
    [Serializable]
    public struct EffectAuthoring
    {
        public EnumEffectType effectType;
        public EnumTargetType targetType;
        public EnumStatusType statusType;
        public int value;

        public Effect CreateRuntimeEffect()
        {
            return new Effect(effectType, targetType, statusType, value);
        }
    }
}