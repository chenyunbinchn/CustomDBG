using cards.definition;
using enums;

namespace cards.instance
{
    public class CardInstance
    {
        public CardDefinition Definition;
        public CardInstanceId Id;
        public EnumEnchantmentType Enchantment;
        public int[] BonusValue; // Add bonus to card instance after battle end. Only used in ValueModifier.

        public CardInstance(CardDefinition definition, CardInstanceId id, EnumEnchantmentType enchantment)
        {
            Definition = definition;
            Id = id;
            Enchantment = enchantment;
            BonusValue = new int[definition.Effects.Length];
        }
    }
}