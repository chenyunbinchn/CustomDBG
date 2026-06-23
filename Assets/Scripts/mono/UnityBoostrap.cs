using action;
using action.cardEffectActions;
using cards.definition;
using gameStates.persistant;
using UnityEngine;

namespace mono
{
    public class UnityBoostrap : MonoBehaviour
    {
        public CardDefinitionLibrary AllCardLibrary;
        private GameState _gameState;
        
        private void Start()
        {
            GameState _gameState = new GameState();
            _gameState.Init(AllCardLibrary, this);
            Debug.Log("MainSeed: " + _gameState.SeedManager.MainSeed + "\n");

            // Todo: Remove test code
            GameActionManager actionManager = _gameState.GameActionManager;
            actionManager.Add(new DamageAction().Create(6));
            actionManager.Add(new DamageAction().Create(9));
        }

        private void Update()
        {
            _gameState.ActionExecutor.Kick(_gameState.GameActionManager);
        }
    }
}