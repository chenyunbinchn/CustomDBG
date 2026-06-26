using System.Collections.Generic;
using cards.instance;
using gameStates.transient;
using random;
using tools.shuffle;
using UnityEngine;

namespace systems
{
    // Note: Stateless operations over a player's card piles — the List<CardInstanceId> piles on
    //       BattlePlayerState (Draw/Hand/Discard/Play/Exhaust). Systems do logic, state holds data.
    public static class BattlePileApi
    {
        // Note: Replace a pile's contents with source (clear then append). Order follows source enumeration.
        public static void FillFrom(List<CardInstanceId> pile, IEnumerable<CardInstanceId> source)
        {
            pile.Clear();
            pile.AddRange(source);
            Debug.Log($"[Pile] FillFrom -> {pile.Count} cards {Describe(pile)}");
        }

        // Note: Move every card from one pile to the end of another, leaving 'from' empty.
        public static void MoveAll(List<CardInstanceId> from, List<CardInstanceId> to)
        {
            int moved = from.Count;
            to.AddRange(from);
            from.Clear();
            Debug.Log($"[Pile] MoveAll -> moved {moved}; from={from.Count}, to={to.Count} {Describe(to)}");
        }

        // Note: Shuffle a pile on the shuffle RNG domain. StableShuffle sorts into a canonical order
        //       first, so the result depends only on {pile contents, seed}, not on input order. Card
        //       shuffling always uses the shuffle domain — callers don't pick the RNG stream here.
        public static void Shuffle(List<CardInstanceId> pile, RandomManager randomManager)
        {
            ShuffleHelper.StableShuffle(pile, randomManager.ShuffleNextInt);
            Debug.Log($"[Pile] Shuffle -> {pile.Count} cards {Describe(pile)}");
        }

        // Note: Build the initial draw pile = all of the player's cards (PileManager registry), then shuffle.
        public static void BuildDrawPile(BattlePlayerState state, RandomManager randomManager)
        {
            FillFrom(state.DrawPile, state.PileManager.Dictionary.Keys);
            Shuffle(state.DrawPile, randomManager);
            Debug.Log($"[Pile] BuildDrawPile -> DrawPile={state.DrawPile.Count} {Describe(state.DrawPile)}");
        }

        // Todo (L3 primitives — add each when it gets its first caller):
        //  - MoveAt(from, to, index): move a single card by index (play a card / pick a card).
        //  - Discard(state, cardId) / Exhaust(state, cardId): semantic wrappers over MoveAt/MoveAll.

        // Note: Move up to 'count' cards from the top (tail) of 'from' to 'to'. Does not shuffle and
        //       does not auto-reshuffle — the caller handles an empty 'from'.
        public static void Draw(List<CardInstanceId> from, List<CardInstanceId> to, int count)
        {
            int n = count < from.Count ? count : from.Count;
            for (int i = 0; i < n; i++)
            {
                int top = from.Count - 1;
                to.Add(from[top]);
                from.RemoveAt(top);
            }
            Debug.Log($"[Pile] Draw -> requested {count}, drew {n}; from={from.Count}, to={to.Count}");
        }

        public static void ReshuffleDiscardIntoDraw(BattlePlayerState state, RandomManager randomManager)
        {
            Debug.Log($"[Pile] ReshuffleDiscardIntoDraw: Discard={state.DiscardPile.Count} -> DrawPile (DrawPile before={state.DrawPile.Count})");
            MoveAll(state.DiscardPile, state.DrawPile);
            Shuffle(state.DrawPile, randomManager);
        }

        // Draw from DrawPile
        public static void DrawCards(BattlePlayerState state, int count, RandomManager randomManager)
        {
            Debug.Log($"[Pile] DrawCards: want {count}; DrawPile={state.DrawPile.Count}, Hand={state.HandCards.Count}, Discard={state.DiscardPile.Count}");
            for (int i = 0; i < count; i++)
            {
                if (state.DrawPile.Count == 0)
                {
                    if (state.DiscardPile.Count == 0)
                    {
                        Debug.Log($"[Pile] DrawCards: stopped early, no cards left (drew {i}/{count})");
                        return; // No card can be drawn
                    }
                    ReshuffleDiscardIntoDraw(state, randomManager);
                }
                Draw(state.DrawPile, state.HandCards, 1);
            }
            Debug.Log($"[Pile] DrawCards done: Hand={state.HandCards.Count}, DrawPile={state.DrawPile.Count}, Discard={state.DiscardPile.Count}");
        }

        // Note: Debug helper — list a pile's card instance ids, e.g. "[3, 1, 5]".
        private static string Describe(List<CardInstanceId> pile)
        {
            string[] ids = new string[pile.Count];
            for (int i = 0; i < pile.Count; i++)
            {
                ids[i] = pile[i].Value.ToString();
            }
            return "[" + string.Join(", ", ids) + "]";
        }
    }
}
