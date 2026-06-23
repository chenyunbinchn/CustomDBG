using action;
using action.cardEffectActions;
using cards.definition;
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

        // Note: Pass in isPlayerTurn but check the bool value immediately makes me feel silly lol
        public void TryPlayCard(bool isPlayerTurn, GameActionManager actionManager, CardPileManager pileManager, ActionExecutor actionExecutor, 
            CardInstanceId cardId, [CanBeNull] EnemyInstance target)
        {
            if (!isPlayerTurn)
            {
                Debug.Log("Not player's turn!!");
                return;
            }
            
            if (actionExecutor.IsRunning)
            {
                Debug.Log("Action executor is running!!"); // Todo: Let UI layer handle defeat. Play defeat animation, and tell player cant do.
                return;
            }

            pileManager.Dictionary.TryGetValue(cardId, out CardInstance cardInstance);
            TranslateEffect(actionManager, cardInstance);
        }
        
        private void TranslateEffect(GameActionManager actionManager, CardInstance instance)
        {
            CardDefinition cardDefinition = instance.Definition;

            for (int i = 0; i < cardDefinition.Effects.Length; i++)
            {
                GameAction action = null;
                switch (cardDefinition.Effects[i].EffectType)
                {
                    case EnumCardEffectType.DealDamage:
                        action = DamageAction(cardDefinition.Effects[i].);
                    case EnumCardEffectType.CostEnergy:
                        action = new CostEnergyAction();
                        break;
                    case EnumCardEffectType.ApplyStatus:
                        action = new ApplyStatusAction();
                        break;
                    // CLAUDE: Todo: Implement here.
                }
                MyAssert.Assert(action != null, "Effect can't be translated to action, null action detected!!");
                actionManager.Add(action);
            }
        }
    }
}