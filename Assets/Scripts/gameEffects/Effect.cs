using enums;

namespace gameEffects
{
    public struct Effect
    {
        public EnumEffectType EffectType;
        public EnumTargetType TargetType;
        public EnumStatusType StatusType;
        public int Value;

        public Effect(EnumEffectType effectType, EnumTargetType targetType, EnumStatusType statusType, int value)
        {
            EffectType = effectType;
            TargetType = targetType;
            StatusType = statusType;
            Value = value;
        }
    }
}