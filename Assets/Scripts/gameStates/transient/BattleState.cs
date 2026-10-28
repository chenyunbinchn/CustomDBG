using System.Collections.Generic;
using enemy.instance;
using System;

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
        public bool IsBattleOver { get; private set; }
        public bool PlayersWon { get; private set; }
        public bool IsFaulted { get; private set; }
        public string FaultReason { get; private set; }

        public void Reset(List<EnemyInstance> enemies)
        {
            if (enemies == null || enemies.Count == 0)
                throw new ArgumentException("A battle requires at least one enemy.", nameof(enemies));
            EnemyList = enemies;
            TurnNum = 1;
            IsPlayerTurn = true;
            IsBattleOver = false;
            PlayersWon = false;
            IsFaulted = false;
            FaultReason = null;
        }

        public void MarkFault(string reason)
        {
            IsFaulted = true;
            IsPlayerTurn = false;
            FaultReason = reason;
        }

        public void EvaluateOutcome()
        {
            if (IsFaulted || IsBattleOver)
            {
                return;
            }

            if (Players != null)
            {
                for (int i = 0; i < Players.Length; i++)
                {
                    if (Players[i].Hp <= 0)
                    {
                        IsBattleOver = true;
                        PlayersWon = false;
                        IsPlayerTurn = false;
                        return;
                    }
                }
            }

            if (EnemyList.Count == 0)
            {
                IsBattleOver = true;
                PlayersWon = true;
                IsPlayerTurn = false;
            }
        }
    }
}
