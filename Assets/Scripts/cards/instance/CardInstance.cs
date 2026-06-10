using cards.definition;
using enums;

namespace cards.instance
{
    public class CardInstance
    {
        public CardDefinition Definition;
        public CardInstanceId Id;
        public EnumEnchantmentType Enchantment;

        public CardInstance(CardDefinition definition, CardInstanceId id, EnumEnchantmentType enchantment)
        {
            Definition = definition;
            Id = id;
            Enchantment = enchantment;
        }
    }
}