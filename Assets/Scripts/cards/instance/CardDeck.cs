using System.Collections.Generic;
using cards.definition;
using enums;

namespace cards.instance
{
    // Work for each game round
    public class CardDeck
    {
        public List<CardInstance> Deck = new List<CardInstance>();
        private uint _index = 0; 

        public CardInstanceId AddCardToDeck(CardDefinition definition)
        {
            CardInstance instance = new CardInstance(definition, new CardInstanceId(GenerateId()), EnumEnchantmentType.None);
            Deck.Add(instance);

            return instance.Id;
        }

        // Todo: CardInstanceId is only unique per player (each deck counts from 1). If a cross-player
        //       card reference ever appears (network message / replay line / give-card effect), qualify
        //       it with the owner's ActionEntityId instead of going global — see
        //       《260717-report-multiplayer-gap-scan》 3.6, option A.
        private uint GenerateId()
        {
            uint result = ++_index;
            return result;
        }
    }
}