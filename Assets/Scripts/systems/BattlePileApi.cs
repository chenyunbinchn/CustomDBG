using System.Collections.Generic;
using cards.instance;
using gameStates.transient;
using random;
using tools.shuffle;

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
        }

        // Todo (L3 primitives — add each when it gets its first caller):
        //  - Draw(from, to, count): move 'count' cards from the head of 'from' to 'to' (draw step).
        //    When 'from' (DrawPile) runs out, ReshuffleDiscardIntoDraw first.
        //  - MoveAt(from, to, index): move a single card by index (play a card / pick a card).
        //  - ReshuffleDiscardIntoDraw(state, randomManager): MoveAll(Discard -> Draw) then Shuffle.
        //  - Discard(state, cardId) / Exhaust(state, cardId): semantic wrappers over MoveAt/MoveAll.
    }
}
