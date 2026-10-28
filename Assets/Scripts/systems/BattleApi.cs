using System.Collections.Generic;
using combat;
using enemy.instance;
using enums;
using gameStates;
using gameStates.persistant;
using gameStates.transient;
using random;

namespace systems
{
    // Note: BattleApi drives the battle lifecycle (EnterBattle). Card play does NOT live here any
    //       more — all battle input goes through BattleCommandApi (command queue + serial resolution).
    public static class BattleApi
    {
        public static void EnterBattle(StateManager stateManager)
        {
            stateManager.GameState.ActionExecutor.Reset();
            stateManager.GameState.GameActionManager.Reset();
            stateManager.GameState.BattleCommandManager.Reset();
            stateManager.GameState.IsInBattle = true;
            EnterBattle(
                stateManager.BattleState,
                stateManager.GamePlayerState,
                stateManager.BattlePlayerStates,
                stateManager.GameState.RandomManager);
        }

        public static void ExitBattle(StateManager stateManager)
        {
            stateManager.GameState.ActionExecutor.Reset();
            stateManager.GameState.GameActionManager.Reset();
            stateManager.GameState.BattleCommandManager.Reset();
            stateManager.GameState.IsInBattle = false;
        }

        public static void EnterBattle(BattleState battleState, GamePlayerState gamePlayerState, BattlePlayerState[] battlePlayerStates,
            RandomManager randomManager)
        {
            List<EnemyInstance> testEnemies = new List<EnemyInstance>(); // Test
            testEnemies.Add(new EnemyInstance { Id = new ActionEntityId(EnumEntityType.Enemy, 0), Hp = 50 }); // Test
            battleState.Reset(testEnemies); // Test
            for (int i = 0; i < battlePlayerStates.Length; i++)
            {
                battlePlayerStates[i].Reset();
                battlePlayerStates[i].PileManager.CopyFromDeck(gamePlayerState.PlayerInfos[i].CardDeck.Deck);
                BattlePileApi.BuildDrawPile(battlePlayerStates[i], randomManager);
                battlePlayerStates[i].PlayerEnergy = gamePlayerState.PlayerInfos[i].EnergiesLimit;
            }
        }
    }
}
