using System.Collections.Generic;
using gameStates;

namespace hook
{
    public class HookApi
    {
        public List<HookListener> IterateHookListeners(StateManager stateManager)
        {
            List<HookListener> hookList = new List<HookListener>();

            if (stateManager.GameState.IsInBattle)
            {
                for (int i = 0; i < stateManager.PlayerNum; i++)
                {
                    hookList.AddRange(stateManager.BattlePlayerStates[i].HookListeners);
                }

                for (int i = 0; i < stateManager.BattleState.EnemyList.Count; i++)
                {
                    hookList.AddRange(stateManager.BattleState.EnemyList[i].HookListeners);
                }
            }
            return hookList;
        }
        
        
    }
}