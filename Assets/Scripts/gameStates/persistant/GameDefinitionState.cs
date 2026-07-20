using cards.definition;

namespace gameStates.persistant
{
    public class GameDefinitionState
    {
        public CardDefinitionManager CardDefinitionManager = new CardDefinitionManager();

        public void Init(string cardsJson)
        {
            CardDefinitionManager.Init(cardsJson);
        }
    }
}
