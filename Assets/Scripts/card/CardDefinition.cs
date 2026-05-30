using cardEffects;
using enums;

namespace card
{
    public class CardDefinition
    {
        public int Cost;
        public EnumCardType Type;
        public EnumEnchantmentType Enchantment;
        public CardEffect[] Effects; // Todo: Probably not each card can be define only by card effects
    }
}