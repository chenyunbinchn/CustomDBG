using cards.definition;
using gameStates;
using gameStates.persistant;
using gameStates.transient;
using systems;
using UnityEngine;

namespace mono
{
    public class UnityBoostrap : MonoBehaviour
    {
        public CardDefinitionLibrary AllCardLibrary;
        public StateManager StateManager = new StateManager();
        private GameState _gameState;
        
        private void Start()
        {
            GameState gameState = new GameState();
            gameState.Init(AllCardLibrary, this);
            Debug.Log("MainSeed: " + gameState.SeedManager.MainSeed + "\n");
        }

        private void Update()
        {
            // Test code
            // if (Input.GetKeyDown(KeyCode.Q))
            // {
            //     BattleApi.TryPlayCard(_gameConfigState. , _gameConfigState.GameActionManager, _gameConfigState.ActionExecutor, _gameConfigState.ActionExecutor, "攻击");    
            // }
            
        }
    }
}