using System.Collections.Generic;
using cards.instance;
using hook;

namespace player
{
    public class PlayerInfo : IHookListener
    {
        public int Hp;
        public int EnergiesLimit;
        public int Golds;
        public CardDeck CardDeck;

        public List<HookListener> HookListeners { get; } = new List<HookListener>();
        
        public PlayerInfo(int hp, int energiesLimit, int golds) // Todo: Receive PlayerScriptableObject as para
        {
            Hp = hp;
            EnergiesLimit = energiesLimit;
            Golds = golds;
            CardDeck = new CardDeck();
        }
    }
}