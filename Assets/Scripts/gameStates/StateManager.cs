using cards.definition;
using gameStates.persistant;
using gameStates.transient;
using UnityEngine;

namespace gameStates
{
    public class StateManager
    {
        public int PlayerNum;
        // Persistant States
        public GameState GameState = new GameState();
        public GamePlayerState GamePlayerState = new GamePlayerState();
        
        // Transient States
        public BattleState BattleState;
        public BattlePlayerState[] BattlePlayerStates;
        
        public void Init(CardDefinitionLibrary library, MonoBehaviour unityBoostrap, int playerNum)
        {
            PlayerNum = playerNum;
            BattlePlayerStates = new BattlePlayerState[PlayerNum];
            GameState.Init(library, unityBoostrap);
            GamePlayerState.Init(GameState.CardDefinitionManager, 1, 50, 100, 3);
            
        }
        

    }
}