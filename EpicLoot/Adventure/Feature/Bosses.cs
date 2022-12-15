using System.Collections.Generic;

namespace EpicLoot.Adventure.Feature
{
    public static class Bosses
    {
        public class BountyBossConfig
        {
            public Heightmap.Biome Biome;
            public string BossPrefab;
            public string BossDefeatedKey;
        }

        public static List<BountyBossConfig> BossData = new List<BountyBossConfig>
        {
            new BountyBossConfig { Biome = Heightmap.Biome.Meadows,     BossPrefab = "Eikthyr",     BossDefeatedKey = "defeated_eikthyr" },
            new BountyBossConfig { Biome = Heightmap.Biome.BlackForest, BossPrefab = "gd_king",     BossDefeatedKey = "defeated_gdking" },
            new BountyBossConfig { Biome = Heightmap.Biome.Ocean,       BossPrefab = "gd_king",     BossDefeatedKey = "defeated_gdking" }, // unlock ocean biome stuff after Elder
            new BountyBossConfig { Biome = Heightmap.Biome.Swamp,       BossPrefab = "Bonemass",    BossDefeatedKey = "defeated_bonemass" },
            new BountyBossConfig { Biome = Heightmap.Biome.Mountain,    BossPrefab = "Dragon",      BossDefeatedKey = "defeated_dragon" },
            new BountyBossConfig { Biome = Heightmap.Biome.Plains,      BossPrefab = "GoblinKing",  BossDefeatedKey = "defeated_goblinking" },
            new BountyBossConfig { Biome = Heightmap.Biome.Mistlands,   BossPrefab = "SeekerQueen", BossDefeatedKey = "defeated_queen" },
        };

        public static string GetPrevBossKey(string bossKey)
        {
            var index = BossData.FindIndex(x => x.BossDefeatedKey == bossKey);
            if (index == 0 || index >= BossData.Count)
                return null;
            return BossData[index - 1].BossDefeatedKey;
        }
    }
}
