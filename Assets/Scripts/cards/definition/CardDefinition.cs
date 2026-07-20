using enums;
using gameEffects;

namespace cards.definition
{
    public class CardDefinition
    {
        public readonly CardDefinitionId Id;
        public readonly EnumCardType Type;
        // Note: Energy paid to play the card (plan A — cost is a card field, not a CostEnergy effect).
        //       BattleCommandApi validates it, then synthesizes a CostEnergyAction from it at play time.
        public readonly int EnergyCost;
        public readonly Effect[] Effects; // Todo: Probably not each card can be define only by card effects
        public readonly string Description;

        public CardDefinition(CardDefinitionId id, EnumCardType type, int energyCost, Effect[] effects, string description)
        {
            Id = id;
            Type = type;
            EnergyCost = energyCost;
            Effects = effects;
            Description = description;
        }
    }
}