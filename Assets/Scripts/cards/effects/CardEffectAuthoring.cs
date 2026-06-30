using System;
using enums;
using gameEffects;

namespace cards.authoring
{
    [Serializable]
    public struct CardEffectAuthoring
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