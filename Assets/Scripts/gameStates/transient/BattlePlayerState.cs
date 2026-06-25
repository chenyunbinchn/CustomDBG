using System.Collections.Generic;
using cards.instance;

namespace gameStates.transient
{
    public class BattlePlayerState
    {
        // Note: Copy deck from PlayerState when initialing. 
        public CardPileManager PileManager = new CardPileManager();
        // Note: Piles will get card from AllCardPile.
        public List<CardInstanceId> DrawPile = new List<CardInstanceId>();
        public List<CardInstanceId> ExhaustedPile = new List<CardInstanceId>();
        public List<CardInstanceId> HandCards = new List<CardInstanceId>();
        public List<CardInstanceId> DiscardPile = new List<CardInstanceId>();
        public List<CardInstanceId> PlayPile = new List<CardInstanceId>();
        
        public int PlayerEnergy;
        
        public void Reset()
        {
            PileManager.Reset();
            
            if (DrawPile.Count > 0)
            {
                DrawPile.Clear();
            }
            if (ExhaustedPile.Count > 0)
            {
                ExhaustedPile.Clear();
            }
            if (HandCards.Count > 0)
            {
                HandCards.Clear();
            }
            if (DiscardPile.Count > 0)
            {
                DiscardPile.Clear();
            }
            if (PlayPile.Count > 0)
            {
               PlayPile.Clear();
            }
        }
    }
}