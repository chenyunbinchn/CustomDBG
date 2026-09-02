using cards.definition;
using cards.instance;
using combat;
using enums;
using player;

namespace gameStates.persistant
{
    public class GamePlayerState
    {
        public PlayerInfo[] PlayerInfos;
        
        // Todo: Receive PlayerScriptableObject as para, instead of initGold, initHp, initEnergiesLimit ...
        public void Init(CardDefinitionManager cardDefinitionManager, int playerNum, int initGold, int initHp, int initEnergiesLimit)
        {
            PlayerInfos = new PlayerInfo[playerNum];
            
            for (int i = 0; i < playerNum; i++)
            {
                PlayerInfos[i] = new PlayerInfo(initHp, initEnergiesLimit, initGold, new ActionEntityId(EnumEntityType.Player, (uint)i));
                CardDeck curDeck = PlayerInfos[i].CardDeck;
                curDeck.AddCardToDeck(cardDefinitionManager.Get(new CardDefinitionId("攻击"))); // Test
                curDeck.AddCardToDeck(cardDefinitionManager.Get(new CardDefinitionId("攻击"))); // Test
                curDeck.AddCardToDeck(cardDefinitionManager.Get(new CardDefinitionId("防御"))); // Test
                curDeck.AddCardToDeck(cardDefinitionManager.Get(new CardDefinitionId("防御"))); // Test
                curDeck.AddCardToDeck(cardDefinitionManager.Get(new CardDefinitionId("剑柄打击"))); // Test
                curDeck.AddCardToDeck(cardDefinitionManager.Get(new CardDefinitionId("残影"))); // Test
                curDeck.AddCardToDeck(cardDefinitionManager.Get(new CardDefinitionId("残影"))); // Test
            }
        }

    }
}