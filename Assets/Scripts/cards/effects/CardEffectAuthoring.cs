using System;
using enums;

namespace cards.effects
{
    [Serializable]
    public struct CardEffectAuthoring
    {
        public EnumCardEffectType effectType;
        public EnumTargetType targetType;
        public EnumCardStatusType statusType;
        public int value;

        public CardEffect CreateRuntimeEffect()
        {
            return new CardEffect(effectType, targetType, statusType, value);
        }
    }
}