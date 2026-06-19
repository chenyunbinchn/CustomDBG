using cards.definition;
using cards.instance;

namespace gameStates.persistant
{
    public class PlayerState
    {
        public bool IsInBattle;
        public int[] Hps;
        public int[] EnergiesLimit;
        public int[] Golds;
        
        public CardInstanceManager[] DeckManagers;

        // Todo: So many field need to custom; Make CharacterScriptableObject and make multiple playerState instance instead? Only receive SO to initialize?
        //       PlayerState[].
        public void Init(CardDefinitionManager cardDefinitionManager, int playerNum, int initGold, int initHp, int initEnergy)
        {
            DeckManagers = new CardInstanceManager[playerNum];
            Hps = new int[playerNum];
            EnergiesLimit = new int[playerNum];
            Golds = new int[playerNum];
            DeckManagers = new CardInstanceManager[playerNum];
            
            for (int i = 0; i < playerNum; i++)
            {
                Hps[i] = initHp;
                EnergiesLimit[i] = initEnergy;
                Golds[i] = initGold;

                CardInstanceManager curDeck = DeckManagers[i];
                curDeck.AddCardToDeck(cardDefinitionManager.Get(new CardDefinitionId("攻击"))); // Test
                curDeck.AddCardToDeck(cardDefinitionManager.Get(new CardDefinitionId("攻击"))); // Test
                curDeck.AddCardToDeck(cardDefinitionManager.Get(new CardDefinitionId("攻击"))); // Test
                curDeck.AddCardToDeck(cardDefinitionManager.Get(new CardDefinitionId("防御"))); // Test
                curDeck.AddCardToDeck(cardDefinitionManager.Get(new CardDefinitionId("防御"))); // Test
                curDeck.AddCardToDeck(cardDefinitionManager.Get(new CardDefinitionId("防御"))); // Test
            }
        }
    }
}