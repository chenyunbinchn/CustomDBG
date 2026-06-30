using enums;
using gameEffects;

namespace cards.definition
{
    public class CardDefinition
    {
        public readonly CardDefinitionId Id;
        public readonly EnumCardType Type;
        public readonly Effect[] Effects; // Todo: Probably not each card can be define only by card effects
        public readonly string Description;

        public CardDefinition(CardDefinitionId id, EnumCardType type, Effect[] effects, string description)
        {
            Id = id;
            Type = type;
            Effects = effects;
            Description = description;
        }
    }
}