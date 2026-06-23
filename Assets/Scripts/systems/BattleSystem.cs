using System.Collections.Generic;
using action;
using cards.definition;
using cards.instance;
using enemy.instance;
using gameStates.persistant;
using gameStates.transient;
using JetBrains.Annotations;
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
            DrawCard(cardPileState.DrawPile, cardPileState.HandCards);
            // Todo: Hook after draw card.
            PlayCard();
            // Todo: Hook after play card.
            // Todo: Check if player hit turn over
            // Todo: Hook after player's turn over
        }

        public bool TryPlayCard(BattleState battleState, BattleCardPileState pileState, ActionExecutor actionExecutor, 
            CardInstanceId cardId, [CanBeNull] EnemyInstance target)
        {
            if (!battleState.IsPlayerTurn)
            {
                Debug.Log("Not player's turn!!");
                return false;
            }
            
            if (actionExecutor.IsRunning)
            {
                Debug.Log("Action executor is running!!"); // Todo: Let UI layer handle defeat. Play defeat animation, and tell player cant do.
                return false;
            }

            pileState.PileManager.Dictionary.TryGetValue(cardId, out CardInstance cardInstance);
            TranslateAction(cardInstance);
        }

        private void DrawCard(List<CardInstanceId> from, List<CardInstanceId> to)
        {
            
        }

        private void TranslateAction(GameActionManager actionManager, CardInstance instance)
        {
            CardDefinition cardDefinition = instance.Definition;
            
            GameAction action = 
            actionManager.Add();
        }
    }
}