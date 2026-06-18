using System.Collections.Generic;
using enemy.instance;
using tools.assert;

namespace gameStates.transient
{
    public class BattleState
    {
        // public GameActionQueue ActionQueue;
        public List<EnemyInstance> EnemyList;
        public int[] PlayerEnergies;
        public int TurnNum;
        public bool IsPlayerTurn; // Todo: Check if monster really need 'turn'

        public void Init(List<EnemyInstance> enemies, int[] playerEnergiesLimit)
        {
            MyAssert.Assert(enemies.Count != 0, "enemies.Count == 0");
            EnemyList = enemies;
            for (int i = 0; i < PlayerEnergies.Length; i++)
            {
                // Todo: Bad smell here? What if an item eat player's energy while battle begin? Hook?
                PlayerEnergies[i] = playerEnergiesLimit[i]; 
            }
            PlayerEnergies = playerEnergiesLimit;
            TurnNum = 0;
            IsPlayerTurn = true;
        }
    }
}