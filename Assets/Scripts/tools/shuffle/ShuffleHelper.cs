using System;
using System.Collections.Generic;
using tools.assert;

namespace tools.shuffle
{
    public static class ShuffleHelper
    {
        // Fisher-Yates (Durstenfeld) in-place shuffle.
        // nextIntExclusive(maxExclusive) must return a value in [0, maxExclusive).
        // The caller selects the RNG stream by passing its NextInt method, e.g.
        // randomManager.ShuffleNextInt for card piles or randomManager.ItemNextInt for items,
        // so each domain stays on its own deterministic stream.
        public static void Shuffle<T>(IList<T> list, Func<int, int> nextIntExclusive) // Question: What is the difference between IList and List??
        {
            MyAssert.Assert(list != null, "list must not be null");
            MyAssert.Assert(nextIntExclusive != null, "nextIntExclusive must not be null");

            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = nextIntExclusive(i + 1);
                MyAssert.Assert(j >= 0 && j <= i, "nextIntExclusive returned out of range value");
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

        // Stable Fisher-Yates: sort the list into a canonical order first, so the result depends only
        // on {set contents, RNG stream}, not on the input order. Use this when the input order is not
        // guaranteed (e.g. ids coming from a Dictionary/HashSet enumeration). T must define a total
        // order via IComparable<T>. See 260611-report-shuffle-determinism.
        public static void StableShuffle<T>(IList<T> list, Func<int, int> nextIntExclusive) where T : IComparable<T>
        {
            MyAssert.Assert(list != null, "list must not be null");

            List<T> sorted = new List<T>(list);
            sorted.Sort();
            for (int i = 0; i < list.Count; i++)
            {
                list[i] = sorted[i];
            }

            Shuffle(list, nextIntExclusive);
        }
    }
}
