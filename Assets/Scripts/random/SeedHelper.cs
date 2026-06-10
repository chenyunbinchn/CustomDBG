namespace random
{
    public static class SeedHelper
    {
        public static int DeterministicHash(string str)
        {
            int hash1 = 352654597;
            int hash2 = hash1;
            for (int i = 0; i < str.Length; i += 2)
            {
                hash1 = ((hash1 << 5) + hash1) ^ str[i];
                if (i == str.Length - 1)
                {
                    break;
                }
                hash2 = ((hash2 << 5) + hash2) ^ str[i + 1];
            }
            return hash1 + hash2 * 1566083941;
        }

        public static Seed DeriveSeed(Seed mainSeed, string streamName)
        {
            int hash = DeterministicHash(mainSeed.ToString() + streamName);
            return new Seed((uint)hash);
        }
    }
}