using System.Collections.Generic;
using cards.instance;
using random;

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

        // Note: Build the initial draw pile from PileManager.Dictionary. Deterministic for replay:
        //       sort ids by Value first, then Fisher-Yates shuffle via RandomManager's shuffle domain.
        public void BuildDrawPile(RandomManager randomManager)
        {
            DrawPile.Clear();
            List<CardInstanceId> ids = new List<CardInstanceId>(PileManager.Dictionary.Keys);
            ids.Sort((CardInstanceId a, CardInstanceId b) => a.Value.CompareTo(b.Value));
            DrawPile.AddRange(ids);

            for (int i = DrawPile.Count - 1; i > 0; i--)
            {
                int j = randomManager.ShuffleNextInt(i + 1);
                CardInstanceId tmp = DrawPile[i];
                DrawPile[i] = DrawPile[j];
                DrawPile[j] = tmp;
            }
        }
    }
}