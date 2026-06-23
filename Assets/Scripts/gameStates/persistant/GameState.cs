using action;
using cards.definition;
using enums;
using random;
using UnityEngine;

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
        public ActionExecutor ActionExecutor;
        
        public void Init(CardDefinitionLibrary library, MonoBehaviour unityBoostrap)
        {
            ActionExecutor = new ActionExecutor(unityBoostrap);
            SeedManager.Init(41u); // Use 42u for testing. Todo: Let user choose the seed, or generate seed
            RandomManager.Init(SeedManager);
            CardDefinitionManager.Init(library);
        }
    }
}
