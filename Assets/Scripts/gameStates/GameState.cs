using cards.definition;
using cards.instance;
using enums;
using random;

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
        public DeckManager DeckManager = new DeckManager();
        
        public void Init(CardDefinitionLibrary library)
        {
            SeedManager.Init(42u); // Use 42u for testing. Todo: Let user choose the seed, or generate seed
            RandomManager.Init(SeedManager);
            CardDefinitionManager.Init(library);
            // DeckManager.Init();
        }
    }
    
}
