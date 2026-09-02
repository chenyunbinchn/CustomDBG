using cards.definition;
using combat;
using enums;
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
        
        public void Init(string cardsJson, MonoBehaviour unityBoostrap, int playerNum)
        {
            PlayerNum = playerNum;
            // Persistant States Init
            GameState.Init(unityBoostrap);
            GameDefinitionState.Init(cardsJson);
            GamePlayerState.Init(GameDefinitionState.CardDefinitionManager, playerNum, 50, 100, 3);
            // Transient States
            BattlePlayerStates = new BattlePlayerState[PlayerNum];
            for (int i = 0; i < PlayerNum; i++)
            {
                // Note: the battle-side facade forwards Hp / Id to this player's persistent PlayerInfo.
                BattlePlayerStates[i] = new BattlePlayerState(GamePlayerState.PlayerInfos[i]);
            }
            // Note: EntityApi.Resolve finds players by indexing this same array through BattleState.
            BattleState.Players = BattlePlayerStates;
        }
    }
}