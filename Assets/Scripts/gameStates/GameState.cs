using System.Collections.Generic;
using cards.definition;
using cards.instance;
using enums;
using random;
using tools.shuffle;

namespace gameStates
{
    public class GameState
    {
        public EnumDifficulty Difficulty;
        public PlayerState PlayerState = new PlayerState();
        public BattleCardState BattleCardState = new BattleCardState();
        public CardDefinitionManager CardDefinitionManager = new CardDefinitionManager();
        public SeedManager SeedManager = new SeedManager();
        public RandomManager RandomManager = new RandomManager();
        public CardInstanceManager CardInstanceManager = new CardInstanceManager();
        
        public void Init(CardDefinitionLibrary library)
        {
            SeedManager.Init(41u); // Use 42u for testing. Todo: Let user choose the seed, or generate seed
            RandomManager.Init(SeedManager);
            CardDefinitionManager.Init(library);
            CardInstanceManager.AddCardToDeck(CardDefinitionManager.Get(new CardDefinitionId("攻击"))); // Test
            CardInstanceManager.AddCardToDeck(CardDefinitionManager.Get(new CardDefinitionId("攻击"))); // Test
            CardInstanceManager.AddCardToDeck(CardDefinitionManager.Get(new CardDefinitionId("攻击"))); // Test
            CardInstanceManager.AddCardToDeck(CardDefinitionManager.Get(new CardDefinitionId("防御"))); // Test
            CardInstanceManager.AddCardToDeck(CardDefinitionManager.Get(new CardDefinitionId("防御"))); // Test
            CardInstanceManager.AddCardToDeck(CardDefinitionManager.Get(new CardDefinitionId("防御"))); // Test
            ShuffleHelper.Shuffle(CardInstanceManager.CardInstances, RandomManager.ShuffleNextInt);
        }
    }
    
}
