using System.IO;
using combat;
using enums;
using gameStates;
using gameStates.transient;
using hook;
using systems;
using tools.assert;
using ui.controllers;
using UnityEngine;
using UnityEngine.InputSystem;

namespace mono
{
    public class UnityBoostrap : MonoBehaviour
    {
        [SerializeField] private string cardsJsonFileName = "cards.json";
        public StateManager StateManager = new StateManager();

        private BattleUIController _battleUIController;

        private void Start()
        {
            // Note: Card data is JSON under StreamingAssets (replaces the old ScriptableObject library).
            //       Todo: Android / WebGL can't File.ReadAllText StreamingAssets — use UnityWebRequest there.
            string cardsJson = File.ReadAllText(Path.Combine(Application.streamingAssetsPath, cardsJsonFileName));
            // Todo: Base on room's player number set playerNum
            StateManager.Init(cardsJson, this, 4);
            _battleUIController = GetComponent<BattleUIController>();
            MyAssert.Assert(_battleUIController != null,
                "UnityBoostrap requires BattleUIController for the current battle UI test flow.");
            Debug.Log("MainSeed: " + StateManager.GameState.SeedManager.MainSeed + "\n");
        }

        private void Update()
        {
            // Note: The command pump — the ONLY place battle commands get executed. Runs at most one
            //       command per frame, and only at quiescence (action queue drained).
            BattleCommandApi.Pump(StateManager);

            // Test code
            if (Keyboard.current == null)
            {
                return;
            }

            if (Keyboard.current.qKey.wasPressedThisFrame)
            {
                BattleApi.EnterBattle(StateManager.BattleState, StateManager.GamePlayerState, StateManager.BattlePlayerStates,
                    StateManager.GameState.RandomManager);
                _battleUIController?.OpenBattleUi();
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
                _battleUIController?.RefreshBattleHud();
            }

            // Todo: Remove Afterimage test code.
            if (Keyboard.current.eKey.wasPressedThisFrame)
            {
                // Test: apply the Afterimage status via the registry (after each card played, +2 block).
                BattlePlayerState player = StateManager.BattlePlayerStates[0];
                if (StatusRegistry.TryGetTemplate(EnumStatusType.Afterimage, out HookListener template))
                {
                    player.HookListeners.Add(new HookListener
                    {
                        Owner = player.Id,
                        Hook = template.Hook,
                        Filter = template.Filter,
                        Effect = template.Effect
                    });
                    Debug.Log($"[Test] Applied Afterimage. Listeners={player.HookListeners.Count}, Block={player.Block}");
                }
            }

        }
    }
}
