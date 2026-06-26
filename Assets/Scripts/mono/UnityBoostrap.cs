using cards.definition;
using gameStates;
using systems;
using UnityEngine;
using UnityEngine.InputSystem;

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
            if (Keyboard.current == null)
            {
                return;
            }

            if (Keyboard.current.qKey.wasPressedThisFrame)
            {
                BattleApi.EnterBattle(StateManager.BattleState, StateManager.GamePlayerState, StateManager.BattlePlayerStates,
                    StateManager.GameState.RandomManager);
            }

            if (Keyboard.current.wKey.wasPressedThisFrame)
            {
                BattleApi.TryPlayHandCard(StateManager.BattleState, StateManager.BattlePlayerStates[0],
                    StateManager.GameState.GameActionManager, StateManager.GameState.ActionExecutor,
                    0, null);
            }

            if (Keyboard.current.aKey.wasPressedThisFrame)
            {
                // Todo: Add cards
            }

            if (Keyboard.current.sKey.wasPressedThisFrame)
            {
                BattlePileApi.DrawCards(StateManager.BattlePlayerStates[0], 3, StateManager.GameState.RandomManager);
            }
            
        }
    }
}