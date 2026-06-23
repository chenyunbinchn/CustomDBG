using tools;

namespace random
{
    public class SeedManager
    {
        public Seed MainSeed;
        public Seed ShuffleSeed;
        public Seed MonsterAiSeed;
        public Seed MapGenSeed;
        public Seed ItemSeed;
        public Seed LootDropCardSeed;

        public void Init(Seed mainSeed)
        {
            MainSeed = mainSeed;
            ShuffleSeed = SeedHelper.DeriveSeed(mainSeed, "shuffle");
            MonsterAiSeed = SeedHelper.DeriveSeed(mainSeed, "monster_ai");
            MapGenSeed = SeedHelper.DeriveSeed(mainSeed, "map_gen");
            ItemSeed = SeedHelper.DeriveSeed(mainSeed, "item");
            LootDropCardSeed = SeedHelper.DeriveSeed(mainSeed, "loot_drop_card");
        }

        public void Init(uint mainSeedValue)
        {
            Init(new Seed(mainSeedValue));
        }
    }
}
