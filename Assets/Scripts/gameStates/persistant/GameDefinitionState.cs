using cards.definition;

namespace gameStates.persistant
{
    public class GameDefinitionState
    {
        public CardDefinitionManager CardDefinitionManager = new CardDefinitionManager();

        public void Init(CardDefinitionLibrarySO cardDefinitionLibrarySo)
        {
            CardDefinitionManager.Init(cardDefinitionLibrarySo);
        }
    }
}