using cards.definition;
using enums;
using ids;

namespace cards.instance
{
    public class CardInstance
    {
        public CardDefinitionId DefinitionId;
        public CardInstanceId Id;
        public EnumEnchantmentType Enchantment;

        public void Instantiate(CardDefinition definition)
        {
            DefinitionId = definition.Id;
            // Note: All id generation is handled by manager.
        }
    }
}