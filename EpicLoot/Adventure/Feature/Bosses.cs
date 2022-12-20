using System.Collections.Generic;

namespace EpicLoot.Adventure.Feature
{
    public static class Bosses
    {
        public static string GetPrevBossKey(string bossKey)
        {
            if (AdventureDataManager.Config.Bounties.Bosses == null || AdventureDataManager.Config.Bounties.Bosses.Count == 0)
            {
                throw new System.Exception(($"[Adventure Bounty Error]: adventure.json has not been updated or is missing boss config.  Please update JSON files."));

            }

            var index = AdventureDataManager.Config.Bounties.Bosses.FindIndex(x => x.BossDefeatedKey == bossKey);

            if (index == 0 || index >= AdventureDataManager.Config.Bounties.Bosses.Count)
                return null;

            return AdventureDataManager.Config.Bounties.Bosses[index - 1].BossDefeatedKey;
        }
    }
}
