using System.Collections.Generic;

namespace EpicLoot.Adventure.Feature
{
    public static class Bosses
    {
        public static string GetPrevBossKey(string bossKey)
        {
            var index = AdventureDataManager.Config.Bounties.Bosses.FindIndex(x => x.BossDefeatedKey == bossKey);
            if (index == 0 || index >= AdventureDataManager.Config.Bounties.Bosses.Count)
                return null;
            return AdventureDataManager.Config.Bounties.Bosses[index - 1].BossDefeatedKey;
        }
    }
}
