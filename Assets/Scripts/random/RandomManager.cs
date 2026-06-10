using tools;

namespace random
{
    public class RandomManager
    {
        private System.Random _shuffleRandom;
        private System.Random _monsterAiRandom;
        private System.Random _mapGenRandom;
        private System.Random _itemRandom;
        private System.Random _lootDropCardRandom;

        private int _shuffleCounter;
        private int _monsterAiCounter;
        private int _mapGenCounter;
        private int _itemCounter;
        private int _lootDropCardCounter;

        public void Init(SeedManager seedManager)
        {
            _shuffleRandom = new System.Random((int)seedManager.ShuffleSeed.Value);
            _monsterAiRandom = new System.Random((int)seedManager.MonsterAiSeed.Value);
            _mapGenRandom = new System.Random((int)seedManager.MapGenSeed.Value);
            _itemRandom = new System.Random((int)seedManager.ItemSeed.Value);
            _lootDropCardRandom = new System.Random((int)seedManager.LootDropCardSeed.Value);

            _shuffleCounter = 0;
            _monsterAiCounter = 0;
            _mapGenCounter = 0;
            _itemCounter = 0;
            _lootDropCardCounter = 0;
        }

        public int ShuffleNextInt(int maxExclusive)
        {
            MyAssert.Assert(maxExclusive > 0, "maxExclusive must be > 0");
            _shuffleCounter++;
            return _shuffleRandom.Next(maxExclusive);
        }

        public int MonsterAiNextInt(int maxExclusive)
        {
            MyAssert.Assert(maxExclusive > 0, "maxExclusive must be > 0");
            _monsterAiCounter++;
            return _monsterAiRandom.Next(maxExclusive);
        }

        public int MapGenNextInt(int maxExclusive)
        {
            MyAssert.Assert(maxExclusive > 0, "maxExclusive must be > 0");
            _mapGenCounter++;
            return _mapGenRandom.Next(maxExclusive);
        }

        public int ItemNextInt(int maxExclusive)
        {
            MyAssert.Assert(maxExclusive > 0, "maxExclusive must be > 0");
            _itemCounter++;
            return _itemRandom.Next(maxExclusive);
        }

        public int LootDropCardNextInt(int maxExclusive)
        {
            MyAssert.Assert(maxExclusive > 0, "maxExclusive must be > 0");
            _lootDropCardCounter++;
            return _lootDropCardRandom.Next(maxExclusive);
        }

        public int GetShuffleCounter()
        {
            return _shuffleCounter;
        }

        public int GetMonsterAiCounter()
        {
            return _monsterAiCounter;
        }

        public int GetMapGenCounter()
        {
            return _mapGenCounter;
        }

        public int GetItemCounter()
        {
            return _itemCounter;
        }

        public int GetLootDropCardCounter()
        {
            return _lootDropCardCounter;
        }
    }
}
