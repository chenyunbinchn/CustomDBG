using cards.definition;
using gameStates;
using UnityEngine;

namespace mono
{
    public class UnityBoostrap : MonoBehaviour
    {
        public CardDefinitionLibrarySO allCardLibrarySo;
        public StateManager StateManager = new StateManager();
        
        private void Start()
        {
            // Todo: Base on room's player number set playerNum
            StateManager.Init(allCardLibrarySo, this, 4);
            Debug.Log("MainSeed: " + StateManager.GameState.SeedManager.MainSeed + "\n");
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