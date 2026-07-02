using cards.definition;
using enums;
using gameEffects;

namespace cards.instance
{
    public class CardInstance
    {
        public CardDefinitionId DefinitionId;
        public CardInstanceId Id;
        public EnumCardType Type;
        public EnumEnchantmentType Enchantment;
        public Effect[] Effects;

        public CardInstance(CardDefinition definition, CardInstanceId id, EnumEnchantmentType enchantment)
        {
            DefinitionId = definition.Id;
            Enchantment = enchantment;
            Id = id;
            Effects = definition.Effects;
            Type = definition.Type;
        }
    }
}