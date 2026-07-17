using cards.definition;
using combat;
using enums;
using gameStates;
using gameStates.transient;
using hook;
using systems;
using UnityEngine;
using UnityEngine.InputSystem;

namespace mono
{
    public class UnityBoostrap : MonoBehaviour
    {
        public CardDefinitionLibrarySO allCardLibrarySo;
        public StateManager StateManager = new StateManager();
        
        private void Start()
        {
            // Todo: Base on room's player number set playerNum
            StateManager.Init(allCardLibrarySo, this, 4);
            Debug.Log("MainSeed: " + StateManager.GameState.SeedManager.MainSeed + "\n");
        }

        private void Update()
        {
            // Note: The command pump — the ONLY place battle commands get executed. Runs at most one
            //       command per frame, and only at quiescence (action queue drained).
            BattleCommandApi.Pump(StateManager.GameState.BattleCommandManager, StateManager.BattleState,
                StateManager.GameState.GameActionManager, StateManager.GameState.ActionExecutor);

            // Test code
            if (Keyboard.current == null)
            {
                return;
            }

            if (Keyboard.current.qKey.wasPressedThisFrame)
            {
                BattleApi.EnterBattle(StateManager.BattleState, StateManager.GamePlayerState, StateManager.BattlePlayerStates,
                    StateManager.GameState.RandomManager);
            }

            if (Keyboard.current.wKey.wasPressedThisFrame)
            {
                // Note: Test input builds a BattleCommand like any client would — the keyboard is
                //       just the earliest command producer. Plays player #0's first hand card.
                BattlePlayerState player0 = StateManager.BattlePlayerStates[0];
                if (player0.HandCards.Count > 0)
                {
                    ActionEntityId target = StateManager.BattleState.EnemyList.Count > 0
                        ? StateManager.BattleState.EnemyList[0].Id
                        : default;
                    BattleCommand command = new BattleCommand(EnumCommandType.PlayCard, player0.Id, player0.HandCards[0], target);
                    BattleCommandApi.Submit(command, StateManager.GameState.BattleCommandManager);
                }
            }

            if (Keyboard.current.aKey.wasPressedThisFrame)
            {
                // Todo: Add cards
            }

            if (Keyboard.current.sKey.wasPressedThisFrame)
            {
                BattlePileApi.DrawCards(StateManager.BattlePlayerStates[0], 3, StateManager.GameState.RandomManager);
            }

            if (Keyboard.current.eKey.wasPressedThisFrame)
            {
                // Test: apply the Afterimage status via the registry (after each card played, +2 block).
                BattlePlayerState player = StateManager.BattlePlayerStates[0];
                if (StatusRegistry.TryGetTemplate(EnumStatusType.Afterimage, out HookListener template))
                {
                    player.HookListeners.Add(new HookListener { Host = player, Hook = template.Hook, Effect = template.Effect });
                    Debug.Log($"[Test] Applied Afterimage. Listeners={player.HookListeners.Count}, Block={player.Block}");
                }
            }

        }
    }
}