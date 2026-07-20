using System.Collections.Generic;
using cards.instance;
using combat;
using items.instance;

namespace player
{
    // Note: The player's PERSISTENT record — the single storage for Hp / PlayerId (BattlePlayerState
    //       forwards to them, it does not copy). PlayerInfo itself hosts NO hook listeners: persistent
    //       hooks come from Items, and each ItemInstance carries its own, so losing an item takes its
    //       hooks with it (host decides lifetime — no lifetime field anywhere).
    public class PlayerInfo
    {
        public int Hp;
        public int EnergiesLimit;
        public int Golds;
        public CardDeck CardDeck;
        public ActionEntityId PlayerId;
        public List<ItemInstance> Items = new List<ItemInstance>();

        public PlayerInfo(int hp, int energiesLimit, int golds, ActionEntityId id) // Todo: Receive PlayerScriptableObject as para
        {
            Hp = hp;
            EnergiesLimit = energiesLimit;
            Golds = golds;
            CardDeck = new CardDeck();
            PlayerId = id;
        }
    }
}
