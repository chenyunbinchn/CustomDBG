using enums;

namespace cards.effects
{
    public struct CardEffect
    {
        public EnumCardEffectType EffectType;
        public EnumTargetType TargetType;
        public EnumCardStatusType StatusType;
        public int Amount;
        
        public static CardEffect DealDamage(EnumTargetType target, int amount)
        {
            return new CardEffect
            {
                EffectType = EnumCardEffectType.DealDamage,
                TargetType = target,
                Amount = amount,
                StatusType = EnumCardStatusType.None
            };
        }

        public static CardEffect GainBlock(int amount)
        {
            return new CardEffect
            {
                EffectType = EnumCardEffectType.GainBlock,
                TargetType = EnumTargetType.Self,
                Amount = amount,
                StatusType = EnumCardStatusType.None
            };
        }

        public static CardEffect DrawCards(int amount)
        {
            return new CardEffect
            {
                EffectType = EnumCardEffectType.DrawCards,
                TargetType = EnumTargetType.Self,
                Amount = amount,
                StatusType = EnumCardStatusType.None
            };
        }

        public static CardEffect ApplyStatus(EnumTargetType target, EnumCardStatusType statusType, int amount)
        {
            return new CardEffect
            {
                EffectType = EnumCardEffectType.ApplyStatus,
                TargetType = target,
                Amount = amount,
                StatusType = statusType
            };
        }

        public static CardEffect ExhaustSelf()
        {
            return new CardEffect
            {
                EffectType = EnumCardEffectType.ExhaustSelf,
                TargetType = EnumTargetType.Self,
                Amount = 0,
                StatusType = EnumCardStatusType.None
            };
        }
    }
}