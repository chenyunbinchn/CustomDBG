using action;
using combat;
using enums;
using random;
using UnityEngine;

namespace gameStates.persistant
{
    public class GameState
    {
        public EnumDifficulty Difficulty;
        public SeedManager SeedManager = new SeedManager();
        public RandomManager RandomManager = new RandomManager();
        public GameActionManager GameActionManager = new GameActionManager();
        public BattleCommandManager BattleCommandManager = new BattleCommandManager();
        public ActionExecutor ActionExecutor;
        
        public bool IsInBattle = false;
        
        public void Init(MonoBehaviour unityBoostrap)
        {
            ActionExecutor = new ActionExecutor(unityBoostrap);
            SeedManager.Init(41u); // Use 42u for testing. Todo: Let user choose the seed, or generate seed
            RandomManager.Init(SeedManager);
        }
    }
}
