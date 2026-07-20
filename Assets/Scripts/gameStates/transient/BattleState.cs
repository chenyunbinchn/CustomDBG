using System.Collections.Generic;
using enemy.instance;
using tools.assert;

namespace gameStates.transient
{
    // Note: BattleState is shared by all players
    public class BattleState
    {
        // Note: All players in this battle, indexed by ActionEntityId.Id (assigned by
        //       StateManager.Init). Same array instance as StateManager.BattlePlayerStates.
        public BattlePlayerState[] Players;
        public List<EnemyInstance> EnemyList = new List<EnemyInstance>();
        public int TurnNum = 0;
        public bool IsPlayerTurn = false; // Todo: Check if monster really need 'turn'

        public void Reset(List<EnemyInstance> enemies)
        {
            MyAssert.Assert(enemies.Count != 0, "enemies.Count == 0");
            EnemyList = enemies;
            TurnNum++;
            IsPlayerTurn = true;
        }
    }
}