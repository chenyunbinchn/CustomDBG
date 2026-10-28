using System;
using System.Collections.Generic;
using cards.instance;
using gameStates.transient;
using random;
using tools.shuffle;
using UnityEngine;

namespace systems
{
    public enum EnumDrawStoppedReason
    {
        Completed,
        NoCardsAvailable,
        HandFull,
    }

    public sealed class DrawResult
    {
        public int Requested { get; }
        public List<CardInstanceId> DrawnCards { get; }
        public int ReshuffleCount { get; }
        public EnumDrawStoppedReason StoppedReason { get; }

        public DrawResult(int requested, List<CardInstanceId> drawnCards, int reshuffleCount,
            EnumDrawStoppedReason stoppedReason)
        {
            Requested = requested;
            DrawnCards = drawnCards;
            ReshuffleCount = reshuffleCount;
            StoppedReason = stoppedReason;
        }
    }

    // Note: Stateless operations over a player's card piles — the List<CardInstanceId> piles on
    //       BattlePlayerState (Draw/Hand/Discard/Play/Exhaust). Systems do logic, state holds data.
    public static class BattlePileApi
    {
        public const int MaxHandSize = 10;

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

        public static bool MoveCard(List<CardInstanceId> from, List<CardInstanceId> to, CardInstanceId card)
        {
            int sourceIndex = from.IndexOf(card);
            if (sourceIndex < 0)
            {
                return false;
            }
            if (to.Contains(card))
            {
                throw new InvalidOperationException($"Card #{card.Value} already exists in the destination pile.");
            }

            from.RemoveAt(sourceIndex);
            to.Add(card);
            return true;
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
        public static DrawResult DrawCards(BattlePlayerState state, int count, RandomManager randomManager)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }
            if (randomManager == null)
            {
                throw new ArgumentNullException(nameof(randomManager));
            }
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count), "Draw count cannot be negative.");
            }

            List<CardInstanceId> drawnCards = new List<CardInstanceId>(count);
            int reshuffleCount = 0;
            EnumDrawStoppedReason stoppedReason = EnumDrawStoppedReason.Completed;
            for (int i = 0; i < count; i++)
            {
                if (state.HandCards.Count >= MaxHandSize)
                {
                    stoppedReason = EnumDrawStoppedReason.HandFull;
                    break;
                }
                if (state.DrawPile.Count == 0)
                {
                    if (state.DiscardPile.Count == 0)
                    {
                        stoppedReason = EnumDrawStoppedReason.NoCardsAvailable;
                        break; // No card left to draw
                    }
                    ReshuffleDiscardIntoDraw(state, randomManager);
                    reshuffleCount++;
                }
                int top = state.DrawPile.Count - 1;
                CardInstanceId drawnCard = state.DrawPile[top];
                if (state.HandCards.Contains(drawnCard))
                {
                    throw new InvalidOperationException(
                        $"Card #{drawnCard.Value} already exists in HandCards while drawing.");
                }
                state.DrawPile.RemoveAt(top);
                state.HandCards.Add(drawnCard);
                drawnCards.Add(drawnCard);
            }
            Debug.Log($"[Pile] DrawCards -> drew {drawnCards.Count}/{count}; Hand({state.HandCards.Count}): {state.PileManager.DescribePile(state.HandCards)}; DrawPile={state.DrawPile.Count}, Discard={state.DiscardPile.Count}; stopped={stoppedReason}");
            return new DrawResult(count, drawnCards, reshuffleCount, stoppedReason);
        }
    }
}
