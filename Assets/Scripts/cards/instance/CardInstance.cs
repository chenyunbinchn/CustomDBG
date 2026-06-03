using cards.definition;
using enums;

namespace cards.instance
{
    public class CardInstance
    {
        public CardDefinitionId DefinitionId;
        public CardInstanceId Id;
        public EnumEnchantmentType Enchantment;

        public CardInstance(CardDefinitionId definitionId, CardInstanceId id, EnumEnchantmentType enchantment)
        {
            DefinitionId = definitionId;
            Id = id;
            Enchantment = enchantment;
        }
    }
}