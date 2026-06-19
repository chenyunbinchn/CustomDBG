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
            _gameState.Init(AllCardLibrary);
            Debug.Log("MainSeed: " + _gameState.SeedManager.MainSeed + "\n");
        }

        private void Update()
        {

        }
    }
}