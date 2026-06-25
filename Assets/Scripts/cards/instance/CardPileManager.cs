using System.Collections.Generic;
using cards.definition;
using enums;

namespace cards.instance
{
    public class CardPileManager
    {
        public Dictionary<CardInstanceId, CardInstance> Dictionary = new Dictionary<CardInstanceId, CardInstance>();
        private uint _maxIndex = 0; 

        public void CopyFromDeck(List<CardInstance> deck)
        {
            foreach (CardInstance card in deck)
            {
                Dictionary.TryAdd(card.Id, card);
                if (card.Id.Value > _maxIndex)
                {
                    _maxIndex = card.Id.Value;
                }
            }
        }

        public CardInstanceId AddCard(CardDefinition definition)
        {
            uint newIdValue = ++_maxIndex;
            CardInstance newCard = new CardInstance(definition, new CardInstanceId(newIdValue), EnumEnchantmentType.None);
            return newCard.Id;
        }
        
        public void Reset()
        {
            Dictionary.Clear();
        }
    }
}