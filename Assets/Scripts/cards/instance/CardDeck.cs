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

        private uint GenerateId()
        {
            uint result = ++_index;
            return result;
        }
    }
}