using action;
using action.cardEffectActions;
using cards.definition;
using cards.effects;
using cards.instance;
using enemy.instance;
using enums;
using gameStates.transient;
using JetBrains.Annotations;
using tools.assert;
using UnityEngine;

namespace systems
{
    // Note: BattleSystem translate CardEffect to GameAction.
    public class BattleSystem
    {
        // Todo: Do I really need to make it a system?? Do I have to update every frame?? 
        public void Update(BattleState battleState, BattleCardPileState cardPileState)
        {
            if (!battleState.IsPlayerTurn)
            {
                // Todo: Handle monster logic
                return;
            }
            // Todo: Hook before draw card.
            // Todo: Draw cards.
            // Todo: Hook after draw card.
            // Todo: PlayCard();
            // Todo: Hook after play card.
            // Todo: Check if player hit turn over
            // Todo: Hook after player's turn over
        }

        public void TryPlayCard(BattleContext context, GameActionManager actionManager, ActionExecutor actionExecutor,
            CardInstanceId cardId, [CanBeNull] EnemyInstance target)
        {
            // Note: TryPlayCard is ALLOWED to fail (wrong turn / executor busy / not enough energy / bad id).
            //       It must NOT MyAssert/crash — just log and return so the UI layer can give feedback.
            if (!context.BattleState.IsPlayerTurn)
            {
                Debug.Log("Not player's turn!!"); // Todo: Let UI layer handle this — play feedback, tell player cant do.
                return;
            }

            if (actionExecutor.IsRunning)
            {
                Debug.Log("Action executor is running!!"); // Todo: Let UI layer handle this.
                return;
            }

            // Todo: Check energy affordability (sum CostEnergy effects vs BattleState.PlayerEnergies); fail gracefully to UI if not enough.

            bool found = context.CardPileState.PileManager.Dictionary.TryGetValue(cardId, out CardInstance cardInstance);
            if (!found)
            {
                Debug.Log($"CardInstance not found for id {cardId.Value}!!"); // Todo: UI feedback.
                return;
            }

            TranslateEffect(actionManager, cardInstance, target);
            actionExecutor.Kick(actionManager, context);
        }

        private void TranslateEffect(GameActionManager actionManager, CardInstance instance, [CanBeNull] EnemyInstance target)
        {
            CardDefinition cardDefinition = instance.Definition;

            for (int i = 0; i < cardDefinition.Effects.Length; i++)
            {
                CardEffect effect = cardDefinition.Effects[i];
                GameAction action = null;
                switch (effect.EffectType)
                {
                    case EnumCardEffectType.CostEnergy:
                        action = new CostEnergyAction(effect.Value, actionManager.NextId(), EnumActionStatus.WaitingForExecution);
                        break;
                    case EnumCardEffectType.GainEnergy:
                        action = new GainEnergyAction(effect.Value, actionManager.NextId(), EnumActionStatus.WaitingForExecution);
                        break;
                    case EnumCardEffectType.DealDamage:
                        // Todo: multi-target (AllEnemy/RandomEnemy) resolution; for now only the single selected `target` is captured.
                        action = new DamageAction(effect.Value, effect.TargetType, target, actionManager.NextId(), EnumActionStatus.WaitingForExecution);
                        break;
                    case EnumCardEffectType.DrawCards:
                        action = new DrawCardAction(effect.Value, actionManager.NextId(), EnumActionStatus.WaitingForExecution);
                        break;
                    case EnumCardEffectType.GainBlock:
                        action = new GainBlockAction(effect.Value, actionManager.NextId(), EnumActionStatus.WaitingForExecution);
                        break;
                    case EnumCardEffectType.ApplyStatus:
                        // Todo: multi-target resolution; for now only the single selected `target` is captured.
                        action = new ApplyStatusAction(effect.StatusType, effect.Value, effect.TargetType, target, actionManager.NextId(), EnumActionStatus.WaitingForExecution);
                        break;
                    case EnumCardEffectType.Exhaust:
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