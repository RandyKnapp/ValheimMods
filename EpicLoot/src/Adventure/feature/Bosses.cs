using EpicLoot.Biomes;

namespace EpicLoot.Adventure.Feature
{
    public static class Bosses
    {
        /// <summary>
        /// The boss key one step earlier in biome progression order (biomedata.json), or null for the
        /// first key. Never throws: an empty registry just means there is no previous key.
        /// </summary>
        public static string GetPrevBossKey(string bossKey)
        {
            return BiomeDataManager.GetPrevBossKey(bossKey);
        }
    }
}
