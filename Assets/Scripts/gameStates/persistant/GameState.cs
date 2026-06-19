using action;
using cards.definition;
using cards.instance;
using enums;
using random;
using tools.shuffle;

namespace gameStates.persistant
{
    public class GameState
    {
        public EnumDifficulty Difficulty;
        public PlayerState PlayerState = new PlayerState();
        public CardDefinitionManager CardDefinitionManager = new CardDefinitionManager();
        public SeedManager SeedManager = new SeedManager();
        public RandomManager RandomManager = new RandomManager();
        public GameActionManager GameActionManager = new GameActionManager();
        
        public void Init(CardDefinitionLibrary library)
        {
            SeedManager.Init(41u); // Use 42u for testing. Todo: Let user choose the seed, or generate seed
            RandomManager.Init(SeedManager);
            CardDefinitionManager.Init(library);
            
            // Note: Shuffling decks make no sense, just for testing
            foreach (CardInstanceManager deck in PlayerState.DeckManagers)
            {
                ShuffleHelper.Shuffle(deck.CardInstances, RandomManager.ShuffleNextInt); 
            }
        }
    }
    
}
