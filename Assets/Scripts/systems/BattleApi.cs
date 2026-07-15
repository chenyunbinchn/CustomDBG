using System.Collections.Generic;
using action;
using cards.instance;
using combat;
using enemy.instance;
using enums;
using gameStates.persistant;
using gameStates.transient;
using hook;
using JetBrains.Annotations;
using random;
using UnityEngine;

namespace systems
{
    // Note: BattleApi drives card play — translates a card's Effects to GameActions (via EffectApi),
    //       fires hooks (via HookSystem), and kicks the executor.
    public static class BattleApi
    {
        // Todo: hooks for before/after draw card, turn start/end (only AfterCardPlayed is wired so far).

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

        // Todo: Remove test function
        public static void TryPlayHandCard(BattleState battleState, BattlePlayerState battlePlayerState, GameActionManager actionManager, ActionExecutor actionExecutor,
            int index, [CanBeNull] EnemyInstance target)
        {
            if (index < 0 || index >= battlePlayerState.HandCards.Count)
            {
                Debug.Log($"Card Index {index} not in hand (Count={battlePlayerState.HandCards.Count})!!");
                return;
            }

            CardInstanceId cardId = battlePlayerState.HandCards[index];
            if (TryPlayCard(battleState, battlePlayerState, actionManager, actionExecutor, cardId, target))
            {
                battlePlayerState.DiscardPile.Add(cardId);
                battlePlayerState.HandCards.RemoveAt(index);
                Debug.Log($"[Pile] Play {battlePlayerState.PileManager.DescribeCard(cardId)} -> Hand({battlePlayerState.HandCards.Count}): {battlePlayerState.PileManager.DescribePile(battlePlayerState.HandCards)}; Discard={battlePlayerState.DiscardPile.Count}");
            }
        }

        // Note: Get CardInstanceId from UI view layer. Player choose hand card etc.
        public static bool TryPlayCard(BattleState battleState, BattlePlayerState battlePlayerState, GameActionManager actionManager, ActionExecutor actionExecutor,
            CardInstanceId cardId, [CanBeNull] EnemyInstance target)
        {
            // Note: TryPlayCard is ALLOWED to fail (wrong turn / executor busy / not enough energy / bad id).
            //       It must NOT MyAssert/crash — just log and return so the UI layer can give feedback.
            if (!battleState.IsPlayerTurn)
            {
                Debug.Log("Not player's turn!!"); // Todo: Let UI layer handle this.
                return false;
            }

            if (actionExecutor.IsRunning)
            {
                Debug.Log("Action executor is running!!"); // Todo: Let UI layer handle this.
                return false;
            }

            // Todo: Check energy affordability (sum CostEnergy effects vs player energy); fail gracefully to UI if not enough.

            bool found = battlePlayerState.PileManager.Dictionary.TryGetValue(cardId, out CardInstance cardInstance);
            if (!found)
            {
                Debug.Log($"CardInstance not found for id {cardId.Value}!!"); // Todo: UI feedback.
                return false;
            }

            ActionEntityId source = battlePlayerState.Id;
            ActionEntityId picked = target != null ? target.Id : default;
            for (int i = 0; i < cardInstance.Effects.Length; i++)
            {
                EffectApi.Translate(cardInstance.Effects[i], source, picked, actionManager);
            }
            HookSystem.Fire(EnumHookType.AfterCardPlayed, battleState, battlePlayerState, actionManager);
            actionExecutor.Kick(actionManager, battleState, battlePlayerState);
            return true;
        }
    }
}
