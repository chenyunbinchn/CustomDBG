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
        }

        // Note: Move every card from one pile to the end of another, leaving 'from' empty.
        public static void MoveAll(List<CardInstanceId> from, List<CardInstanceId> to)
        {
            to.AddRange(from);
            from.Clear();
        }

        // Note: Shuffle a pile on the shuffle RNG domain. StableShuffle sorts into a canonical order
        //       first, so the result depends only on {pile contents, seed}, not on input order. Card
        //       shuffling always uses the shuffle domain — callers don't pick the RNG stream here.
        public static void Shuffle(List<CardInstanceId> pile, RandomManager randomManager)
        {
            ShuffleHelper.StableShuffle(pile, randomManager.ShuffleNextInt);
        }

        // Note: Build the initial draw pile = all of the player's cards (PileManager registry), then shuffle.
        public static void BuildDrawPile(BattlePlayerState state, RandomManager randomManager)
        {
            FillFrom(state.DrawPile, state.PileManager.Dictionary.Keys);
            Shuffle(state.DrawPile, randomManager);
            Debug.Log($"[Pile] BuildDrawPile -> DrawPile({state.DrawPile.Count}): {state.PileManager.DescribePile(state.DrawPile)}");
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
        }

        public static void ReshuffleDiscardIntoDraw(BattlePlayerState state, RandomManager randomManager)
        {
            int discardCount = state.DiscardPile.Count;
            MoveAll(state.DiscardPile, state.DrawPile);
            Shuffle(state.DrawPile, randomManager);
            Debug.Log($"[Pile] Reshuffle Discard({discardCount}) into Draw -> DrawPile({state.DrawPile.Count}): {state.PileManager.DescribePile(state.DrawPile)}");
        }

        // Draw from DrawPile
        public static void DrawCards(BattlePlayerState state, int count, RandomManager randomManager)
        {
            int drawn = 0;
            for (int i = 0; i < count; i++)
            {
                if (state.DrawPile.Count == 0)
                {
                    if (state.DiscardPile.Count == 0)
                    {
                        break; // No card left to draw
                    }
                    ReshuffleDiscardIntoDraw(state, randomManager);
                }
                Draw(state.DrawPile, state.HandCards, 1);
                drawn++;
            }
            Debug.Log($"[Pile] DrawCards -> drew {drawn}/{count}; Hand({state.HandCards.Count}): {state.PileManager.DescribePile(state.HandCards)}; DrawPile={state.DrawPile.Count}, Discard={state.DiscardPile.Count}");
        }
    }
}
