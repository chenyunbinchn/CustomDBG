using cards.definition;
using cards.instance;

namespace gameStates.persistant
{
    public class GamePlayerState
    {
        public int[] Hps;
        public int[] EnergiesLimit;
        public int[] Golds;
        
        public CardDeckManager[] DeckManagers;

        // Todo: So many field need to custom; Make CharacterScriptableObject and make multiple playerState instance instead? Only receive SO to initialize?
        //       PlayerState[].
        public void Init(CardDefinitionManager cardDefinitionManager, int playerNum, int initGold, int initHp, int initEnergy)
        {
            DeckManagers = new CardDeckManager[playerNum];
            Hps = new int[playerNum];
            EnergiesLimit = new int[playerNum];
            Golds = new int[playerNum];

            for (int i = 0; i < playerNum; i++)
            {
                Hps[i] = initHp;
                EnergiesLimit[i] = initEnergy;
                Golds[i] = initGold;

                DeckManagers[i] = new CardDeckManager();
                CardDeckManager curDeck = DeckManagers[i];
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