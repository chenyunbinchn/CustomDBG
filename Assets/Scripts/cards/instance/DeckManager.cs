using System.Collections.Generic;
using cards.definition;
using enums;

namespace cards.instance
{
    // Work for each game round
    public class DeckManager
    {
        public List<CardInstance> CardInstances = new List<CardInstance>();
        private uint _index = 0; 

        public CardInstanceId AddCardToDeck(CardDefinitionManager definitionManager, CardDefinitionId definitionId)
        {
            CardDefinition definition = definitionManager.Get(definitionId);
            CardInstance instance = new CardInstance(definitionId, new CardInstanceId(GenerateId()), EnumEnchantmentType.None);
            CardInstances.Add(instance);

            return instance.Id;
        }

        private uint GenerateId()
        {
            uint result = ++_index;
            return result;
        }
    }
}