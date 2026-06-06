using enums;

namespace cards.effects
{
    public struct CardEffect
    {
        public EnumCardEffectType EffectType;
        public EnumTargetType TargetType;
        public EnumCardStatusType StatusType;
        public int Value;

        public CardEffect(EnumCardEffectType effectType, EnumTargetType targetType, EnumCardStatusType statusType, int value)
        {
            EffectType = effectType;
            TargetType = targetType;
            StatusType = statusType;
            Value = value;
        }
    }
}