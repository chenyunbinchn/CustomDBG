using System.Collections.Generic;
using tools;

namespace cards.definition
{
    public class CardDefinitionManager
    {
        private readonly Dictionary<CardDefinitionId, CardDefinition> _cards = new Dictionary<CardDefinitionId, CardDefinition>();

        public CardDefinitionId RegisterCard(CardDefinition newCardDefinition)
        {
            MyAssert.Assert(newCardDefinition != null, "Card definition is null.");

            bool added = _cards.TryAdd(newCardDefinition.Id, newCardDefinition);

            MyAssert.Assert(
                added,
                "There is already the same card definition! Card name: " + newCardDefinition.Id.Name
            );

            return newCardDefinition.Id;
        }

        public bool TryGet(CardDefinitionId id, out CardDefinition result)
        {
            return _cards.TryGetValue(id, out result);
        }

        public CardDefinition Get(CardDefinitionId id)
        {
            if (!_cards.TryGetValue(id, out CardDefinition result))
            {
                MyAssert.Assert(false, "CardDefinition not found! DefinitionId: " + id);
            }

            return result;
        }
    }
}