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
        // Note: Copied from the definition, but mutable per-instance so a future "reduce this card's cost"
        //       effect can lower it without touching the immutable definition.
        public int EnergyCost;
        public Effect[] Effects;

        public CardInstance(CardDefinition definition, CardInstanceId id, EnumEnchantmentType enchantment)
        {
            DefinitionId = definition.Id;
            Enchantment = enchantment;
            Id = id;
            EnergyCost = definition.EnergyCost;
            Effects = definition.Effects;
            Type = definition.Type;
        }
    }
}