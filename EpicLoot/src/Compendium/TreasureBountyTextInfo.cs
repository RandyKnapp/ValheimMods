using EpicLoot.Adventure;
using EpicLoot.Biomes;
using System.Collections.Generic;
using System.Linq;

namespace EpicLoot.Compendium;

public class TreasureBountyTextInfo(string topic) : MagicTextInfo(topic)
{
    public override void Build(MagicPages instance)
    {
        if (!Player.m_localPlayer)
        {
            return;
        }

        List<string> content = new();
        AdventureSaveData saveData = Player.m_localPlayer.GetAdventureSaveData();

        bool hasValues = false;

        if (saveData.TreasureMaps.Count > 0)
        {
            hasValues = true;
            IOrderedEnumerable<TreasureMapChestInfo> sortedTreasureMaps = saveData.TreasureMaps
                .Where(x => x.State == TreasureMapState.Purchased)
                .OrderBy(x => GetBiomeOrder(x.Biome));
            
            foreach (TreasureMapChestInfo treasureMap in sortedTreasureMaps)
            {
                content.Add($" - $mod_epicloot_merchant_treasuremaps: " +
                            $"<color={GetBiomeColor(treasureMap.Biome)}>{BiomeDataManager.GetLocalizationToken(treasureMap.Biome)} " +
                            $"#{treasureMap.Interval + 1}</color>");
            }

            instance.MagicPagesTextArea.Add($"<color=#FFA626>" + 
                                            $"<size={MagicPages.LARGE_FONT_SIZE}>" + 
                                            "$mod_epicloot_merchant_treasuremaps" + 
                                            "</size></color>", content.ToArray());
            content.Clear();
        }

        if (saveData.Bounties.Count > 0)
        {
            hasValues = true;
            IOrderedEnumerable<BountyInfo> sortedBounties = saveData.Bounties.OrderBy(x => x.State);
            
            foreach (BountyInfo bounty in sortedBounties)
            {
                if (bounty.State != BountyState.InProgress && bounty.State != BountyState.Complete)
                {
                    continue;
                }

                string targetName = AdventureDataManager.GetBountyName(bounty);
                content.Add($" - <size={MagicPages.LARGE_FONT_SIZE}>{targetName}</size>  " +
                    $"<color=#c0c0c0ff>$mod_epicloot_activebounties_classification:</color> " +
                    $"<color=#d66660>{AdventureDataManager.GetMonsterName(bounty.Target.MonsterID)}</color>, ");

                string info = $" $mod_epicloot_activebounties_biome: <color={GetBiomeColor(bounty.Biome)}>{BiomeDataManager.GetLocalizationToken(bounty.Biome)}</color>";

                string status = "";
                switch (bounty.State)
                {
                    case BountyState.InProgress:
                        status = "<color=#00f0ff>$mod_epicloot_bounties_tooltip_inprogress</color>";
                        break;
                    case BountyState.Complete:
                        status = "<color=#70f56c>$mod_epicloot_bounties_tooltip_vanquished</color>";
                        break;
                }

                info += $"  <color=#c0c0c0ff>$mod_epicloot_bounties_tooltip_status {status}</color>";


                int iron = bounty.RewardIron;
                int gold = bounty.RewardGold;
                info += $", $mod_epicloot_bounties_tooltip_rewards " +
                        $"{(iron > 0 ? $"<color=white>{MerchantPanel.GetIronBountyTokenName()} x{iron}</color>" : "")}" +
                        $"{(iron > 0 && gold > 0 ? ", " : "")}" +
                        $"{(gold > 0 ? $"<color=#f5da53>{MerchantPanel.GetGoldBountyTokenName()} x{gold}</color>" : "")}";
                
                content.Add(info);
                
            }

            instance.MagicPagesTextArea.Add($"<color=#FFA626><size={MagicPages.LARGE_FONT_SIZE}>" 
                                            + $"$mod_epicloot_activebounties</size></color>", content.ToArray());
        }

        if (!hasValues)
        {
            instance.MagicPagesTextArea.Add("$mod_epicloot_no_active_adventures");
        }
    }

    public static string GetBiomeColor(Heightmap.Biome biome)
    {
        return BiomeDataManager.GetColor(biome);
    }

    public static float GetBiomeOrder(Heightmap.Biome biome)
    {
        return BiomeDataManager.GetOrder(biome);
    }
}