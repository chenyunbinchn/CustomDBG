using gameStates.transient;

namespace systems
{
    public class BattleSystem
    {
        // Todo: Do I really need to make it a system?? Do I have to update every frame?? 
        public void Update(BattleState battleState, BattleCardState cardState)
        {
            if (!battleState.IsPlayerTurn)
            {
                // Todo: Handle monster logic
                return;
            }
            // Todo: Hook before draw card.
            DrawCard();
            // Todo: Hook after draw card.
            PlayCard();
            // Todo: Hook after play card.
            // Todo: Check if player hit turn over
            // Todo: Hook after player's turn over
        }

        private void PlayCard()
        {
            throw new System.NotImplementedException();
        }

        private void DrawCard()
        {
            
        }
    }
}