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
        public GameDefinitionState GameDefinitionState = new GameDefinitionState();
        public GamePlayerState GamePlayerState = new GamePlayerState();
        
        // Transient States
        public BattleState BattleState = new BattleState();
        public BattlePlayerState[] BattlePlayerStates;
        
        public void Init(CardDefinitionLibrarySO librarySO, MonoBehaviour unityBoostrap, int playerNum)
        {
            PlayerNum = playerNum;
            // Persistant States Init
            GameState.Init(unityBoostrap);
            GameDefinitionState.Init(librarySO);
            GamePlayerState.Init(GameDefinitionState.CardDefinitionManager, 1, 50, 100, 3);
            // Transient States
            BattlePlayerStates = new BattlePlayerState[PlayerNum];
        }
    }
}