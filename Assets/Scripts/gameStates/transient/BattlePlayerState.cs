using System.Collections.Generic;
using cards.instance;
using combat;
using hook;
using player;
using UnityEngine;

namespace gameStates.transient
{
    // Note: The player's BATTLE-SIDE FACADE. It forwards persistent values (Hp / Id) to the one and
    //       only storage on PlayerInfo — no value copy, so there is no second source of truth and no
    //       "write back at battle end" step. Everything battle-scoped (Block, piles, energy, in-battle
    //       power listeners) is stored here and rebuilt by Reset() at battle start.
    public class BattlePlayerState : ICombatActor
    {
        private readonly PlayerInfo _playerInfo;

        // Note: Copy deck from PlayerState when initialing.
        public CardPileManager PileManager = new CardPileManager();
        // Note: Piles will get card from AllCardPile.
        public List<CardInstanceId> DrawPile = new List<CardInstanceId>();
        public List<CardInstanceId> ExhaustedPile = new List<CardInstanceId>();
        public List<CardInstanceId> HandCards = new List<CardInstanceId>();
        public List<CardInstanceId> DiscardPile = new List<CardInstanceId>();

        public int PlayerEnergy;

        // Note: In-battle powers only (e.g. Afterimage). Persistent hooks live on PlayerInfo.Items —
        //       each ItemInstance carries its own. The host decides the lifetime, so no lifetime field.
        public List<HookListener> HookListeners { get; } = new List<HookListener>();

        // Note: Pass-through — Hp is stored ONCE, on PlayerInfo. Writing here writes there.
        public int Hp
        {
            get => _playerInfo.Hp;
            set => _playerInfo.Hp = value;
        }

        // Note: Battle-scoped, so it is stored here and cleared by Reset().
        public int Block { get; set; }

        public ActionEntityId Id => _playerInfo.PlayerId;

        public BattlePlayerState(PlayerInfo playerInfo)
        {
            _playerInfo = playerInfo;
        }

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
            Block = 0;
            // Note: last battle's powers die here — BattleApi.EnterBattle calls Reset() on every player,
            //       so no ExitBattle cleanup pass is needed.
            HookListeners.Clear();
            Debug.Log("[Pile] Reset -> all piles cleared (Draw/Hand/Discard/Exhaust)");
        }

    }
}
