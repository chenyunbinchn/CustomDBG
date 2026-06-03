using System.Collections.Generic;
using cards.definition;

namespace cards.instance
{
    // Work for each game round
    public class DeckManager
    {
        public List<CardInstance> CardInstances = new List<CardInstance>();

        public CardInstanceId AddCardToDeck(CardDefinitionManager definitionManager, CardDefinitionId definitionId)
        {
            CardDefinition definition = definitionManager.Get(definitionId);
            CardInstance newCard = new CardInstance();
            newCard.Instantiate(definition);
            // Todo: Which is better? Generate id while Adding Card to deck? Or Generate id in Instantiate function?
            CardInstances.Add(newCard);
            // Todo: Temp, fix 
            return new CardInstanceId();
        }
    }
}