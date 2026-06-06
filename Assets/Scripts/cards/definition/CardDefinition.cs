using cards.effects;
using enums;

namespace cards.definition
{
    public class CardDefinition
    {
        public readonly CardDefinitionId Id;
        public readonly EnumCardType Type;
        public readonly int Cost;
        public readonly CardEffect[] Effects; // Todo: Probably not each card can be define only by card effects
        public readonly string Description;
        public readonly CardArtworkId ArtworkId;

        public CardDefinition(CardDefinitionId id, EnumCardType type, int cost, CardEffect[] effects, string description)
        {
            Id = id;
            Type = type;
            Cost = cost;
            Effects = effects;
            Description = description;
        }
    }
}