using System.Collections.Generic;
using action;
using action.gameEffectActions;
using cards.definition;
using cards.effects;
using cards.instance;
using enemy.instance;
using enums;
using gameEffects;
using gameStates.persistant;
using gameStates.transient;
using JetBrains.Annotations;
using random;
using tools.assert;
using UnityEngine;

namespace systems
{
    // Note: BattleSystem translate CardEffect to GameAction.
    public static class BattleApi
    {
        // Todo: !!! Implement HookSystem. Should request hooking while: 1. Enter player turn / End player turn / etc;
        //       2. Some CardEffectAction trigger (ex. DrawCardAction) 
        
        // Todo: Hook before draw card.
        // Todo: Draw cards.
        // Todo: Hook after draw card.
        // Todo: PlayCard();
        // Todo: Hook after play card.
        // Todo: Check if player hit turn over
        // Todo: Hook after player's turn over

        public static void EnterBattle(BattleState battleState, GamePlayerState gamePlayerState, BattlePlayerState[] battlePlayerStates,
            RandomManager randomManager)
        {
            List<EnemyInstance> testEnemies = new List<EnemyInstance>(); // Test
             testEnemies.Add(new EnemyInstance()); // Test
            battleState.Reset(testEnemies); // Test
            for (int i = 0; i < battlePlayerStates.Length; i++)
            {
                battlePlayerStates[i].Reset();
                battlePlayerStates[i].PileManager.CopyFromDeck(gamePlayerState.DeckManagers[i].Deck);
                BattlePileApi.BuildDrawPile(battlePlayerStates[i], randomManager);
                battlePlayerStates[i].PlayerEnergy = gamePlayerState.EnergiesLimit[i];
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
                Debug.Log("Not player's turn!!"); // Todo: Let UI layer handle this — play feedback, tell player cant do.
                return false;
            }

            if (actionExecutor.IsRunning)
            {
                Debug.Log("Action executor is running!!"); // Todo: Let UI layer handle this.
                return false;
            }

            // Todo: Check energy affordability (sum CostEnergy effects vs BattleState.PlayerEnergies); fail gracefully to UI if not enough.

            bool found = battlePlayerState.PileManager.Dictionary.TryGetValue(cardId, out CardInstance cardInstance);
            if (!found)
            {
                Debug.Log($"CardInstance not found for id {cardId.Value}!!"); // Todo: UI feedback.
                return false;
            }

            TranslateEffect(actionManager, cardInstance, target);
            actionExecutor.Kick(actionManager, battleState, battlePlayerState);
            return true;
        }

        private static void TranslateEffect(GameActionManager actionManager, CardInstance instance, [CanBeNull] EnemyInstance target)
        {
            CardDefinition cardDefinition = instance.Definition;

            for (int i = 0; i < cardDefinition.Effects.Length; i++)
            {
                Effect effect = cardDefinition.Effects[i];
                GameAction action = null;
                switch (effect.EffectType)
                {
                    case EnumEffectType.CostEnergy:
                        action = new CostEnergyAction(effect.Value, actionManager.NextId(), EnumActionStatus.WaitingForExecution);
                        break;
                    case EnumEffectType.GainEnergy:
                        action = new GainEnergyAction(effect.Value, actionManager.NextId(), EnumActionStatus.WaitingForExecution);
                        break;
                    case EnumEffectType.DealDamage:
                        // Todo: multi-target (AllEnemy/RandomEnemy) resolution; for now only the single selected `target` is captured.
                        action = new DamageAction(effect.Value, effect.TargetType, target, actionManager.NextId(), EnumActionStatus.WaitingForExecution);
                        break;
                    case EnumEffectType.DrawCards:
                        action = new DrawCardAction(effect.Value, actionManager.NextId(), EnumActionStatus.WaitingForExecution);
                        break;
                    case EnumEffectType.GainBlock:
                        action = new GainBlockAction(effect.Value, actionManager.NextId(), EnumActionStatus.WaitingForExecution);
                        break;
                    case EnumEffectType.ApplyStatus:
                        // Todo: multi-target resolution; for now only the single selected `target` is captured.
                        action = new ApplyStatusAction(effect.StatusType, effect.Value, effect.TargetType, target, actionManager.NextId(), EnumActionStatus.WaitingForExecution);
                        break;
                    case EnumEffectType.Exhaust:
                        // Todo: resolve exhaust targets — Self => the played card (instance.Id); selected => via HandCardChooseAction multi-select.
                        action = new ExhaustCardAction(effect.TargetType, System.Array.Empty<int>(), actionManager.NextId(), EnumActionStatus.WaitingForExecution);
                        break;
                    default:
                        MyAssert.Assert(false, $"Unhandled EffectType: {effect.EffectType}");
                        break;
                }
                MyAssert.Assert(action != null, "Effect can't be translated to action, null action detected!!");
                actionManager.Add(action);
            }
        }
    }
}