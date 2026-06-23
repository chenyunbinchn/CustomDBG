using cards.definition;
using cards.instance;
using gameStates.transient;

namespace gameStates.persistant
{
    public class PlayerState
    {
        public bool IsInBattle; // Todo: Consider to change to GameState, since players will enter battle together.
        public int PlayerNum;
        public int[] Hps;
        public int[] EnergiesLimit;
        public int[] Golds;
        
        public CardDeckManager[] DeckManagers;
        public BattleCardPileState[] CardPileStates;

        // Todo: So many field need to custom; Make CharacterScriptableObject and make multiple playerState instance instead? Only receive SO to initialize?
        //       PlayerState[].
        public void Init(CardDefinitionManager cardDefinitionManager, int playerNum, int initGold, int initHp, int initEnergy)
        {
            PlayerNum = playerNum;
            DeckManagers = new CardDeckManager[playerNum];
            Hps = new int[playerNum];
            EnergiesLimit = new int[playerNum];
            Golds = new int[playerNum];
            DeckManagers = new CardDeckManager[playerNum];
            CardPileStates = new BattleCardPileState[playerNum];
            
            for (int i = 0; i < playerNum; i++)
            {
                Hps[i] = initHp;
                EnergiesLimit[i] = initEnergy;
                Golds[i] = initGold;

                CardDeckManager curDeck = DeckManagers[i];
                curDeck.AddCardToDeck(cardDefinitionManager.Get(new CardDefinitionId("攻击"))); // Test
                curDeck.AddCardToDeck(cardDefinitionManager.Get(new CardDefinitionId("攻击"))); // Test
                curDeck.AddCardToDeck(cardDefinitionManager.Get(new CardDefinitionId("攻击"))); // Test
                curDeck.AddCardToDeck(cardDefinitionManager.Get(new CardDefinitionId("防御"))); // Test
                curDeck.AddCardToDeck(cardDefinitionManager.Get(new CardDefinitionId("防御"))); // Test
                curDeck.AddCardToDeck(cardDefinitionManager.Get(new CardDefinitionId("防御"))); // Test
            }
        }

        public void EnterBattle()
        {
            for (int i = 0; i < PlayerNum; i++)
            {
                CardPileStates[i].Reset();
                CardPileStates[i].PileManager.CopyFromDeck(DeckManagers[i].Deck);
            }
        }
    }
}