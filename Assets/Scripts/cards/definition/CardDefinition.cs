using cards.effects;
using enums;

namespace cards.definition
{
    public class CardDefinition
    {
        public readonly CardDefinitionId Id;
        public readonly EnumCardType Type;
        public readonly CardEffect[] Effects; // Todo: Probably not each card can be define only by card effects
        public readonly string Description;

        public CardDefinition(CardDefinitionId id, EnumCardType type, CardEffect[] effects, string description)
        {
            Id = id;
            Type = type;
            Effects = effects;
            Description = description;
        }
    }
}